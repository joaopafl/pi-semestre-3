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

namespace Pi_Odonto.Controllers
{
    // Classe Auxiliar para o JSON de retorno (Mantida)
    public class AvailableTimeSlot
    {
        public string Time { get; set; } = string.Empty;
        public int DentistaId { get; set; }
        public string DentistaName { get; set; } = string.Empty;
    }

    // [Authorize] // Recomendado adicionar para garantir que apenas usuários logados acessem o Controller
    public class AgendamentoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeSpan _slotDuration = TimeSpan.FromHours(1); 

        public AgendamentoController(AppDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // MÉTODOS AUXILIARES CHAVE (AUTORIZAÇÃO POR PERFIL)
        // ====================================================================
        
        // Retorna o ID do usuário/responsável logado.
        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue("ResponsavelId"); // Tenta a claim customizada
            if (userIdString != null && int.TryParse(userIdString, out int id))
            {
                return id;
            }
            userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Fallback: Tenta a claim padrão
            if (userIdString != null && int.TryParse(userIdString, out int fallbackId))
            {
                 return fallbackId;
            }
            return 0;
        }

        // NOVO: Retorna a Query de Crianças já filtrada pela Role do usuário
        private IQueryable<Crianca> GetChildrenQueryBase()
        {
            // 1. Inicia a query base
            IQueryable<Crianca> query = _context.Criancas.Where(c => c.Ativa);
            
            // 2. Define as Roles de alto nível (acesso a TODAS as crianças)
            bool isAdminOrDentista = User.IsInRole("Admin") || User.IsInRole("Dentista");
            
            // 3. Aplica a Lógica de Filtragem:
            if (isAdminOrDentista)
            {
                // Admin e Dentista veem TODAS as crianças ativas
                return query; 
            }
            
            // Se não for Admin/Dentista (assumindo que seja Responsável ou similar)
            var responsavelId = GetCurrentUserId();
            if (responsavelId == 0)
            {
                // Usuário sem ID válido não deve ver nenhuma criança
                return query.Where(c => false);
            }
            
            // Responsável vê APENAS suas crianças
            return query.Where(c => c.IdResponsavel == responsavelId);
        }

        // NOVO: Retorna a Query de Agendamentos já filtrada pela Role do usuário
        private IQueryable<Agendamento> GetAgendamentosQueryBase()
        {
            // 1. Inicia a query base com os includes
            IQueryable<Agendamento> query = _context.Agendamentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista);

            // 2. Define as Roles de alto nível (acesso a TODOS os agendamentos)
            bool isAdminOrDentista = User.IsInRole("Admin") || User.IsInRole("Dentista");
            
            // 3. Aplica a Lógica de Filtragem:
            if (isAdminOrDentista)
            {
                // Admin e Dentista veem TODOS os agendamentos
                return query;
            }

            // Se for Responsável ou similar
            var responsavelId = GetCurrentUserId();
            if (responsavelId == 0)
            {
                // Usuário sem ID válido não deve ver nenhum agendamento
                return query.Where(a => false);
            }
            
