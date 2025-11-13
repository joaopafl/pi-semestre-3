using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth,DentistaAuth")] // Aceita Admin e Dentista
    public class AtendimentoController : Controller
    {
        private readonly AppDbContext _context;

        public AtendimentoController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // MÉTODOS AUXILIARES
        // ==========================================================

        private bool IsAdmin() => User.HasClaim("TipoUsuario", "Admin");

        private bool IsDentista() => User.HasClaim("TipoUsuario", "Dentista");

        private int GetCurrentDentistaId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "DentistaId");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private string GetRedirectAction()
        {
            // Admin vai para Index (neste controller), Dentista vai para a rota específica
            return IsAdmin() ? "Index" : "/Dentista/MeusAtendimentos";
        }

        // ==========================================================
        // INDEX - APENAS ADMIN
        // ==========================================================

        public async Task<IActionResult> Index()
        {
            // Apenas Admin pode ver todos os atendimentos
            if (!IsAdmin())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            var atendimentos = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .OrderByDescending(a => a.DataAtendimento)
                .ThenByDescending(a => a.HorarioAtendimento)
                .ToListAsync();

            return View(atendimentos);
        }

        // ==========================================================
        // DETAILS - ADMIN OU DENTISTA (APENAS SEUS)
        // ==========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null)
            {
                return NotFound();
            }

            // Dentista só pode ver seus próprios atendimentos
            if (IsDentista() && atendimento.IdDentista != GetCurrentDentistaId())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            return View(atendimento);
        }

        // ==========================================================
        // CREATE - ADMIN E DENTISTA
        // ==========================================================

        public async Task<IActionResult> Create()
        {
            var viewModel = new AtendimentoViewModel
            {
                DataAtendimento = DateTime.Now,
                HorarioAtendimento = TimeSpan.FromHours(9),
                DuracaoAtendimento = 30,
                CriancasDisponiveis = await _context.Criancas.ToListAsync(),
                DentistasDisponiveis = await _context.Dentistas.ToListAsync()
            };

            // Se for dentista, já preenche o IdDentista
            if (IsDentista())
            {
                viewModel.IdDentista = GetCurrentDentistaId();
            }

            // CORREÇÃO: Define a URL de retorno para a View
            ViewData["ReturnUrl"] = GetRedirectAction();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AtendimentoViewModel viewModel)
        {
            // Se for dentista, força o IdDentista dele
            if (IsDentista())
            {
                viewModel.IdDentista = GetCurrentDentistaId();
            }

            if (ModelState.IsValid)
            {
                var atendimento = new Atendimento
                {
                    DataAtendimento = viewModel.DataAtendimento,
                    HorarioAtendimento = viewModel.HorarioAtendimento,
                    DuracaoAtendimento = viewModel.DuracaoAtendimento,
                    Observacao = viewModel.Observacao,
                    IdCrianca = viewModel.IdCrianca,
                    IdDentista = viewModel.IdDentista,
                    IdAgenda = viewModel.IdAgenda,
                    IdOdontograma = viewModel.IdOdontograma
                };

                _context.Add(atendimento);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Atendimento registrado com sucesso!";

                // Redireciona conforme o tipo de usuário
                if (IsAdmin())
                    return RedirectToAction("Index");
                else
                    return Redirect("/Dentista/MeusAtendimentos");
            }

            // Recarregar dados para dropdowns em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.ToListAsync();
            viewModel.DentistasDisponiveis = await _context.Dentistas.ToListAsync();
            
            // Re-define a URL de retorno em caso de erro de validação
            ViewData["ReturnUrl"] = GetRedirectAction(); 

            return View(viewModel);
        }

        // ==========================================================
        // EDIT - ADMIN OU DENTISTA (APENAS SEUS)
        // ==========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento == null)
            {
                return NotFound();
            }

            // Dentista só pode editar seus próprios atendimentos
            if (IsDentista() && atendimento.IdDentista != GetCurrentDentistaId())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            var viewModel = new AtendimentoViewModel
            {
                Id = atendimento.Id,
                DataAtendimento = atendimento.DataAtendimento,
                HorarioAtendimento = atendimento.HorarioAtendimento,
                DuracaoAtendimento = atendimento.DuracaoAtendimento,
                Observacao = atendimento.Observacao,
                IdCrianca = atendimento.IdCrianca,
                IdDentista = atendimento.IdDentista,
                IdAgenda = atendimento.IdAgenda,
                IdOdontograma = atendimento.IdOdontograma,
                CriancasDisponiveis = await _context.Criancas.ToListAsync(),
                DentistasDisponiveis = await _context.Dentistas.ToListAsync()
            };

            // Define a URL de retorno para a View de Edit
            ViewData["ReturnUrl"] = GetRedirectAction();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AtendimentoViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento == null)
            {
                return NotFound();
            }

            // Dentista só pode editar seus próprios atendimentos
            if (IsDentista() && atendimento.IdDentista != GetCurrentDentistaId())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    atendimento.DataAtendimento = viewModel.DataAtendimento;
                    atendimento.HorarioAtendimento = viewModel.HorarioAtendimento;
                    atendimento.DuracaoAtendimento = viewModel.DuracaoAtendimento;
                    atendimento.Observacao = viewModel.Observacao;
                    atendimento.IdCrianca = viewModel.IdCrianca;

                    // Admin pode mudar o dentista, mas dentista não pode
                    if (IsAdmin())
                    {
                        atendimento.IdDentista = viewModel.IdDentista;
                    }

                    atendimento.IdAgenda = viewModel.IdAgenda;
                    atendimento.IdOdontograma = viewModel.IdOdontograma;

                    _context.Update(atendimento);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Atendimento atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AtendimentoExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redireciona conforme o tipo de usuário
                if (IsAdmin())
                    return RedirectToAction("Index");
                else
                    return Redirect("/Dentista/MeusAtendimentos");
            }

            // Recarregar dados para dropdowns em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.ToListAsync();
            viewModel.DentistasDisponiveis = await _context.Dentistas.ToListAsync();
            
            // Re-define a URL de retorno em caso de erro de validação
            ViewData["ReturnUrl"] = GetRedirectAction();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Historico(string? nomeCrianca, string? cpfCrianca, string? nomeDentista, DateTime? dataInicio, DateTime? dataFim)
        {
            // Verifica se algum filtro foi usado
            bool pesquisaRealizada = !string.IsNullOrEmpty(nomeCrianca) ||
                                     !string.IsNullOrEmpty(cpfCrianca) ||
                                     !string.IsNullOrEmpty(nomeDentista) ||
                                     dataInicio.HasValue ||
                                     dataFim.HasValue;

            List<Atendimento> atendimentos = new List<Atendimento>();

            // Só busca se houver pesquisa
            if (pesquisaRealizada)
            {
                IQueryable<Atendimento> query = _context.Atendimentos
                    .Include(a => a.Crianca)
                    .Include(a => a.Dentista)
                    .OrderByDescending(a => a.DataAtendimento)
                    .ThenByDescending(a => a.HorarioAtendimento);

                // Filtros
                if (!string.IsNullOrEmpty(nomeCrianca))
                {
                    query = query.Where(a => a.Crianca!.Nome.Contains(nomeCrianca));
                }

                if (!string.IsNullOrEmpty(cpfCrianca))
                {
                    string cpfLimpo = cpfCrianca.Replace(".", "").Replace("-", "");
                    query = query.Where(a => a.Crianca!.Cpf.Replace(".", "").Replace("-", "").Contains(cpfLimpo));
                }

                if (!string.IsNullOrEmpty(nomeDentista))
                {
                    query = query.Where(a => a.Dentista!.Nome.Contains(nomeDentista));
                }

                if (dataInicio.HasValue)
                {
                    query = query.Where(a => a.DataAtendimento >= dataInicio.Value);
                }

                if (dataFim.HasValue)
                {
                    query = query.Where(a => a.DataAtendimento <= dataFim.Value);
                }

                atendimentos = await query.ToListAsync();
            }

            // Buscar todas as crianças e dentistas para o autocomplete
            ViewBag.Criancas = await _context.Criancas
                .Where(c => c.Ativa)
                .OrderBy(c => c.Nome)
                .Select(c => new { c.Nome })
                .ToListAsync();

            ViewBag.Dentistas = await _context.Dentistas
                .Where(d => d.Ativo)
                .OrderBy(d => d.Nome)
                .Select(d => new { d.Nome })
                .ToListAsync();

            // Manter valores dos filtros
            ViewBag.NomeCrianca = nomeCrianca;
            ViewBag.CpfCrianca = cpfCrianca;
            ViewBag.NomeDentista = nomeDentista;
            ViewBag.DataInicio = dataInicio?.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim?.ToString("yyyy-MM-dd");
            ViewBag.PesquisaRealizada = pesquisaRealizada;

            return View(atendimentos);
        }
        // ==========================================================
        // DELETE - APENAS ADMIN
        // ==========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            // Apenas Admin pode deletar
            if (!IsAdmin())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null)
            {
                return NotFound();
            }
            
            // Define a URL de retorno para a View de Delete
            ViewData["ReturnUrl"] = GetRedirectAction();

            return View(atendimento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Apenas Admin pode deletar
            if (!IsAdmin())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento != null)
            {
                _context.Atendimentos.Remove(atendimento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Atendimento excluído com sucesso!";
            }

            return RedirectToAction("Index");
        }

        private bool AtendimentoExists(int id)
        {
            return _context.Atendimentos.Any(e => e.Id == id);
        }
    }
}