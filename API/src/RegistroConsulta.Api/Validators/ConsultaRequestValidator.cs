using FluentValidation;
using RegistroConsulta.Api.Models;

namespace RegistroConsulta.Api.Validators;

/// <summary>
/// 'entidadId' identifica a quién consulta.
/// 'identificador' y 'nombre' son ambos requeridos para la búsqueda del registro:
/// no se permite buscar solo por identificador.
/// </summary>
public class ConsultaRequestValidator : AbstractValidator<ConsultaRequest>
{
    public ConsultaRequestValidator()
    {
        RuleFor(x => x.EntidadId)
            .NotNull()
            .WithMessage("El campo 'entidadId' es requerido.")
            .GreaterThan(0)
            .WithMessage("El campo 'entidadId' debe ser un identificador válido.");

        RuleFor(x => x.Identificador)
            .NotEmpty()
            .WithMessage("El campo 'identificador' es requerido.")
            .MaximumLength(50);

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .WithMessage("El campo 'nombre' es requerido. No se permite búsqueda solo por identificador.")
            .MaximumLength(200);
    }
}
