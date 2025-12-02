using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Pi_Odonto.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth,DentistaAuth")]
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

        /// <summary>
        /// Define a URL de redirecionamento/retorno com base no perfil do usuário.
        /// </summary>
        private string GetRedirectAction()
        {
            // Admin volta para a lista geral de atendimentos
            // Dentista volta para a lista específica de seus atendimentos
            return IsAdmin() ? "/Atendimento" : "/Dentista/MeusAtendimentos";
        }

        /// <summary>
        /// Verifica se há conflito de horário para o dentista e período especificados.
        /// </summary>
        private async Task<bool> CheckForConflict(int idDentista, DateTime data, TimeSpan horario, int duracao, int? atendimentoIdToExclude = null)
        {
            var startTime = horario;
            var endTime = horario.Add(TimeSpan.FromMinutes(duracao));

            var conflictingAppointmentsQuery = _context.Atendimentos
                .Where(a => a.IdDentista == idDentista && a.DataAtendimento.Date == data.Date);

            if (atendimentoIdToExclude.HasValue)
            {
                conflictingAppointmentsQuery = conflictingAppointmentsQuery.Where(a => a.Id != atendimentoIdToExclude.Value);
            }

            var conflictingAppointments = await conflictingAppointmentsQuery.ToListAsync();

            foreach (var existingAppointment in conflictingAppointments)
            {
                var existingStartTime = existingAppointment.HorarioAtendimento;
                var existingEndTime = existingAppointment.HorarioAtendimento.Add(TimeSpan.FromMinutes(existingAppointment.DuracaoAtendimento));

                if (startTime < existingEndTime && endTime > existingStartTime)
                {
                    return true;
                }
            }
            return false;
        }

        // ==========================================================
        // ENDPOINT: CHECAGEM DE ESCALA POR DATA (AJAX) - CORRIGIDO
        // ==========================================================
        // Verifica se há uma EscalaMensalDentista ativa para o dentista na data.

        [HttpGet]
        public async Task<IActionResult> CheckEscala(int idDentista, DateTime dataAtendimento)
        {
            if (idDentista <= 0)
            {
                return Json(new { success = false, message = "Dentista inválido." });
            }

            // 1. Procura por qualquer registro de escala para este dentista nesta data específica.
            bool estaNaEscala = await _context.EscalasMensaisDentista
                                            .AnyAsync(e => e.IdDentista == idDentista &&
                                                           e.DataEscala.Date == dataAtendimento.Date &&
                                                           e.Ativo);

            if (estaNaEscala)
            {
                return Json(new { success = true, message = "Data permitida pela escala do dentista." });
            }
            else
            {
                var dentista = await _context.Dentistas.FindAsync(idDentista);
                string nomeDentista = dentista?.Nome ?? "O dentista selecionado";

                return Json(new { success = false, message = $"{nomeDentista} não possui escala de trabalho registrada para a data {dataAtendimento.ToShortDateString()}." });
            }
        }

        // ==========================================================
        // INDEX - APENAS ADMIN
        // ==========================================================

        public async Task<IActionResult> Index()
        {
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
        // DETAILS
        // ==========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null) return NotFound();

            if (IsDentista() && atendimento.IdDentista != GetCurrentDentistaId())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            // Define o retorno dinâmico para o botão Voltar na View
            ViewData["ReturnUrl"] = GetRedirectAction();
            return View(atendimento);
        }

        // ==========================================================
        // CREATE (GET)
        // ==========================================================

        public async Task<IActionResult> Create()
        {
            var viewModel = new AtendimentoViewModel
            {
                DataAtendimento = DateTime.Now.Date,
                HorarioAtendimento = TimeSpan.FromHours(9),
                DuracaoAtendimento = 30,
                CriancasDisponiveis = await _context.Criancas.Where(c => c.Ativa).ToListAsync(),
                DentistasDisponiveis = new List<Dentista>()
            };

            if (IsDentista())
            {
                var dentistaId = GetCurrentDentistaId();
                viewModel.IdDentista = dentistaId;
                var dentista = await _context.Dentistas.FindAsync(dentistaId);
                if (dentista != null)
                {
                    viewModel.DentistasDisponiveis.Add(dentista);
                }
            }
            else // É Admin
            {
                viewModel.DentistasDisponiveis = await _context.Dentistas.Where(d => d.Ativo).ToListAsync();
            }

            // Define o retorno dinâmico para o botão Voltar na View
            ViewData["ReturnUrl"] = GetRedirectAction();
            return View(viewModel);
        }

        // ==========================================================
        // CREATE (POST)
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AtendimentoViewModel viewModel)
        {
            if (IsDentista())
            {
                viewModel.IdDentista = GetCurrentDentistaId();
            }

            // 1. Validação: Checa se IdDentista está preenchido
            if (viewModel.IdDentista == 0)
            {
                ModelState.AddModelError(nameof(viewModel.IdDentista), "O campo Dentista é obrigatório.");
            }

            // 2. Validação: Checa Escala (Backend) 
            if (viewModel.IdDentista > 0)
            {
                var escalaResult = await CheckEscala(viewModel.IdDentista, viewModel.DataAtendimento) as JsonResult;
                if (escalaResult?.Value is { } value && value.GetType().GetProperty("success")?.GetValue(value) is bool success && !success)
                {
                    // Adiciona o erro de escala ao ModelState
                    ModelState.AddModelError(nameof(viewModel.DataAtendimento), value.GetType().GetProperty("message")?.GetValue(value) as string ?? "Data fora da escala de trabalho do dentista.");
                }
            }

            // 3. Validação: Checagem de Conflito de Horário
            if (viewModel.IdDentista > 0 &&
                await CheckForConflict(viewModel.IdDentista, viewModel.DataAtendimento, viewModel.HorarioAtendimento, viewModel.DuracaoAtendimento))
            {
                ModelState.AddModelError(string.Empty, "O dentista selecionado já possui um agendamento neste horário e dia.");
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

                if (IsAdmin())
                    return RedirectToAction("Index");
                else
                    return Redirect("/Dentista/MeusAtendimentos");
            }

            // Recarregar dados para dropdowns em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.Where(c => c.Ativa).ToListAsync();

            if (IsDentista())
            {
                var dentista = await _context.Dentistas.FindAsync(GetCurrentDentistaId());
                viewModel.DentistasDisponiveis = new List<Dentista>();
                if (dentista != null)
                {
                    viewModel.DentistasDisponiveis.Add(dentista);
                }
            }
            else
            {
                viewModel.DentistasDisponiveis = await _context.Dentistas.Where(d => d.Ativo).ToListAsync();
            }

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
        // EDIT (GET)
        // ==========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento == null) return NotFound();

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
                CriancasDisponiveis = await _context.Criancas.Where(c => c.Ativa).ToListAsync(),
                DentistasDisponiveis = new List<Dentista>()
            };

            if (IsDentista())
            {
                var dentista = await _context.Dentistas.FindAsync(GetCurrentDentistaId());
                if (dentista != null) viewModel.DentistasDisponiveis.Add(dentista);
            }
            else
            {
                viewModel.DentistasDisponiveis = await _context.Dentistas.Where(d => d.Ativo).ToListAsync();
            }

            // Define o retorno dinâmico para o botão Voltar na View
            ViewData["ReturnUrl"] = GetRedirectAction();
            return View(viewModel);
        }

        // ==========================================================
        // EDIT (POST)
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AtendimentoViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento == null) return NotFound();

            if (IsDentista() && atendimento.IdDentista != GetCurrentDentistaId())
            {
                return RedirectToAction("AcessoNegado", "Auth");
            }

            if (IsDentista())
            {
                viewModel.IdDentista = GetCurrentDentistaId();
            }

            // 1. Validação: Checa Escala (Backend) 
            if (viewModel.IdDentista > 0)
            {
                var escalaResult = await CheckEscala(viewModel.IdDentista, viewModel.DataAtendimento) as JsonResult;
                if (escalaResult?.Value is { } value && value.GetType().GetProperty("success")?.GetValue(value) is bool success && !success)
                {
                    ModelState.AddModelError(nameof(viewModel.DataAtendimento), value.GetType().GetProperty("message")?.GetValue(value) as string ?? "Data fora da escala de trabalho do dentista.");
                }
            }

            // 2. Validação: Checagem de Conflito de Horário
            if (viewModel.IdDentista > 0 && ModelState.IsValid &&
                await CheckForConflict(viewModel.IdDentista, viewModel.DataAtendimento, viewModel.HorarioAtendimento, viewModel.DuracaoAtendimento, id))
            {
                ModelState.AddModelError(string.Empty, "O dentista selecionado já possui um agendamento neste horário e dia.");
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

                if (IsAdmin())
                    return RedirectToAction("Index"); // Vai para /Atendimento/Index
                else
                    return Redirect("/Dentista/MeusAtendimentos"); // Vai para a lista do Dentista
            }

            // Recarregar dados em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.Where(c => c.Ativa).ToListAsync();
            if (IsDentista())
            {
                var dentista = await _context.Dentistas.FindAsync(GetCurrentDentistaId());
                viewModel.DentistasDisponiveis = new List<Dentista>();
                if (dentista != null) viewModel.DentistasDisponiveis.Add(dentista);
            }
            else
            {
                viewModel.DentistasDisponiveis = await _context.Dentistas.Where(d => d.Ativo).ToListAsync();
            }

            // Define o retorno dinâmico para o botão Voltar na View
            ViewData["ReturnUrl"] = GetRedirectAction();
            return View(viewModel);
        }

        // ==========================================================
        // DELETE (GET e POST)
        // ==========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("AcessoNegado", "Auth");
            if (id == null) return NotFound();

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null) return NotFound();

            ViewData["ReturnUrl"] = GetRedirectAction();
            return View(atendimento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AcessoNegado", "Auth");

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento != null)
            {
                _context.Atendimentos.Remove(atendimento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Atendimento excluído com sucesso!";
            }

            return RedirectToAction("Index");
        }

        // ==========================================================
        // UTILS
        // ==========================================================

        private bool AtendimentoExists(int id)
        {
            return _context.Atendimentos.Any(e => e.Id == id);
        }
    }
}