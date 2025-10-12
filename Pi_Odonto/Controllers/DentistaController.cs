using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.Models;

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
            var dentistas = _context.Dentistas.ToList();
            return View(dentistas);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Dentista dentista)
        {
            if (ModelState.IsValid)
            {
                _context.Dentistas.Add(dentista);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(dentista);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dentista = _context.Dentistas.Find(id);
            if (dentista == null) return NotFound();

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

            return View(dentista);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var dentista = _context.Dentistas.Find(id);
            if (dentista == null) return NotFound();

            return View(dentista);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var dentista = _context.Dentistas.Find(id);
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
            var dentista = _context.Dentistas.Find(id);
            if (dentista == null) return NotFound();
            return View(dentista);
        }
    }
}