using RegistroConsulta.Api.Models;

namespace RegistroConsulta.Api.Services;

public interface IRegistroConsultaService
{
    /// <summary>
    /// Orquesta las validaciones (1)-(5) descritas en la lógica de negocio y
    /// devuelve un resultado que el Controller mapea a la respuesta HTTP correspondiente.
    /// </summary>
    Task<ConsultaResult> ConsultarAsync(ConsultaRequest request, CancellationToken ct = default);
}
