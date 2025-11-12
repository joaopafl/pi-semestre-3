using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Globalization; // Necessário para a cultura

namespace Pi_Odonto.Controllers
{
    // A rota base para todas as ações desta controller
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Escala")]
    public class AdminEscalaController : Controller
    {
        private readonly AppDbContext _context;

        public AdminEscalaController(AppDbContext context)
        {
            _context = context;
        }

        // Método de suporte para verificar se é Admin
        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // ==========================================================
        // CALENDÁRIO GERAL DE ESCALAS (CORRIGIDO ERRO DE VIEW)
        // ==========================================================
        
        // GET: Admin/Escala/Calendario
        [HttpGet("Calendario")]
        public async Task<IActionResult> Calendario(DateTime? data)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dataReferencia = data ?? DateTime.Today;
            
            // Calcula o primeiro dia do mês e o último dia do mês
            var primeiroDiaDoMes = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
            var ultimoDiaDoMes = primeiroDiaDoMes.AddMonths(1).AddDays(-1);

            // 1. Popula as ViewBags para Navegação e Título (CORREÇÃO DE BINDING)
            ViewBag.Ano = dataReferencia.Year;
            ViewBag.Mes = dataReferencia.Month;
            ViewBag.PrimeiroDia = primeiroDiaDoMes;
            ViewBag.UltimoDia = ultimoDiaDoMes;

            // 2. Busca todos os dentistas ativos para a legenda e dicionário
            var dentistas = await _context.Dentistas
                .Where(d => d.Ativo)
                .OrderBy(d => d.Nome)
                .ToListAsync();
            ViewBag.Dentistas = dentistas;

            // 3. Busca as escalas
            var escalasNoMes = await _context.EscalasMensaisDentista
                .Include(e => e.Dentista)
                .Where(e => e.DataEscala.Year == dataReferencia.Year &&
                            e.DataEscala.Month == dataReferencia.Month)
                .OrderBy(e => e.DataEscala)
                .ThenBy(e => e.HoraInicio)
                .ToListAsync();

            // 4. Cria o dicionário de escalas agrupadas (Data -> DentistaId -> Lista de Escalas)
            // Isso resolve o erro de conversão de tipos na View (RuntimeBinderException)
            var escalasPorData = escalasNoMes
                .GroupBy(e => e.DataEscala.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(e => e.IdDentista)
                          .ToDictionary(d => d.Key, d => d.ToList())
                );
            ViewBag.EscalasPorData = escalasPorData;

            return View(escalasNoMes);
        }

        // ==========================================================
        // CRIAR ESCALA (Bloco Múltiplo - Checkboxes)
        // ==========================================================

        // GET: Admin/Escala/Criar
        [HttpGet("Criar")]
        public IActionResult Criar(DateTime? data)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // Popula a lista de dentistas para o dropdown
            ViewBag.Dentistas = _context.Dentistas
                                    .Where(d => d.Ativo)
                                    .OrderBy(d => d.Nome)
                                    .ToList();

            if (data.HasValue)
            {
                ViewBag.DataSelecionada = data.Value;
            }

