using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Helpers;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels; // Garanta que este namespace exista ou remova/ajuste conforme seu projeto
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pi_Odonto.Controllers
{
    // Aplica a política de autenticação para Dentistas
    [Authorize(Policy = "DentistaOnly", AuthenticationSchemes = "DentistaAuth")]
    public class DentistaController : Controller
    {
        private readonly AppDbContext _context;

        public DentistaController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // MÉTODOS DE SUPORTE
        // ==========================================================

        private bool IsDentista() => User.HasClaim("TipoUsuario", "Dentista");

        private int GetCurrentDentistaId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "DentistaId");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // ==========================================================
        // DASHBOARD DO DENTISTA
        // ==========================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            var dentistaId = GetCurrentDentistaId();

            if (dentistaId == 0)
            {
                return RedirectToAction("DentistaLogin", "Auth");
            }

            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
            {
                return RedirectToAction("DentistaLogin", "Auth");
            }

            // Estatísticas gerais
            ViewBag.TotalAgendamentos = _context.Agendamentos
                .Count(a => a.IdDentista == dentistaId);

            ViewBag.AgendamentosHoje = _context.Agendamentos
                .Count(a => a.IdDentista == dentistaId &&
                            a.DataAgendamento.Date == DateTime.Today);

            ViewBag.AtendimentosRealizados = _context.Atendimentos
                .Count(a => a.IdDentista == dentistaId);

            // Próximos 5 agendamentos
            ViewBag.ProximosAgendamentos = _context.Agendamentos
                .Include(a => a.Crianca)
                .Where(a => a.IdDentista == dentistaId &&
                            a.DataAgendamento.Date >= DateTime.Today)
                .OrderBy(a => a.DataAgendamento)
                .ThenBy(a => a.HoraAgendamento)
                .Take(5)
                .ToList();

            return View(dentista);
        }

        // ==========================================================
        // ESCALA DE TRABALHO DO DENTISTA (CALENDÁRIO MENSAL)
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> EscalaTrabalho(DateTime? data) // Adicionado 'data' para navegação
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            if (dentistaId == 0)
                return RedirectToAction("DentistaLogin", "Auth");

            var dataReferencia = data ?? DateTime.Today;

            // Calcula o primeiro dia do mês e o último dia do mês
            var primeiroDiaDoMes = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
            var ultimoDiaDoMes = primeiroDiaDoMes.AddMonths(1).AddDays(-1);

            // 1. Popula as ViewBags para Navegação e Título
            ViewBag.Ano = dataReferencia.Year;
            ViewBag.Mes = dataReferencia.Month;
            ViewBag.PrimeiroDia = primeiroDiaDoMes;
            ViewBag.UltimoDia = ultimoDiaDoMes;

            // 2. Busca o dentista logado para a legenda
            var dentistaLogado = await _context.Dentistas.FindAsync(dentistaId);
            ViewBag.DentistaLogado = dentistaLogado;
            // Cria uma lista com o dentista logado para a lógica de iteração da View
            ViewBag.Dentistas = new List<Dentista> { dentistaLogado };

            // 3. Busca as escalas APENAS DO DENTISTA LOGADO para o mês de referência
            var escalasNoMes = await _context.EscalasMensaisDentista
                .Where(e => e.IdDentista == dentistaId &&
                            e.DataEscala.Year == dataReferencia.Year &&
                            e.DataEscala.Month == dataReferencia.Month)
                .OrderBy(e => e.DataEscala)
                .ThenBy(e => e.HoraInicio)
                .ToListAsync();

            // 4. Cria o dicionário de escalas agrupadas (Data -> DentistaId -> Lista de Escalas)
            // Essencial para a View em formato calendário
            var escalasPorData = escalasNoMes
                .GroupBy(e => e.DataEscala.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(e => e.IdDentista)
                          .ToDictionary(d => d.Key, d => d.ToList())
                );
            ViewBag.EscalasPorData = escalasPorData;

            // Retorna a lista de escalas para a View usar como Model (opcional, mas mantido)
            return View(escalasNoMes);
        }

        // ==========================================================
        // CRIAR DISPONIBILIDADE (Mantido, mas sugere-se controle pelo Admin)
        // ==========================================================

        [HttpGet]
        public IActionResult CreateEscala()
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var viewModel = new EscalaMensalDentista
            {
                IdDentista = GetCurrentDentistaId(),
                DataEscala = DateTime.Today
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEscala(EscalaMensalDentista model)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();
            model.IdDentista = dentistaId;
            model.Ativo = true;
            model.DataCadastro = DateTime.Now;

            // Remove a validação do Model.Id, pois é gerado no banco
            ModelState.Remove(nameof(model.Id));

            if (ModelState.IsValid)
            {
                _context.EscalasMensaisDentista.Add(model);
                _context.SaveChanges();

                TempData["Sucesso"] = $"Escala cadastrada para {model.DataEscala:dd/MM/yyyy} com sucesso!";
                return RedirectToAction("EscalaTrabalho", new { data = model.DataEscala.ToString("yyyy-MM-dd") });
            }

            return View(model);
        }

        // ==========================================================
        // EDITAR DISPONIBILIDADE
        // ==========================================================

        [HttpGet]
        public IActionResult EditEscala(int id)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var escala = _context.EscalasMensaisDentista
                .FirstOrDefault(e => e.Id == id && e.IdDentista == dentistaId);

            if (escala == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("EscalaTrabalho");
            }

            return View(escala);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditEscala(EscalaMensalDentista model)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var escalaExistente = _context.EscalasMensaisDentista
                .FirstOrDefault(e => e.Id == model.Id && e.IdDentista == dentistaId);

            if (escalaExistente == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("EscalaTrabalho");
            }

            if (ModelState.IsValid)
            {
                // Atualiza APENAS os campos mutáveis
                escalaExistente.DataEscala = model.DataEscala;
                escalaExistente.HoraInicio = model.HoraInicio;
                escalaExistente.HoraFim = model.HoraFim;
                escalaExistente.Ativo = model.Ativo;

                _context.SaveChanges();

                TempData["Sucesso"] = "Escala atualizada com sucesso!";
                return RedirectToAction("EscalaTrabalho", new { data = model.DataEscala.ToString("yyyy-MM-dd") });
            }

            return View(model);
        }

        // ==========================================================
        // DELETAR DISPONIBILIDADE
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEscala(int id)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var escala = _context.EscalasMensaisDentista
                .FirstOrDefault(e => e.Id == id && e.IdDentista == dentistaId);

            if (escala == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("EscalaTrabalho");
            }

            // Você pode adicionar a verificação de agendamentos futuros aqui se desejar, como no AdminController

            _context.EscalasMensaisDentista.Remove(escala);
            _context.SaveChanges();

            TempData["Sucesso"] = "Escala removida com sucesso!";
            return RedirectToAction("EscalaTrabalho", new { data = escala.DataEscala.ToString("yyyy-MM-dd") });
        }

        // ==========================================================
        // MEU PERFIL (DENTISTA)
        // ==========================================================

        [HttpGet]
        public IActionResult MeuPerfil()
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var dentista = _context.Dentistas
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
                return RedirectToAction("DentistaLogin", "Auth");

            return View(dentista);
        }

        // ==========================================================
        // EDITAR MEU PERFIL (DENTISTA)
        // ==========================================================

        [HttpGet]
        public IActionResult EditarMeuPerfil()
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var dentista = _context.Dentistas
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
                return RedirectToAction("DentistaLogin", "Auth");

            // View Model para evitar expor a senha
            var viewModel = new EditarPerfilDentistaViewModel
            {
                Nome = dentista.Nome,
                Email = dentista.Email,
                Telefone = dentista.Telefone,
                Endereco = dentista.Endereco
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarMeuPerfil(EditarPerfilDentistaViewModel model)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var dentista = _context.Dentistas
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
                return RedirectToAction("DentistaLogin", "Auth");

            // Remove validação de senha se estiver vazia (para não exigir a alteração)
            if (string.IsNullOrEmpty(model.NovaSenha))
            {
                ModelState.Remove(nameof(model.NovaSenha));
                ModelState.Remove(nameof(model.ConfirmarSenha));
            }

            if (ModelState.IsValid)
            {
                dentista.Nome = model.Nome;
                dentista.Email = model.Email;
                dentista.Telefone = model.Telefone;
                dentista.Endereco = model.Endereco;

                // Se forneceu nova senha
                if (!string.IsNullOrEmpty(model.NovaSenha))
                {
                    dentista.Senha = PasswordHelper.HashPassword(model.NovaSenha);
                }

                _context.SaveChanges();

                TempData["MensagemSucesso"] = "Perfil atualizado com sucesso! Você precisará se logar novamente para ver as alterações.";
                return RedirectToAction("PerfilAtualizado");
            }

            return View(model);
        }

        // View de confirmação com redirecionamento automático
        [HttpGet]
        public IActionResult PerfilAtualizado()
        {
            if (TempData["MensagemSucesso"] == null)
            {
                return RedirectToAction("MeuPerfil");
            }

            ViewBag.Mensagem = TempData["MensagemSucesso"];
            return View();
        }

        // ==========================================================
        // MEUS ATENDIMENTOS (DENTISTA)
        // ==========================================================

        [HttpGet]
        public IActionResult MeusAtendimentos()
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var atendimentos = _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                .Where(a => a.IdDentista == dentistaId)
                .OrderByDescending(a => a.DataAtendimento)
                .ThenByDescending(a => a.HorarioAtendimento)
                .ToList();

            return View(atendimentos);
        }
    }

    // ==========================================================
    // VIEW MODEL PARA EDITAR PERFIL DO DENTISTA (Deve estar em Pi_Odonto.ViewModels)
    // ==========================================================
    // NOTA: Se esta classe estiver em um arquivo separado chamado EditarPerfilDentistaViewModel.cs 
    // na pasta ViewModels, remova-a daqui. Se não, mantenha-a.
    public class EditarPerfilDentistaViewModel
    {
        // Propriedades Nullable para resolver CS8618
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public string? NovaSenha { get; set; }
        public string? ConfirmarSenha { get; set; }
    }
}