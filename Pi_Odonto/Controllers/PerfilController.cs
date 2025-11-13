using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;
using Pi_Odonto.Helpers;
using System.Security.Claims;

namespace Pi_Odonto.Controllers
{
    // CORRIGIDO: Adicionados os esquemas de autenticação
    [Authorize(AuthenticationSchemes = "AdminAuth,ResponsavelAuth")]
    public class PerfilController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PerfilController> _logger;

        public PerfilController(AppDbContext context, ILogger<PerfilController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        private int GetCurrentResponsavelId()
        {
            var userIdClaim = User.FindFirst("ResponsavelId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int responsavelId))
            {
                return responsavelId;
            }
            return 0;
        }

        // GET: /Perfil/Index
        public IActionResult Index()
        {
            // === ADMIN: VÊ TODAS AS CRIANÇAS ===
            if (IsAdmin())
            {
                _logger.LogInformation("=== ADMIN ACESSANDO PERFIL ===");

                var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "admin@piodonto.com";
                var adminNome = User.FindFirst(ClaimTypes.Name)?.Value ?? "Administrador";

                // Busca TODAS as crianças ativas do sistema (SEM FILTRO DE ID_RESP)
                var todasCriancas = _context.Criancas
                    .Include(c => c.Responsavel)
                    .Where(c => c.Ativa)
                    .OrderBy(c => c.Responsavel != null ? c.Responsavel.Nome : "")
                    .ThenBy(c => c.Nome)
                    .AsNoTracking()
                    .ToList();

                _logger.LogInformation($"Admin carregou {todasCriancas.Count} crianças ativas do sistema");

                // Cria um objeto Responsavel fake para o admin
                var responsavelAdmin = new Responsavel
                {
                    Id = 999999,
                    Nome = adminNome,
                    Email = adminEmail,
                    Cpf = "000.000.000-00",
                    Telefone = "00000000000",
                    Endereco = "Sistema",
                    Ativo = true,
                    Criancas = todasCriancas
                };

                return View(responsavelAdmin);
            }

            // === USUÁRIO NORMAL: VÊ APENAS SUAS CRIANÇAS ===
            var responsavelId = GetCurrentResponsavelId();
            if (responsavelId == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas.Where(c => c.Ativa))
                .FirstOrDefault(r => r.Id == responsavelId);

            if (responsavel == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            _logger.LogInformation($"Usuário {responsavel.Nome} carregou {responsavel.Criancas.Count} crianças");

            return View(responsavel);
        }

        [HttpGet]
        public IActionResult Editar()
        {
            if (IsAdmin())
            {
                TempData["Info"] = "Administradores não podem editar seu perfil por aqui. Use a área administrativa.";
                return RedirectToAction("Dashboard", "Admin");
            }

            var responsavelId = GetCurrentResponsavelId();
            if (responsavelId == 0) return RedirectToAction("Login", "Auth");

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .AsNoTracking()
                .FirstOrDefault(r => r.Id == responsavelId);

            if (responsavel == null)
            {
                TempData["Erro"] = "Seu perfil não foi encontrado.";
                return RedirectToAction("Index");
            }

            var viewModel = new EditarPerfilResponsavelViewModel
            {
                Id = responsavel.Id,
                Nome = responsavel.Nome,
                Email = responsavel.Email,
                Cpf = responsavel.Cpf,
                Telefone = responsavel.Telefone,
                Endereco = responsavel.Endereco,
                Ativo = responsavel.Ativo,
                Criancas = responsavel.Criancas.Where(c => c.Ativa).ToList(),
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarPerfilResponsavelViewModel viewModel)
        {
            var responsavelId = GetCurrentResponsavelId();
            if (responsavelId == 0 || viewModel.Id != responsavelId || IsAdmin())
            {
                TempData["Erro"] = "Operação inválida ou não autorizada.";
                return RedirectToAction("Index");
            }

            ModelState.Remove("Cpf");
            ModelState.Remove("Email");
            ModelState.Remove("Ativo");
            ModelState.Remove("Criancas");

            var responsavelAtual = await _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefaultAsync(r => r.Id == viewModel.Id);

            if (responsavelAtual == null) return NotFound();

            bool senhaDeveSerAlterada = !string.IsNullOrEmpty(viewModel.NovaSenha);

            if (senhaDeveSerAlterada)
            {
                if (string.IsNullOrEmpty(viewModel.SenhaAtual) ||
                    !PasswordHelper.VerifyPassword(viewModel.SenhaAtual, responsavelAtual.Senha ?? ""))
                {
                    ModelState.AddModelError("SenhaAtual", "Senha atual incorreta.");
                }
            }
            else
            {
                ModelState.Remove("SenhaAtual");
                ModelState.Remove("NovaSenha");
                ModelState.Remove("ConfirmarNovaSenha");
            }

            var telefoneLimpo = viewModel.Telefone?.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            if (!string.IsNullOrEmpty(telefoneLimpo) &&
                await _context.Responsaveis.AnyAsync(r => r.Telefone == telefoneLimpo && r.Id != viewModel.Id))
            {
                ModelState.AddModelError("Telefone", "Este telefone já está em uso por outra conta.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.Cpf = responsavelAtual.Cpf;
                viewModel.Email = responsavelAtual.Email;
                viewModel.Ativo = responsavelAtual.Ativo;
                viewModel.OpcoesParentesco = new List<string>
                 {
                     "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                 };
                viewModel.SenhaAtual = null;
                viewModel.NovaSenha = null;
                viewModel.ConfirmarNovaSenha = null;

                return View(viewModel);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    responsavelAtual.Nome = viewModel.Nome;
                    responsavelAtual.Telefone = telefoneLimpo;
                    responsavelAtual.Endereco = viewModel.Endereco;

                    if (senhaDeveSerAlterada)
                    {
                        responsavelAtual.Senha = PasswordHelper.HashPassword(viewModel.NovaSenha ?? "");
                    }

                    var criancasForm = viewModel.Criancas ?? new List<Crianca>();
                    var criancasAtuais = responsavelAtual.Criancas.ToList();

                    var criancasParaDesativar = criancasAtuais
                        .Where(c => c.Id != 0 && c.Ativa && !criancasForm.Any(v => v.Id == c.Id))
                        .ToList();

                    foreach (var crianca in criancasParaDesativar)
                    {
                        crianca.Ativa = false;
                    }

                    foreach (var criancaForm in criancasForm)
                    {
                        criancaForm.Cpf = criancaForm.Cpf?.Replace(".", "").Replace("-", "");
                        criancaForm.IdResponsavel = responsavelAtual.Id;

                        if (criancaForm.Id == 0)
                        {
                            criancaForm.Ativa = true;
                            _context.Criancas.Add(criancaForm);
                        }
                        else
                        {
                            var criancaDb = criancasAtuais.FirstOrDefault(c => c.Id == criancaForm.Id);
                            if (criancaDb != null)
                            {
                                criancaDb.Nome = criancaForm.Nome;
                                criancaDb.Cpf = criancaForm.Cpf;
                                criancaDb.DataNascimento = criancaForm.DataNascimento;
                                criancaDb.Parentesco = criancaForm.Parentesco;
                                criancaDb.Ativa = true;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Sucesso"] = "Perfil e crianças atualizados com sucesso! 🎉";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, $"Erro ao atualizar perfil do responsável ID {viewModel.Id}");
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar os dados. Tente novamente.");

                    var responsavelData = _context.Responsaveis.AsNoTracking().FirstOrDefault(r => r.Id == viewModel.Id);
                    if (responsavelData != null)
                    {
                        viewModel.Cpf = responsavelData.Cpf;
                        viewModel.Email = responsavelData.Email;
                        viewModel.Ativo = responsavelData.Ativo;
                    }
                    viewModel.OpcoesParentesco = new List<string>
                    {
                        "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                    };
                    viewModel.SenhaAtual = null;
                    viewModel.NovaSenha = null;
                    viewModel.ConfirmarNovaSenha = null;
                    return View(viewModel);
                }
            }
        }

        [HttpGet]
        public IActionResult MinhasCriancas(int? responsavelId = null)
        {
            IQueryable<Crianca> query = _context.Criancas;

            if (IsAdmin())
            {
                if (responsavelId.HasValue)
                {
                    query = query.Where(c => c.IdResponsavel == responsavelId.Value);
                    ViewBag.ResponsavelNome = _context.Responsaveis.Where(r => r.Id == responsavelId.Value).Select(r => r.Nome).FirstOrDefault();
                }
            }
            else
            {
                var currentResponsavelId = GetCurrentResponsavelId();
                if (currentResponsavelId == 0) return RedirectToAction("Login", "Auth");
                query = query.Where(c => c.IdResponsavel == currentResponsavelId);
            }

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

            if (!IsAdmin() && !crianca.Ativa)
            {
                TempData["Erro"] = "Não é possível editar uma criança inativa.";
                return RedirectToAction("MinhasCriancas");
            }

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

            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis.Where(r => r.Ativo).Select(r => new { r.Id, r.Nome }).OrderBy(r => r.Nome).ToList();
            }

            return View(crianca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarCrianca(Crianca model)
        {
            ModelState.Remove("Responsavel");

            if (!IsAdmin())
            {
                var criancaExistente = _context.Criancas.AsNoTracking().FirstOrDefault(c => c.Id == model.Id);
                if (criancaExistente == null || criancaExistente.IdResponsavel != GetCurrentResponsavelId())
                {
                    TempData["Erro"] = "Acesso negado ou criança não encontrada.";
                    return RedirectToAction("MinhasCriancas");
                }
                model.IdResponsavel = criancaExistente.IdResponsavel;
            }

            model.Ativa = true;
            model.Cpf = model.Cpf?.Replace(".", "").Replace("-", "");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Criancas.Update(model);
                    _context.SaveChanges();

                    TempData["Sucesso"] = "Dados da criança atualizados com sucesso!";

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
                    _logger.LogError(ex, $"Erro ao atualizar criança ID {model.Id}");
                    ModelState.AddModelError("", "Erro ao atualizar os dados. Tente novamente.");
                }
            }

            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };
            ViewBag.IsAdmin = IsAdmin();
            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis.Where(r => r.Ativo).Select(r => new { r.Id, r.Nome }).OrderBy(r => r.Nome).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CadastrarCrianca(int? responsavelId = null)
        {
            var crianca = new Crianca();

            if (IsAdmin())
            {
                if (responsavelId.HasValue)
                {
                    crianca.IdResponsavel = responsavelId.Value;
                }
                ViewBag.Responsaveis = _context.Responsaveis.Where(r => r.Ativo).Select(r => new { r.Id, r.Nome }).OrderBy(r => r.Nome).ToList();
            }
            else
            {
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
        [ValidateAntiForgeryToken]
        public IActionResult CadastrarCrianca(Crianca model)
        {
            ModelState.Remove("Responsavel");

            if (!IsAdmin())
            {
                model.IdResponsavel = GetCurrentResponsavelId();
            }

            model.Ativa = true;
            model.Cpf = model.Cpf?.Replace(".", "").Replace("-", "");

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
                    _logger.LogError(ex, "Erro ao cadastrar criança.");
                    ModelState.AddModelError("", "Erro ao cadastrar criança. Tente novamente.");
                }
            }

            ViewBag.OpcoesParentesco = new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            };
            ViewBag.IsAdmin = IsAdmin();
            if (IsAdmin())
            {
                ViewBag.Responsaveis = _context.Responsaveis.Where(r => r.Ativo).Select(r => new { r.Id, r.Nome }).OrderBy(r => r.Nome).ToList();
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult AlterarStatusCrianca(int id, bool ativar)
        {
            try
            {
                var crianca = _context.Criancas.FirstOrDefault(c => c.Id == id);
                if (crianca == null) return Json(new { success = false, message = "Criança não encontrada." });

                if (!IsAdmin())
                {
                    var responsavelId = GetCurrentResponsavelId();
                    if (crianca.IdResponsavel != responsavelId) return Json(new { success = false, message = "Você não tem permissão." });

                    if (!ativar)
                    {
                        var qtdCriancasAtivas = _context.Criancas.Count(c => c.IdResponsavel == responsavelId && c.Ativa && c.Id != id);
                        if (qtdCriancasAtivas < 1) return Json(new { success = false, message = "Você deve manter pelo menos uma criança ativa." });
                    }
                }

                crianca.Ativa = ativar;
                _context.SaveChanges();

                string mensagem = ativar ? $"Criança {crianca.Nome} foi reativada com sucesso." : $"Criança {crianca.Nome} foi desativada com sucesso.";
                return Json(new { success = true, message = mensagem });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao alterar status da criança ID {id}");
                return Json(new { success = false, message = "Erro interno do servidor." });
            }
        }
    }
}