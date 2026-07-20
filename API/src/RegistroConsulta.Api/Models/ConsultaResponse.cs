namespace RegistroConsulta.Api.Models;

public class ConsultaResponse
{
    public string Estado { get; set; } = default!;
    public string NumeroRegistro { get; set; } = default!;
    public DateTime FechaEvento { get; set; }
    public DateTime FechaInscripcion { get; set; }
}

/// <summary>
/// Formato estándar de error para las respuestas 4xx/5xx.
/// </summary>
public class ErrorResponse
{
    public string Codigo { get; set; } = default!;
    public string Mensaje { get; set; } = default!;

    public ErrorResponse(string codigo, string mensaje)
    {
        Codigo = codigo;
        Mensaje = mensaje;
    }
}
