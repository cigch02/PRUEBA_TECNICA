namespace RegistroConsulta.Api.Models;

/// <summary>
/// Estados posibles del procesamiento de una consulta. Cada uno mapea 1:1
/// a un código HTTP en el Controller y a un "motivo" en el log de auditoría.
/// </summary>
public enum ConsultaResultStatus
{
    Ok,                 // 200
    EntidadNoEncontrada, // 401
    ConvenioInactivo,   // 403
    CamposIncompletos,  // 400
    CuotaExcedida,      // 429
    NoEncontrado,       // 404
    ErrorInterno        // 500
}

public class ConsultaResult
{
    public ConsultaResultStatus Status { get; }
    public string Motivo { get; }
    public ConsultaResponse? Data { get; }

    private ConsultaResult(ConsultaResultStatus status, string motivo, ConsultaResponse? data)
    {
        Status = status;
        Motivo = motivo;
        Data = data;
    }

    public static ConsultaResult Ok(ConsultaResponse data) =>
        new(ConsultaResultStatus.Ok, "OK", data);

    public static ConsultaResult Fail(ConsultaResultStatus status, string motivo) =>
        new(status, motivo, null);
}
