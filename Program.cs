using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;

// 1) Cargar el archivo .env (busca en el directorio actual y junto al ejecutable)
var envFile = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
    .Select(d => Path.Combine(d, ".env"))
    .FirstOrDefault(File.Exists);

if (envFile is null)
    Console.WriteLine("[ENV] No se encontró .env en: " + Directory.GetCurrentDirectory());
else
    CargarEnv(envFile);

static void CargarEnv(string ruta)
{
    foreach (var linea in File.ReadAllLines(ruta))
    {
        var l = linea.Trim();
        if (l.Length == 0 || l.StartsWith('#')) continue;
        if (l.StartsWith("export ")) l = l[7..].TrimStart();

        var i = l.IndexOf('=');
        if (i <= 0) continue;

        var clave = l[..i].Trim();
        var valor = l[(i + 1)..].Trim();

        // Quita un par de comillas dobles O simples, solo si rodean todo el valor
        if (valor.Length >= 2 &&
            ((valor[0] == '"' && valor[^1] == '"') || (valor[0] == '\'' && valor[^1] == '\'')))
            valor = valor[1..^1];

        Environment.SetEnvironmentVariable(clave, valor);
    }
}

// Usar punto decimal siempre
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Todo el sitio exige iniciar sesión
var politicaAutenticado = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(politicaAutenticado));
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccesoDenegado";
        options.Cookie.Name = "SistemaVentas.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async ctx =>
            {
                const string clave = "validado_utc";

                if (ctx.Properties.Items.TryGetValue(clave, out var texto)
                    && DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ultima)
                    && DateTimeOffset.UtcNow - ultima < TimeSpan.FromMinutes(2))
                {
                    return;
                }

                if (!int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

                if (usuario is null || !usuario.Activo)
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var identity = new ClaimsIdentity(usuario.ObtenerClaims(), ctx.Scheme.Name);
                ctx.ReplacePrincipal(new ClaimsPrincipal(identity));
                ctx.Properties.Items[clave] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                ctx.ShouldRenew = true;
            }
        };
    });

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// 2) BASE DE DATOS: SQLite local (archivo sistema-ventas.db junto al proyecto).
// Turso queda desactivado por ahora: el driver Turso.Data.Sqlite no acepta 'url' en la cadena de conexión.
var connectionString = "Data Source=sistema-ventas.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 3) Puerto en producción
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// 4) Migraciones y usuario admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        db.Database.Migrate(); // Aplica las migraciones automáticamente

        // Actualizar roles antiguos si los hubiera
        await db.Usuarios.Where(u => u.Rol == "Admin")
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Roles.Administrador));
        await db.Usuarios.Where(u => u.Rol == "Cajero")
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Roles.Vendedor));

        var adminUser = (Environment.GetEnvironmentVariable("ADMIN_USER") ?? "admin").Trim().ToLowerInvariant();
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")?.Trim();

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("ADMIN_PASSWORD no está definida: NO se creó ni actualizó el admin.");
        }
        else
        {
            logger.LogInformation(
                "ADMIN_PASSWORD leída del entorno, longitud = {Len} (esperado para Admin123$: 9).",
                adminPassword.Length);

            var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == adminUser);

            if (admin is null)
            {
                admin = new Usuario
                {
                    NombreUsuario = adminUser,
                    NombreCompleto = "Administrador",
                    Rol = Roles.Administrador,
                    Activo = true
                };
                admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
                db.Usuarios.Add(admin);
                logger.LogInformation("Usuario administrador '{Usuario}' creado.", adminUser);
            }
            else
            {
                // Solo reescribe el hash si el actual no corresponde a la clave del .env
                var actual = hasher.VerifyHashedPassword(admin, admin.PasswordHash ?? "", adminPassword);
                if (actual == PasswordVerificationResult.Failed)
                {
                    admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
                    logger.LogInformation("Hash del admin '{Usuario}' actualizado desde el .env.", adminUser);
                }
                admin.Rol = Roles.Administrador;
                admin.Activo = true;
            }

            await db.SaveChangesAsync();

            // Verificación contra lo que realmente quedó guardado en la base
            var guardado = await db.Usuarios.AsNoTracking()
                .FirstAsync(u => u.NombreUsuario == adminUser);
            var ok = hasher.VerifyHashedPassword(guardado, guardado.PasswordHash, adminPassword)
                     != PasswordVerificationResult.Failed;

            logger.LogInformation("Verificación post-guardado del admin '{Usuario}': {Ok}", adminUser, ok);
            if (!ok)
                throw new InvalidOperationException("El hash del admin no verifica tras guardarse.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo inicializar la base de datos o los usuarios.");
        throw; // que falle al arrancar en vez de arrancar con el admin sin actualizar
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();