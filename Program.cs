using GlowNic.Data;
using GlowNic.Models.Entities;
using GlowNic.Services;
using GlowNic.Services.IServices;
using GlowNic.Services.Interfaces;
using GlowNic.Services.Implementations;
using GlowNic.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.IO;

// Configurar Npgsql para manejar DateTime correctamente con PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permitir deserializar strings como nombres de enum
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddControllersWithViews(); // Mantener para MVC web

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "GlowNic API", 
        Version = "v1",
        Description = "API para gestión de reservas de salones de belleza"
    });
    
    // Configurar JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Configurar soporte para DateOnly y TimeOnly
    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Example = new Microsoft.OpenApi.Any.OpenApiString("2024-01-01")
    });
    
    c.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "time",
        Example = new Microsoft.OpenApi.Any.OpenApiString("10:30:00")
    });
    
    // Ignorar referencias circulares
    c.UseAllOfToExtendReferenceSchemas();
    c.SupportNonNullableReferenceTypes();
    
    // Incluir comentarios XML si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Configurar para ignorar errores de esquema
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// Configurar URLs en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Configurar Entity Framework con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Configurar JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = jwtSettings["Issuer"] ?? "GlowNic";
        var audience = jwtSettings["Audience"] ?? "GlowNicUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Cookies para MVC web
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Cookies para MVC web
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Esquema por defecto
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => // Para MVC web
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "GlowNic.Auth";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // Para API
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});


// Configurar Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Barber", policy => policy.RequireRole("Barber"));
    // Mantener políticas antiguas para MVC
    options.AddPolicy("Administrador", policy => policy.RequireClaim("Rol", SD.RolAdministrador));
    options.AddPolicy("Normal", policy => policy.RequireClaim("Rol", SD.RolNormal, SD.RolAdministrador));
});

// Configurar CORS para API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar sesiones (para MVC web)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar servicios nuevos (API)
        builder.Services.AddScoped<GlowNic.Services.Interfaces.IAuthService, GlowNic.Services.Implementations.AuthService>();
builder.Services.AddScoped<IBarberService, BarberService>();
        builder.Services.AddScoped<IAppointmentService, GlowNic.Services.Implementations.AppointmentService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkingHoursService, WorkingHoursService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IHelpSupportService, HelpSupportService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Registrar servicios antiguos (MVC web - mantener compatibilidad)
        builder.Services.AddScoped<GlowNic.Services.IServices.IAuthService, GlowNic.Services.AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();

var app = builder.Build();

// Aplicar migraciones e inicializar datos
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Aplicar migraciones
        dbContext.Database.Migrate();

        // Crear usuario admin antiguo si no existe (MVC web)
        InicializarUsuarioAdmin.CrearAdminSiNoExiste(dbContext, logger);
        
        // Crear usuario admin nuevo si no existe (API)
        InicializarSistema.CrearAdminUserSiNoExiste(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
    // Habilitar Swagger solo en desarrollo
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GlowNic API v1");
        c.RoutePrefix = "swagger"; // Swagger disponible en /swagger
    });
}

// Manejar códigos de estado
app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");

// Configurar cache para archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        else
        {
            var path = ctx.File.Name.ToLower();
            if (path.EndsWith(".js") || path.EndsWith(".css"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            }
            else if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") || 
                     path.EndsWith(".gif") || path.EndsWith(".svg") || path.EndsWith(".ico"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=2592000");
            }
            else
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
            }
        }
    }
});

app.UseRouting();

// Habilitar CORS
app.UseCors("AllowAll");

// Habilitar sesiones
app.UseSession();

// Habilitar Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();

// Ruta raíz - Redirigir al login del sistema web
app.MapGet("/", () => Results.Redirect("/login")).ExcludeFromDescription();

// Mapear controladores API
app.MapControllers();

// Configurar rutas MVC (mantener para web)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
