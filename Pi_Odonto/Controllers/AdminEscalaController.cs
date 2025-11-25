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
using System.Globalization;
using System.Text.Json;

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
        // CALENDÁRIO GERAL DE ESCALAS
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

            // 1. Popula as ViewBags para Navegação e Título
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
        public async Task<IActionResult> Criar(DateTime? data)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var dataSelecionada = data ?? DateTime.Today;

            // Popula a lista de dentistas para o dropdown - apenas ativos e contratados
            ViewBag.Dentistas = await _context.Dentistas
                                         .Where(d => d.Ativo && d.Situacao == "contratado")
                                         .OrderBy(d => d.Nome)
                                         .ToListAsync();

            ViewBag.DataSelecionada = dataSelecionada;

            // Busca as escalas existentes para a data selecionada (TODOS os dentistas)
            var horariosOcupados = await _context.EscalasMensaisDentista
                .Where(e => e.DataEscala.Date == dataSelecionada.Date)
                .Select(e => e.HoraInicio.ToString(@"hh\:mm")) // Seleciona apenas o horário
                .Distinct() // Garante que não haja repetição de horário na lista
                .ToListAsync();

            // Serializa apenas a lista de horários ocupados
            ViewBag.HorariosOcupadosJson = JsonSerializer.Serialize(horariosOcupados);

            return View();
        }

        // POST: Admin/Escala/CriarMultiplos
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
            // Flag para horários que já passaram
            bool houveHorarioPassado = false; 

            foreach (var horarioStr in horariosSelecionados)
            {
                if (TimeSpan.TryParse(horarioStr, out TimeSpan horaInicio))
                {
                    // 1. VERIFICAÇÃO DE HORÁRIO PASSADO
                    DateTime dataEHoraInicio = dataEscala.Date.Add(horaInicio);
                    
                    // Se a data da escala for HOJE E o horário de início JÁ PASSOU, ignora.
                    if (dataEHoraInicio <= DateTime.Now)
                    {
                        houveHorarioPassado = true;
                        continue;
                    }
                    
                    TimeSpan horaFim = horaInicio.Add(TimeSpan.FromHours(1));

                    // Verifica se JÁ EXISTE um bloco (para QUALQUER dentista) na Data e HoraInicio
                    bool jaExiste = _context.EscalasMensaisDentista.Any(e =>
                        e.DataEscala.Date == dataEscala.Date && // Apenas Data
                        e.HoraInicio == horaInicio); // Apenas Hora

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

            // ATUALIZAÇÃO DAS MENSAGENS DE FEEDBACK
            if (novasEscalas.Any())
            {
                _context.EscalasMensaisDentista.AddRange(novasEscalas);
                _context.SaveChanges();

                string mensagemSucesso = $"{escalasCriadas} blocos de escala criados com sucesso para {dataEscala:dd/MM/yyyy}!";
                
                if (houveDuplicidade)
                {
                    mensagemSucesso += " (Alguns horários já existiam ou estavam ocupados e foram ignorados).";
                }
                if (houveHorarioPassado)
                {
                    mensagemSucesso += " (Horários que já se passaram no dia de hoje foram desconsiderados).";
                }
                
                TempData["Sucesso"] = mensagemSucesso;
            }
            else if (houveDuplicidade || houveHorarioPassado) // Se não criou nenhum mas houve motivo (duplicidade/passado)
            {
                string mensagemErro = "Nenhum novo bloco de escala foi criado. ";
                if (houveDuplicidade)
                {
                    mensagemErro += "Todos os horários selecionados já existiam ou estavam ocupados. ";
                }
                if (houveHorarioPassado)
                {
                    mensagemErro += "Alguns horários que já se passaram foram desconsiderados. ";
                }
                 TempData["Erro"] = mensagemErro.Trim();
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

            // VERIFICAÇÃO DE EDIÇÃO: Se houver agendamento futuro, bloqueia a edição (redireciona para o calendário e exibe o modal de aviso via JS)
            var temAgendamento = await _context.Agendamentos.AnyAsync(a =>
                a.IdDentista == escala.IdDentista &&
                a.DataAgendamento.Date == escala.DataEscala.Date &&
                a.HoraAgendamento == escala.HoraInicio);

            if (temAgendamento)
            {
                // Configura o TempData para que a View exiba o modal de aviso
                TempData["AgendamentoVinculado"] = "true";
                TempData["WarningMessage"] = "Atenção a data selecionada possui um agendamento vinculado e não pode ser editada ou deletada, se preferir pode fazer alterações na área minha agenda.";
                
                // Redireciona para o calendário para exibir o aviso no modal.
                return RedirectToAction("Calendario", new { data = escala.DataEscala.ToString("yyyy-MM-dd") });
            }

            ViewBag.Dentistas = await _context.Dentistas
                .Where(d => (d.Ativo && d.Situacao == "contratado") || d.Id == escala.IdDentista)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            // Busca todos os horários ocupados para esta data, independentemente do dentista.
            var horariosOcupados = await _context.EscalasMensaisDentista
                .Where(e => e.DataEscala.Date == escala.DataEscala.Date &&
                            e.Id != id) // Exclui a escala atual da lista de ocupados
                .Select(e => e.HoraInicio)
                .ToListAsync();

            // Armazena os horários ocupados como strings hh:mm para fácil comparação na View
            ViewBag.HorariosOcupadosStr = horariosOcupados
                .Select(ts => ts.ToString(@"hh\:mm"))
                .ToList();

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

            // Recalcula HoraFim
            model.HoraFim = model.HoraInicio.Add(TimeSpan.FromHours(1));

            if (ModelState.IsValid)
            {
                // Adicionando a verificação de agendamento (redundância de segurança)
                var temAgendamento = await _context.Agendamentos.AnyAsync(a =>
                    a.IdDentista == model.IdDentista &&
                    a.DataAgendamento.Date == model.DataEscala.Date &&
                    a.HoraAgendamento == model.HoraInicio);
                
                if (temAgendamento)
                {
                    TempData["Erro"] = "Esta escala possui agendamentos futuros vinculados. Não é permitido salvar alterações.";
                    // Se houver erro, precisamos recarregar as ViewBags para retornar a View
                    ViewBag.Dentistas = await _context.Dentistas.Where(d => (d.Ativo && d.Situacao == "contratado") || d.Id == model.IdDentista).OrderBy(d => d.Nome).ToListAsync();
                    return View(model);
                }

                // Verifica se JÁ EXISTE outro bloco (para QUALQUER dentista) na Data e HoraInicio
                var jaExiste = await _context.EscalasMensaisDentista.AnyAsync(e =>
                    e.DataEscala.Date == model.DataEscala.Date && // Apenas Data
                    e.HoraInicio == model.HoraInicio && // Apenas Hora
                    e.Id != model.Id); // CRUCIAL: Ignora o registro que está sendo editado

                if (jaExiste)
                {
                    TempData["Erro"] = $"O horário de {model.HoraInicio.ToString(@"hh\:mm")} já está ocupado por outro dentista ou bloco de escala nesta data. Por favor, escolha um horário diferente.";

                    // Popula a ViewBag de dentistas (necessário para retornar a View)
                    ViewBag.Dentistas = await _context.Dentistas.Where(d => (d.Ativo && d.Situacao == "contratado") || d.Id == model.IdDentista).OrderBy(d => d.Nome).ToListAsync();

                    // Recarrega os horários ocupados (para a View)
                    var horariosOcupados = await _context.EscalasMensaisDentista
                        .Where(e => e.DataEscala.Date == model.DataEscala.Date &&
                                     e.Id != id)
                        .Select(e => e.HoraInicio)
                        .ToListAsync();

                    ViewBag.HorariosOcupadosStr = horariosOcupados
                        .Select(ts => ts.ToString(@"hh\:mm"))
                        .ToList();

                    return View(model);
                }

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
                    _context.Update(model);
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

            // Popula a ViewBag de dentistas (necessário para retornar a View)
            ViewBag.Dentistas = await _context.Dentistas.Where(d => (d.Ativo && d.Situacao == "contratado") || d.Id == model.IdDentista).OrderBy(d => d.Nome).ToListAsync();
            return View(model);
        }

        // ==========================================================
        // DELETAR ESCALA (Revertido para TempData padrão)
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

            // VERIFICAÇÃO DE SEGURANÇA: Se o JS falhar, o Controller ainda bloqueia.
            var temAgendamento = await _context.Agendamentos.AnyAsync(a =>
                a.IdDentista == escala.IdDentista &&
                a.DataAgendamento.Date == escala.DataEscala.Date &&
                a.HoraAgendamento == escala.HoraInicio);

            if (temAgendamento)
            {
                TempData["Erro"] = "Esta escala possui agendamentos futuros vinculados e não pode ser excluída.";
                return RedirectToAction("Calendario", new { data = dataReferencia.ToString("yyyy-MM-dd") });
            }

            _context.EscalasMensaisDentista.Remove(escala);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Escala removida com sucesso!";
            return RedirectToAction("Calendario", new { data = dataReferencia.ToString("yyyy-MM-dd") });
        }
        
        // ==========================================================
        // VERIFICAÇÃO RÁPIDA (AJAX) - NOVO PARA JS
        // ==========================================================

        // GET: Admin/Escala/VerificarAgendamento/5
        [HttpGet("VerificarAgendamento/{id}")]
        public async Task<IActionResult> VerificarAgendamento(int id)
        {
            var escala = await _context.EscalasMensaisDentista.FindAsync(id);

            if (escala == null)
            {
                return Json(new { hasAgendamento = false });
            }

            // Verifica se há agendamentos futuros neste bloco
            var temAgendamento = await _context.Agendamentos.AnyAsync(a =>
                a.IdDentista == escala.IdDentista &&
                a.DataAgendamento.Date == escala.DataEscala.Date &&
                a.HoraAgendamento == escala.HoraInicio);

            return Json(new { hasAgendamento = temAgendamento });
        }
    }
}