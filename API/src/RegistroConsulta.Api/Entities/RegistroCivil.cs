namespace RegistroConsulta.Api.Entities;

/// <summary>
/// Registro consultable (p. ej. registro civil / registro de un evento).
/// </summary>
public class RegistroCivil
{
    public int Id { get; set; }

    /// <summary>
    /// Identificador (cédula u otro) de la persona asociada al registro.
    /// </summary>
    public string Identificador { get; set; } = default!;

    public string Nombre { get; set; } = default!;

    public string Estado { get; set; } = default!;

    public string NumeroRegistro { get; set; } = default!;

    public DateTime FechaEvento { get; set; }

    public DateTime FechaInscripcion { get; set; }
}
