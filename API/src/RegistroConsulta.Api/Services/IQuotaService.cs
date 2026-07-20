using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Services;

public interface IQuotaService
{
    /// <summary>
    /// Valida (3): la entidad no ha superado su cuota diaria de consultas.
    /// Devuelve true si la entidad TODAVÍA puede realizar al menos una consulta más hoy.
    /// </summary>
    Task<bool> PuedeConsultarAsync(Entidad entidad, CancellationToken ct = default);
}
