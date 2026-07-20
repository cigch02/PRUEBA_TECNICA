using Microsoft.EntityFrameworkCore;
using RegistroConsulta.Api.Data;
using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public class LogConsultaRepository : ILogConsultaRepository
{
    private readonly AppDbContext _context;

    public LogConsultaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(LogConsulta log, CancellationToken ct = default)
    {
        _context.Logs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> ContarConsultasDelDiaAsync(int entidadId, DateTime inicioDia, DateTime finDia, CancellationToken ct = default)
    {
        // Solo cuentan para la cuota los intentos que efectivamente llegaron a
        // ejecutar la consulta contra el repositorio de registros (Aprobado),
        // es decir, los que pasaron autenticación, validación y la propia
        // verificación de cuota. Los rechazos por 401/403/400/429/500 no consumen cuota.
        return await _context.Logs
            .AsNoTracking()
            .Where(l => l.EntidadId == entidadId
                        && l.Resultado == ResultadoConsulta.Aprobado
                        && l.FechaHora >= inicioDia
                        && l.FechaHora < finDia)
            .CountAsync(ct);
    }
}
