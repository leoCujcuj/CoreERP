using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using UsuariosAPI.Data;
using UsuariosAPI.Models;
using UsuariosAPI.Seeder;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtención de configuración y variables de entorno (prioridad a variables Docker)
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=CoreERP_UsuariosDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? "CoreERP_Default_Development_Secret_Key_2026_MinimumLengthRequirement";

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "CoreERP";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "CoreERPClient";

// 2. Base de Datos (Entity Framework Core) con resiliencia ante reconexión (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

// 3. ASP.NET Core Identity y Providers
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. Autenticación JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// 5. Controladores
builder.Services.AddControllers();

// 6. Swagger con AddSecurityDefinition para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UsuariosAPI - CoreERP",
        Version = "v1",
        Description = "Microservicio de gestión de usuarios, roles y autenticación para CoreERP"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer.\r\n\r\nEscribe 'Bearer' [espacio] y luego tu token en el campo.\r\nEjemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 7. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UsuariosAPI v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 8. Aplicación de migraciones y Seed automático al iniciar con reintentos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var retryCount = 10;
    var delay = TimeSpan.FromSeconds(3);

    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            logger.LogInformation("Verificando base de datos y aplicando migraciones (Intento {Intento}/{Total})...", i + 1, retryCount);
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas exitosamente.");

            logger.LogInformation("Ejecutando seeder de roles y usuario administrador inicial...");
            await IdentitySeeder.SeedAsync(services);
            logger.LogInformation("Seeder ejecutado exitosamente.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Base de datos no disponible aún o error al migrar. Reintentando en {Delay} segundos...", delay.TotalSeconds);
            if (i == retryCount - 1)
            {
                logger.LogError(ex, "Se agotaron los reintentos para inicializar y migrar la base de datos.");
            }
            else
            {
                await Task.Delay(delay);
            }
        }
    }
}

app.Run();
