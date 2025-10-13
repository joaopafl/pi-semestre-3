using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    public class DentistaController : Controller
    {
        private readonly AppDbContext _context;

        public DentistaController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var dentistas = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .ToList();
            return View(dentistas);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            
            var viewModel = new DentistaViewModel();
            viewModel.Disponibilidades = ObterDisponibilidadesPadrao();
            
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Create(DentistaViewModel viewModel, int? IdEscala)
        {
            if (ModelState.IsValid)
            {
                // Criar o dentista
                var dentista = new Dentista
                {
                    Nome = viewModel.Nome,
                    Cpf = viewModel.Cpf,
                    Cro = viewModel.Cro,
                    Endereco = viewModel.Endereco,
                    Email = viewModel.Email,
                    Telefone = viewModel.Telefone,
                    IdEscala = IdEscala
                };

                _context.Dentistas.Add(dentista);
                _context.SaveChanges();

                // Adicionar as disponibilidades selecionadas
                foreach (var disponibilidade in viewModel.Disponibilidades.Where(d => d.Selecionado))
                {
                    var novaDisponibilidade = new DisponibilidadeDentista
                    {
                        IdDentista = dentista.Id,
                        DiaSemana = disponibilidade.DiaSemana,
                        HoraInicio = disponibilidade.HoraInicio,
                        HoraFim = disponibilidade.HoraFim
                    };

                    _context.DisponibilidadesDentista.Add(novaDisponibilidade);
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            viewModel.Disponibilidades = ObterDisponibilidadesPadrao();
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == id);
            if (dentista == null) return NotFound();

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            return View(dentista);
        }

        [HttpPost]
        public IActionResult Edit(Dentista dentista)
        {
            if (ModelState.IsValid)
            {
                _context.Dentistas.Update(dentista);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Escalas = _context.EscalaTrabalho.ToList();
            return View(dentista);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .FirstOrDefault(d => d.Id == id);
            if (dentista == null) return NotFound();

            return View(dentista);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var dentista = _context.Dentistas.FirstOrDefault(d => d.Id == id);
            if (dentista != null)
            {
                _context.Dentistas.Remove(dentista);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var dentista = _context.Dentistas
                .Include(d => d.EscalaTrabalho)
                .Include(d => d.Disponibilidades)
                .FirstOrDefault(d => d.Id == id);
            if (dentista == null) return NotFound();
            return View(dentista);
        }

        // Métodos para gerenciar Escala de Trabalho
        [HttpGet]
        public IActionResult EscalaTrabalho()
        {
            var escalas = _context.EscalaTrabalho.ToList();
            return View(escalas);
        }

        [HttpGet]
        public IActionResult CreateEscala()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateEscala(EscalaTrabalho escala)
        {
            if (ModelState.IsValid)
            {
                _context.EscalaTrabalho.Add(escala);
                _context.SaveChanges();
                return RedirectToAction("EscalaTrabalho");
            }

            return View(escala);
        }

        [HttpGet]
        public IActionResult EditEscala(int id)
        {
            var escala = _context.EscalaTrabalho.FirstOrDefault(e => e.IdEscala == id);
            if (escala == null) return NotFound();

            return View(escala);
        }

        [HttpPost]
        public IActionResult EditEscala(EscalaTrabalho escala)
        {
            if (ModelState.IsValid)
            {
                _context.EscalaTrabalho.Update(escala);
                _context.SaveChanges();
                return RedirectToAction("EscalaTrabalho");
            }

            return View(escala);
        }

        [HttpGet]
        public IActionResult DeleteEscala(int id)
        {
            var escala = _context.EscalaTrabalho.FirstOrDefault(e => e.IdEscala == id);
            if (escala == null) return NotFound();

            return View(escala);
        }

        [HttpPost]
        public IActionResult DeleteEscalaConfirmed(int id)
        {
            var escala = _context.EscalaTrabalho.FirstOrDefault(e => e.IdEscala == id);
            if (escala != null)
            {
                _context.EscalaTrabalho.Remove(escala);
                _context.SaveChanges();
            }

            return RedirectToAction("EscalaTrabalho");
        }

        private List<DisponibilidadeItem> ObterDisponibilidadesPadrao()
        {
            var diasSemana = new[] { "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado" };
            var horarios = new[]
            {
                new { Inicio = new TimeSpan(8, 0, 0), Fim = new TimeSpan(12, 0, 0), Descricao = "Manhã (08:00 - 12:00)" },
                new { Inicio = new TimeSpan(13, 0, 0), Fim = new TimeSpan(17, 0, 0), Descricao = "Tarde (13:00 - 17:00)" },
                new { Inicio = new TimeSpan(18, 0, 0), Fim = new TimeSpan(22, 0, 0), Descricao = "Noite (18:00 - 22:00)" }
            };

            var disponibilidades = new List<DisponibilidadeItem>();

            foreach (var dia in diasSemana)
            {
                foreach (var horario in horarios)
                {
                    disponibilidades.Add(new DisponibilidadeItem
                    {
                        DiaSemana = dia,
                        HoraInicio = horario.Inicio,
                        HoraFim = horario.Fim,
                        Selecionado = false
                    });
                }
            }

            return disponibilidades;
        }
    }
}