using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.Helpers;
using Pi_Odonto.ViewModels;

namespace Pi_Odonto.Controllers
{
    [Authorize]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Verificar se é admin
        private bool IsAdmin()
        {
            return User.HasClaim("TipoUsuario", "Admin");
        }

        // GET: Dashboard Admin
        [Route("Dashboard")]
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // Estatísticas para o dashboard
            var totalResponsaveis = _context.Responsaveis.Count();
            var responsaveisAtivos = _context.Responsaveis.Count(r => r.Ativo);
            var cadastrosHoje = _context.Responsaveis.Count(r => r.DataCadastro.Date == DateTime.Today);
            var cadastrosEsteMes = _context.Responsaveis.Count(r => r.DataCadastro.Month == DateTime.Now.Month && r.DataCadastro.Year == DateTime.Now.Year);

            ViewBag.TotalResponsaveis = totalResponsaveis;
            ViewBag.ResponsaveisAtivos = responsaveisAtivos;
            ViewBag.CadastrosHoje = cadastrosHoje;
            ViewBag.CadastrosEsteMes = cadastrosEsteMes;

            // Últimos cadastros
            var ultimosCadastros = _context.Responsaveis
                .OrderByDescending(r => r.DataCadastro)
                .Take(5)
                .ToList();

            return View(ultimosCadastros);
        }

        // GET: Lista de Responsáveis (Admin)
        [Route("Responsaveis")]
        public IActionResult Responsaveis(string busca = "", bool? ativo = null)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var query = _context.Responsaveis.AsQueryable();

            // Filtro de busca
            if (!string.IsNullOrEmpty(busca))
            {
                query = query.Where(r =>
                    r.Nome.Contains(busca) ||
                    r.Email.Contains(busca) ||
                    r.Cpf.Contains(busca) ||
                    r.Telefone.Contains(busca));
            }

            // Filtro de status
            if (ativo.HasValue)
            {
                query = query.Where(r => r.Ativo == ativo.Value);
            }

            var responsaveis = query
                .Include(r => r.Criancas) // ← LINHA ADICIONADA AQUI!
                .OrderByDescending(r => r.DataCadastro)
                .ToList();

            ViewBag.Busca = busca;
            ViewBag.Ativo = ativo;

            return View(responsaveis);
        }

        // GET: Visualizar Responsável
        [Route("Responsaveis/Detalhes/{id}")]
        public IActionResult DetalhesResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            return View(responsavel);
        }

        // GET: Editar Responsável (Admin)
        [Route("Responsaveis/Editar/{id}")]
        public IActionResult EditarResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            // Limpar senha para não mostrar
            responsavel.Senha = "";

            // Criar ViewModel
            var viewModel = new ResponsavelCriancaViewModel
            {
                Responsavel = responsavel,
                Criancas = responsavel.Criancas?.ToList() ?? new List<Crianca>(),
                OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                }
            };

            return View(viewModel);
        }

        // POST: Editar Responsável (Admin)
        [HttpPost]
        [Route("Responsaveis/Editar/{id}")]
        public IActionResult EditarResponsavel(int id, ResponsavelCriancaViewModel viewModel)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            if (id != viewModel.Responsavel.Id)
            {
                return BadRequest();
            }

            var responsavelAtual = _context.Responsaveis.Find(id);
            if (responsavelAtual == null)
            {
                return NotFound();
            }

            // Validar email único
            if (_context.Responsaveis.Any(r => r.Email == viewModel.Responsavel.Email && r.Id != id))
            {
                ModelState.AddModelError("Responsavel.Email", "Este email já está em uso");
            }

            // Remove validação de senha se estiver vazia
            if (string.IsNullOrEmpty(viewModel.Responsavel.Senha))
            {
                ModelState.Remove("Responsavel.Senha");
                ModelState.Remove("ConfirmarSenha");
            }

            if (ModelState.IsValid)
            {
                // Remover máscara
                viewModel.Responsavel.Cpf = viewModel.Responsavel.Cpf.Replace(".", "").Replace("-", "");
                viewModel.Responsavel.Telefone = viewModel.Responsavel.Telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");

                // Atualizar dados
                responsavelAtual.Nome = viewModel.Responsavel.Nome;
                responsavelAtual.Email = viewModel.Responsavel.Email;
                responsavelAtual.Telefone = viewModel.Responsavel.Telefone;
                responsavelAtual.Endereco = viewModel.Responsavel.Endereco;
                responsavelAtual.Cpf = viewModel.Responsavel.Cpf;
                responsavelAtual.Ativo = viewModel.Responsavel.Ativo;

                // Se forneceu nova senha
                if (!string.IsNullOrEmpty(viewModel.Responsavel.Senha))
                {
                    responsavelAtual.Senha = PasswordHelper.HashPassword(viewModel.Responsavel.Senha);
                }

                _context.Responsaveis.Update(responsavelAtual);
                _context.SaveChanges();

                TempData["Sucesso"] = "Responsável atualizado com sucesso!";
                return RedirectToAction("Responsaveis");
            }

            // Se deu erro, recarregar as opções e as crianças
            if (viewModel.OpcoesParentesco == null || !viewModel.OpcoesParentesco.Any())
            {
                viewModel.OpcoesParentesco = new List<string>
                {
                    "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
                };
            }

            // Recarregar as crianças do banco se não existirem
            if (viewModel.Criancas == null || !viewModel.Criancas.Any())
            {
                var responsavelComCriancas = _context.Responsaveis
                    .Include(r => r.Criancas)
                    .FirstOrDefault(r => r.Id == id);

                viewModel.Criancas = responsavelComCriancas?.Criancas?.ToList() ?? new List<Crianca>();
            }

            return View(viewModel);
        }

        // POST: Desativar/Ativar Responsável
        [HttpPost]
        [Route("Responsaveis/ToggleStatus/{id}")]
        public IActionResult ToggleStatus(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis.Find(id);
            if (responsavel == null)
            {
                return NotFound();
            }

            responsavel.Ativo = !responsavel.Ativo;
            _context.Responsaveis.Update(responsavel);
            _context.SaveChanges();

            TempData["Sucesso"] = $"Responsável {(responsavel.Ativo ? "ativado" : "desativado")} com sucesso!";
            return RedirectToAction("Responsaveis");
        }

        // POST: Excluir Responsável (Admin)
        [HttpPost]
        [Route("Responsaveis/Excluir/{id}")]
        public IActionResult ExcluirResponsavel(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            var responsavel = _context.Responsaveis
                .Include(r => r.Criancas)
                .FirstOrDefault(r => r.Id == id);

            if (responsavel == null)
            {
                return NotFound();
            }

            // Remove as crianças primeiro (devido ao FK)
            if (responsavel.Criancas.Any())
            {
                _context.Criancas.RemoveRange(responsavel.Criancas);
            }

            _context.Responsaveis.Remove(responsavel);
            _context.SaveChanges();

            TempData["Sucesso"] = "Responsável e suas crianças excluídos com sucesso!";
            return RedirectToAction("Responsaveis");
        }

        // GET: Relatórios
        [Route("Relatorios")]
        public IActionResult Relatorios()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "Auth");
            }

            // Dados para relatórios
            var totalResponsaveis = _context.Responsaveis.Count();
            var responsaveisAtivos = _context.Responsaveis.Count(r => r.Ativo);
            var responsaveisInativos = _context.Responsaveis.Count(r => !r.Ativo);

            // Cadastros por mês (últimos 6 meses)
            var cadastrosPorMes = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var data = DateTime.Now.AddMonths(-i);
                var count = _context.Responsaveis.Count(r =>
                    r.DataCadastro.Month == data.Month &&
                    r.DataCadastro.Year == data.Year);

                cadastrosPorMes.Add(new
                {
                    Mes = data.ToString("MMM/yyyy"),
                    Count = count
                });
            }

            ViewBag.TotalResponsaveis = totalResponsaveis;
            ViewBag.ResponsaveisAtivos = responsaveisAtivos;
            ViewBag.ResponsaveisInativos = responsaveisInativos;
            ViewBag.CadastrosPorMes = cadastrosPorMes;

            return View();
        }
    }
}