using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Services;

public enum EntidadAuthStatus
{
    Ok,
    EntidadNoEncontrada,  // no existe ninguna entidad con ese Id -> 401
    ConvenioInactivo      // existe la entidad pero el convenio no está activo/vigente -> 403
}

public record EntidadAuthResult(EntidadAuthStatus Status, Entidad? Entidad);

public interface IEntidadAuthService
{
    /// <summary>
    /// Valida (1): el 'entidadId' recibido corresponde a una entidad con convenio activo y vigente.
    /// </summary>
    Task<EntidadAuthResult> ValidarAsync(int? entidadId, CancellationToken ct = default);
}
