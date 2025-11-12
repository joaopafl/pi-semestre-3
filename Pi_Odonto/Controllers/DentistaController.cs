using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Helpers;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pi_Odonto.Controllers
{
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
                Console.WriteLine("DentistaId não encontrado nos claims");
                return RedirectToAction("DentistaLogin", "Auth");
            }

            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
            {
                Console.WriteLine($"Dentista com ID {dentistaId} não encontrado no banco");
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
        // ESCALA DE TRABALHO DO DENTISTA (REFATORADO PARA ESCALA MENSAL)
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> EscalaTrabalho()
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            // Puxa o dentista com suas escalas mensais
            var escalas = await _context.EscalasMensaisDentista
                .Where(e => e.IdDentista == dentistaId)
                .OrderByDescending(e => e.DataEscala)
                .ThenBy(e => e.HoraInicio)
                .ToListAsync();

            ViewBag.Dentista = await _context.Dentistas.FindAsync(dentistaId);
            
            return View(escalas);
        }

        // ==========================================================
        // CRIAR DISPONIBILIDADE (REFATORADO PARA ESCALA MENSAL)
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

            if (ModelState.IsValid)
            {
                _context.EscalasMensaisDentista.Add(model);
                _context.SaveChanges();

                TempData["Sucesso"] = $"Escala cadastrada para {model.DataEscala:dd/MM/yyyy} com sucesso!";
                return RedirectToAction("EscalaTrabalho");
            }

            return View(model);
        }

        // ==========================================================
        // EDITAR DISPONIBILIDADE (REFATORADO PARA ESCALA MENSAL)
        // ==========================================================

        [HttpGet]
        public IActionResult EditEscala(int id)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            // CORRIGIDO CS1061: Usa 'Id'
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

            // CORRIGIDO CS1061: Usa 'Id'
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
                return RedirectToAction("EscalaTrabalho");
            }

            return View(model);
        }

        // ==========================================================
        // DELETAR DISPONIBILIDADE (REFATORADO PARA ESCALA MENSAL)
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEscala(int id)
        {
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            // CORRIGIDO CS1061: Usa 'Id'
            var escala = _context.EscalasMensaisDentista
                .FirstOrDefault(e => e.Id == id && e.IdDentista == dentistaId);

            if (escala == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("EscalaTrabalho");
            }

            _context.EscalasMensaisDentista.Remove(escala);
            _context.SaveChanges();

            TempData["Sucesso"] = "Escala removida com sucesso!";
            return RedirectToAction("EscalaTrabalho");
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
                .Include(d => d.EscalaTrabalho)
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

            // Remove validação de senha se estiver vazia
            if (string.IsNullOrEmpty(model.NovaSenha))
            {
                ModelState.Remove(nameof(model.NovaSenha));
                ModelState.Remove(nameof(model.ConfirmarSenha));
            }

            if (ModelState.IsValid)
            {
                // Atualizar apenas os campos permitidos
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

                TempData["MensagemSucesso"] = "Perfil atualizado com sucesso!";
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
    // VIEW MODEL PARA EDITAR PERFIL DO DENTISTA (CORRIGIDO CS8618)
    // ==========================================================
    public class EditarPerfilDentistaViewModel
    {
        // Adicionando '?' para resolver os avisos CS8618
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public string? NovaSenha { get; set; }
        public string? ConfirmarSenha { get; set; }
    }
}