using RegistroConsulta.Api.Repositories;

namespace RegistroConsulta.Api.Services;

public class EntidadAuthService : IEntidadAuthService
{
    private readonly IEntidadRepository _entidadRepository;
    private readonly TimeProvider _timeProvider;

    public EntidadAuthService(IEntidadRepository entidadRepository, TimeProvider timeProvider)
    {
        _entidadRepository = entidadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<EntidadAuthResult> ValidarAsync(int? entidadId, CancellationToken ct = default)
    {
        if (entidadId is null || entidadId <= 0)
        {
            return new EntidadAuthResult(EntidadAuthStatus.EntidadNoEncontrada, null);
        }

        var entidad = await _entidadRepository.ObtenerPorIdAsync(entidadId.Value, ct);

        if (entidad is null)
        {
            return new EntidadAuthResult(EntidadAuthStatus.EntidadNoEncontrada, null);
        }

        var ahora = _timeProvider.GetUtcNow().DateTime;
        if (!entidad.EsConvenioVigente(ahora))
        {
            return new EntidadAuthResult(EntidadAuthStatus.ConvenioInactivo, entidad);
        }

        return new EntidadAuthResult(EntidadAuthStatus.Ok, entidad);
    }
}
