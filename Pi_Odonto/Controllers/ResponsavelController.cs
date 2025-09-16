using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Helpers;
using Pi_Odonto.Models;
using Pi_Odonto.Services;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    [Authorize(Policy = "AdminOnly")] // SÓ ADMINS podem acessar
    public class ResponsavelController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailCadastroService _emailService;

        public ResponsavelController(AppDbContext context, IEmailCadastroService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            var responsaveis = _context.Responsaveis
                .Include(r => r.Criancas)
                .ToList();
            return View(responsaveis);
        }

        [HttpGet]
        [AllowAnonymous] // Permite acesso público para cadastro
        public IActionResult Create()
        {
            var viewModel = new ResponsavelCriancaViewModel
            {
                Responsavel = new Responsavel(),
                Criancas = new List<Crianca> { new Crianca() },
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
            };
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous] // Permite acesso público para cadastro
        public async Task<IActionResult> Create(ResponsavelCriancaViewModel viewModel)
        {
            // Limpa erros da propriedade Responsavel da ViewModel
            ModelState.Remove("Responsavel");

            // Debug
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            Console.WriteLine($"Responsavel é null? {viewModel?.Responsavel == null}");
            Console.WriteLine($"Nome: {viewModel?.Responsavel?.Nome ?? "NULL"}");

            if (!ModelState.IsValid)
            {
                // Debug - mostra erros
                Console.WriteLine("=== ERROS DE VALIDAÇÃO ===");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Campo: {error.Key}");
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"  Erro: {err.ErrorMessage}");
                    }
                }
                Console.WriteLine("=========================");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Define valores padrão
                        viewModel.Responsavel!.Ativo = false;
                        viewModel.Responsavel.DataCadastro = DateTime.Now;
                        viewModel.Responsavel.EmailVerificado = false;
                        viewModel.Responsavel.TokenVerificacao = Guid.NewGuid().ToString();

                        // Criptografar a senha
                        viewModel.Responsavel.Senha = PasswordHelper.HashPassword(viewModel.Responsavel.Senha ?? "");

                        // Debug - mostra SQL gerado
                        Console.WriteLine("=== DADOS RESPONSÁVEL ===");
                        Console.WriteLine($"Nome: {viewModel.Responsavel.Nome}");
                        Console.WriteLine($"CPF: {viewModel.Responsavel.Cpf}");
                        Console.WriteLine($"Email: {viewModel.Responsavel.Email}");
                        Console.WriteLine($"Telefone: {viewModel.Responsavel.Telefone}");
                        Console.WriteLine($"Endereco: {viewModel.Responsavel.Endereco}");
                        Console.WriteLine($"Ativo: {viewModel.Responsavel.Ativo}");
                        Console.WriteLine($"DataCadastro: {viewModel.Responsavel.DataCadastro}");
                        Console.WriteLine("==========================");

                        // Salva o responsável
                        _context.Responsaveis.Add(viewModel.Responsavel);
                        Console.WriteLine("Salvando responsável...");
                        _context.SaveChanges();
                        Console.WriteLine($"Responsável salvo com ID: {viewModel.Responsavel.Id}");

                        // Debug crianças
                        Console.WriteLine("=== DADOS CRIANÇAS ===");
                        foreach (var crianca in viewModel.Criancas)
                        {
                            Console.WriteLine($"Nome: {crianca.Nome}");
                            Console.WriteLine($"CPF: {crianca.Cpf}");
                            Console.WriteLine($"Data Nasc: {crianca.DataNascimento}");
                            Console.WriteLine($"Parentesco: {crianca.Parentesco}");
                            Console.WriteLine("---");
                        }
                        Console.WriteLine("======================");

                        // Salva as crianças
                        foreach (var crianca in viewModel.Criancas)
                        {
                            crianca.IdResponsavel = viewModel.Responsavel.Id;
                            _context.Criancas.Add(crianca);
                            Console.WriteLine($"Adicionando criança: {crianca.Nome}");
                        }
                        Console.WriteLine("Salvando crianças...");
                        _context.SaveChanges();
                        Console.WriteLine("Crianças salvas!");

                        // Enviar email de verificação
                        try
                        {
                            await _emailService.EnviarEmailVerificacaoAsync(
                                viewModel.Responsavel.Email,
                                viewModel.Responsavel.Nome,
                                viewModel.Responsavel.TokenVerificacao
                            );
                            Console.WriteLine("Email de verificação enviado!");
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"Erro ao enviar email: {emailEx.Message}");
                            // Não falha o cadastro por causa do email
                        }

                        transaction.Commit();
                        return RedirectToAction("EmailEnviado");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Erro ao salvar: {ex.Message}");
                        ModelState.AddModelError("", "Erro ao salvar os dados. Tente novamente.");
                    }
                }
            }

            // Se deu erro, recarrega as opções
            if (viewModel.OpcoesParentesco == null || !viewModel.OpcoesParentesco!.Any())
            {
                viewModel.OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                };
            }

            return View(viewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailEnviado()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return View("ErroVerificacao");
            }

            var responsavel = _context.Responsaveis
                .FirstOrDefault(r => r.TokenVerificacao == token && !r.EmailVerificado);

            if (responsavel == null)
            {
                return View("ErroVerificacao");
            }

            // Verifica se o token não expirou (24 horas)
            if (responsavel.DataCadastro.AddHours(24) < DateTime.Now)
            {
                return View("TokenExpirado");
            }

            // Ativa o responsável
            responsavel.EmailVerificado = true;
            responsavel.Ativo = true;
            responsavel.TokenVerificacao = null;

            _context.SaveChanges();

            // Envia email de boas-vindas
            try
            {
                await _emailService.EnviarEmailBoasVindasAsync(responsavel.Email, responsavel.Nome);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de boas-vindas: {ex.Message}");
            }

            return RedirectToAction("Sucesso");
        }

        [HttpGet]
        [AllowAnonymous] // Permite acesso público
        public IActionResult Sucesso()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null) return NotFound();

            var viewModel = new ResponsavelCriancaViewModel
            {
                Responsavel = responsavel,
                Criancas = responsavel.Criancas.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Edit(ResponsavelCriancaViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Atualiza o responsável
                        _context.Responsaveis.Update(viewModel.Responsavel);

                        // Remove todas as crianças existentes
                        var criancasExistentes = _context.Criancas
                            .Where(c => c.IdResponsavel == viewModel.Responsavel.Id)
                            .ToList();
                        _context.Criancas.RemoveRange(criancasExistentes);

                        // Adiciona as novas crianças
                        foreach (var crianca in viewModel.Criancas)
                        {
                            crianca.IdResponsavel = viewModel.Responsavel.Id;
                            crianca.Id = 0; // Força novo ID
                            _context.Criancas.Add(crianca);
                        }

                        _context.SaveChanges();
                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Erro ao salvar os dados. Tente novamente.");
                    }
                }
            }

            // Se chegou aqui, algo deu errado, recarrega as opções
            viewModel.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null) return NotFound();
            return View(responsavel);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Remove todas as crianças primeiro (devido ao FK)
                    var criancas = _context.Criancas
                        .Where(c => c.IdResponsavel == id)
                        .ToList();
                    _context.Criancas.RemoveRange(criancas);

                    // Remove o responsável
                    var responsavel = _context.Responsaveis.Find(id);
                    if (responsavel != null)
                    {
                        _context.Responsaveis.Remove(responsavel);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null) return NotFound();
            return View(responsavel);
        }

        // Método para adicionar criança via AJAX (será usado no JavaScript)
        [HttpPost]
        public IActionResult AdicionarCrianca(int idResponsavel)
        {
            var responsavel = _context.Responsaveis.Find(idResponsavel);
            if (responsavel == null) return NotFound();

            var novaCrianca = new Crianca { IdResponsavel = idResponsavel };
            return PartialView("_CriancaForm", novaCrianca);
        }

        // Método para remover criança via AJAX
        [HttpPost]
        public IActionResult RemoverCrianca(int idCrianca)
        {
            var crianca = _context.Criancas.Find(idCrianca);
            if (crianca != null)
            {
                // Verifica se não é a única criança
                var qtdCriancas = _context.Criancas.Count(c => c.IdResponsavel == crianca.IdResponsavel);
                if (qtdCriancas > 1)
                {
                    _context.Criancas.Remove(crianca);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Todo responsável deve ter pelo menos uma criança." });
                }
            }
            return Json(new { success = false, message = "Criança não encontrada." });
        }

        [HttpGet]
        [AllowAnonymous] // Permite acesso público para cadastro
        public IActionResult Cadastro()
        {
            var viewModel = new ResponsavelCriancaViewModel
            {
                Responsavel = new Responsavel(),
                Criancas = new List<Crianca> { new Crianca() },
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
            };
            return View("Create", viewModel);
        }

        [HttpPost]
        [AllowAnonymous] // Permite acesso público para cadastro
        public async Task<IActionResult> Cadastro(ResponsavelCriancaViewModel viewModel)
        {
            return await Create(viewModel);
        }

        // Adicione estas ações no seu ResponsavelController:

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CreateCrianca()
        {
            // Redireciona para o PerfilController se o usuário estiver logado
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("CadastrarCrianca", "Perfil");
            }

            // Para cadastro inicial junto com responsável
            var crianca = new Crianca();
            ViewBag.OpcoesParentesco = new List<string>
    {
        "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
    };
            return View(crianca);
        }
    }
}