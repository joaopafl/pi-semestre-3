using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.ViewModels;
using Pi_Odonto.Helpers;
using System;
using System.Linq;
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
            var viewModel = VoluntarioCadastroViewModel.CriarComDisponibilidades();
            return View(viewModel);
        }

        // POST: Voluntario/Cadastro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(VoluntarioCadastroViewModel viewModel)
        {
            // Validar se pelo menos uma disponibilidade foi selecionada
            if (viewModel.Disponibilidades == null || !viewModel.Disponibilidades.Any(d => d.Selecionado))
            {
                ModelState.AddModelError("", "Por favor, selecione pelo menos um turno de disponibilidade.");
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe CPF, Email ou CRO em Dentistas
                bool cpfExiste = await _context.Dentistas
                    .AnyAsync(v => v.Cpf == viewModel.Cpf);

                bool emailExiste = await _context.Dentistas
                    .AnyAsync(v => v.Email == viewModel.Email);

                bool croExiste = await _context.Dentistas
                    .AnyAsync(v => v.Cro == viewModel.Cro);

                if (cpfExiste)
                {
                    TempData["Erro"] = "Este CPF já está cadastrado no sistema.";
                    // Garantir que as disponibilidades estão inicializadas
                    if (viewModel.Disponibilidades == null || viewModel.Disponibilidades.Count == 0)
                    {
                        viewModel.Disponibilidades = VoluntarioCadastroViewModel.CriarComDisponibilidades().Disponibilidades;
                    }
                    return View(viewModel);
                }

                if (emailExiste)
                {
                    TempData["Erro"] = "Este email já está cadastrado no sistema.";
                    if (viewModel.Disponibilidades == null || viewModel.Disponibilidades.Count == 0)
                    {
                        viewModel.Disponibilidades = VoluntarioCadastroViewModel.CriarComDisponibilidades().Disponibilidades;
                    }
                    return View(viewModel);
                }

                if (croExiste)
                {
                    TempData["Erro"] = "Este CRO já está cadastrado no sistema.";
                    if (viewModel.Disponibilidades == null || viewModel.Disponibilidades.Count == 0)
                    {
                        viewModel.Disponibilidades = VoluntarioCadastroViewModel.CriarComDisponibilidades().Disponibilidades;
                    }
                    return View(viewModel);
                }

                // Criar novo dentista
                var dentista = new Dentista
                {
                    Nome = viewModel.Nome,
                    Cpf = viewModel.Cpf,
                    Cro = viewModel.Cro,
                    Email = viewModel.Email,
                    Telefone = viewModel.Telefone,
                    Endereco = viewModel.Endereco ?? string.Empty,
                    Ativo = true,
                    // Senha temporária baseada no CRO (o admin pode alterar depois)
                    Senha = PasswordHelper.HashPassword(viewModel.Cro + "123")
                };

                _context.Dentistas.Add(dentista);
                await _context.SaveChangesAsync();

                // Criar disponibilidades selecionadas
                var disponibilidadesSelecionadas = viewModel.Disponibilidades
                    .Where(d => d.Selecionado)
                    .ToList();

                foreach (var disponibilidade in disponibilidadesSelecionadas)
                {
                    var disponibilidadeDentista = new DisponibilidadeDentista
                    {
                        IdDentista = dentista.Id,
                        DiaSemana = disponibilidade.DiaSemana,
                        HoraInicio = disponibilidade.HoraInicio,
                        HoraFim = disponibilidade.HoraFim,
                        Ativo = true,
                        DataCadastro = DateTime.Now
                    };

                    _context.DisponibilidadesDentista.Add(disponibilidadeDentista);
                }

                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Cadastro realizado com sucesso! Você foi cadastrado como dentista voluntário.";
                return RedirectToAction("Cadastro");
            }

            // Se houver erro, recriar as disponibilidades para não perder os dados do formulário
            if (viewModel.Disponibilidades == null || viewModel.Disponibilidades.Count == 0)
            {
                viewModel = VoluntarioCadastroViewModel.CriarComDisponibilidades();
            }

            return View(viewModel);
        }

        // POST: Voluntario/ValidarCpf
        [HttpPost]
        public async Task<JsonResult> ValidarCpfVoluntario([FromBody] dynamic data)
        {
            string cpf = data.cpf;
            bool existe = await _context.Dentistas.AnyAsync(v => v.Cpf == cpf);
            return Json(new { existe });
        }

        // POST: Voluntario/ValidarEmail
        [HttpPost]
        public async Task<JsonResult> ValidarEmail([FromBody] dynamic data)
        {
            string email = data.email;
            bool existe = await _context.Dentistas.AnyAsync(v => v.Email == email);
            return Json(new { existe });
        }

        // POST: Voluntario/ValidarCro
        [HttpPost]
        public async Task<JsonResult> ValidarCro([FromBody] dynamic data)
        {
            string cro = data.cro;
            bool existe = await _context.Dentistas.AnyAsync(v => v.Cro == cro);
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
