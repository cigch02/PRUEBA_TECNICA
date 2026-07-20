using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RegistroConsulta.Api.Data;
using RegistroConsulta.Api.Models;
using RegistroConsulta.Api.Repositories;
using RegistroConsulta.Api.Services;
using RegistroConsulta.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC / Controllers ----------
builder.Services.AddControllers();

// Dejamos que FluentValidation sea la única fuente de verdad para las reglas
// de negocio de validación de entrada (evita respuestas 400 con el formato
// por defecto de ASP.NET antes de llegar al Service).
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- EF Core / SQL Server ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- FluentValidation ----------
builder.Services.AddValidatorsFromAssemblyContaining<ConsultaRequestValidator>();

// ---------- Repositorios ----------
builder.Services.AddScoped<IEntidadRepository, EntidadRepository>();
builder.Services.AddScoped<IRegistroRepository, RegistroRepository>();
builder.Services.AddScoped<ILogConsultaRepository, LogConsultaRepository>();

// ---------- Servicios de dominio ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IEntidadAuthService, EntidadAuthService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IRegistroConsultaService, RegistroConsultaService>();

var app = builder.Build();

// ---------- Manejo global de errores no controlados (500) ----------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        logger.LogError(feature?.Error, "Excepción no controlada");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(
            new ErrorResponse("ERROR_INTERNO", "Ocurrió un error inesperado al procesar la solicitud."));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> funcione en tests de integración.
public partial class Program { }
