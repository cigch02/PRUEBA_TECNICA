using RegistroConsulta.Api.Entities;
using RegistroConsulta.Api.Repositories;

namespace RegistroConsulta.Api.Services;

public class QuotaService : IQuotaService
{
    private readonly ILogConsultaRepository _logRepository;
    private readonly TimeProvider _timeProvider;

    public QuotaService(ILogConsultaRepository logRepository, TimeProvider timeProvider)
    {
        _logRepository = logRepository;
        _timeProvider = timeProvider;
    }

    public async Task<bool> PuedeConsultarAsync(Entidad entidad, CancellationToken ct = default)
    {
        var ahora = _timeProvider.GetUtcNow().DateTime;
        var inicioDia = ahora.Date;
        var finDia = inicioDia.AddDays(1);

        var consultasHoy = await _logRepository.ContarConsultasDelDiaAsync(entidad.Id, inicioDia, finDia, ct);

        return EvaluarCuota(consultasHoy, entidad.CuotaDiaria);
    }

    /// <summary>
    /// Lógica pura de la validación de cuota diaria, aislada para poder testearla
    /// sin necesidad de una base de datos ni de mocks de tiempo.
    /// </summary>
    /// <param name="consultasRealizadasHoy">Cantidad de consultas aprobadas que la entidad ya realizó hoy.</param>
    /// <param name="cuotaDiaria">Cuota máxima configurada para la entidad.</param>
    /// <returns>true si la entidad puede realizar una consulta adicional.</returns>
    public static bool EvaluarCuota(int consultasRealizadasHoy, int cuotaDiaria)
    {
        if (cuotaDiaria <= 0)
        {
            // Cuota configurada en 0 (o inválida) significa que la entidad no tiene consultas permitidas.
            return false;
        }

        return consultasRealizadasHoy < cuotaDiaria;
    }
}
