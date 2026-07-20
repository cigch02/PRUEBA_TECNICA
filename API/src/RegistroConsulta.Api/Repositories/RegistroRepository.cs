using Microsoft.EntityFrameworkCore;
using RegistroConsulta.Api.Data;
using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public class RegistroRepository : IRegistroRepository
{
    private readonly AppDbContext _context;

    public RegistroRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RegistroCivil?> BuscarAsync(string identificador, string nombre, CancellationToken ct = default)
    {
        var identificadorNormalizado = identificador.Trim();
        var nombreNormalizado = nombre.Trim();

        // Comparación case-insensitive del nombre a nivel de servidor SQL.
        return await _context.Registros
            .AsNoTracking()
            .Where(r => r.Identificador == identificadorNormalizado
                        && r.Nombre.ToUpper() == nombreNormalizado.ToUpper())
            .FirstOrDefaultAsync(ct);
    }
}
