using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pi_Odonto.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminEscalaController : Controller
    {
        private readonly AppDbContext _context;

        public AdminEscalaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Escala/Calendario
        [HttpGet]
        public IActionResult Calendario(int? ano, int? mes)
        {
            try
            {
                var dataAtual = DateTime.Now;
                var anoSelecionado = ano ?? dataAtual.Year;
                var mesSelecionado = mes ?? dataAtual.Month;

                // Ajustar ano ao mudar de mês
                if (mesSelecionado < 1)
                {
                    mesSelecionado = 12;
                    anoSelecionado--;
                }
                else if (mesSelecionado > 12)
                {
                    mesSelecionado = 1;
                    anoSelecionado++;
                }

                var primeiroDia = new DateTime(anoSelecionado, mesSelecionado, 1);
                var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);

                // Buscar todas as escalas do mês (tratando erro caso a tabela não exista)
                List<EscalaMensalDentista> escalas = new List<EscalaMensalDentista>();
                try
                {
                    escalas = _context.EscalasMensaisDentista
                        .Include(e => e.Dentista)
                        .Where(e => e.DataEscala >= primeiroDia && e.DataEscala <= ultimoDia && e.Ativo)
                        .OrderBy(e => e.DataEscala)
                        .ThenBy(e => e.HoraInicio)
                        .ToList();
                }
                catch (Exception ex)
                {
                    // Se a tabela não existir, mostrar mensagem
                    TempData["ErrorMessage"] = "A tabela de escalas ainda não foi criada. Execute o script SQL create_escala_mensal_table.sql no banco de dados.";
                    escalas = new List<EscalaMensalDentista>();
                }

                // Agrupar por data e dentista
                var escalasPorData = escalas
                    .GroupBy(e => e.DataEscala.Date)
                    .ToDictionary(g => g.Key, g => g.GroupBy(e => e.IdDentista).ToDictionary(g2 => g2.Key, g2 => g2.ToList()));

                ViewBag.Ano = anoSelecionado;
                ViewBag.Mes = mesSelecionado;
                ViewBag.PrimeiroDia = primeiroDia;
                ViewBag.UltimoDia = ultimoDia;
                ViewBag.EscalasPorData = escalasPorData;
                ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao carregar calendário: {ex.Message}";
                ViewBag.Ano = DateTime.Now.Year;
                ViewBag.Mes = DateTime.Now.Month;
                ViewBag.PrimeiroDia = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                ViewBag.UltimoDia = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);
                ViewBag.EscalasPorData = new Dictionary<DateTime, Dictionary<int, List<EscalaMensalDentista>>>();
                ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                return View();
            }
        }

        // GET: Admin/Escala/Criar
        [HttpGet]
        public IActionResult Criar(DateTime? data)
        {
            ViewBag.Dentistas = _context.Dentistas
                .Where(d => d.Ativo)
                .OrderBy(d => d.Nome)
                .ToList();

            ViewBag.DataSelecionada = data ?? DateTime.Today;

            return View();
        }

        // POST: Admin/Escala/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Criar(EscalaMensalDentista escala)
        {
            if (ModelState.IsValid)
            {
                // NOVA VALIDAÇÃO: Não permitir criar escalas em datas passadas
                if (escala.DataEscala.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Não é possível criar escalas para datas anteriores ao dia atual.";
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    ViewBag.DataSelecionada = escala.DataEscala;
                    return View(escala);
                }

                // Validar que a hora fim é maior que a hora início
                if (escala.HoraFim <= escala.HoraInicio)
                {
                    ModelState.AddModelError("HoraFim", "A hora de fim deve ser maior que a hora de início.");
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    ViewBag.DataSelecionada = escala.DataEscala;
                    return View(escala);
                }

                // Validar que o bloco tem exatamente 1 hora
                var duracao = escala.HoraFim - escala.HoraInicio;
                if (duracao.TotalHours != 1)
                {
                    ModelState.AddModelError("HoraFim", "Cada bloco deve ter exatamente 1 hora de duração.");
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    ViewBag.DataSelecionada = escala.DataEscala;
                    return View(escala);
                }

                // NOVA VALIDAÇÃO: Verifica se já existe UMA escala para esta data (qualquer dentista)
                var escalaExistenteNaData = _context.EscalasMensaisDentista
                    .Any(e => e.DataEscala.Date == escala.DataEscala.Date &&
                              e.HoraInicio == escala.HoraInicio &&
                              e.Ativo);

                if (escalaExistenteNaData)
                {
                    TempData["ErrorMessage"] = "Já existe um horário alocado para este dia neste horário. Delete o horário existente antes de adicionar um novo.";
                    return RedirectToAction("Calendario", new { ano = escala.DataEscala.Year, mes = escala.DataEscala.Month });
                }

                // Verificar se já existe uma escala para o mesmo dentista, data e horário
                var existeEscala = _context.EscalasMensaisDentista
                    .Any(e => e.IdDentista == escala.IdDentista &&
                              e.DataEscala.Date == escala.DataEscala.Date &&
                              e.HoraInicio == escala.HoraInicio &&
                              e.Ativo);

                if (existeEscala)
                {
                    ModelState.AddModelError("", "Já existe uma escala para este dentista neste horário.");
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    ViewBag.DataSelecionada = escala.DataEscala;
                    return View(escala);
                }

                escala.DataCadastro = DateTime.Now;
                escala.Ativo = true;

                _context.EscalasMensaisDentista.Add(escala);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Escala criada com sucesso!";
                return RedirectToAction("Calendario", new { ano = escala.DataEscala.Year, mes = escala.DataEscala.Month });
            }

            ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
            ViewBag.DataSelecionada = escala.DataEscala;
            return View(escala);
        }

        // POST: Admin/Escala/CriarMultiplos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CriarMultiplos(int idDentista, DateTime dataEscala, string horarios)
        {
            if (idDentista <= 0 || string.IsNullOrWhiteSpace(horarios))
            {
                TempData["ErrorMessage"] = "Dados inválidos.";
                return RedirectToAction("Criar", new { data = dataEscala });
            }

            // Parse dos horários (formato: "08:00,09:00,10:00")
            var horariosList = horarios.Split(',')
                .Select(h => h.Trim())
                .Where(h => !string.IsNullOrEmpty(h))
                .ToList();

            var escalasCriadas = 0;
            var escalasDuplicadas = 0;
            var escalasBloquadas = 0;

            foreach (var horarioStr in horariosList)
            {
                if (TimeSpan.TryParse(horarioStr, out TimeSpan horaInicio))
                {
                    var horaFim = horaInicio.Add(TimeSpan.FromHours(1));

                    // NOVA VALIDAÇÃO: Verifica se já existe alguma escala neste horário
                    var existeEscalaNaData = _context.EscalasMensaisDentista
                        .Any(e => e.DataEscala.Date == dataEscala.Date &&
                                  e.HoraInicio == horaInicio &&
                                  e.Ativo);

                    if (existeEscalaNaData)
                    {
                        escalasBloquadas++;
                        continue;
                    }

                    // Verificar se já existe para este dentista específico
                    var existe = _context.EscalasMensaisDentista
                        .Any(e => e.IdDentista == idDentista &&
                                  e.DataEscala.Date == dataEscala.Date &&
                                  e.HoraInicio == horaInicio &&
                                  e.Ativo);

                    if (!existe)
                    {
                        var escala = new EscalaMensalDentista
                        {
                            IdDentista = idDentista,
                            DataEscala = dataEscala.Date,
                            HoraInicio = horaInicio,
                            HoraFim = horaFim,
                            Ativo = true,
                            DataCadastro = DateTime.Now
                        };

                        _context.EscalasMensaisDentista.Add(escala);
                        escalasCriadas++;
                    }
                    else
                    {
                        escalasDuplicadas++;
                    }
                }
            }

            _context.SaveChanges();

            if (escalasCriadas > 0)
            {
                TempData["SuccessMessage"] = $"{escalasCriadas} escala(s) criada(s) com sucesso!";
            }
            if (escalasDuplicadas > 0)
            {
                TempData["WarningMessage"] = $"{escalasDuplicadas} escala(s) já existiam e foram ignoradas.";
            }
            if (escalasBloquadas > 0)
            {
                TempData["ErrorMessage"] = $"{escalasBloquadas} horário(s) já estavam alocados e foram ignorados.";
            }

            return RedirectToAction("Calendario", new { ano = dataEscala.Year, mes = dataEscala.Month });
        }

        // GET: Admin/Escala/Editar/{id}
        [HttpGet]
        public IActionResult Editar(int id)
        {
            var escala = _context.EscalasMensaisDentista
                .Include(e => e.Dentista)
                .FirstOrDefault(e => e.Id == id);

            if (escala == null)
            {
                return NotFound();
            }

            ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
            return View(escala);
        }

        // POST: Admin/Escala/Editar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(int id, EscalaMensalDentista escala)
        {
            if (id != escala.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (escala.HoraFim <= escala.HoraInicio)
                {
                    ModelState.AddModelError("HoraFim", "A hora de fim deve ser maior que a hora de início.");
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    return View(escala);
                }

                var duracao = escala.HoraFim - escala.HoraInicio;
                if (duracao.TotalHours != 1)
                {
                    ModelState.AddModelError("HoraFim", "Cada bloco deve ter exatamente 1 hora de duração.");
                    ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
                    return View(escala);
                }

                // NOVA VALIDAÇÃO: Verifica se existe outra escala para esta data e horário (excluindo a atual)
                var escalaExistenteNaData = _context.EscalasMensaisDentista
                    .Any(e => e.DataEscala.Date == escala.DataEscala.Date &&
                              e.HoraInicio == escala.HoraInicio &&
                              e.Id != id &&
                              e.Ativo);

                if (escalaExistenteNaData)
                {
                    TempData["ErrorMessage"] = "Já existe outro horário alocado para este dia e horário. Delete o horário existente antes de modificar.";
                    return RedirectToAction("Calendario", new { ano = escala.DataEscala.Year, mes = escala.DataEscala.Month });
                }

                try
                {
                    _context.Update(escala);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Escala atualizada com sucesso!";
                    return RedirectToAction("Calendario", new { ano = escala.DataEscala.Year, mes = escala.DataEscala.Month });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EscalaExists(escala.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.Dentistas = _context.Dentistas.Where(d => d.Ativo).OrderBy(d => d.Nome).ToList();
            return View(escala);
        }

        // POST: Admin/Escala/Excluir/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            var escala = _context.EscalasMensaisDentista.Find(id);
            if (escala == null)
            {
                TempData["ErrorMessage"] = "Escala não encontrada.";
                return RedirectToAction("Calendario");
            }

            var ano = escala.DataEscala.Year;
            var mes = escala.DataEscala.Month;

            try
            {
                _context.EscalasMensaisDentista.Remove(escala);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Escala excluída com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erro ao excluir escala: {ex.Message}";
            }

            return RedirectToAction("Calendario", new { ano, mes });
        }

        private bool EscalaExists(int id)
        {
            return _context.EscalasMensaisDentista.Any(e => e.Id == id);
        }
    }
}