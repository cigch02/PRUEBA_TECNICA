using System.ComponentModel.DataAnnotations;

namespace RegistroConsulta.Api.Models;

/// <summary>
/// Body de entrada de POST /api/registros/consulta.
/// La entidad que consulta se identifica mediante 'entidadId' (ya no API Key).
/// La búsqueda del registro requiere 'identificador' y 'nombre' juntos:
/// no se permite buscar solo por identificador.
/// </summary>
public class ConsultaRequest
{
    [Required(ErrorMessage = "El campo 'entidadId' es requerido.")]
    public int? EntidadId { get; set; }

    [Required(ErrorMessage = "El campo 'identificador' es requerido.")]
    public string? Identificador { get; set; }

    [Required(ErrorMessage = "El campo 'nombre' es requerido.")]
    public string? Nombre { get; set; }
}
