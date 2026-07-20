using Microsoft.EntityFrameworkCore;
using RegistroConsulta.Api.Data;
using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public class EntidadRepository : IEntidadRepository
{
    private readonly AppDbContext _context;

    public EntidadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Entidad?> ObtenerPorIdAsync(int entidadId, CancellationToken ct = default)
    {
        return await _context.Entidades
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entidadId, ct);
    }
}
