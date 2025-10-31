using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Pi_Odonto.Controllers
{
    [Authorize]
    public class OdontogramaController : Controller
    {
        private readonly AppDbContext _context;

        // ✅ Construtor com corpo de expressão (forma moderna e limpa)
        public OdontogramaController(AppDbContext context) => _context = context;

        // 🔹 Método auxiliar para verificar se o usuário é administrador
        private bool IsAdmin()
        {
            return User.HasClaim(c => c.Type == "Role" && c.Value == "Admin");
        }

        // 🔹 GET: Odontograma
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return Forbid();

            var odontogramas = await _context.Odontogramas
                .Include(o => o.Crianca)
                .ToListAsync();

            return View(odontogramas);
        }

        // 🔹 GET: Odontograma/Detalhes/5
        [HttpGet]
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
                return NotFound();

            var odontograma = await _context.Odontogramas
                .Include(o => o.Crianca)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (odontograma == null)
                return NotFound();

            return View(odontograma);
        }

        // 🔹 GET: Odontograma/Criar
        [HttpGet]
        public IActionResult Criar()
        {
            return View();
        }

        // 🔹 POST: Odontograma/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Odontograma odontograma)
        {
            if (ModelState.IsValid)
            {
                _context.Add(odontograma);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(odontograma);
        }

        // 🔹 GET: Odontograma/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
                return NotFound();

            var odontograma = await _context.Odontogramas.FindAsync(id);
            if (odontograma == null)
                return NotFound();

            return View(odontograma);
        }

        // 🔹 POST: Odontograma/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Odontograma odontograma)
        {
            if (id != odontograma.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(odontograma);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OdontogramaExists(odontograma.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(odontograma);
        }

        // 🔹 GET: Odontograma/Excluir/5
        [HttpGet]
        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
                return NotFound();

            var odontograma = await _context.Odontogramas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (odontograma == null)
                return NotFound();

            return View(odontograma);
        }

        // 🔹 POST: Odontograma/Excluir/5
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var odontograma = await _context.Odontogramas.FindAsync(id);
            if (odontograma != null)
            {
                _context.Odontogramas.Remove(odontograma);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 🔹 Método auxiliar
        private bool OdontogramaExists(int id)
        {
            return _context.Odontogramas.Any(e => e.Id == id);
        }
    }
}
