using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public interface IEntidadRepository
{
    /// <summary>
    /// Busca la entidad por su Id (enviado por el cliente como 'entidadId'). Devuelve null si no existe.
    /// </summary>
    Task<Entidad?> ObtenerPorIdAsync(int entidadId, CancellationToken ct = default);
}
