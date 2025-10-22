// Pi_Odonto.Controllers/AgendamentoController.cs

using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.ViewModels; 
using Pi_Odonto.Models;
using System.Security.Claims; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq; 
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Pi_Odonto.Controllers
{
    // Classe Auxiliar para o JSON de retorno (para incluir o ID do Dentista)
    public class AvailableTimeSlot
    {
        public string Time { get; set; } // Ex: "08:00"
        public int DentistaId { get; set; }
        public string DentistaName { get; set; }
    }

    // [Authorize] 
    public class AgendamentoController : Controller
    {
        private readonly AppDbContext _context;
        // Duração da consulta
        private readonly TimeSpan _slotDuration = TimeSpan.FromHours(1);

        public AgendamentoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Agendamento/Index
        [HttpGet]
        public IActionResult Index()
        {
            var userIdString = User.FindFirstValue("ResponsavelId");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int responsavelId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // 1. GERA OS PRÓXIMOS 7 DIAS ÚTEIS
            var availableDates = GetNextAvailableDates(7); 

            // 2. BUSCA A LISTA DE CRIANÇAS
            var children = _context.Criancas
                                   .Where(c => c.Ativa && c.IdResponsavel == responsavelId)
                                   .ToList();

            var vm = new AppointmentViewModel
            {
                Children = children,
                AvailableDates = availableDates,
            };

            return View(vm);
        }
        
        // MÉTODO: Obter Horários Disponíveis de TODOS os Dentistas (Via AJAX)
        [HttpGet]
        public JsonResult GetAvailableTimes(string dateString)
        {
            if (!DateTime.TryParse(dateString, out DateTime selectedDate))
            {
                return Json(new { success = false, message = "Formato de data inválido." });
            }

            var dayOfWeekString = GetDayOfWeekString(selectedDate.DayOfWeek);
            
            // 1. BUSCA TODAS AS DISPONIBILIDADES ATIVAS PARA O DIA SELECIONADO
            var allDisponibilidades = _context.DisponibilidadesDentista
                                              .Include(d => d.Dentista) 
                                              .Where(d => d.DiaSemana == dayOfWeekString && d.Ativo)
                                              .ToList();
            
            if (!allDisponibilidades.Any())
            {
                return Json(new { success = true, times = new List<AvailableTimeSlot>() }); 
            }

            // 2. BUSCA TODOS OS AGENDAMENTOS JÁ EXISTENTES PARA A DATA E CRIA O DICIONÁRIO
            var bookedTimes = _context.Agendamentos
                              .Where(a => a.DataAgendamento.Date == selectedDate.Date)
                              // *** CORREÇÃO DEFINITIVA (Resolve FormatException na linha 101) ***
                              .ToDictionary(a => $"{a.HoraAgendamento.Hours:D2}:{a.HoraAgendamento.Minutes:D2}-{a.IdDentista}", a => true);
            
            // 3. GERA OS SLOTS DISPONÍVEIS, INCLUINDO O ID E NOME DO DENTISTA
            var finalAvailableSlots = new List<AvailableTimeSlot>();

            foreach (var disp in allDisponibilidades)
            {
                var current = disp.HoraInicio;
                while (current.Add(_slotDuration) <= disp.HoraFim)
                {
                    // Usa a mesma formatação segura para o slotKey
                    var timeString = $"{current.Hours:D2}:{current.Minutes:D2}"; 
                    var slotKey = $"{timeString}-{disp.IdDentista}";
                    
                    // Verifica se o slot jÁ está agendado para ESTE dentista
                    if (!bookedTimes.ContainsKey(slotKey))
                    {
                        finalAvailableSlots.Add(new AvailableTimeSlot
                        {
                            Time = timeString,
                            DentistaId = disp.IdDentista,
                            DentistaName = disp.Dentista.Nome 
                        });
                    }
                    current = current.Add(_slotDuration);
                }
            }
            
            // 4. ORDENA POR HORÁRIO e depois por Nome do Dentista
            var orderedSlots = finalAvailableSlots
                                .OrderBy(s => TimeSpan.Parse(s.Time))
                                .ToList();

            return Json(new { success = true, times = orderedSlots });
        }

        // POST: /Agendamento/Confirmar
        [HttpPost]
        public IActionResult Confirmar(AppointmentViewModel model)
        {
            // 1. VALIDAÇÃO DE USUÁRIO E DADOS BÁSICOS
            var userIdString = User.FindFirstValue("ResponsavelId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int responsavelId))
            {
                TempData["ErrorMessage"] = "Sessão expirada. Faça login novamente.";
                return RedirectToAction("Login", "Auth");
            }

            if (model.SelectedChildId <= 0 || string.IsNullOrEmpty(model.SelectedDateString) || string.IsNullOrEmpty(model.SelectedTime) || model.SelectedDentistaId <= 0)
            {
                TempData["ErrorMessage"] = "Por favor, selecione a criança, a data, o horário e o dentista para agendar.";
                return RedirectToAction("Index"); 
            }
            
            // 2. CONVERSÃO DE DATA E HORA
            if (!DateTime.TryParse(model.SelectedDateString, out DateTime dataConsulta))
            {
                TempData["ErrorMessage"] = "Formato de data inválido.";
                return RedirectToAction("Index");
            }

            if (!TimeSpan.TryParse(model.SelectedTime, out TimeSpan horaConsulta))
            {
                TempData["ErrorMessage"] = "Formato de hora inválido.";
                return RedirectToAction("Index");
            }

            // 3. VALIDAÇÃO DE POSSE DA CRIANÇA E BUSCA DOS DADOS
            var crianca = _context.Criancas
                                  .FirstOrDefault(c => c.Id == model.SelectedChildId && c.IdResponsavel == responsavelId);
                              
            if (crianca == null)
            {
                TempData["ErrorMessage"] = "Operação inválida. A criança selecionada não pertence à sua conta.";
                return RedirectToAction("Index");
            }
            
            // 4. VALIDAÇÃO: CRIANÇA JÁ AGENDADA NO DIA
            var agendamentoExistente = _context.Agendamentos
                                                .Where(a => a.IdCrianca == model.SelectedChildId && 
                                                            a.DataAgendamento.Date == dataConsulta.Date)
                                                .FirstOrDefault();
            
            if (agendamentoExistente != null)
            {
                TempData["ErrorMessage"] = $"A criança {crianca.Nome} já possui um agendamento confirmado para o dia {dataConsulta:dd/MM/yyyy}. Por favor, escolha outra data.";
                return RedirectToAction("Index"); 
            }
            

            // 5. LÓGICA DE SALVAMENTO NO BANCO DE DADOS
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
                _context.SaveChanges(); 
            }
            catch (DbUpdateException ex)
            {
                // Tratamento de falha de concorrência ou Unique Constraint
                TempData["ErrorMessage"] = "Erro: O horário selecionado não está mais disponível. Por favor, escolha outro horário.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro interno ao finalizar o agendamento. Tente novamente.";
                return RedirectToAction("Index");
            }
            
            // 6. REDIRECIONAMENTO DE SUCESSO
            TempData["AgendamentoSucesso"] = true;
            TempData["SuccessMessageTitle"] = "Agendamento Confirmado!";
            TempData["SuccessMessageBody"] = $"A consulta para a criança {crianca.Nome} foi agendada com sucesso para {dataConsulta.ToString("dd/MM/yyyy")} às {model.SelectedTime}.";
            
            return RedirectToAction("Index", "Perfil");
        }
        
        // =======================================================
        // MÉTODOS AUXILIARES
        // =======================================================

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

        private List<DateTime> GetNextAvailableDates(int count)
        {
            var dates = new List<DateTime>();
            var currentDate = DateTime.Today.AddDays(1); // Começa a partir de amanhã

            while (dates.Count < count)
            {
                // Pega apenas dias úteis (Segunda a Sexta).
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }
            return dates;
        }
    }
}