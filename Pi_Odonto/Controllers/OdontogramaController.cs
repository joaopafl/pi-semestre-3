using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using System.Security.Claims;

namespace Pi_Odonto.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth,DentistaAuth")] // Admin e Dentista podem acessar
    public class OdontogramaController : Controller
    {
        private readonly AppDbContext _context;

        public OdontogramaController(AppDbContext context) => _context = context;

        private bool PodeEditar()
        {
            return User.HasClaim("TipoUsuario", "Admin") ||
                   User.HasClaim("TipoUsuario", "Dentista");
        }

        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // 🔹 GET: Odontograma/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? criancaId)
        {
            // Dentistas e Admins veem todos os odontogramas
            IQueryable<Odontograma> query = _context.Odontogramas
                .Include(o => o.Crianca)
                    .ThenInclude(c => c.Responsavel)
                .Include(o => o.Tratamentos)
                .OrderByDescending(o => o.DataAtualizacao);

            if (criancaId.HasValue)
            {
                query = query.Where(o => o.IdCrianca == criancaId.Value);
            }

            var odontogramas = await query.ToListAsync();

            if (criancaId.HasValue && odontogramas.Count == 1)
            {
                return RedirectToAction("Visualizar", new { id = odontogramas[0].Id });
            }

            return View(odontogramas);
        }

        // 🔹 GET: Odontograma/Visualizar/5
        [HttpGet]
        public async Task<IActionResult> Visualizar(int id)
        {
            var odontograma = await _context.Odontogramas
                .Include(o => o.Crianca)
                    .ThenInclude(c => c.Responsavel)
                .Include(o => o.Tratamentos)
                    .ThenInclude(t => t.Dentista)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (odontograma == null)
            {
                TempData["ErrorMessage"] = "Odontograma não encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Crianca = odontograma.Crianca;
            ViewBag.IsAdmin = PodeEditar();
            ViewBag.Dentistas = await _context.Dentistas
                .Where(d => d.Ativo)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            return View(odontograma);
        }

        // 🔹 GET: Odontograma/PorCrianca/id
        [HttpGet]
        public async Task<IActionResult> PorCrianca(int id)
        {
            var odontograma = await _context.Odontogramas
                .Include(o => o.Crianca)
                    .ThenInclude(c => c.Responsavel)
                .Include(o => o.Tratamentos)
                    .ThenInclude(t => t.Dentista)
                .FirstOrDefaultAsync(o => o.IdCrianca == id);

            if (odontograma == null)
            {
                var crianca = await _context.Criancas
                    .Include(c => c.Responsavel)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (crianca == null)
                {
                    TempData["ErrorMessage"] = "Criança não encontrada.";
                    return RedirectToAction("Dashboard", "Dentista");
                }

                odontograma = new Odontograma
                {
                    IdCrianca = id,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now,
                    ObservacoesGerais = ""
                };

                _context.Odontogramas.Add(odontograma);
                await _context.SaveChangesAsync();

                odontograma = await _context.Odontogramas
                    .Include(o => o.Crianca)
                        .ThenInclude(c => c.Responsavel)
                    .Include(o => o.Tratamentos)
                        .ThenInclude(t => t.Dentista)
                    .FirstOrDefaultAsync(o => o.Id == odontograma.Id);
            }

            ViewBag.Crianca = odontograma.Crianca;
            ViewBag.IsAdmin = PodeEditar();
            ViewBag.Dentistas = await _context.Dentistas
                .Where(d => d.Ativo)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            return View("PorCrianca", odontograma);
        }

        // 🔹 GET: Tratamentos de um dente específico
        [HttpGet]
        public async Task<IActionResult> ObterTratamentosDente(int idOdontograma, int numeroDente)
        {
            try
            {
                var tratamentos = await _context.TratamentosDente
                    .Include(t => t.Dentista)
                    .Where(t => t.IdOdontograma == idOdontograma && t.NumeroDente == numeroDente)
                    .OrderByDescending(t => t.DataTratamento)
                    .Select(t => new
                    {
                        id = t.Id,
                        numeroDente = t.NumeroDente,
                        tipoTratamento = t.TipoTratamento,
                        face = t.Face,
                        status = t.Status,
                        observacao = t.Observacao,
                        dataTratamento = t.DataTratamento.HasValue ? t.DataTratamento.Value.ToString("dd/MM/yyyy") : null,
                        idDentista = t.IdDentista,
                        dentistaNome = t.Dentista != null ? t.Dentista.Nome : "Não especificado"
                    })
                    .ToListAsync();

                return Json(new { success = true, tratamentos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 🔹 POST: Adicionar tratamento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarTratamento([FromForm] TratamentoDente tratamento)
        {
            if (!PodeEditar())
                return Json(new { success = false, message = "Sem permissão." });

            try
            {
                _context.TratamentosDente.Add(tratamento);

                var odontograma = await _context.Odontogramas.FindAsync(tratamento.IdOdontograma);
                if (odontograma != null)
                    odontograma.DataAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tratamento adicionado com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao adicionar tratamento: " + ex.Message });
            }
        }

        // 🔹 POST: Editar tratamento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarTratamento(int id, [FromForm] TratamentoDente tratamento)
        {
            if (!PodeEditar())
                return Json(new { success = false, message = "Sem permissão." });

            try
            {
                var tratamentoExistente = await _context.TratamentosDente.FindAsync(id);
                if (tratamentoExistente == null)
                    return Json(new { success = false, message = "Tratamento não encontrado." });

                tratamentoExistente.TipoTratamento = tratamento.TipoTratamento;
                tratamentoExistente.Face = tratamento.Face;
                tratamentoExistente.Status = tratamento.Status;
                tratamentoExistente.DataTratamento = tratamento.DataTratamento;
                tratamentoExistente.IdDentista = tratamento.IdDentista;
                tratamentoExistente.Observacao = tratamento.Observacao;

                var odontograma = await _context.Odontogramas.FindAsync(tratamentoExistente.IdOdontograma);
                if (odontograma != null)
                    odontograma.DataAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tratamento atualizado com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao editar tratamento: " + ex.Message });
            }
        }

        // 🔹 POST: Excluir tratamento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirTratamento(int id)
        {
            if (!PodeEditar())
                return Json(new { success = false, message = "Sem permissão." });

            try
            {
                var tratamento = await _context.TratamentosDente.FindAsync(id);
                if (tratamento == null)
                    return Json(new { success = false, message = "Tratamento não encontrado." });

                var idOdontograma = tratamento.IdOdontograma;
                _context.TratamentosDente.Remove(tratamento);

                var odontograma = await _context.Odontogramas.FindAsync(idOdontograma);
                if (odontograma != null)
                    odontograma.DataAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tratamento excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao excluir tratamento: " + ex.Message });
            }
        }

        // 🔹 POST: Atualizar observações gerais
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarObservacoes(int id, string observacoes)
        {
            if (!PodeEditar())
                return Json(new { success = false, message = "Sem permissão." });

            try
            {
                var odontograma = await _context.Odontogramas.FindAsync(id);
                if (odontograma == null)
                    return Json(new { success = false, message = "Odontograma não encontrado." });

                odontograma.ObservacoesGerais = observacoes;
                odontograma.DataAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Observações salvas com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro ao salvar observações: " + ex.Message });
            }
        }

        // 🔹 GET: Odontograma/Imprimir
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            var odontograma = await _context.Odontogramas
                .Include(o => o.Crianca)
                    .ThenInclude(c => c.Responsavel)
                .Include(o => o.Tratamentos)
                    .ThenInclude(t => t.Dentista)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (odontograma == null)
                return NotFound();

            ViewBag.Crianca = odontograma.Crianca;
            ViewBag.IsAdmin = PodeEditar();

            return View(odontograma);
        }

        // 🔹 GET: Odontograma/Detalhes
        [HttpGet]
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var odontograma = await _context.Odontogramas
                .Include(o => o.Crianca)
                    .ThenInclude(c => c.Responsavel)
                .Include(o => o.Tratamentos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (odontograma == null) return NotFound();

            return View(odontograma);
        }

        private bool OdontogramaExists(int id)
        {
            return _context.Odontogramas.Any(e => e.Id == id);
        }
    }
}