            return View();
        }

        // POST: Admin/Escala/CriarMultiplos (Ação refatorada com List<string>)
        [HttpPost("CriarMultiplos")]
        [ValidateAntiForgeryToken]
        public IActionResult CriarMultiplos(
            int idDentista, 
            DateTime dataEscala, 
            [FromForm(Name = "horariosSelecionados")] List<string> horariosSelecionados) // Mapeia o array de checkboxes
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "Auth");

            if (idDentista == 0 || dataEscala == DateTime.MinValue || horariosSelecionados == null || !horariosSelecionados.Any())
            {
                TempData["Erro"] = "Por favor, selecione um dentista, uma data e pelo menos um horário.";
                return RedirectToAction("Criar", new { data = dataEscala }); 
            }

            var novasEscalas = new List<EscalaMensalDentista>();
            int escalasCriadas = 0;
            bool houveDuplicidade = false;

            foreach (var horarioStr in horariosSelecionados)
            {
                if (TimeSpan.TryParse(horarioStr, out TimeSpan horaInicio))
                {
                    TimeSpan horaFim = horaInicio.Add(TimeSpan.FromHours(1));

                    bool jaExiste = _context.EscalasMensaisDentista.Any(e => 
                        e.IdDentista == idDentista &&
                        e.DataEscala.Date == dataEscala.Date &&
                        e.HoraInicio == horaInicio);

                    if (jaExiste)
                    {
                        houveDuplicidade = true;
                        continue; 
                    }

                    novasEscalas.Add(new EscalaMensalDentista
                    {
                        IdDentista = idDentista,
                        DataEscala = dataEscala,
                        HoraInicio = horaInicio,
                        HoraFim = horaFim,
                        Ativo = true,
                        DataCadastro = DateTime.Now
                    });
                    escalasCriadas++;
                }
            }

            if (novasEscalas.Any())
            {
                _context.EscalasMensaisDentista.AddRange(novasEscalas);
                _context.SaveChanges();
                
                string mensagemSucesso = $"{escalasCriadas} blocos de escala criados com sucesso para {dataEscala:dd/MM/yyyy}!";
                if(houveDuplicidade)
                {
                    mensagemSucesso += " (Alguns horários já existiam e foram ignorados).";
                }
                TempData["Sucesso"] = mensagemSucesso;
            }
            else if (houveDuplicidade)
            {
                 TempData["Erro"] = "Nenhum novo bloco de escala foi criado, pois todos os horários selecionados já existiam.";
            }
            else
            {
                 TempData["Erro"] = "Nenhum bloco de escala foi criado. Verifique os dados.";
            }

            return RedirectToAction("Calendario", new { data = dataEscala.ToString("yyyy-MM-dd") });
        }


        // ==========================================================
        // EDITAR ESCALA (Bloco Único - Dropdown)
        // ==========================================================

        // GET: Admin/Escala/Editar/5
        [HttpGet("Editar/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var escala = await _context.EscalasMensaisDentista
                .FirstOrDefaultAsync(e => e.Id == id);

            if (escala == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("Calendario");
            }

            ViewBag.Dentistas = await _context.Dentistas
                .Where(d => d.Ativo || d.Id == escala.IdDentista) 
                .OrderBy(d => d.Nome)
                .ToListAsync();

            return View(escala);
        }

        // POST: Admin/Escala/Editar/5
        [HttpPost("Editar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EscalaMensalDentista model)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "Auth");

            if (id != model.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                var escalaExistente = await _context.EscalasMensaisDentista
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (escalaExistente == null)
                {
                    TempData["Erro"] = "Escala não encontrada.";
                    return RedirectToAction("Calendario");
                }

                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] = $"Escala para {model.DataEscala:dd/MM/yyyy} atualizada com sucesso!";
                    return RedirectToAction("Calendario", new { data = model.DataEscala.ToString("yyyy-MM-dd") });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.EscalasMensaisDentista.Any(e => e.Id == model.Id))
                    {
                        TempData["Erro"] = "Escala não encontrada (concorrência).";
                        return RedirectToAction("Calendario");
                    }
                    throw;
                }
            }

            ViewBag.Dentistas = await _context.Dentistas.Where(d => d.Ativo || d.Id == model.IdDentista).OrderBy(d => d.Nome).ToListAsync();
            return View(model);
        }

        // ==========================================================
        // DELETAR ESCALA
        // ==========================================================

        // POST: Admin/Escala/Deletar/5
        [HttpPost("Deletar/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deletar(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var escala = await _context.EscalasMensaisDentista.FindAsync(id);

            if (escala == null)
            {
                TempData["Erro"] = "Escala não encontrada.";
                return RedirectToAction("Calendario");
            }

            var dataReferencia = escala.DataEscala;

            // Verifica se há agendamentos futuros neste bloco para impedir a exclusão
            var temAgendamento = await _context.Agendamentos.AnyAsync(a =>
                a.IdDentista == escala.IdDentista &&
                a.DataAgendamento.Date == escala.DataEscala.Date &&
                a.HoraAgendamento == escala.HoraInicio);
            
            if(temAgendamento)
            {
                TempData["Erro"] = "Esta escala não pode ser excluída, pois possui agendamentos futuros vinculados.";
                return RedirectToAction("Calendario", new { data = dataReferencia.ToString("yyyy-MM-dd") });
            }

            _context.EscalasMensaisDentista.Remove(escala);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Escala removida com sucesso!";
            return RedirectToAction("Calendario", new { data = dataReferencia.ToString("yyyy-MM-dd") });
        }
    }
}