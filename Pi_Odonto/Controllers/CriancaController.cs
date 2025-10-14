using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;

namespace Pi_Odonto.Controllers
{
    public class CriancaController : Controller
    {
        private readonly AppDbContext _context;

        public CriancaController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var criancas = _context.Criancas
                .Include(c => c.Responsavel)
                .ToList();
            return View(criancas);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Responsaveis = new SelectList(_context.Responsaveis.ToList(), "Id", "Nome");
            ViewBag.OpcoesParentesco = new SelectList(new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            });
            return View();
        }

        [HttpPost]
        public IActionResult Create(Crianca crianca)
        {
            if (ModelState.IsValid)
            {
                _context.Criancas.Add(crianca);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Responsaveis = new SelectList(_context.Responsaveis.ToList(), "Id", "Nome");
            ViewBag.OpcoesParentesco = new SelectList(new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            });
            return View(crianca);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var crianca = _context.Criancas.Find(id);
            if (crianca == null) return NotFound();

            ViewBag.Responsaveis = new SelectList(_context.Responsaveis.ToList(), "Id", "Nome", crianca.IdResponsavel);
            ViewBag.OpcoesParentesco = new SelectList(new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            }, crianca.Parentesco);

            return View(crianca);
        }

        [HttpPost]
        public IActionResult Edit(Crianca crianca)
        {
            if (ModelState.IsValid)
            {
                _context.Criancas.Update(crianca);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Responsaveis = new SelectList(_context.Responsaveis.ToList(), "Id", "Nome", crianca.IdResponsavel);
            ViewBag.OpcoesParentesco = new SelectList(new List<string>
            {
                "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
            }, crianca.Parentesco);

            return View(crianca);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var crianca = _context.Criancas
                .Include(c => c.Responsavel)
                .FirstOrDefault(c => c.Id == id);

            if (crianca == null) return NotFound();

            // Verifica se é a única criança do responsável
            var qtdCriancas = _context.Criancas
                .Count(c => c.IdResponsavel == crianca.IdResponsavel);

            if (qtdCriancas <= 1)
            {
                TempData["Erro"] = "Não é possível excluir a criança. Todo responsável deve ter pelo menos uma criança cadastrada.";
                return RedirectToAction("Index");
            }

            return View(crianca);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var crianca = _context.Criancas.Find(id);
            if (crianca != null)
            {
                // Verifica novamente se é a única criança
                var qtdCriancas = _context.Criancas
                    .Count(c => c.IdResponsavel == crianca.IdResponsavel);

                if (qtdCriancas > 1)
                {
                    _context.Criancas.Remove(crianca);
                    _context.SaveChanges();
                }
                else
                {
                    TempData["Erro"] = "Não é possível excluir a criança. Todo responsável deve ter pelo menos uma criança cadastrada.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var crianca = _context.Criancas
                .Include(c => c.Responsavel)
                .FirstOrDefault(c => c.Id == id);

            if (crianca == null) return NotFound();
            return View(crianca);
        }
    }
}