            // Responsável vê APENAS os agendamentos das suas crianças
            return query.Where(a => a.Crianca!.IdResponsavel == responsavelId);
        }

        private List<DateTime> GetNextAvailableDates(int count)
        {
            var dates = new List<DateTime>();
            var currentDate = DateTime.Today.AddDays(1); 

            while (dates.Count < count)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }
            return dates;
        }

        private string GetDayOfWeekString(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Segunda-feira",
                DayOfWeek.Tuesday => "Terça-feira",
                DayOfWeek.Wednesday => "Quarta-feira",
                DayOfWeek.Thursday => "Quinta-feira",
                DayOfWeek.Friday => "Sexta-feira",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => string.Empty,
            };
        }

        // ====================================================================
        // 1. TELA DE NOVO AGENDAMENTO (INDEX - GET) - REVISADO
        // ====================================================================

        // GET: /Agendamento/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Auth");
            
            try
            {
                var availableDates = GetNextAvailableDates(7); 

                // *** ALTERADO: Usa o GetChildrenQueryBase para filtrar as crianças ***
                var children = await GetChildrenQueryBase().ToListAsync();

                if (!children.Any())
                {
                    TempData["ErrorMessage"] = "Nenhuma criança ativa encontrada para agendamento.";
                    return RedirectToAction("MinhaAgenda");
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
                TempData["ErrorMessage"] = "Erro ao carregar a tela de agendamento. " + ex.Message;
                return RedirectToAction("MinhaAgenda"); 
            }
        }
        
        // ====================================================================
        // 2. ACTION API PARA CARREGAR HORÁRIOS (AJAX) - MANTIDO
        // ====================================================================

        // MÉTODO: Obter Horários Disponíveis de TODOS os Dentistas
        [HttpGet]
        public JsonResult GetAvailableTimes(string dateString)
        {
            if (!DateTime.TryParse(dateString, out DateTime selectedDate))
            {
                return Json(new { success = false, message = "Formato de data inválido." });
            }

            var dayOfWeekString = GetDayOfWeekString(selectedDate.DayOfWeek);
            
            var allDisponibilidades = _context.DisponibilidadesDentista
                    .Include(d => d.Dentista) 
                    .Where(d => d.DiaSemana == dayOfWeekString && d.Ativo)
                    .ToList();
            
            if (!allDisponibilidades.Any())
            {
                return Json(new { success = true, times = new List<AvailableTimeSlot>() }); 
            }

            var bookedTimes = _context.Agendamentos
                             .Where(a => a.DataAgendamento.Date == selectedDate.Date)
                             .ToDictionary(a => $"{a.HoraAgendamento.Hours:D2}:{a.HoraAgendamento.Minutes:D2}-{a.IdDentista}", a => true);
            
            var finalAvailableSlots = new List<AvailableTimeSlot>();

            foreach (var disp in allDisponibilidades)
            {
                var current = disp.HoraInicio;
                var horaFim = disp.HoraFim;

                while (current.Add(_slotDuration) <= horaFim)
                {
                    var timeString = $"{current.Hours:D2}:{current.Minutes:D2}"; 
                    var slotKey = $"{timeString}-{disp.IdDentista}";
                    
                    if (!bookedTimes.ContainsKey(slotKey))
                    {
                        finalAvailableSlots.Add(new AvailableTimeSlot
                        {
                            Time = timeString,
                            DentistaId = disp.IdDentista,
                            DentistaName = disp.Dentista?.Nome ?? "Dentista Não Encontrado" 
                        });
                    }
                    current = current.Add(_slotDuration);
                }
            }
            
            var orderedSlots = finalAvailableSlots
                                .OrderBy(s => TimeSpan.Parse(s.Time))
                                .ToList();

            return Json(new { success = true, times = orderedSlots });
        }

        // ====================================================================
        // 3. CONFIRMAR NOVO AGENDAMENTO (POST) - REVISADO (Validação de Criança)
        // ====================================================================

        // POST: /Agendamento/Confirmar
        [HttpPost]
        public async Task<IActionResult> Confirmar(AppointmentViewModel model)
        {
            if (model.AgendamentoId > 0) return BadRequest("Action Inválida para Edição.");
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Auth");

            if (!DateTime.TryParse(model.SelectedDateString, out DateTime dataConsulta) ||
                !TimeSpan.TryParse(model.SelectedTime, out TimeSpan horaConsulta))
            {
                TempData["ErrorMessage"] = "Formato de data ou hora inválido.";
                return RedirectToAction("Index");
            }

            // *** ALTERADO: Usa o GetChildrenQueryBase para validar que a criança pertence ao usuário ***
            var crianca = await GetChildrenQueryBase()
                .FirstOrDefaultAsync(c => c.Id == model.SelectedChildId);
            
            if (crianca == null)
            {
                TempData["ErrorMessage"] = "Operação inválida. A criança selecionada não existe ou não pode ser agendada por este perfil.";
                return RedirectToAction("Index");
            }

            // ... (Restante da lógica de validação de agendamento existente)
            var agendamentoExistente = await _context.Agendamentos
                .Where(a => a.IdCrianca == model.SelectedChildId && a.DataAgendamento.Date == dataConsulta.Date)
                .FirstOrDefaultAsync();
            
            if (agendamentoExistente != null)
            {
                TempData["ErrorMessage"] = $"A criança {crianca.Nome} já possui um agendamento para o dia {dataConsulta:dd/MM/yyyy}.";
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
                TempData["SuccessMessageBody"] = $"A consulta para {crianca.Nome} foi agendada para {dataConsulta.ToString("dd/MM/yyyy")} às {model.SelectedTime}.";
                
                return RedirectToAction("MinhaAgenda");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro: O horário selecionado não está mais disponível ou houve uma falha de conexão.";
                return RedirectToAction("Index");
            }
        }

        // ====================================================================
        // 4. EDIÇÃO DE AGENDAMENTO (EDITAR - GET) - REVISADO (Filtro Agendamento)
        // ====================================================================

        // GET: /Agendamento/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Auth");

            // *** ALTERADO: Usa o GetAgendamentosQueryBase para garantir o acesso ao agendamento ***
            var agendamento = await GetAgendamentosQueryBase()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agendamento == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado ou você não tem permissão para editar.";
                return RedirectToAction("MinhaAgenda");
            }

            var availableDates = GetNextAvailableDates(7);

            // *** ALTERADO: Usa o GetChildrenQueryBase para listar as crianças ***
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

        // ====================================================================
        // 5. SALVAR EDIÇÃO DE AGENDAMENTO (ATUALIZAR - POST) - REVISADO (Validação)
        // ====================================================================

        // POST: /Agendamento/Atualizar
        [HttpPost]
        public async Task<IActionResult> Atualizar(AppointmentViewModel model)
        {
            if (model.AgendamentoId <= 0) return BadRequest("ID de agendamento inválido.");
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Auth");
            
            // 1. Verifica se o usuário tem permissão para editar ESTE agendamento
            var agendamentoToUpdate = await GetAgendamentosQueryBase()
                .FirstOrDefaultAsync(a => a.Id == model.AgendamentoId);

            if (agendamentoToUpdate == null)
            {
                TempData["ErrorMessage"] = "Agendamento não encontrado ou você não tem permissão para atualizar.";
                return RedirectToAction("MinhaAgenda");
            }

            // 2. Verifica se o usuário tem permissão para usar a NOVA criança selecionada
            var novaCrianca = await GetChildrenQueryBase()
                .FirstOrDefaultAsync(c => c.Id == model.SelectedChildId);

            if (novaCrianca == null)
            {
                TempData["ErrorMessage"] = "A criança selecionada para edição não é válida para este perfil.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }
            
            // ... (Restante da lógica de parsing de data/hora e validações)
            if (!DateTime.TryParse(model.SelectedDateString, out DateTime novaData) || 
                !TimeSpan.TryParse(model.SelectedTime, out TimeSpan novaHora))
            {
                TempData["ErrorMessage"] = "Formato de data ou hora inválido.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }
            
            // ... (Restante da lógica de validação de agendamento existente)
            var agendamentoExistente = await _context.Agendamentos
                .Where(a => a.Id != model.AgendamentoId) 
                .Where(a => a.IdCrianca == model.SelectedChildId && a.DataAgendamento.Date == novaData.Date)
                .FirstOrDefaultAsync();
            
            if (agendamentoExistente != null)
            {
                TempData["ErrorMessage"] = $"A criança já possui outro agendamento para o dia {novaData:dd/MM/yyyy}.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }

            // ... (Aplicação das mudanças e save)
            agendamentoToUpdate.IdCrianca = model.SelectedChildId;
            agendamentoToUpdate.DataAgendamento = novaData.Date;
            agendamentoToUpdate.HoraAgendamento = novaHora;
            agendamentoToUpdate.IdDentista = model.SelectedDentistaId;
            
            try
            {
                _context.Agendamentos.Update(agendamentoToUpdate);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessageTitle"] = "Agendamento Atualizado!";
                TempData["SuccessMessageBody"] = $"O agendamento foi alterado com sucesso para {novaData.ToString("dd/MM/yyyy")} às {model.SelectedTime}.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro interno ao atualizar o agendamento. Tente novamente.";
                return RedirectToAction("Editar", new { id = model.AgendamentoId });
            }
            
            return RedirectToAction("MinhaAgenda");
        }

        // ====================================================================
        // 6. MINHA AGENDA (LISTAGEM) - REVISADO (Filtro Agendamento)
        // ====================================================================

        // GET: /Agendamento/MinhaAgenda
        [HttpGet]
        public async Task<IActionResult> MinhaAgenda()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Auth");
            
            // *** ALTERADO: Usa o GetAgendamentosQueryBase para obter a lista filtrada ***
            IQueryable<Agendamento> query = GetAgendamentosQueryBase()
                .OrderByDescending(a => a.DataAgendamento)
                .ThenBy(a => a.HoraAgendamento);
            
            var agendamentos = await query.ToListAsync();

            return View(agendamentos); 
        }

        // ====================================================================
        // 7. CANCELAR AGENDAMENTO (CANCELAR - POST) - REVISADO (Filtro Agendamento)
        // ====================================================================

        // POST: /Agendamento/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!User.Identity.IsAuthenticated) 
            {
                TempData["ErrorMessage"] = "Sessão expirada. Faça login novamente.";
                return RedirectToAction("Login", "Auth");
            }

            // *** ALTERADO: Usa o GetAgendamentosQueryBase para garantir o acesso ao agendamento ***
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
                TempData["SuccessMessageBody"] = $"A consulta da criança {agendamento.Crianca?.Nome} para {agendamento.DataAgendamento:dd/MM/yyyy} foi cancelada com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao tentar cancelar o agendamento: {ex.Message}";
            }

            return RedirectToAction("MinhaAgenda");
        }
    }
}