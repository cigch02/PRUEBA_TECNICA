using Microsoft.AspNetCore.Mvc;
using RegistroConsulta.Api.Models;
using RegistroConsulta.Api.Services;

namespace RegistroConsulta.Api.Controllers;

[ApiController]
[Route("api/registros")]
public class RegistrosController : ControllerBase
{
    private readonly IRegistroConsultaService _service;

    public RegistrosController(IRegistroConsultaService service)
    {
        _service = service;
    }

    /// <summary>
    /// POST /api/registros/consulta
    /// Body: { "entidadId": 1, "identificador": "8-123-456", "nombre": "Carlos" }
    /// El Controller solo se encarga de: extraer inputs, invocar al Service y
    /// mapear el resultado de negocio a la respuesta HTTP correspondiente.
    /// </summary>
    [HttpPost("consulta")]
    public async Task<IActionResult> Consultar([FromBody] ConsultaRequest request, CancellationToken ct)
    {
        var resultado = await _service.ConsultarAsync(request, ct);

        return resultado.Status switch
        {
            ConsultaResultStatus.Ok =>
                Ok(resultado.Data),

            ConsultaResultStatus.CamposIncompletos =>
                BadRequest(new ErrorResponse("CAMPOS_INCOMPLETOS",
                    "Los campos 'entidadId', 'identificador' y 'nombre' son requeridos.")),

            ConsultaResultStatus.EntidadNoEncontrada =>
                Unauthorized(new ErrorResponse("ENTIDAD_NO_ENCONTRADA",
                    "El 'entidadId' no corresponde a ninguna entidad registrada.")),

            ConsultaResultStatus.ConvenioInactivo =>
                StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse("CONVENIO_INACTIVO", "La entidad no tiene un convenio activo y vigente.")),

            ConsultaResultStatus.CuotaExcedida =>
                StatusCode(StatusCodes.Status429TooManyRequests,
                    new ErrorResponse("CUOTA_EXCEDIDA", "Se ha superado la cuota diaria de consultas para esta entidad.")),

            ConsultaResultStatus.NoEncontrado =>
                NotFound(new ErrorResponse("NO_ENCONTRADO", "No se encontró un registro con el identificador proporcionado.")),

            ConsultaResultStatus.ErrorInterno =>
                StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse("ERROR_INTERNO", "Ocurrió un error inesperado al procesar la solicitud.")),

            _ => StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorResponse("ERROR_INTERNO", "Estado de resultado no reconocido."))
        };
    }
}
