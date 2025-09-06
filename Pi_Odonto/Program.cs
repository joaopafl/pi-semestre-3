using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Services;
using Pi_Odonto.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21)));
    // Ativar log SQL em desenvolvimento
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Information);
        options.EnableDetailedErrors();
    }
});

// === CONFIGURAÇÃO DE EMAIL ===
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Registrar os serviços de email
builder.Services.AddScoped<IEmailCadastroService, EmailCadastroService>();
builder.Services.AddScoped<EmailService>();

// === CONFIGURAÇÃO DE AUTENTICAÇÃO (CORRIGIDA) ===
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";           // ✅ CORRIGIDO
        options.LogoutPath = "/Auth/Logout";         // ✅ CORRIGIDO
        options.AccessDeniedPath = "/Auth/Login";    // ✅ CORRIGIDO
        options.ExpireTimeSpan = TimeSpan.FromHours(2);    // Sessão expira em 2 horas
        options.SlidingExpiration = true;       // Renova automaticamente se usuario ativo
        options.Cookie.Name = "PiOdontoAuth";   // Nome do cookie
        options.Cookie.HttpOnly = true;         // Segurança - só server acessa
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS em produção
    });

// Configurar políticas de autorização (opcional)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Admin"));
    options.AddPolicy("ResponsavelOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Responsavel"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// === IMPORTANTE: A ORDEM IMPORTA! ===
app.UseAuthentication();    // DEVE vir ANTES de UseAuthorization
app.UseAuthorization();

// Substitua a seção de rotas no seu Program.cs por esta:

// === ROTAS ESPECÍFICAS PRIMEIRO (ANTES DA ROTA PADRÃO) ===
app.MapControllerRoute(
    name: "cadastro_crianca",
    pattern: "Cadastro_crianca",
    defaults: new { controller = "Responsavel", action = "CreateCrianca" });

app.MapControllerRoute(
    name: "create_crianca",
    pattern: "Responsavel/CreateCrianca",
    defaults: new { controller = "Responsavel", action = "CreateCrianca" });

app.MapControllerRoute(
    name: "cadastrar_crianca",
    pattern: "Perfil/CadastrarCrianca",
    defaults: new { controller = "Perfil", action = "CadastrarCrianca" });

app.MapControllerRoute(
    name: "minhas_criancas",
    pattern: "Perfil/MinhasCriancas",
    defaults: new { controller = "Perfil", action = "MinhasCriancas" });

app.MapControllerRoute(
    name: "detalhes_crianca",
    pattern: "Perfil/DetalhesCrianca/{id}",
    defaults: new { controller = "Perfil", action = "DetalhesCrianca" });

app.MapControllerRoute(
    name: "editar_crianca",
    pattern: "Perfil/EditarCrianca/{id}",
    defaults: new { controller = "Perfil", action = "EditarCrianca" });

app.MapControllerRoute(
    name: "cadastro",
    pattern: "Cadastro",
    defaults: new { controller = "Responsavel", action = "Create" });

app.MapControllerRoute(
    name: "login",
    pattern: "Login",
    defaults: new { controller = "Auth", action = "Login" });

app.MapControllerRoute(
    name: "esqueceuSenha",
    pattern: "Auth/EsqueceuSenha",
    defaults: new { controller = "Auth", action = "EsqueceuSenha" });

app.MapControllerRoute(
    name: "redefinirSenha",
    pattern: "Auth/RedefinirSenha",
    defaults: new { controller = "Auth", action = "RedefinirSenha" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}",
    defaults: new { controller = "Admin" });

// === ROTA PADRÃO POR ÚLTIMO ===
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();