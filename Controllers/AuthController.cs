using Microsoft.AspNetCore.Mvc;
using Pi_Odonto.Data;
using Pi_Odonto.Models;
using Pi_Odonto.Helpers;
using Pi_Odonto.ViewModels;
using Pi_Odonto.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Necessário para List<Claim>

namespace Pi_Odonto.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        // Usando o Fully Qualified Name para evitar ambiguidade com ViewModels
        private readonly Pi_Odonto.Services.IEmailService _emailService; 
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, Pi_Odonto.Services.IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _emailService = emailService; 
            _logger = logger;
        }

        // =========================================================================
        // === LOGIN DE RESPONSÁVEL (AÇÃO: Login, ROTA: /Login) ===
        // =========================================================================

        // GET: Login de Responsável
        [HttpGet]
        [Route("Login")]
        public IActionResult Login()
        {
            // Se já estiver logado como Responsável, redireciona para o perfil
            if (User.Identity?.IsAuthenticated == true && User.HasClaim("TipoUsuario", "Responsavel"))
            {
                return RedirectToAction("Index", "Perfil");
            }
            return View();
        }

        // POST: Login de Responsável
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var responsavel = _context.Responsaveis
                    .FirstOrDefault(r => r.Email == model.Email && r.Ativo && r.EmailVerificado);

                if (responsavel != null && PasswordHelper.VerifyPassword(model.Senha, responsavel.Senha ?? ""))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, responsavel.Nome),
                        new Claim(ClaimTypes.Email, responsavel.Email),
                        new Claim("ResponsavelId", responsavel.Id.ToString()),
                        new Claim("TipoUsuario", "Responsavel") // CLAIM
                    };

                    // MUDANÇA CRÍTICA: Usando esquema de autenticação "ResponsavelAuth"
                    var claimsIdentity = new ClaimsIdentity(claims, "ResponsavelAuth"); 
                    var principal = new ClaimsPrincipal(claimsIdentity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.LembrarMe,
                        ExpiresUtc = model.LembrarMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
                    };

                    // MUDANÇA CRÍTICA: Especifique o esquema "ResponsavelAuth"
                    await HttpContext.SignInAsync("ResponsavelAuth", principal, authProperties); 

                    return RedirectToAction("Index", "Perfil");
                }

                ModelState.AddModelError("", "Email ou senha inválidos, ou email não verificado");
            }

            return View(model);
        }

        // =========================================================================
        // === LOGIN DE DENTISTA (AÇÃO: DentistaLogin, ROTA: /Auth/DentistaLogin) ===
        // =========================================================================
        
        // GET: Login de Dentista
        [HttpGet]
        [Route("Auth/DentistaLogin")]
        public IActionResult DentistaLogin()
        {
            // Se já estiver logado como Dentista, redireciona
            if (User.Identity?.IsAuthenticated == true && User.HasClaim("TipoUsuario", "Dentista"))
            {
                return RedirectToAction("Dashboard", "Dentista");
            }
            return View();
        }

        // POST: Login de Dentista
        [HttpPost]
        [Route("Auth/DentistaLogin")]
        public async Task<IActionResult> DentistaLogin(DentistaLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var dentista = _context.Dentistas
                    .FirstOrDefault(d => d.Email == model.Email && d.Ativo);

                if (dentista != null && PasswordHelper.VerifyPassword(model.Senha, dentista.Senha ?? ""))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, dentista.Nome),
                        new Claim(ClaimTypes.Email, dentista.Email),
                        new Claim("DentistaId", dentista.Id.ToString()),
                        new Claim("TipoUsuario", "Dentista") // CLAIM
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "DentistaAuth");
                    var principal = new ClaimsPrincipal(claimsIdentity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.LembrarMe,
                        ExpiresUtc = model.LembrarMe ?
                            DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync("DentistaAuth", principal, authProperties);

                    return RedirectToAction("Dashboard", "Dentista");
                }

                ModelState.AddModelError("", "Email ou senha inválidos");
            }

            return View(model);
        }

        // =========================================================================
        // === LOGIN DE ADMIN (AÇÃO: AdminLogin, ROTA: /Admin/Login) ===
        // =========================================================================

        // GET: Login de Admin
        [HttpGet]
        [Route("Admin/Login")]
        public IActionResult AdminLogin()
        {
            // Se já estiver logado como admin, redireciona
            if (User.Identity?.IsAuthenticated == true && User.HasClaim("TipoUsuario", "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            return View();
        }

        // POST: Login de Admin
        [HttpPost]
        [Route("Admin/Login")]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // NOTA DE SEGURANÇA: Esta lógica busca o Admin na tabela de Responsaveis
                // Deve ser ajustada para a lógica real do seu Admin, se for uma tabela separada.
                var admin = _context.Responsaveis 
                    .FirstOrDefault(r => r.Email == model.Email && r.Ativo);
                
                // VERIFICAÇÃO FIXA DE EMAIL PARA TESTE: Substitua pela lógica real do seu Admin
                bool isAdmin = admin != null && admin.Email == "admin@piodonto.com"; 

                if (isAdmin && PasswordHelper.VerifyPassword(model.Senha, admin.Senha ?? ""))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, admin.Nome),
                        new Claim(ClaimTypes.Email, admin.Email),
                        new Claim("ResponsavelId", admin.Id.ToString()),
                        new Claim("TipoUsuario", "Admin") // <--- ESTA É A CLAIM CRÍTICA!
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "AdminAuth"); // <--- ESQUEMA ADMIN
                    var principal = new ClaimsPrincipal(claimsIdentity);

                    // MUDANÇA CRÍTICA: Especifique o esquema "AdminAuth"
                    await HttpContext.SignInAsync("AdminAuth", principal); 

                    return RedirectToAction("Dashboard", "Admin");
                }

                ModelState.AddModelError("", "Credenciais de administrador inválidas");
            }

            return View(model);
        }

        // =========================================================================
        // === LOGOUT ===
        // =========================================================================

        // POST: Logout universal (detecta automaticamente o tipo de usuário)
        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            var tipoUsuario = User.FindFirst("TipoUsuario")?.Value;

            // Faz logout do esquema correto
            if (tipoUsuario == "Dentista")
            {
                await HttpContext.SignOutAsync("DentistaAuth");
                return RedirectToAction("DentistaLogin", "Auth");
            }
            else if (tipoUsuario == "Admin")
            {
                await HttpContext.SignOutAsync("AdminAuth");
                return RedirectToAction("AdminLogin", "Auth"); // Redireciona para o login do Admin
            }
            else if (tipoUsuario == "Responsavel")
            {
                await HttpContext.SignOutAsync("ResponsavelAuth"); 
                return RedirectToAction("Login", "Auth"); // Redireciona para o login do Responsável
            }

            // Fallback: faz logout de tudo e redireciona para a home
            await HttpContext.SignOutAsync("AdminAuth");
            await HttpContext.SignOutAsync("DentistaAuth");
            await HttpContext.SignOutAsync("ResponsavelAuth"); 
            return RedirectToAction("Index", "Home");
        }
        
        // =========================================================================
        // === RECUPERAÇÃO DE SENHA ===
        // =========================================================================

        // POST: Esqueceu a senha
        [HttpPost]
        [Route("EsqueceuSenha")]
        public async Task<IActionResult> EsqueceuSenha(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ErrorMessage"] = "Por favor, digite um email válido.";
                    return RedirectToAction("Login");
                }

                // Verificar se o responsável existe
                var responsavel = await _context.Responsaveis
                    .FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower() && r.Ativo);

                // Por segurança, sempre mostramos a mesma mensagem
                if (responsavel != null)
                {
                    // Gerar token único e seguro
                    var token = GerarTokenSeguro();
                    var dataExpiracao = DateTime.Now.AddHours(1); // Token válido por 1 hora

                    // Salvar token no banco (necessita da tabela RecuperacaoSenhaToken no AppDbContext)
                    var recuperacaoToken = new RecuperacaoSenhaToken
                    {
                        Email = email.ToLower(),
                        Token = token,
                        DataCriacao = DateTime.Now,
                        DataExpiracao = dataExpiracao,
                        Usado = false
                    };

                    _context.RecuperacaoSenhaTokens.Add(recuperacaoToken);
                    await _context.SaveChangesAsync();

                    // Enviar email usando o IEmailService
                    await _emailService.EnviarEmailRecuperacaoSenhaAsync(
                        email,
                        responsavel.Nome,
                        token
                    );

                    _logger.LogInformation($"Token de recuperação enviado para: {email}");
                }

                TempData["SuccessMessage"] = "Se o email estiver cadastrado, você receberá as instruções de recuperação em instantes! ❤️";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar recuperação de senha para: {email}");
                TempData["ErrorMessage"] = "Ocorreu um erro ao processar sua solicitação. Tente novamente.";
                return RedirectToAction("Login");
            }
        }

        // GET: Redefinir senha
        [HttpGet]
        [Route("RedefinirSenha")]
        public async Task<IActionResult> RedefinirSenha(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Link de recuperação inválido.";
                return RedirectToAction("Login");
            }

            // Verificar se o token existe, não foi usado e não expirou
            var recuperacaoToken = await _context.RecuperacaoSenhaTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.Usado && t.DataExpiracao > DateTime.Now);

            if (recuperacaoToken == null)
            {
                TempData["ErrorMessage"] = "Link de recuperação expirado ou inválido.";
                return RedirectToAction("Login");
            }

            var model = new RedefinirSenhaViewModel
            {
                Token = token
            };

            return View(model);
        }

        // POST: Redefinir senha
        [HttpPost]
        [Route("RedefinirSenha")]
        public async Task<IActionResult> RedefinirSenha(RedefinirSenhaViewModel model)
        {
            // O campo no seu ViewModel é ConfirmarSenha, a validação ocorre aqui.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Verificar token antes de tudo
                var recuperacaoToken = await _context.RecuperacaoSenhaTokens
                    .FirstOrDefaultAsync(t => t.Token == model.Token && !t.Usado && t.DataExpiracao > DateTime.Now);

                if (recuperacaoToken == null)
                {
                    TempData["ErrorMessage"] = "Link de recuperação expirado ou inválido.";
                    return RedirectToAction("Login");
                }

                // Buscar responsável
                var responsavel = await _context.Responsaveis
                    .FirstOrDefaultAsync(r => r.Email.ToLower() == recuperacaoToken.Email);

                if (responsavel == null)
                {
                    TempData["ErrorMessage"] = "Usuário não encontrado.";
                    return RedirectToAction("Login");
                }

                // Atualizar senha do responsável
                responsavel.Senha = PasswordHelper.HashPassword(model.NovaSenha);
                responsavel.DataAtualizacao = DateTime.Now;

                // Marcar token como usado
                recuperacaoToken.Usado = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Senha redefinida com sucesso para: {recuperacaoToken.Email}");

                TempData["SuccessMessage"] = "Senha redefinida com sucesso! Agora você pode fazer login com sua nova senha. ❤️";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao redefinir senha para token: {model.Token}");
                TempData["ErrorMessage"] = "Ocorreu um erro ao redefinir sua senha. Tente novamente.";
                return View(model);
            }
        }
        
        // =========================================================================
        // === MÉTODOS PRIVADOS E UTILITÁRIOS ===
        // =========================================================================

        // Método privado para gerar token seguro
        private string GerarTokenSeguro()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 32);
            }
        }

        // Método para limpeza de tokens expirados (pode ser chamado por um serviço de fundo)
        public async Task LimparTokensExpirados()
        {
            try
            {
                var tokensExpirados = await _context.RecuperacaoSenhaTokens
                    .Where(t => t.DataExpiracao < DateTime.Now || t.Usado)
                    .ToListAsync();

                if (tokensExpirados.Any())
                {
                    _context.RecuperacaoSenhaTokens.RemoveRange(tokensExpirados);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Removidos {tokensExpirados.Count} tokens expirados");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar tokens expirados");
            }
        }

        // GET: Área restrita - para fins de teste de autenticação
        [HttpGet]
        [Route("AreaRestrita")]
        public IActionResult AreaRestrita()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            // Exibir informações do usuário logado para debug
            ViewBag.UserName = User.Identity.Name;
            ViewBag.AuthType = User.Identity.AuthenticationType;
            ViewBag.Claims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();

            return View();
        }
    }
}
