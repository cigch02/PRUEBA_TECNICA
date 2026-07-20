using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Repositories;

public interface IRegistroRepository
{
    /// <summary>
    /// Busca un registro por identificador y nombre (ambos son obligatorios en la regla de negocio).
    /// </summary>
    Task<RegistroCivil?> BuscarAsync(string identificador, string nombre, CancellationToken ct = default);
}
