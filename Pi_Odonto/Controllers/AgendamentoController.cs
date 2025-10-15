using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.ViewModels; 
using Pi_Odonto.Models; // Necessário para a Model Agendamento e Crianca
using System.Security.Claims; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Pi_Odonto.Controllers
{
    public class AgendamentoController : Controller
    {
        private readonly AppDbContext _context;

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

            var vm = new AppointmentViewModel
            {
                // Busca crianças onde o IdResponsavel seja o ID do usuário logado
                Children = _context.Criancas
                                   .Where(c => c.Ativa && c.IdResponsavel == responsavelId)
                                   .ToList(),

                AvailableSaturdays = GetNextSaturdays(6)
            };

            return View(vm);
        }

        // POST: /Agendamento/Confirmar
        [HttpPost]
        public IActionResult Confirmar(AppointmentViewModel model)
        {
            // 1. OBTENDO O ID DO RESPONSÁVEL (Para validação)
            var userIdString = User.FindFirstValue("ResponsavelId");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int responsavelId))
            {
                TempData["ErrorMessage"] = "Sessão expirada. Faça login novamente.";
                return RedirectToAction("Login", "Auth");
            }

            // 2. VALIDAÇÃO BÁSICA DO MODELO
            // USANDO A NOVA PROPRIEDADE: model.SelectedDateString
            if (model.SelectedChildId <= 0 || string.IsNullOrEmpty(model.SelectedDateString) || string.IsNullOrEmpty(model.SelectedTime))
            {
                TempData["ErrorMessage"] = "Por favor, selecione a criança, a data e o horário para agendar.";
                return RedirectToAction("Index"); 
            }
            
            // 3. VALIDAÇÃO DE POSSE DA CRIANÇA
            var crianca = _context.Criancas
                                  .FirstOrDefault(c => c.Id == model.SelectedChildId && c.IdResponsavel == responsavelId);
                              
            if (crianca == null)
            {
                TempData["ErrorMessage"] = "Operação inválida. A criança selecionada não pertence à sua conta.";
                return RedirectToAction("Index");
            }

            // 4. CONVERSÃO DE DATA E HORA
            // USANDO A NOVA PROPRIEDADE: model.SelectedDateString
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
            
            // 5. LÓGICA DE SALVAMENTO NO BANCO DE DADOS
            
            // ******************************************************************************
            // ÁREA DE INTEGRAÇÃO FUTURA: Dentista e Escala
            // Você substituirá 'DENTISTA_ID_FIXO' por model.SelectedDentistaId após a integração.
            // ******************************************************************************
            const int DENTISTA_ID_FIXO = 1; // ID temporário
            
            try
            {
                var novoAgendamento = new Agendamento
                {
                    IdCrianca = model.SelectedChildId,
                    DataAgendamento = dataConsulta.Date,
                    HoraAgendamento = horaConsulta,
                    IdDentista = DENTISTA_ID_FIXO, // <-- MUDAR AQUI APÓS INTEGRAÇÃO
                };

                // ADICIONAR AO CONTEXTO (DbSet<Agendamento> deve existir em AppDbContext)
                _context.Agendamentos.Add(novoAgendamento);
                _context.SaveChanges(); 
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro interno ao finalizar o agendamento. Tente novamente.";
                return RedirectToAction("Index");
            }
            
            // 6. REDIRECIONAMENTO DE SUCESSO (Com Pop-up)
            TempData["AgendamentoSucesso"] = true;
            TempData["SuccessMessageTitle"] = "Agendamento Confirmado!";
            TempData["SuccessMessageBody"] = $"A consulta para a criança {crianca.Nome} foi agendada com sucesso para {dataConsulta.ToString("dd/MM/yyyy")} às {model.SelectedTime}. Nos vemos em breve! 😊";
            
            return RedirectToAction("Index", "Perfil");
        }

        private List<DateTime> GetNextSaturdays(int count)
        {
            var saturdays = new List<DateTime>();
            var currentDate = DateTime.Today;

            while (saturdays.Count < count)
            {
                if (currentDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    saturdays.Add(currentDate);
                }
                currentDate = currentDate.AddDays(1);
            }
            return saturdays;
        }
    }
}