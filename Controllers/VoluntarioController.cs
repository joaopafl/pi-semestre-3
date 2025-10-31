using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using System.Threading.Tasks;

namespace Pi_Odonto.Controllers
{
    public class VoluntarioController : Controller
    {
        private readonly AppDbContext _context;

        public VoluntarioController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Voluntario/Cadastro
        [HttpGet]
        public IActionResult Cadastro()
        {
            return View();
        }

        // POST: Voluntario/Cadastro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(SolicitacaoVoluntario voluntario)
        {
            if (ModelState.IsValid)
            {
                // Verificar se já existe CPF ou Email
                bool cpfExiste = await _context.SolicitacoesVoluntario
                    .AnyAsync(v => v.Cpf == voluntario.Cpf);

                bool emailExiste = await _context.SolicitacoesVoluntario
                    .AnyAsync(v => v.Email == voluntario.Email);

                if (cpfExiste)
                {
                    TempData["Erro"] = "Este CPF já possui uma solicitação cadastrada.";
                    return View(voluntario);
                }

                if (emailExiste)
                {
                    TempData["Erro"] = "Este email já possui uma solicitação cadastrada.";
                    return View(voluntario);
                }

                voluntario.DataSolicitacao = System.DateTime.Now;
                _context.SolicitacoesVoluntario.Add(voluntario);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Solicitação enviada com sucesso!";
                return RedirectToAction("Cadastro");
            }

            return View(voluntario);
        }

        // POST: Voluntario/ValidarCpf
        [HttpPost]
        public async Task<JsonResult> ValidarCpfVoluntario([FromBody] dynamic data)
        {
            string cpf = data.cpf;
            bool existe = await _context.SolicitacoesVoluntario.AnyAsync(v => v.Cpf == cpf);
            return Json(new { existe });
        }

        // POST: Voluntario/ValidarEmail
        [HttpPost]
        public async Task<JsonResult> ValidarEmail([FromBody] dynamic data)
        {
            string email = data.email;
            bool existe = await _context.SolicitacoesVoluntario.AnyAsync(v => v.Email == email);
            return Json(new { existe });
        }

        // POST: Voluntario/ValidarCro
        [HttpPost]
        public async Task<JsonResult> ValidarCro([FromBody] dynamic data)
        {
            string cro = data.cro;
            bool existe = await _context.SolicitacoesVoluntario.AnyAsync(v => v.Cro == cro);
            return Json(new { existe });
        }

        // GET: Voluntario/Listar
        // Apenas para admins (exemplo)
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            // Aqui você pode implementar uma verificação de admin
            var voluntarios = await _context.SolicitacoesVoluntario
                .OrderByDescending(v => v.DataSolicitacao)
                .ToListAsync();

            return View(voluntarios);
        }
    }
}
