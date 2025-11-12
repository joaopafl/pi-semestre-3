using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.Helpers;
using Pi_Odonto.ViewModels;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pi_Odonto.Controllers
{
    [Authorize]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Verificar se é admin
        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // GET: Dashboard Admin
        [Route("Dashboard")]
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // Estatísticas para o dashboard
            var totalResponsaveis = _context.Responsaveis.Count();
            var responsaveisAtivos = _context.Responsaveis.Count(r => r.Ativo);
            var cadastrosHoje = _context.Responsaveis.Count(r => r.DataCadastro.Date == DateTime.Today);
            var cadastrosEsteMes = _context.Responsaveis.Count(r => r.DataCadastro.Month == DateTime.Now.Month && r.DataCadastro.Year == DateTime.Now.Year);

            ViewBag.TotalResponsaveis = totalResponsaveis;
            ViewBag.ResponsaveisAtivos = responsaveisAtivos;
            ViewBag.CadastrosHoje = cadastrosHoje;
            ViewBag.CadastrosEsteMes = cadastrosEsteMes;

            // Últimos cadastros
            var ultimosCadastros = _context.Responsaveis
                .OrderByDescending(r => r.DataCadastro)
                .Take(5)
                .ToList();

            return View(ultimosCadastros);
        }

        // GET: Lista de Responsáveis (Admin)
        [Route("Responsaveis")]
        public IActionResult Responsaveis(string busca = "", bool? ativo = null)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var query = _context.Responsaveis.AsQueryable();

            // Filtro de busca
            if (!string.IsNullOrEmpty(busca))
            {
                query = query.Where(r =>
                    r.Nome.Contains(busca) ||
                    r.Email.Contains(busca) ||
                    r.Cpf.Contains(busca) ||
                    r.Telefone.Contains(busca));
            }

            // Filtro de status
            if (ativo.HasValue)
            {
                query = query.Where(r => r.Ativo == ativo.Value);
            }

            var responsaveis = query
                .Include(r => r.Criancas)
                .OrderByDescending(r => r.DataCadastro)
                .ToList();

            ViewBag.Busca = busca;
            ViewBag.Ativo = ativo;

            return View(responsaveis);
        }

        // GET: Visualizar Responsável
        [Route("Responsaveis/Detalhes/{id}")]
        public IActionResult DetalhesResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            return View(responsavel);
        }

        // GET: Editar Responsável (Admin)
        [Route("Responsaveis/Editar/{id}")]
        public IActionResult EditarResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            // Limpar senha para não mostrar
            responsavel.Senha = "";

            // Criar ViewModel
            var viewModel = new ResponsavelCriancaViewModel
            {
                Responsavel = responsavel,
                Criancas = responsavel.Criancas?.ToList() ?? new List<Crianca>(),
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
            };

            return View(viewModel);
        }

        // POST: Editar Responsável (Admin)
        [HttpPost]
        [Route("Responsaveis/Editar/{id}")]
        public IActionResult EditarResponsavel(int id, ResponsavelCriancaViewModel viewModel)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            if (id != viewModel.Responsavel.Id)
            {
                return BadRequest();
            }

            var responsavelAtual = _context.Responsaveis.Find(id);
            if (responsavelAtual == null)
            {
                return NotFound();
            }

            // Validar email único
            if (_context.Responsaveis.Any(r => r.Email == viewModel.Responsavel.Email && r.Id != id))
            {
                ModelState.AddModelError("Responsavel.Email", "Este email já está em uso");
            }

            // Remove validação de senha se estiver vazia
            if (string.IsNullOrEmpty(viewModel.Responsavel.Senha))
            {
                ModelState.Remove("Responsavel.Senha");
                ModelState.Remove("ConfirmarSenha");
            }

            if (ModelState.IsValid)
            {
                // Remover máscara
                viewModel.Responsavel.Cpf = viewModel.Responsavel.Cpf.Replace(".", "").Replace("-", "");
                viewModel.Responsavel.Telefone = viewModel.Responsavel.Telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");

                // Atualizar dados
                responsavelAtual.Nome = viewModel.Responsavel.Nome;
                responsavelAtual.Email = viewModel.Responsavel.Email;
                responsavelAtual.Telefone = viewModel.Responsavel.Telefone;
                responsavelAtual.Endereco = viewModel.Responsavel.Endereco;
                responsavelAtual.Cpf = viewModel.Responsavel.Cpf;
                responsavelAtual.Ativo = viewModel.Responsavel.Ativo;

                // Se forneceu nova senha
                if (!string.IsNullOrEmpty(viewModel.Responsavel.Senha))
                {
                    responsavelAtual.Senha = PasswordHelper.HashPassword(viewModel.Responsavel.Senha);
                }

                _context.Responsaveis.Update(responsavelAtual);
                _context.SaveChanges();

                TempData["Sucesso"] = "Responsável atualizado com sucesso!";
                return RedirectToAction("Responsaveis");
            }

            // Se deu erro, recarregar as opções e as crianças
            if (viewModel.OpcoesParentesco == null || !viewModel.OpcoesParentesco.Any())
            {
                viewModel.OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                };
            }

            // Recarregar as crianças do banco se não existirem
            if (viewModel.Criancas == null || !viewModel.Criancas.Any())
            {
                var responsavelComCriancas = _context.Responsaveis
                    .Include(r => r.Criancas)
                    .FirstOrDefault(r => r.Id == id);

                viewModel.Criancas = responsavelComCriancas?.Criancas?.ToList() ?? new List<Crianca>();
            }

            return View(viewModel);
        }

        // POST: Desativar/Ativar Responsável
        [HttpPost]
        [Route("Responsaveis/ToggleStatus/{id}")]
        public IActionResult ToggleStatus(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis.Find(id);
            if (responsavel == null)
            {
                return NotFound();
            }

            responsavel.Ativo = !responsavel.Ativo;
            _context.Responsaveis.Update(responsavel);
            _context.SaveChanges();

            TempData["Sucesso"] = $"Responsável {(responsavel.Ativo ? "ativado" : "desativado")} com sucesso!";
            return RedirectToAction("Responsaveis");
        }

        // POST: Excluir Responsável (Admin)
        [HttpPost]
        [Route("Responsaveis/Excluir/{id}")]
        public IActionResult ExcluirResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            // Remove as crianças primeiro (devido ao FK)
            if (responsavel.Criancas.Any())
            {
                _context.Criancas.RemoveRange(responsavel.Criancas);
            }

            _context.Responsaveis.Remove(responsavel);
            _context.SaveChanges();

            TempData["Sucesso"] = "Responsável e suas crianças excluídos com sucesso!";
            return RedirectToAction("Responsaveis");
        }

        // GET: Relatórios
        [Route("Relatorios")]
        public IActionResult Relatorios()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // Dados para relatórios
            var totalResponsaveis = _context.Responsaveis.Count();
            var responsaveisAtivos = _context.Responsaveis.Count(r => r.Ativo);
            var responsaveisInativos = _context.Responsaveis.Count(r => !r.Ativo);

            // Cadastros por mês (últimos 6 meses)
            var cadastrosPorMes = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var data = DateTime.Now.AddMonths(-i);
                var count = _context.Responsaveis.Count(r =>
                    r.DataCadastro.Month == data.Month &&
                    r.DataCadastro.Year == data.Year);

                cadastrosPorMes.Add(new
                {
                    Mes = data.ToString("MMM/yyyy"),
                    Count = count
                });
            }

            ViewBag.TotalResponsaveis = totalResponsaveis;
            ViewBag.ResponsaveisAtivos = responsaveisAtivos;
            ViewBag.ResponsaveisInativos = responsaveisInativos;
            ViewBag.CadastrosPorMes = cadastrosPorMes;

            return View();
        }

        // ========================================
        // GERENCIAMENTO DE DENTISTAS (REFATORADO)
        // ========================================

        // GET: Lista de Dentistas (REFATORADO INCLUSÃO)
        [Route("Dentistas")]
        public IActionResult Dentistas()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dentistas = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                // REMOVIDO: .Include(d => d.Disponibilidades)
                .OrderBy(d => d.Nome)
                .ToList();

            return View(dentistas);
        }

        // GET: Criar Dentista (REFATORADO)
        [Route("CriarDentista")]
        public IActionResult CriarDentista()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();

            // REMOVIDO: View Model de Disponibilidade não é mais necessária aqui.
            var viewModel = new DentistaViewModel();

            return View(viewModel);
        }

        // POST: Criar Dentista (REFATORADO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CriarDentista")]
        public IActionResult CriarDentista(DentistaViewModel viewModel, int? IdEscala)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Escalas = _context.EscalaTrabalho.ToList();
                // REMOVIDO: viewModel.Disponibilidades = ObterDisponibilidadesPadrao();
                return View(viewModel);
            }

            var dentista = new Dentista
            {
                Nome = viewModel.Nome,
                Cpf = viewModel.Cpf,
                Cro = viewModel.Cro,
                Endereco = viewModel.Endereco,
                Email = viewModel.Email,
                Telefone = viewModel.Telefone,
                IdEscala = IdEscala,
                Ativo = true,
                Senha = PasswordHelper.HashPassword(viewModel.Cro + "123")
            };

            _context.Dentistas.Add(dentista);
            _context.SaveChanges();

            // REMOVIDO: Lógica de vinculação de disponibilidades obsoletas.
            // O dentista deve ter suas escalas cadastradas através do AdminEscalaController.

            TempData["Sucesso"] = $"Dentista cadastrado com sucesso! Senha inicial: {viewModel.Cro}123";
            return RedirectToAction("Dentistas");
        }

        // GET: Editar Dentista (REFATORADO INCLUSÃO)
        [Route("EditarDentista/{id}")]
        public IActionResult EditarDentista(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dentista = _context.Dentistas
                // REMOVIDO: .Include(d => d.Disponibilidades)
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == id);

            if (dentista == null)
            {
                TempData["Erro"] = "Dentista não encontrado.";
                return RedirectToAction("Dentistas");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();

            var viewModel = new DentistaViewModel
            {
                Id = dentista.Id,
                Nome = dentista.Nome,
                Cpf = dentista.Cpf,
                Cro = dentista.Cro,
                Endereco = dentista.Endereco,
                Email = dentista.Email,
                Telefone = dentista.Telefone,
                IdEscala = dentista.IdEscala,
                // REMOVIDO: Inicialização de Disponibilidades obsoletas.
                // Disponibilidades agora é gerenciada separadamente.
            };

            return View(viewModel);
        }

        // POST: Editar Dentista (REFATORADO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("EditarDentista/{id}")]
        public IActionResult EditarDentista(DentistaViewModel viewModel, int? IdEscala)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // O ModelState.IsValid deve ser reavaliado aqui, pois a ViewModel pode ter sido simplificada.
            if (!ModelState.IsValid)
            {
                ViewBag.Escalas = _context.EscalaTrabalho.ToList();
                return View(viewModel);
            }

            var dentista = _context.Dentistas
                // REMOVIDO: .Include(d => d.Disponibilidades)
                .FirstOrDefault(d => d.Id == viewModel.Id);

            if (dentista == null)
            {
                TempData["Erro"] = "Dentista não encontrado.";
                return RedirectToAction("Dentistas");
            }

            // Atualizar dados
            dentista.Nome = viewModel.Nome;
            dentista.Cpf = viewModel.Cpf;
            dentista.Cro = viewModel.Cro;
            dentista.Endereco = viewModel.Endereco;
            dentista.Email = viewModel.Email;
            dentista.Telefone = viewModel.Telefone;
            dentista.IdEscala = IdEscala;

            // REMOVIDO: Toda a lógica de Atualizar/Remover disponibilidades.
            // Isso era a causa dos erros de compilação CS1061.

            _context.SaveChanges();

            TempData["Sucesso"] = "Dentista atualizado com sucesso!";
            return RedirectToAction("Dentistas");
        }

        // POST: Deletar Dentista (MANTIDO - Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("DeletarDentista/{id}")]
        public IActionResult DeletarDentista(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dentista = _context.Dentistas.Find(id);

            if (dentista == null)
            {
                TempData["Erro"] = "Dentista não encontrado.";
                return RedirectToAction("Dentistas");
            }

            // Desativar em vez de deletar (soft delete)
            dentista.Ativo = false;
            _context.SaveChanges();

            TempData["Sucesso"] = "Dentista desativado com sucesso!";
            return RedirectToAction("Dentistas");
        }

        // GET: Detalhes do Dentista (REFATORADO INCLUSÃO)
        [Route("DetalhesDentista/{id}")]
        public IActionResult DetalhesDentista(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dentista = _context.Dentistas
                // REMOVIDO: .Include(d => d.Disponibilidades)
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == id);

            if (dentista == null)
            {
                TempData["Erro"] = "Dentista não encontrado.";
                return RedirectToAction("Dentistas");
            }

            return View(dentista);
        }

        // ========================================
        // MÉTODOS AUXILIARES - DENTISTAS (REMOVIDOS)
        // ========================================

        // REMOVIDO: private List<DisponibilidadeItem> ObterDisponibilidadesPadrao() { ... }
        // REMOVIDO: private List<DisponibilidadeItem> ObterDisponibilidadesComSelecoes(ICollection<DisponibilidadeDentista> existentes) { ... }


        // ========================================
        // FUNCIONALIDADES - VOLUNTÁRIOS (MANTIDO)
        // ========================================

        // GET: Solicitações de Voluntários
        [Route("Solicitacoes")]
        public async Task<IActionResult> Solicitacoes(string filtro = "todas")
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var query = _context.SolicitacoesVoluntario.AsQueryable();

            switch (filtro.ToLower())
            {
                case "pendentes":
                    query = query.Where(s => s.Status == "Pendente");
                    break;
                case "aprovadas":
                    query = query.Where(s => s.Status == "Aprovado");
                    break;
                case "rejeitadas":
                    query = query.Where(s => s.Status == "Rejeitado");
                    break;
                case "nao_visualizadas":
                    query = query.Where(s => !s.Visualizado);
                    break;
            }

            var solicitacoes = await query
                .OrderByDescending(s => s.DataSolicitacao)
                .ToListAsync();

            ViewBag.Filtro = filtro;
            ViewBag.TotalNaoVisualizadas = await _context.SolicitacoesVoluntario.CountAsync(s => !s.Visualizado);
            ViewBag.TotalPendentes = await _context.SolicitacoesVoluntario.CountAsync(s => s.Status == "Pendente");

            return View(solicitacoes);
        }

        // POST: Marcar como visualizado
        [HttpPost]
        [Route("Solicitacoes/MarcarVisualizado/{id}")]
        public async Task<IActionResult> MarcarComoVisualizado(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var solicitacao = await _context.SolicitacoesVoluntario.FindAsync(id);
            if (solicitacao == null)
            {
                return Json(new { success = false, message = "Solicitação não encontrada" });
            }

            solicitacao.Visualizado = true;
            solicitacao.DataResposta = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Aprovar solicitação
        [HttpPost]
        [Route("Solicitacoes/Aprovar/{id}")]
        public async Task<IActionResult> AprovarSolicitacao(int id, string? observacao)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var solicitacao = await _context.SolicitacoesVoluntario.FindAsync(id);
            if (solicitacao == null)
            {
                return Json(new { success = false, message = "Solicitação não encontrada" });
            }

            solicitacao.Status = "Aprovado";
            solicitacao.Visualizado = true;
            solicitacao.DataResposta = DateTime.Now;
            solicitacao.ObservacaoAdmin = observacao;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Solicitação aprovada com sucesso!" });
        }

        // POST: Rejeitar solicitação
        [HttpPost]
        [Route("Solicitacoes/Rejeitar/{id}")]
        public async Task<IActionResult> RejeitarSolicitacao(int id, string? observacao)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Acesso negado" });
            }

            var solicitacao = await _context.SolicitacoesVoluntario.FindAsync(id);
            if (solicitacao == null)
            {
                return Json(new { success = false, message = "Solicitação não encontrada" });
            }

            solicitacao.Status = "Rejeitado";
            solicitacao.Visualizado = true;
            solicitacao.DataResposta = DateTime.Now;
            solicitacao.ObservacaoAdmin = observacao;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Solicitação rejeitada." });
        }

        // POST: Excluir solicitação
        [HttpPost]
        [Route("Solicitacoes/Excluir/{id}")]
        public async Task<IActionResult> ExcluirSolicitacao(int id)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Solicitacoes");
            }

            var solicitacao = await _context.SolicitacoesVoluntario.FindAsync(id);
            if (solicitacao != null)
            {
                _context.SolicitacoesVoluntario.Remove(solicitacao);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Solicitação excluída com sucesso.";
            }
            else
            {
                TempData["Erro"] = "Solicitação não encontrada.";
            }

            return RedirectToAction("Solicitacoes");
        }
    }
}