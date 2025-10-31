using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;
using Pi_Odonto.Helpers;
using System;
using System.Linq;
using System.Collections.Generic;

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

        private bool IsAdmin() => User.HasClaim("TipoUsuario", "Admin");

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
            if (!IsDentista())
                return RedirectToAction("DentistaLogin", "Auth");

            var dentistaId = GetCurrentDentistaId();

            var dentista = _context.Dentistas
                .Include(d => d.Disponibilidades)
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == dentistaId);

            if (dentista == null)
                return RedirectToAction("DentistaLogin", "Auth");

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
                            a.DataAgendamento >= DateTime.Today)
                .OrderBy(a => a.DataAgendamento)
                .ThenBy(a => a.HoraAgendamento)
                .Take(5)
                .ToList();

            return View(dentista);
        }

        // ==========================================================
        // ÁREA ADMINISTRATIVA (Apenas Admin)
        // ==========================================================

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado. Apenas administradores podem gerenciar dentistas.";
                return RedirectToAction("Index", "Home");
            }

            var dentistas = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .Include(d => d.Disponibilidades)
                .ToList();

            return View(dentistas);
        }

        // ==========================================================
        // CADASTRO DE DENTISTAS (Admin)
        // ==========================================================

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado. Apenas administradores podem cadastrar dentistas.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();

            var viewModel = new DentistaViewModel
            {
                Disponibilidades = ObterDisponibilidadesPadrao()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DentistaViewModel viewModel, int? IdEscala)
        {
            if (!IsAdmin())
            {
                TempData["Erro"] = "Acesso negado.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Escalas = _context.EscalaTrabalho.ToList();
                viewModel.Disponibilidades = ObterDisponibilidadesPadrao();
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

            // Vincular disponibilidades selecionadas
            foreach (var disp in viewModel.Disponibilidades.Where(d => d.Selecionado))
            {
                _context.DisponibilidadesDentista.Add(new DisponibilidadeDentista
                {
                    IdDentista = dentista.Id,
                    DiaSemana = disp.DiaSemana,
                    HoraInicio = disp.HoraInicio,
                    HoraFim = disp.HoraFim
                });
            }

            _context.SaveChanges();

            TempData["Sucesso"] = $"Dentista cadastrado com sucesso! Senha inicial: {viewModel.Cro}123";
            return RedirectToAction("Index");
        }

        // ==========================================================
        // MÉTODOS AUXILIARES
        // ==========================================================

        private List<DisponibilidadeItem> ObterDisponibilidadesPadrao()
        {
            var diasSemana = new[] { "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado" };
            var horarios = new[] {
                (TimeSpan.FromHours(8), TimeSpan.FromHours(12)),
                (TimeSpan.FromHours(14), TimeSpan.FromHours(18))
            };

            var lista = new List<DisponibilidadeItem>();
            foreach (var dia in diasSemana)
            {
                foreach (var (inicio, fim) in horarios)
                {
                    lista.Add(new DisponibilidadeItem
                    {
                        DiaSemana = dia,
                        HoraInicio = inicio,
                        HoraFim = fim,
                        Selecionado = false
                    });
                }
            }
            return lista;
        }

        private List<DisponibilidadeItem> ObterDisponibilidadesComSelecoes(ICollection<DisponibilidadeDentista> existentes)
        {
            var todas = ObterDisponibilidadesPadrao();
            foreach (var item in todas)
            {
                item.Selecionado = existentes.Any(d =>
                    d.DiaSemana == item.DiaSemana &&
                    d.HoraInicio == item.HoraInicio &&
                    d.HoraFim == item.HoraFim);
            }
            return todas;
        }
    }
}
