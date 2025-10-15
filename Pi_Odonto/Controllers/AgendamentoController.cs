// AgendamentoController.cs (Ajustado)

using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.ViewModels; // Garanta que o ViewModel está aqui
using System.Security.Claims; // Garanta que este using está presente
using Microsoft.EntityFrameworkCore;
// ... outros usings

namespace Pi_Odonto.Controllers
{
    // ... O resto da sua classe AgendamentoController

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
            // 1. OBTENDO O ID USANDO A CLAIM PERSONALIZADA "ResponsavelId"
            var userIdString = User.FindFirstValue("ResponsavelId");

            // 2. VERIFICAÇÃO DE AUTENTICAÇÃO
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int responsavelId))
            {
                // Se não estiver logado ou o ID for inválido, redireciona para o Login
                return RedirectToAction("Login", "Auth");
            }

            // 3. BUSCA DE DADOS REAIS
            var vm = new AppointmentViewModel
            {
                SelectedDate = DateTime.Today,

                // Busca crianças onde o IdResponsavel seja o ID do usuário logado
                Children = _context.Criancas
                                   .Where(c => c.Ativa && c.IdResponsavel == responsavelId)
                                   .ToList(),

                // Assumindo que você tem o método GetNextSaturdays implementado
                AvailableSaturdays = GetNextSaturdays(6)
            };

            return View(vm);
        }

        // ... Seus outros métodos, como Post e o GetNextSaturdays ...

        private List<DateTime> GetNextSaturdays(int count)
        {
            // Implementação do método auxiliar de datas (usado no seu código)
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
        
        // AgendamentoController.cs

// ... (seus outros métodos) ...

// AgendamentoController.cs

// POST: /Agendamento/Confirmar
[HttpPost]
public IActionResult Confirmar(AppointmentViewModel model)
{
    // ... (Sua lógica de validação e salvamento) ...
    
    // Supondo que o salvamento foi um sucesso:
    
    // ******************************************************
    // *** GATILHO PARA MOSTRAR O POP-UP NA PRÓXIMA TELA ***
    // ******************************************************
    TempData["AgendamentoSucesso"] = true; // Chave que a View Perfil vai verificar
    
    // Opcional: Você pode enviar a mensagem final aqui, se quiser
    TempData["SuccessMessageTitle"] = "Agendamento Confirmado!";
    TempData["SuccessMessageBody"] = "Seu agendamento foi realizado com sucesso. Nos vemos em breve! 😊";
    
    // *** REDIRECIONAMENTO ***
    // O servidor manda o navegador ir para a página de Perfil
    return RedirectToAction("Index", "Perfil");
}

        // ... O restante do seu controller
    }
}