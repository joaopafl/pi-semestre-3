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
            // Remove validação da propriedade de navegação
            ModelState.Remove("Responsavel.Criancas");

            if (ModelState.IsValid)
            {
                // Verificações de duplicação antes de salvar
                var cpfExiste = await _context.Responsaveis
                    .AnyAsync(r => r.Cpf == viewModel.Responsavel.Cpf);

                var emailExiste = await _context.Responsaveis
                    .AnyAsync(r => r.Email == viewModel.Responsavel.Email);

                var telefoneExiste = await _context.Responsaveis
                    .AnyAsync(r => r.Telefone == viewModel.Responsavel.Telefone);

                if (cpfExiste)
                {
                    ModelState.AddModelError("Responsavel.Cpf", "Este CPF já está cadastrado no sistema.");
                }

                if (emailExiste)
                {
                    ModelState.AddModelError("Responsavel.Email", "Este email já está cadastrado no sistema.");
                }

                if (telefoneExiste)
                {
                    ModelState.AddModelError("Responsavel.Telefone", "Este telefone já está cadastrado no sistema.");
                }

                // Verificar CPFs das crianças
                foreach (var crianca in viewModel.Criancas)
                {
                    var cpfCriancaExiste = await _context.Criancas
                        .AnyAsync(c => c.Cpf == crianca.Cpf) ||
                        await _context.Responsaveis
                        .AnyAsync(r => r.Cpf == crianca.Cpf);

                    if (cpfCriancaExiste)
                    {
                        ModelState.AddModelError("", $"O CPF {crianca.Cpf} (criança {crianca.Nome}) já está cadastrado no sistema.");
                    }
                }

                // Se passou por todas as validações, tenta salvar
                if (ModelState.IsValid)
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Define valores padrão
                            viewModel.Responsavel.Ativo = false;
                            viewModel.Responsavel.DataCadastro = DateTime.Now;
                            viewModel.Responsavel.EmailVerificado = false;
                            viewModel.Responsavel.TokenVerificacao = Guid.NewGuid().ToString();

                            // Criptografar a senha
                            viewModel.Responsavel.Senha = PasswordHelper.HashPassword(viewModel.Responsavel.Senha);

                            // Salva o responsável
                            _context.Responsaveis.Add(viewModel.Responsavel);
                            await _context.SaveChangesAsync();

                            // Salva as crianças
                            foreach (var crianca in viewModel.Criancas)
                            {
                                crianca.IdResponsavel = viewModel.Responsavel.Id;
                                crianca.Ativa = true; // Define criança como ativa por padrão
                                _context.Criancas.Add(crianca);
                            }
                            await _context.SaveChangesAsync();

                            // Enviar email de verificação
                            try
                            {
                                await _emailService.EnviarEmailVerificacaoAsync(
                                    viewModel.Responsavel.Email,
                                    viewModel.Responsavel.Nome,
                                    viewModel.Responsavel.TokenVerificacao
                                );
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"Erro ao enviar email: {emailEx.Message}");
                                // Não falha o cadastro por causa do email
                            }

                            transaction.Commit();
                            return RedirectToAction("EmailEnviado");
                        }
                        catch (DbUpdateException ex)
                        {
                            transaction.Rollback();

                            // Trata erros específicos do banco
                            if (ex.InnerException?.Message.Contains("CPF") == true)
                            {
                                ModelState.AddModelError("Responsavel.Cpf", "Este CPF já está cadastrado no sistema.");
                            }
                            else if (ex.InnerException?.Message.Contains("Email") == true)
                            {
                                ModelState.AddModelError("Responsavel.Email", "Este email já está cadastrado no sistema.");
                            }
                            else if (ex.InnerException?.Message.Contains("Telefone") == true)
                            {
                                ModelState.AddModelError("Responsavel.Telefone", "Este telefone já está cadastrado no sistema.");
                            }
                            else
                            {
                                ModelState.AddModelError("", "Dados duplicados encontrados. Verifique CPF, email e telefone.");
                            }

                            Console.WriteLine($"Erro de banco: {ex.InnerException?.Message}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", "Erro ao salvar os dados. Tente novamente.");
                            Console.WriteLine($"Erro geral: {ex.Message}");
                        }
                    }
                }
            }

            // Se chegou até aqui, tem erro - recarrega as opções
            if (viewModel.OpcoesParentesco == null || !viewModel.OpcoesParentesco.Any())
            {
                viewModel.OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                };
            }

            return View(viewModel);
        }

        // MÉTODOS DE VALIDAÇÃO AJAX
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarCpf([FromBody] ValidarCpfRequest request)
        {
            try
            {
                var cpfLimpo = request.Cpf.Replace(".", "").Replace("-", "");

                var existeResponsavel = await _context.Responsaveis
                    .AnyAsync(r => r.Cpf == cpfLimpo);

                var existeCrianca = await _context.Criancas
                    .AnyAsync(c => c.Cpf == cpfLimpo);

                return Json(new { existe = existeResponsavel || existeCrianca });
            }
            catch
            {
                return Json(new { existe = false });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarEmail([FromBody] ValidarEmailRequest request)
        {
            try
            {
                var existe = await _context.Responsaveis
                    .AnyAsync(r => r.Email.ToLower() == request.Email.ToLower());

                return Json(new { existe = existe });
            }
            catch
            {
                return Json(new { existe = false });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarTelefone([FromBody] ValidarTelefoneRequest request)
        {
            try
            {
                var existe = await _context.Responsaveis
                    .AnyAsync(r => r.Telefone == request.Telefone);

                return Json(new { existe = existe });
            }
            catch
            {
                return Json(new { existe = false });
            }
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
                Criancas = responsavel.Criancas.ToList(),
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
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

        [HttpGet]
        [AllowAnonymous] // Permite acesso público para cadastro
        public IActionResult Cadastro()
        {
            return RedirectToAction("Create");
        }

        [HttpPost]
        [AllowAnonymous] // Permite acesso público para cadastro
        public async Task<IActionResult> Cadastro(ResponsavelCriancaViewModel viewModel)
        {
            return await Create(viewModel);
        }

        // Classes para requests de validação
        public class ValidarCpfRequest { public string Cpf { get; set; } }
        public class ValidarEmailRequest { public string Email { get; set; } }
        public class ValidarTelefoneRequest { public string Telefone { get; set; } }
    }
}