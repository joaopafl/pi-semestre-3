using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    public class AtendimentoController : Controller
    {
        private readonly AppDbContext _context;

        public AtendimentoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Atendimento
        public async Task<IActionResult> Index()
        {
            var atendimentos = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                //.Include(a => a.Agenda)
                //.Include(a => a.Odontograma)
                .OrderByDescending(a => a.DataAtendimento)
                .ToListAsync();

            return View(atendimentos);
        }

        // GET: Atendimento/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                // .Include(a => a.Agenda) // Comentado temporariamente
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null)
            {
                return NotFound();
            }

            return View(atendimento);
        }

        // GET: Atendimento/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new AtendimentoViewModel
            {
                DataAtendimento = DateTime.Now,
                HorarioAtendimento = TimeSpan.FromHours(9), // Horário padrão 09:00
                DuracaoAtendimento = 30, // 30 minutos padrão
                CriancasDisponiveis = await _context.Criancas.ToListAsync(),
                DentistasDisponiveis = await _context.Dentistas.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Atendimento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AtendimentoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var atendimento = new Atendimento
                {
                    DataAtendimento = viewModel.DataAtendimento,
                    HorarioAtendimento = viewModel.HorarioAtendimento,
                    DuracaoAtendimento = viewModel.DuracaoAtendimento,
                    Observacao = viewModel.Observacao,
                    IdCrianca = viewModel.IdCrianca,
                    IdDentista = viewModel.IdDentista,
                    IdAgenda = viewModel.IdAgenda,
                    IdOdontograma = viewModel.IdOdontograma
                };

                _context.Add(atendimento);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registro do atendimento salvo!";

                return RedirectToAction(nameof(Index));
            }

            // Recarregar dados para dropdowns em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.ToListAsync();
            viewModel.DentistasDisponiveis = await _context.Dentistas.ToListAsync();

            return View(viewModel);
        }

        // GET: Atendimento/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento == null)
            {
                return NotFound();
            }

            var viewModel = new AtendimentoViewModel
            {
                Id = atendimento.Id,
                DataAtendimento = atendimento.DataAtendimento,
                HorarioAtendimento = atendimento.HorarioAtendimento,
                DuracaoAtendimento = atendimento.DuracaoAtendimento,
                Observacao = atendimento.Observacao,
                IdCrianca = atendimento.IdCrianca,
                IdDentista = atendimento.IdDentista,
                IdAgenda = atendimento.IdAgenda,
                IdOdontograma = atendimento.IdOdontograma,
                CriancasDisponiveis = await _context.Criancas.ToListAsync(),
                DentistasDisponiveis = await _context.Dentistas.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Atendimento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AtendimentoViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var atendimento = await _context.Atendimentos.FindAsync(id);
                    if (atendimento == null)
                    {
                        return NotFound();
                    }

                    atendimento.DataAtendimento = viewModel.DataAtendimento;
                    atendimento.HorarioAtendimento = viewModel.HorarioAtendimento;
                    atendimento.DuracaoAtendimento = viewModel.DuracaoAtendimento;
                    atendimento.Observacao = viewModel.Observacao;
                    atendimento.IdCrianca = viewModel.IdCrianca;
                    atendimento.IdDentista = viewModel.IdDentista;
                    atendimento.IdAgenda = viewModel.IdAgenda;
                    atendimento.IdOdontograma = viewModel.IdOdontograma;

                    _context.Update(atendimento);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registro do atendimento salvo!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AtendimentoExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Recarregar dados para dropdowns em caso de erro
            viewModel.CriancasDisponiveis = await _context.Criancas.ToListAsync();
            viewModel.DentistasDisponiveis = await _context.Dentistas.ToListAsync();

            return View(viewModel);
        }

        // GET: Atendimento/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var atendimento = await _context.Atendimentos
                .Include(a => a.Crianca)
                .Include(a => a.Dentista)
                //.Include(a => a.Agenda)
                //.Include(a => a.Odontograma)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (atendimento == null)
            {
                return NotFound();
            }

            return View(atendimento);
        }

        // POST: Atendimento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var atendimento = await _context.Atendimentos.FindAsync(id);
            if (atendimento != null)
            {
                _context.Atendimentos.Remove(atendimento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Atendimento excluído com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AtendimentoExists(int id)
        {
            return _context.Atendimentos.Any(e => e.Id == id);
        }
    }
}