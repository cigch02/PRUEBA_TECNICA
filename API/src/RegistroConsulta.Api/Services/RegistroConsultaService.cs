using FluentValidation;
using Microsoft.Extensions.Logging;
using RegistroConsulta.Api.Entities;
using RegistroConsulta.Api.Models;
using RegistroConsulta.Api.Repositories;

namespace RegistroConsulta.Api.Services;

public class RegistroConsultaService : IRegistroConsultaService
{
    private readonly IEntidadAuthService _entidadAuthService;
    private readonly IValidator<ConsultaRequest> _validator;
    private readonly IQuotaService _quotaService;
    private readonly IRegistroRepository _registroRepository;
    private readonly ILogConsultaRepository _logRepository;
    private readonly ILogger<RegistroConsultaService> _logger;

    public RegistroConsultaService(
        IEntidadAuthService entidadAuthService,
        IValidator<ConsultaRequest> validator,
        IQuotaService quotaService,
        IRegistroRepository registroRepository,
        ILogConsultaRepository logRepository,
        ILogger<RegistroConsultaService> logger)
    {
        _entidadAuthService = entidadAuthService;
        _validator = validator;
        _quotaService = quotaService;
        _registroRepository = registroRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<ConsultaResult> ConsultarAsync(ConsultaRequest request, CancellationToken ct = default)
    {
        // (2) Validar entrada primero: entidadId e identificador son requeridos.
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            await LogAsync(request.EntidadId, request, ResultadoConsulta.Rechazado, "CAMPOS_INCOMPLETOS", ct);
            return ConsultaResult.Fail(ConsultaResultStatus.CamposIncompletos, "CAMPOS_INCOMPLETOS");
        }

        // (1) Validar que 'entidadId' corresponde a una entidad con convenio activo y vigente.
        var auth = await _entidadAuthService.ValidarAsync(request.EntidadId, ct);

        if (auth.Status == EntidadAuthStatus.EntidadNoEncontrada)
        {
            await LogAsync(request.EntidadId, request, ResultadoConsulta.Rechazado, "ENTIDAD_NO_ENCONTRADA", ct);
            return ConsultaResult.Fail(ConsultaResultStatus.EntidadNoEncontrada, "ENTIDAD_NO_ENCONTRADA");
        }

        if (auth.Status == EntidadAuthStatus.ConvenioInactivo)
        {
            await LogAsync(auth.Entidad!.Id, request, ResultadoConsulta.Rechazado, "CONVENIO_INACTIVO_O_NO_VIGENTE", ct);
            return ConsultaResult.Fail(ConsultaResultStatus.ConvenioInactivo, "CONVENIO_INACTIVO_O_NO_VIGENTE");
        }

        var entidad = auth.Entidad!;

        try
        {
            // (3) Verificar que la entidad no ha superado su cuota diaria.
            var puedeConsultar = await _quotaService.PuedeConsultarAsync(entidad, ct);
            if (!puedeConsultar)
            {
                await LogAsync(entidad.Id, request, ResultadoConsulta.Rechazado, "CUOTA_DIARIA_EXCEDIDA", ct);
                return ConsultaResult.Fail(ConsultaResultStatus.CuotaExcedida, "CUOTA_DIARIA_EXCEDIDA");
            }

            // (4) Consultar el repositorio de registros, por identificador y nombre.
            var registro = await _registroRepository.BuscarAsync(request.Identificador!, request.Nombre!, ct);

            if (registro is null)
            {
                // Consumió cuota (llegó a ejecutar la búsqueda) aunque no haya match.
                await LogAsync(entidad.Id, request, ResultadoConsulta.Aprobado, "NO_ENCONTRADO", ct);
                return ConsultaResult.Fail(ConsultaResultStatus.NoEncontrado, "NO_ENCONTRADO");
            }

            var response = new ConsultaResponse
            {
                Estado = registro.Estado,
                NumeroRegistro = registro.NumeroRegistro,
                FechaEvento = registro.FechaEvento,
                FechaInscripcion = registro.FechaInscripcion
            };

            await LogAsync(entidad.Id, request, ResultadoConsulta.Aprobado, "OK", ct);
            return ConsultaResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar consulta para entidad {EntidadId}", entidad.Id);
            await LogAsync(entidad.Id, request, ResultadoConsulta.Rechazado, "ERROR_INTERNO", ct);
            return ConsultaResult.Fail(ConsultaResultStatus.ErrorInterno, "ERROR_INTERNO");
        }
    }

    private async Task LogAsync(int? entidadId, ConsultaRequest request, ResultadoConsulta resultado, string motivo, CancellationToken ct)
    {
        try
        {
            await _logRepository.RegistrarAsync(new LogConsulta
            {
                EntidadId = entidadId,
                IdentificadorConsultado = request.Identificador,
                NombreConsultado = request.Nombre,
                Resultado = resultado,
                Motivo = motivo,
                FechaHora = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar el log de auditoría (motivo original: {Motivo})", motivo);
        }
    }
}
