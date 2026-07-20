using Moq;
using RegistroConsulta.Api.Entities;
using RegistroConsulta.Api.Repositories;
using RegistroConsulta.Api.Services;
using Xunit;

namespace RegistroConsulta.Tests;

/// <summary>
/// TimeProvider fijo para poder controlar "ahora" en los tests sin depender del reloj real.
/// </summary>
public class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;

    public FixedTimeProvider(DateTimeOffset fixedUtcNow)
    {
        _fixedUtcNow = fixedUtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;
}

public class QuotaServiceTests
{
    // ---------------------------------------------------------------
    // Tests de la lógica pura EvaluarCuota (sin BD, sin mocks de tiempo)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(0, 100, true)]   // no ha consultado nada, cuota amplia -> puede consultar
    [InlineData(50, 100, true)]  // está por debajo de la cuota -> puede consultar
    [InlineData(99, 100, true)]  // justo un intento antes del límite -> puede consultar
    [InlineData(100, 100, false)] // llegó exactamente a la cuota -> NO puede consultar más
    [InlineData(101, 100, false)] // ya la superó -> NO puede consultar
    public void EvaluarCuota_RespetaElLimiteConfigurado(int consultasHoy, int cuotaDiaria, bool esperado)
    {
        var resultado = QuotaService.EvaluarCuota(consultasHoy, cuotaDiaria);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void EvaluarCuota_CuotaCero_SiempreRechaza()
    {
        var resultado = QuotaService.EvaluarCuota(consultasRealizadasHoy: 0, cuotaDiaria: 0);

        Assert.False(resultado);
    }

    [Fact]
    public void EvaluarCuota_CuotaNegativa_SiempreRechaza()
    {
        var resultado = QuotaService.EvaluarCuota(consultasRealizadasHoy: 0, cuotaDiaria: -5);

        Assert.False(resultado);
    }

    // ---------------------------------------------------------------
    // Tests de PuedeConsultarAsync (integra con el repositorio vía mock)
    // ---------------------------------------------------------------

    [Fact]
    public async Task PuedeConsultarAsync_DebeoValidarSoloElRangoDelDiaActual()
    {
        // Arrange
        var ahora = new DateTimeOffset(2026, 7, 19, 15, 30, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(ahora);

        var entidad = new Entidad { Id = 7, CuotaDiaria = 10 };

        var logRepoMock = new Mock<ILogConsultaRepository>();
        logRepoMock
            .Setup(r => r.ContarConsultasDelDiaAsync(
                entidad.Id,
                new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Unspecified),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);

        var service = new QuotaService(logRepoMock.Object, timeProvider);

        // Act
        var puedeConsultar = await service.PuedeConsultarAsync(entidad);

        // Assert
        Assert.True(puedeConsultar);
        logRepoMock.Verify(r => r.ContarConsultasDelDiaAsync(
            entidad.Id,
            new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified),
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Unspecified),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PuedeConsultarAsync_CuandoYaAlcanzoLaCuota_RetornaFalse()
    {
        // Arrange
        var ahora = new DateTimeOffset(2026, 7, 19, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(ahora);

        var entidad = new Entidad { Id = 3, CuotaDiaria = 5 };

        var logRepoMock = new Mock<ILogConsultaRepository>();
        logRepoMock
            .Setup(r => r.ContarConsultasDelDiaAsync(
                entidad.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // ya consumió toda la cuota

        var service = new QuotaService(logRepoMock.Object, timeProvider);

        // Act
        var puedeConsultar = await service.PuedeConsultarAsync(entidad);

        // Assert
        Assert.False(puedeConsultar);
    }
}
