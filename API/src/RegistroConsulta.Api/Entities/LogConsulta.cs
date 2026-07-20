namespace RegistroConsulta.Api.Entities;

public enum ResultadoConsulta
{
    Aprobado = 1,
    Rechazado = 2
}

/// <summary>
/// Registro de auditoría de cada intento de consulta (aprobado o rechazado).
/// </summary>
public class LogConsulta
{
    public long Id { get; set; }

    public int? EntidadId { get; set; }
    public Entidad? Entidad { get; set; }

    public string? IdentificadorConsultado { get; set; }

    public string? NombreConsultado { get; set; }

    public ResultadoConsulta Resultado { get; set; }

    /// <summary>
    /// Motivo del resultado, p. ej. "OK", "CONVENIO_INACTIVO",
    /// "CAMPOS_INCOMPLETOS", "CUOTA_EXCEDIDA", "NO_ENCONTRADO", "ERROR_INTERNO".
    /// </summary>
    public string Motivo { get; set; } = default!;

    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
}
