using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public interface ILogConsultaRepository
{
    Task RegistrarAsync(LogConsulta log, CancellationToken ct = default);

    /// <summary>
    /// Cuenta cuántas consultas aprobadas ha hecho una entidad en el día (rango [inicioDia, finDia)).
    /// Usado por la validación de cuota diaria.
    /// </summary>
    Task<int> ContarConsultasDelDiaAsync(int entidadId, DateTime inicioDia, DateTime finDia, CancellationToken ct = default);
}
