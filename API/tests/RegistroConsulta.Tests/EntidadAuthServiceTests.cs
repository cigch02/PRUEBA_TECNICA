using Moq;
using RegistroConsulta.Api.Entities;
using RegistroConsulta.Api.Repositories;
using RegistroConsulta.Api.Services;
using Xunit;

namespace RegistroConsulta.Tests;

public class EntidadAuthServiceTests
{
    private static Entidad CrearEntidadVigente(int id = 1) => new()
    {
        Id = id,
        Nombre = "Entidad de prueba",
        ConvenioActivo = true,
        ConvenioVigenciaInicio = new DateTime(2026, 1, 1),
        ConvenioVigenciaFin = new DateTime(2026, 12, 31),
        CuotaDiaria = 100
    };

    [Fact]
    public async Task ValidarAsync_EntidadIdNulo_RetornaEntidadNoEncontrada()
    {
        var repoMock = new Mock<IEntidadRepository>();
        var service = new EntidadAuthService(repoMock.Object, new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        var resultado = await service.ValidarAsync(null);

        Assert.Equal(EntidadAuthStatus.EntidadNoEncontrada, resultado.Status);
        repoMock.Verify(r => r.ObtenerPorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidarAsync_EntidadIdNoExiste_RetornaEntidadNoEncontrada()
    {
        var repoMock = new Mock<IEntidadRepository>();
        repoMock.Setup(r => r.ObtenerPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entidad?)null);

        var service = new EntidadAuthService(repoMock.Object, new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        var resultado = await service.ValidarAsync(99);

        Assert.Equal(EntidadAuthStatus.EntidadNoEncontrada, resultado.Status);
    }

    [Fact]
    public async Task ValidarAsync_ConvenioFueraDeVigencia_RetornaConvenioInactivo()
    {
        var entidad = CrearEntidadVigente();
        var repoMock = new Mock<IEntidadRepository>();
        repoMock.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entidad);

        // "Ahora" cae fuera del rango de vigencia (2027, cuando el convenio venció en 2026-12-31).
        var service = new EntidadAuthService(repoMock.Object, new FixedTimeProvider(new DateTimeOffset(2027, 1, 15, 0, 0, 0, TimeSpan.Zero)));

        var resultado = await service.ValidarAsync(1);

        Assert.Equal(EntidadAuthStatus.ConvenioInactivo, resultado.Status);
    }

    [Fact]
    public async Task ValidarAsync_ConvenioActivoYVigente_RetornaOk()
    {
        var entidad = CrearEntidadVigente();
        var repoMock = new Mock<IEntidadRepository>();
        repoMock.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entidad);

        var service = new EntidadAuthService(repoMock.Object, new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

        var resultado = await service.ValidarAsync(1);

        Assert.Equal(EntidadAuthStatus.Ok, resultado.Status);
        Assert.NotNull(resultado.Entidad);
    }
}
