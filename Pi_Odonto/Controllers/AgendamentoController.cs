using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.ViewModels;
using Pi_Odonto.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace Pi_Odonto.Controllers
{
    public class AvailableTimeSlot
    {
        public string Time { get; set; } = string.Empty;
        public int DentistaId { get; set; }
        public string DentistaName { get; set; } = string.Empty;
    }

    [Authorize(AuthenticationSchemes = "AdminAuth,DentistaAuth,ResponsavelAuth")]
    public class AgendamentoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeSpan _slotDuration = TimeSpan.FromHours(1);

        public AgendamentoController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // MÉTODOS AUXILIARES (Auth e Queries Base)
        // ====================================================================

        private bool IsAdmin() => User.HasClaim("TipoUsuario", "Admin");
        private bool IsDentista() => User.HasClaim("TipoUsuario", "Dentista");
        private bool IsResponsavel() => User.HasClaim("TipoUsuario", "Responsavel");

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue("ResponsavelId");
            if (userIdString != null && int.TryParse(userIdString, out int id))
            {
                return id;
            }
            return 0;
        }

        private int GetCurrentDentistaId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "DentistaId");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private IQueryable<Crianca> GetChildrenQueryBase()
        {
            IQueryable<Crianca> query = _context.Criancas.Where(c => c.Ativa);

            if (IsAdmin() || IsDentista())
                return query;

            var responsavelId = GetCurrentUserId();
            if (responsavelId == 0) return query.Where(c => false);

            return query.Where(c => c.IdResponsavel == responsavelId);
        }

        private IQueryable<Agendamento> GetAgendamentosQueryBase()
        {
            IQueryable<Agendamento> query = _context.Agendamentos
              .Include(a => a.Crianca)
              .Include(a => a.Dentista);

            if (IsAdmin()) return query;

            if (IsDentista())
            {
                var dentistaId = GetCurrentDentistaId();
                return query.Where(a => a.IdDentista == dentistaId);
            }

            var responsavelId = GetCurrentUserId();
            if (responsavelId == 0) return query.Where(a => false);

            return query.Where(a => a.Crianca!.IdResponsavel == responsavelId);
        }

        // --- LÓGICA DO BOTÃO VOLTAR INTELIGENTE ---
        private void SetReturnUrl()
        {
            // Tenta obter o 'Referer' (URL anterior)
            var refererUrl = Request.Headers["Referer"].ToString();

            // Se a URL anterior for esta mesma página ou for nula, define um fallback seguro
            if (string.IsNullOrEmpty(refererUrl) || refererUrl.Contains(Url.Action("Index", "Agendamento")))
            {
                // Fallback baseado no perfil do usuário logado
                if (IsAdmin()) ViewBag.ReturnUrl = Url.Action("Index", "Admin");
                else if (IsDentista()) ViewBag.ReturnUrl = Url.Action("Dashboard", "Dentista");
                // Assumindo que a página de perfil/agenda do Responsável seja MinhaAgenda
                else if (IsResponsavel()) ViewBag.ReturnUrl = Url.Action("MinhaAgenda", "Agendamento");
                else ViewBag.ReturnUrl = Url.Action("Index", "Home"); // Fallback final
            }
            else
            {
                ViewBag.ReturnUrl = refererUrl;
            }
        }
        
        // ====================================================================
        // LÓGICA CORE: FILTRAR DIAS DISPONÍVEIS E NÃO LOTADOS
        // ====================================================================

        private async Task<List<DateTime>> GetNextAvailableDates()
        {
            // 1. Busca TODAS as escalas (slots de horário) futuros ativos
            var escalasFuturas = await _context.EscalasMensaisDentista
                .Where(e => e.DataEscala.Date >= DateTime.Today.Date && e.Ativo)
                .Select(e => new
                {
                    e.DataEscala,
                    e.HoraInicio,
                    e.IdDentista
                })
                .ToListAsync();

            if (!escalasFuturas.Any()) return new List<DateTime>();

            // 2. Busca TODOS os agendamentos futuros para saber o que está ocupado
            var agendamentosFuturos = await _context.Agendamentos
                .Where(a => a.DataAgendamento.Date >= DateTime.Today.Date)
                .Select(a => new
                {
                    a.DataAgendamento,
                    a.HoraAgendamento,
                    a.IdDentista
                })
                .ToListAsync();

            // 3. Cria um HashSet das chaves de agendamento para comparação ultra-rápida (O(1))
            var agendamentosOcupados = new HashSet<string>(
                agendamentosFuturos.Select(a =>
                    $"{a.DataAgendamento:yyyyMMdd}-{a.HoraAgendamento}-{a.IdDentista}")
            );

            var diasComVagasLivres = new List<DateTime>();

            // 4. Agrupa as escalas por dia para verificar se o dia tem pelo menos 1 vaga
            var diasDeTrabalho = escalasFuturas
                .GroupBy(e => e.DataEscala.Date)
                .OrderBy(g => g.Key);

            foreach (var diaGroup in diasDeTrabalho)
            {
                var dataAtual = diaGroup.Key;
                bool existePeloMenosUmaVaga = false;

                foreach (var slot in diaGroup)
                {
                    string slotKey = $"{dataAtual:yyyyMMdd}-{slot.HoraInicio}-{slot.IdDentista}";

                    if (!agendamentosOcupados.Contains(slotKey))
                    {
                        existePeloMenosUmaVaga = true;
                        break;
                    }
                }

                if (existePeloMenosUmaVaga)
                {
                    diasComVagasLivres.Add(dataAtual);
                }
            }

            return diasComVagasLivres;
        }

        // ====================================================================
        // ACTIONS
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // NOVIDADE: Define a URL para o botão Voltar
                SetReturnUrl(); 
                
                var availableDates = await GetNextAvailableDates();
                var children = await GetChildrenQueryBase().ToListAsync();

                if (!children.Any())
                {
                    TempData["ErrorMessage"] = "Nenhuma criança ativa encontrada para agendamento.";
                    return RedirectToAction("MinhaAgenda");
                }

                // Regra: Se for Responsável, não deixar agendar criança que já tem consulta futura
                if (IsResponsavel())
                {
                    var agora = DateTime.Now;
                    var criancasComAgendamentoAtivo = await _context.Agendamentos
                        .Where(a => children.Select(c => c.Id).Contains(a.IdCrianca))
                        .Where(a => a.DataAgendamento.Date > agora.Date ||
                             (a.DataAgendamento.Date == agora.Date && a.HoraAgendamento > agora.TimeOfDay))
                        .Select(a => a.IdCrianca)
                        .Distinct()
                        .ToListAsync();

                    children = children.Where(c => !criancasComAgendamentoAtivo.Contains(c.Id)).ToList();

                    if (!children.Any())
                    {
                        TempData["ErrorMessage"] = "Todas as suas crianças já possuem agendamentos ativos. Você pode editar ou cancelar na tela 'Minha Agenda'.";
                        return RedirectToAction("MinhaAgenda");
                    }
                }

                var vm = new AppointmentViewModel
                {
                    Children = children,
                    AvailableDates = availableDates,
                };

                ViewBag.IsEditing = false;
                ViewBag.Action = "Confirmar";
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao carregar a tela de agendamento: " + ex.Message;
                return RedirectToAction("MinhaAgenda");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableTimes(string dateString)
        {
            if (!DateTime.TryParse(dateString, out DateTime selectedDate))
            {
                return Json(new { success = false, message = "Formato de data inválido." });
            }

            var todasEscalasNaData = await _context.EscalasMensaisDentista
                .Include(e => e.Dentista)
                .Where(e => e.DataEscala.Date == selectedDate.Date && e.Ativo)
                .ToListAsync();

            if (!todasEscalasNaData.Any())
            {
                return Json(new { success = true, times = new List<AvailableTimeSlot>() });
            }

            var bookedTimes = _context.Agendamentos
              .Where(a => a.DataAgendamento.Date == selectedDate.Date)
              .ToDictionary(a => $"{a.HoraAgendamento.Hours:D2}:{a.HoraAgendamento.Minutes:D2}-{a.IdDentista}", a => true);

            var finalAvailableSlots = new List<AvailableTimeSlot>();

            foreach (var escala in todasEscalasNaData)
            {
                var timeString = $"{escala.HoraInicio.Hours:D2}:{escala.HoraInicio.Minutes:D2}";
                var slotKey = $"{timeString}-{escala.IdDentista}";

                if (!bookedTimes.ContainsKey(slotKey))
                {
                    finalAvailableSlots.Add(new AvailableTimeSlot
                    {
                        Time = timeString,
                        DentistaId = escala.IdDentista,
                        DentistaName = escala.Dentista?.Nome ?? "Dentista"
                    });
                }
            }

            var orderedSlots = finalAvailableSlots
                .OrderBy(s => TimeSpan.Parse(s.Time))
                .ToList();

            return Json(new { success = true, times = orderedSlots });
        }

        [HttpPost]
        public async Task<IActionResult> Confirmar(AppointmentViewModel model)
        {
            if (model.AgendamentoId > 0) return BadRequest("Action Inválida para Edição.");

            if (!DateTime.TryParse(model.SelectedDateString, out DateTime dataConsulta) ||
              !TimeSpan.TryParse(model.SelectedTime, out TimeSpan horaConsulta))
            {
                TempData["ErrorMessage"] = "Formato de data ou hora inválido.";
                return RedirectToAction("Index");
            }

            var crianca = await GetChildrenQueryBase()
              .FirstOrDefaultAsync(c => c.Id == model.SelectedChildId);

            if (crianca == null)
            {
                TempData["ErrorMessage"] = "Operação inválida. A criança selecionada não existe ou não pode ser agendada por este perfil.";
                return RedirectToAction("Index");
            }

            if (IsResponsavel())
            {
                var agora = DateTime.Now;
                var temAgendamentoAtivo = await _context.Agendamentos
                  .Where(a => a.IdCrianca == model.SelectedChildId)
                  .Where(a => a.DataAgendamento.Date > agora.Date ||
                       (a.DataAgendamento.Date == agora.Date && a.HoraAgendamento > agora.TimeOfDay))
                  .AnyAsync();

                if (temAgendamentoAtivo)
                {
                    TempData["ErrorMessage"] = $"A criança {crianca.Nome} já possui um agendamento ativo.";
                    return RedirectToAction("MinhaAgenda");
                }
            }

            var jaOcupado = await _context.Agendamentos.AnyAsync(a => 
                a.DataAgendamento == dataConsulta.Date && 
                a.HoraAgendamento == horaConsulta && 
                a.IdDentista == model.SelectedDentistaId);

            if (jaOcupado)
            {
                TempData["ErrorMessage"] = "Desculpe, este horário acabou de ser reservado por outra pessoa.";
                return RedirectToAction("Index");
            }

            try
            {
                var novoAgendamento = new Agendamento
                {
                    IdCrianca = model.SelectedChildId,
                    DataAgendamento = dataConsulta.Date,
                    HoraAgendamento = horaConsulta,
                    IdDentista = model.SelectedDentistaId,
                };

                _context.Agendamentos.Add(novoAgendamento);
                await _context.SaveChangesAsync();

                TempData["SuccessMessageTitle"] = "Agendamento Confirmado!";
                TempData["SuccessMessageBody"] = $"A consulta para {crianca.Nome} foi agendada para {dataConsulta:dd/MM/yyyy} às {model.SelectedTime}.";

                return RedirectToAction("MinhaAgenda");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro ao salvar agendamento.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            // NOVIDADE: Define a URL para o botão Voltar
            SetReturnUrl();

            var agendamento = await GetAgendamentosQueryBase()
              .FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado ou acesso negado.";
                return RedirectToAction("MinhaAgenda");
            }

            var availableDates = await GetNextAvailableDates();
            
            var children = await GetChildrenQueryBase().ToListAsync();

            var vm = new AppointmentViewModel
            {
                AgendamentoId = agendamento.Id,
                SelectedChildId = agendamento.IdCrianca,
                SelectedDateString = agendamento.DataAgendamento.ToString("yyyy-MM-dd"),
                SelectedTime = agendamento.HoraAgendamento.ToString(@"hh\:mm"),
                SelectedDentistaId = agendamento.IdDentista,
                Children = children,
                AvailableDates = availableDates,
            };

            ViewBag.IsEditing = true;
            ViewBag.Action = "Atualizar";

            return View("Index", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(AppointmentViewModel model)
        {
            if (model.AgendamentoId <= 0) return BadRequest("ID inválido.");

            var agendamentoToUpdate = await GetAgendamentosQueryBase()
              .FirstOrDefaultAsync(a => a.Id == model.AgendamentoId);

            if (agendamentoToUpdate == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado.";
                return RedirectToAction("MinhaAgenda");
            }

            if (!DateTime.TryParse(model.SelectedDateString, out DateTime novaData) ||
              !TimeSpan.TryParse(model.SelectedTime, out TimeSpan novaHora))
            {
                TempData["ErrorMessage"] = "Dados inválidos.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }

            var conflitoCrianca = await _context.Agendamentos
              .Where(a => a.Id != model.AgendamentoId)
              .Where(a => a.IdCrianca == model.SelectedChildId && a.DataAgendamento.Date == novaData.Date)
              .AnyAsync();

            if (conflitoCrianca)
            {
                TempData["ErrorMessage"] = $"A criança já possui outro agendamento para o dia {novaData:dd/MM/yyyy}.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }

            var conflitoHorario = await _context.Agendamentos
                .Where(a => a.Id != model.AgendamentoId)
                .Where(a => a.DataAgendamento == novaData.Date && 
                            a.HoraAgendamento == novaHora && 
                            a.IdDentista == model.SelectedDentistaId)
                .AnyAsync();

            if (conflitoHorario)
            {
                 TempData["ErrorMessage"] = "O horário selecionado não está mais disponível.";
                 return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }

            agendamentoToUpdate.IdCrianca = model.SelectedChildId;
            agendamentoToUpdate.DataAgendamento = novaData.Date;
            agendamentoToUpdate.HoraAgendamento = novaHora;
            agendamentoToUpdate.IdDentista = model.SelectedDentistaId;

            try
            {
                _context.Agendamentos.Update(agendamentoToUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessageTitle"] = "Agendamento Atualizado!";
                TempData["SuccessMessageBody"] = $"O agendamento foi alterado para {novaData:dd/MM/yyyy} às {model.SelectedTime}.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro ao atualizar agendamento.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }

            return RedirectToAction("MinhaAgenda");
        }

        [HttpGet]
        public async Task<IActionResult> MinhaAgenda(
            string? searchName,
            List<int>? dentists,
            string? selectedDate)
        {
            IQueryable<Agendamento> query = GetAgendamentosQueryBase();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(a =>
                    a.Crianca != null && a.Crianca.Nome.Contains(searchName));
                ViewData["CurrentNameFilter"] = searchName;
            }

            if (dentists != null && dentists.Any())
            {
                query = query.Where(a => a.Dentista != null && dentists.Contains(a.IdDentista));
                ViewData["SelectedDentists"] = dentists;
            }

            if (!string.IsNullOrEmpty(selectedDate) &&
                DateTime.TryParseExact(selectedDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime filterDate))
            {
                query = query.Where(a => a.DataAgendamento.Date == filterDate.Date);
                ViewData["CurrentDateFilter"] = selectedDate;
            }

            var agendamentos = await query.ToListAsync();

            var agendamentosOrdenados = agendamentos
                .OrderByDescending(a => a.DataAgendamento.Date.Add(a.HoraAgendamento) >= DateTime.Now)
                .ThenByDescending(a => a.DataAgendamento)
                .ThenByDescending(a => a.HoraAgendamento)
                .ToList();

            ViewBag.Dentistas = await _context.Dentistas.Where(d => d.Ativo).ToListAsync();

            var allAgendamentos = await GetAgendamentosQueryBase().ToListAsync();
            var datasAgendadas = allAgendamentos
                .Select(a => a.DataAgendamento.Date)
                .Distinct()
                .ToList();

            ViewBag.DatasAgendadas = datasAgendadas.Select(d => d.ToString("M/d/yyyy")).ToList();

            return View(agendamentosOrdenados);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var agendamento = await GetAgendamentosQueryBase()
              .FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado.";
                return RedirectToAction("MinhaAgenda");
            }

            if (agendamento.DataAgendamento.Date.Add(agendamento.HoraAgendamento) < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Não é possível cancelar agendamentos passados.";
                return RedirectToAction("MinhaAgenda");
            }

            try
            {
                _context.Agendamentos.Remove(agendamento);
                await _context.SaveChangesAsync();

                TempData["SuccessMessageTitle"] = "Agendamento Cancelado!";
                TempData["SuccessMessageBody"] = $"A consulta da criança {agendamento.Crianca?.Nome} foi cancelada.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao cancelar: {ex.Message}";
            }

            return RedirectToAction("MinhaAgenda");
        }
    }
}