namespace RegistroConsulta.Api.Entities;

/// <summary>
/// Representa a la entidad externa (institución) que consume la API mediante un convenio.
/// Se identifica por 'Id' (enviado por el cliente en el body como 'entidadId'), ya no por API Key.
/// </summary>
public class Entidad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = default!;

    public bool ConvenioActivo { get; set; }

    public DateTime ConvenioVigenciaInicio { get; set; }

    public DateTime ConvenioVigenciaFin { get; set; }

    /// <summary>
    /// Cuota máxima de consultas permitidas por día, configurable por entidad.
    /// </summary>
    public int CuotaDiaria { get; set; }

    public ICollection<LogConsulta> Logs { get; set; } = new List<LogConsulta>();

    /// <summary>
    /// Indica si el convenio está activo y dentro del período de vigencia en la fecha dada.
    /// </summary>
    public bool EsConvenioVigente(DateTime fechaReferencia)
    {
        return ConvenioActivo
               && fechaReferencia.Date >= ConvenioVigenciaInicio.Date
               && fechaReferencia.Date <= ConvenioVigenciaFin.Date;
    }
}
