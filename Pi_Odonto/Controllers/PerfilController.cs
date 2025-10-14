using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    [Authorize] // Permite ResponsavelOnly e AdminOnly
    public class PerfilController : Controller
    {
        private readonly AppDbContext _context;

        public PerfilController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (IsAdmin())
            {
                // Admin vê painel geral
                return RedirectToAction("Dashboard", "Admin");
            }

            var responsavelId = GetCurrentResponsavelId();
            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == responsavelId);

            if (responsavel == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(responsavel);
        }

        [HttpGet]
        public IActionResult MinhasCriancas(int? responsavelId = null)
        {
            IQueryable<Crianca> query = _context.Criancas;

            if (IsAdmin())
            {
                // Admin pode ver crianças de qualquer responsável
                if (responsavelId.HasValue)
                {
                    query = query.Where(c => c.IdResponsavel == responsavelId.Value);
                    ViewBag.ResponsavelNome = _context.Responsaveis
                        .Where(r => r.Id == responsavelId.Value)
                        .Select(r => r.Nome)
                        .FirstOrDefault();
                }
                // Se não especificou responsavelId, mostra todas as crianças
            }
            else
            {
                // Responsável só vê suas próprias crianças
                var currentResponsavelId = GetCurrentResponsavelId();
                query = query.Where(c => c.IdResponsavel == currentResponsavelId);
            }

            // Inclui tanto crianças ativas quanto inativas, ordenadas por ativas primeiro
            var criancas = query
                .Include(c => c.Responsavel)
                .OrderByDescending(c => c.Ativa)
                .ThenBy(c => c.Nome)
                .ToList();

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.ResponsavelId = responsavelId;

            return View(criancas);
        }

        [HttpGet]
        public IActionResult DetalhesCrianca(int id)
        {
            var crianca = _context.Criancas
                .Include(c => c.Responsavel)
                .FirstOrDefault(c => c.Id == id);

            if (crianca == null)
            {
                TempData["Erro"] = "Criança não encontrada.";
                return RedirectToAction("MinhasCriancas");
            }

            // Verifica se o usuário tem permissão para ver essa criança
            if (!IsAdmin())
            {
                var responsavelId = GetCurrentResponsavelId();
                if (crianca.IdResponsavel != responsavelId)
                {
                    TempData["Erro"] = "Você não tem permissão para ver esta criança.";
                    return RedirectToAction("MinhasCriancas");
                }
            }

            ViewBag.IsAdmin = IsAdmin();
            return View(crianca);
        }

        [HttpGet]
        public IActionResult EditarCrianca(int id)
        {
            var crianca = _context.Criancas
                .Include(c => c.Responsavel)
                .FirstOrDefault(c => c.Id == id);

            if (crianca == null)
            {
                TempData["Erro"] = "Criança não encontrada.";
                return RedirectToAction("MinhasCriancas");
            }

            // Verifica se a criança está ativa
            if (!crianca.Ativa)
            {
                TempData["Erro"] = "Não é possível editar uma criança inativa.";
                return RedirectToAction("DetalhesCrianca", new { id });
            }

            // Verifica se o usuário tem permissão para editar essa criança
            if (!IsAdmin())
            {
                var responsavelId = GetCurrentResponsavelId();
                if (crianca.IdResponsavel != responsavelId)
                {
                    TempData["Erro"] = "Você não tem permissão para editar esta criança.";
                    return RedirectToAction("MinhasCriancas");
                }
            }

            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };

            ViewBag.IsAdmin = IsAdmin();

            // Se for admin, carrega lista de responsáveis para poder trocar
            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis
                    .Where(r => r.Ativo)
                    .Select(r => new { r.Id, r.Nome })
                    .OrderBy(r => r.Nome)
                    .ToList();
            }

            return View(crianca);
        }

        [HttpPost]
        public IActionResult EditarCrianca(Crianca model)
        {
            // Remove validação da propriedade de navegação
            ModelState.Remove("Responsavel");

            if (ModelState.IsValid)
            {
                var criancaExistente = _context.Criancas
                    .FirstOrDefault(c => c.Id == model.Id);

                if (criancaExistente == null)
                {
                    TempData["Erro"] = "Criança não encontrada.";
                    return RedirectToAction("MinhasCriancas");
                }

                // Verifica se a criança está ativa
                if (!criancaExistente.Ativa)
                {
                    TempData["Erro"] = "Não é possível editar uma criança inativa.";
                    return RedirectToAction("DetalhesCrianca", new { id = model.Id });
                }

                // Verifica se o usuário tem permissão para editar essa criança
                if (!IsAdmin())
                {
                    var responsavelId = GetCurrentResponsavelId();
                    if (criancaExistente.IdResponsavel != responsavelId)
                    {
                        TempData["Erro"] = "Você não tem permissão para editar esta criança.";
                        return RedirectToAction("MinhasCriancas");
                    }
                    // Responsável não pode alterar o IdResponsavel
                    model.IdResponsavel = criancaExistente.IdResponsavel;
                }

                try
                {
                    // Atualizar campos
                    criancaExistente.Nome = model.Nome;
                    criancaExistente.Cpf = model.Cpf;
                    criancaExistente.DataNascimento = model.DataNascimento;
                    criancaExistente.Parentesco = model.Parentesco;

                    // Admin pode alterar o responsável
                    if (IsAdmin())
                    {
                        criancaExistente.IdResponsavel = model.IdResponsavel;
                    }

                    _context.SaveChanges();

                    TempData["Sucesso"] = "Dados da criança atualizados com sucesso!";
                    return RedirectToAction("DetalhesCrianca", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao atualizar criança: {ex.Message}");
                    ModelState.AddModelError("", "Erro ao atualizar os dados. Tente novamente.");
                }
            }

            // Se deu erro, recarrega as opções
            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };

            ViewBag.IsAdmin = IsAdmin();

            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis
                    .Where(r => r.Ativo)
                    .Select(r => new { r.Id, r.Nome })
                    .OrderBy(r => r.Nome)
                    .ToList();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CadastrarCrianca(int? responsavelId = null)
        {
            var crianca = new Crianca();

            if (IsAdmin())
            {
                // Admin pode cadastrar para qualquer responsável
                if (responsavelId.HasValue)
                {
                    crianca.IdResponsavel = responsavelId.Value;
                }

                ViewBag.Responsaveis = _context.Responsaveis
                    .Where(r => r.Ativo)
                    .Select(r => new { r.Id, r.Nome })
                    .OrderBy(r => r.Nome)
                    .ToList();
            }
            else
            {
                // Responsável só pode cadastrar para si mesmo
                crianca.IdResponsavel = GetCurrentResponsavelId();
            }

            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };

            ViewBag.IsAdmin = IsAdmin();
            return View(crianca);
        }

        [HttpPost]
        public IActionResult CadastrarCrianca(Crianca model)
        {
            // Remove validação da propriedade de navegação
            ModelState.Remove("Responsavel");

            // Se não for admin, força o responsável atual
            if (!IsAdmin())
            {
                model.IdResponsavel = GetCurrentResponsavelId();
            }

            // Define a criança como ativa por padrão
            model.Ativa = true;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Criancas.Add(model);
                    _context.SaveChanges();

                    TempData["Sucesso"] = "Criança cadastrada com sucesso!";

                    if (IsAdmin())
                    {
                        return RedirectToAction("MinhasCriancas", new { responsavelId = model.IdResponsavel });
                    }
                    else
                    {
                        return RedirectToAction("MinhasCriancas");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao cadastrar criança: {ex.Message}");
                    ModelState.AddModelError("", "Erro ao cadastrar criança. Tente novamente.");
                }
            }

            // Se deu erro, recarrega as opções
            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };

            ViewBag.IsAdmin = IsAdmin();

            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis
                    .Where(r => r.Ativo)
                    .Select(r => new { r.Id, r.Nome })
                    .OrderBy(r => r.Nome)
                    .ToList();
            }

            return View(model);
        }

        // NOVO MÉTODO: Alterar Status da Criança (substitui ExcluirCrianca)
        [HttpPost]
        public IActionResult AlterarStatusCrianca(int id, bool ativar)
        {
            try
            {
                var crianca = _context.Criancas
                    .FirstOrDefault(c => c.Id == id);

                if (crianca == null)
                {
                    return Json(new { success = false, message = "Criança não encontrada." });
                }

                // Verifica se o usuário tem permissão para alterar essa criança
                if (!IsAdmin())
                {
                    var responsavelId = GetCurrentResponsavelId();
                    if (crianca.IdResponsavel != responsavelId)
                    {
                        return Json(new { success = false, message = "Você não tem permissão para alterar esta criança." });
                    }

                    // Se está desativando, verifica se não é a única criança ativa do responsável
                    if (!ativar)
                    {
                        var qtdCriancasAtivas = _context.Criancas.Count(c => c.IdResponsavel == responsavelId && c.Ativa && c.Id != id);
                        if (qtdCriancasAtivas < 1)
                        {
                            return Json(new { success = false, message = "Você deve ter pelo menos uma criança ativa." });
                        }
                    }
                }

                // Altera o status da criança
                crianca.Ativa = ativar;
                _context.SaveChanges();

                string mensagem = ativar
                    ? $"Criança {crianca.Nome} foi reativada com sucesso."
                    : $"Criança {crianca.Nome} foi desativada com sucesso.";

                return Json(new { success = true, message = mensagem });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao alterar status da criança: {ex.Message}");
                return Json(new { success = false, message = "Erro interno do servidor." });
            }
        }

        // Método auxiliar para verificar se é admin
        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // Método auxiliar para pegar o ID do responsável logado
        private int GetCurrentResponsavelId()
        {
            // Se for admin, pode retornar 0 (será tratado nas ações)
            if (IsAdmin())
            {
                return 0;
            }

            // Se você estiver usando autenticação por Claims
            var userIdClaim = User.FindFirst("ResponsavelId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int responsavelId))
            {
                return responsavelId;
            }

            // Alternativa: buscar pelo email se estiver usando Claims padrão
            var emailClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                            ?? User.FindFirst("email");

            if (emailClaim != null)
            {
                var responsavel = _context.Responsaveis
                    .FirstOrDefault(r => r.Email == emailClaim.Value && r.Ativo);

                if (responsavel != null)
                {
                    return responsavel.Id;
                }
            }

            // Se não encontrou, redireciona para login
            throw new UnauthorizedAccessException("Responsável não identificado");
        }
    }
}