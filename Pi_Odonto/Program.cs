using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Data;
using Pi_Odonto.Services;
using Pi_Odonto.Models;

var builder = WebApplication.CreateBuilder(args);

// === Serviços MVC ===
builder.Services.AddControllersWithViews();

// === Entity Framework ===
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

// === Email ===
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailCadastroService, EmailCadastroService>();
builder.Services.AddScoped<EmailService>();

// === Autenticação com múltiplos cookies (CORRIGIDO: ResponsavelAuth ADICIONADO) ===
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "AdminAuth";
    options.DefaultChallengeScheme = "AdminAuth";
})
    .AddCookie("AdminAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddCookie("DentistaAuth", options =>
    {
        options.LoginPath = "/Auth/DentistaLogin";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "DentistaAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    })
    .AddCookie("ResponsavelAuth", options => // <-- NOVO ESQUEMA ADICIONADO
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ResponsavelAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    });

// === Políticas de autorização ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Admin")
              .AddAuthenticationSchemes("AdminAuth"));

    options.AddPolicy("ResponsavelOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Responsavel")
              .AddAuthenticationSchemes("ResponsavelAuth")); // ALTERADO: Usar ResponsavelAuth

    options.AddPolicy("DentistaOnly", policy =>
        policy.RequireClaim("TipoUsuario", "Dentista")
              .AddAuthenticationSchemes("DentistaAuth"));
});

var app = builder.Build();

// === Popular dados iniciais se necessário ===
var loggerInit = app.Services.GetRequiredService<ILogger<Program>>();
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000);
        
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            loggerInit.LogInformation("Verificando conexão com banco de dados...");
            await context.Database.EnsureCreatedAsync();
            loggerInit.LogInformation("Conexão com banco de dados estabelecida.");

            if (!await context.EscalaTrabalho.AnyAsync())
            {
                var escalas = new[]
                {
                    new EscalaTrabalho { DtDisponivel = "Segunda-feira", HrInicio = 8, HrFim = 17 },
                    new EscalaTrabalho { DtDisponivel = "Terça-feira", HrInicio = 8, HrFim = 17 },
                    new EscalaTrabalho { DtDisponivel = "Quarta-feira", HrInicio = 8, HrFim = 17 },
                    new EscalaTrabalho { DtDisponivel = "Quinta-feira", HrInicio = 8, HrFim = 17 },
                    new EscalaTrabalho { DtDisponivel = "Sexta-feira", HrInicio = 8, HrFim = 17 },
                    new EscalaTrabalho { DtDisponivel = "Sábado", HrInicio = 8, HrFim = 12 }
                };

                await context.EscalaTrabalho.AddRangeAsync(escalas);
                await context.SaveChangesAsync();
                loggerInit.LogInformation("Escalas de trabalho inicializadas.");
            }
        }
    }
    catch (Exception ex)
    {
        loggerInit.LogError(ex, "Erro ao inicializar banco de dados. A aplicação continuará funcionando, mas algumas funcionalidades podem não estar disponíveis.");
    }
});

// === Pipeline ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var hasHttps = app.Configuration["ASPNETCORE_URLS"]?.Contains("https") == true;
if (hasHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// === IMPORTANTE: A ORDEM IMPORTA! ===
app.UseAuthentication();
app.UseAuthorization();

// === ROTAS ESPECÍFICAS PRIMEIRO (ANTES DA ROTA PADRÃO) ===
app.MapControllerRoute(name: "login", pattern: "Login", defaults: new { controller = "Auth", action = "Login" });
app.MapControllerRoute(name: "admin_login", pattern: "Auth/Login", defaults: new { controller = "Auth", action = "Login" });
app.MapControllerRoute(name: "dentista_login", pattern: "Auth/DentistaLogin", defaults: new { controller = "Auth", action = "DentistaLogin" });
app.MapControllerRoute(name: "logout", pattern: "Auth/Logout", defaults: new { controller = "Auth", action = "Logout" });
app.MapControllerRoute(name: "esqueceuSenha", pattern: "Auth/EsqueceuSenha", defaults: new { controller = "Auth", action = "EsqueceuSenha" });
app.MapControllerRoute(name: "redefinirSenha", pattern: "Auth/RedefinirSenha", defaults: new { controller = "Auth", action = "RedefinirSenha" });
app.MapControllerRoute(name: "acessoNegado", pattern: "Auth/AcessoNegado", defaults: new { controller = "Auth", action = "AcessoNegado" });
app.MapControllerRoute(name: "cadastro", pattern: "Cadastro", defaults: new { controller = "Responsavel", action = "Create" });
app.MapControllerRoute(name: "cadastro_crianca", pattern: "Cadastro_crianca", defaults: new { controller = "Responsavel", action = "CreateCrianca" });
app.MapControllerRoute(name: "create_crianca", pattern: "Responsavel/CreateCrianca", defaults: new { controller = "Responsavel", action = "CreateCrianca" });
app.MapControllerRoute(name: "cadastrar_crianca", pattern: "Perfil/CadastrarCrianca", defaults: new { controller = "Perfil", action = "CadastrarCrianca" });
app.MapControllerRoute(name: "minhas_criancas", pattern: "Perfil/MinhasCriancas", defaults: new { controller = "Perfil", action = "MinhasCriancas" });
app.MapControllerRoute(name: "detalhes_crianca", pattern: "Perfil/DetalhesCrianca/{id}", defaults: new { controller = "Perfil", action = "DetalhesCrianca" });
app.MapControllerRoute(name: "editar_crianca", pattern: "Perfil/EditarCrianca/{id}", defaults: new { controller = "Perfil", action = "EditarCrianca" });
app.MapControllerRoute(name: "admin_dashboard", pattern: "Admin", defaults: new { controller = "Admin", action = "Dashboard" });
app.MapControllerRoute(name: "admin_escala_calendario", pattern: "Admin/Escala/Calendario", defaults: new { controller = "AdminEscala", action = "Calendario" });
app.MapControllerRoute(name: "admin_escala_criar", pattern: "Admin/Escala/Criar", defaults: new { controller = "AdminEscala", action = "Criar" });
app.MapControllerRoute(name: "admin_escala_editar", pattern: "Admin/Escala/Editar/{id}", defaults: new { controller = "AdminEscala", action = "Editar" });
app.MapControllerRoute(name: "admin_escala_excluir", pattern: "Admin/Escala/Excluir/{id}", defaults: new { controller = "AdminEscala", action = "Excluir" });
app.MapControllerRoute(name: "admin_escala_criar_multiplos", pattern: "Admin/Escala/CriarMultiplos", defaults: new { controller = "AdminEscala", action = "CriarMultiplos" });
app.MapControllerRoute(name: "admin_actions", pattern: "Admin/{action}/{id?}", defaults: new { controller = "Admin" });

// === ROTA PADRÃO (ÚLTIMA) ===
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();