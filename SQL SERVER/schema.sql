-- =================================================================
-- Script de referencia (SQL Server / T-SQL). En la práctica, generar y aplicar con:
--   dotnet ef migrations add InitialCreate -p src/RegistroConsulta.Api
--   dotnet ef database update -p src/RegistroConsulta.Api
-- =================================================================

-- -----------------------------------------------------------------
-- 12. Entidades autorizadas
-- -----------------------------------------------------------------
-- Guarda las entidades que pueden consultar, junto con los atributos
-- de su convenio: vigencia (rango de fechas), cuota diaria configurable
-- y un flag de activo/inactivo independiente de la vigencia (permite
-- suspender una entidad manualmente sin tocar las fechas del convenio).
CREATE TABLE Entidades (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    ConvenioActivo BIT NOT NULL,                 -- activo/inactivo manual
    ConvenioVigenciaInicio DATETIME2 NOT NULL,    -- inicio de vigencia del convenio
    ConvenioVigenciaFin DATETIME2 NOT NULL,       -- fin de vigencia del convenio
    CuotaDiaria INT NOT NULL                      -- máximo de consultas APROBADAS por día
);

-- -----------------------------------------------------------------
-- 11. Tabla principal de registros
-- -----------------------------------------------------------------
-- Campos de salida del endpoint: Estado, NumeroRegistro, FechaEvento,
-- FechaInscripcion. Campos de identificación para la búsqueda:
-- Identificador (p. ej. cédula) y Nombre.
CREATE TABLE Registros (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Identificador NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(200) NOT NULL,
    Estado NVARCHAR(50) NOT NULL,
    NumeroRegistro NVARCHAR(50) NOT NULL,
    FechaEvento DATETIME2 NOT NULL,
    FechaInscripcion DATETIME2 NOT NULL
);

-- -----------------------------------------------------------------
-- 14a. Índice para búsqueda individual por identificador + nombre
-- -----------------------------------------------------------------
-- El endpoint POST /api/registros/consulta busca por Identificador y
-- Nombre simultáneamente. Un índice compuesto (Identificador, Nombre)
-- permite resolver esa búsqueda con un seek en lugar de un scan.
-- Identificador va primero porque es el campo de mayor selectividad
-- (más discriminante que Nombre) y porque también podría usarse solo
-- en escenarios de búsqueda parcial.
CREATE INDEX IX_Registros_Identificador_Nombre ON Registros (Identificador, Nombre);

-- -----------------------------------------------------------------
-- 13. Log de accesos (auditoría + cálculo de cuota diaria)
-- -----------------------------------------------------------------
-- Cada consulta al endpoint genera un registro de log, se haya
-- aprobado o rechazado. EntidadId permite NULL con ON DELETE SET NULL
-- para conservar el historial de auditoría aunque la entidad se elimine
-- (los logs no deben desaparecer ni bloquear el borrado de la entidad).
-- Resultado se guarda como INT (1 = Aprobado, 2 = Rechazado) para
-- soportar cálculos agregados de forma eficiente; Motivo detalla la
-- razón (p. ej. "Fuera de vigencia", "Cuota excedida", "No encontrado").
CREATE TABLE LogsConsulta (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EntidadId INT NULL,
    IdentificadorConsultado NVARCHAR(50) NULL,
    NombreConsultado NVARCHAR(200) NULL,
    Resultado INT NOT NULL,       -- 1 = Aprobado, 2 = Rechazado
    Motivo NVARCHAR(100) NOT NULL,
    FechaHora DATETIME2 NOT NULL,
    CONSTRAINT FK_LogsConsulta_Entidades FOREIGN KEY (EntidadId)
        REFERENCES Entidades(Id) ON DELETE SET NULL
);

-- -----------------------------------------------------------------
-- 14b. Índice para conteo de consultas diarias por entidad
-- -----------------------------------------------------------------
-- El cálculo de cuota diaria (¿cuántas consultas APROBADAS lleva hoy
-- esta entidad?) filtra por EntidadId y por un rango de FechaHora
-- (el día en curso). Un índice compuesto (EntidadId, FechaHora) permite
-- ubicar rápidamente las filas de una entidad y luego recorrer solo el
-- rango de fechas relevante, sin escanear toda la tabla de logs.
CREATE INDEX IX_LogsConsulta_EntidadId_FechaHora ON LogsConsulta (EntidadId, FechaHora);

GO

-- -----------------------------------------------------------------
-- 15. Trigger: impedir un acceso APROBADO si la entidad ya superó
--     su cuota diaria configurada
-- -----------------------------------------------------------------
-- Se implementa como trigger (en vez de CHECK constraint) porque la
-- validación depende de datos agregados de OTRA tabla (COUNT de logs
-- previos del día en LogsConsulta, comparado contra CuotaDiaria en
-- Entidades). Un CHECK constraint en SQL Server no puede referenciar
-- otras tablas ni hacer agregaciones, por lo que un trigger es la
-- única forma nativa de aplicar esta regla a nivel de base de datos.
--
-- Es INSTEAD OF INSERT (no AFTER INSERT) para evitar el costo de
-- insertar la fila y luego revertirla: se valida antes de escribir.
--
-- Filas con Resultado <> 1 (Rechazado) o sin EntidadId (falla en la
-- identificación de la entidad, no aplica cuota) se insertan directo,
-- ya que la cuota diaria solo restringe accesos APROBADOS.
--
-- Se procesa fila por fila (con una tabla temporal) para soportar
-- correctamente inserciones múltiples en un mismo lote para la misma
-- entidad: cada fila nueva debe considerar las aprobaciones ya
-- contabilizadas dentro del propio lote, no solo las que ya existían
-- en la tabla antes del INSERT.
CREATE TRIGGER TRG_LogsConsulta_ValidarCuotaDiaria
ON LogsConsulta
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Filas que no requieren validación de cuota: se insertan tal cual.
    INSERT INTO LogsConsulta (EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora)
    SELECT EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora
    FROM inserted i
    WHERE i.Resultado <> 1 OR i.EntidadId IS NULL;

    -- Filas APROBADAS con entidad identificada: se validan una a una.
    DECLARE @Pendientes TABLE (
        RowNum INT IDENTITY(1,1) PRIMARY KEY,
        EntidadId INT,
        IdentificadorConsultado NVARCHAR(50),
        NombreConsultado NVARCHAR(200),
        Resultado INT,
        Motivo NVARCHAR(100),
        FechaHora DATETIME2
    );

    INSERT INTO @Pendientes (EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora)
    SELECT EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora
    FROM inserted i
    WHERE i.Resultado = 1 AND i.EntidadId IS NOT NULL
    ORDER BY i.FechaHora;

    DECLARE @i INT = 1, @max INT;
    SELECT @max = MAX(RowNum) FROM @Pendientes;

    WHILE @i <= ISNULL(@max, 0)
    BEGIN
        DECLARE @EntidadId INT, @FechaHora DATETIME2, @CuotaDiaria INT, @ConsultasHoy INT;

        SELECT @EntidadId = EntidadId, @FechaHora = FechaHora
        FROM @Pendientes WHERE RowNum = @i;

        SELECT @CuotaDiaria = CuotaDiaria FROM Entidades WHERE Id = @EntidadId;

        -- Cuenta las aprobaciones del mismo día calendario, ya insertadas
        -- en LogsConsulta (incluye las que este mismo trigger ya insertó
        -- en iteraciones previas del lote).
        SELECT @ConsultasHoy = COUNT(*)
        FROM LogsConsulta
        WHERE EntidadId = @EntidadId
          AND Resultado = 1
          AND CAST(FechaHora AS DATE) = CAST(@FechaHora AS DATE);

        IF @CuotaDiaria IS NOT NULL AND @ConsultasHoy >= @CuotaDiaria
        BEGIN
            RAISERROR(
                'La entidad %d ya superó su cuota diaria configurada (%d). No se puede registrar un nuevo acceso aprobado.',
                16, 1, @EntidadId, @CuotaDiaria
            );
            RETURN;
        END

        INSERT INTO LogsConsulta (EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora)
        SELECT EntidadId, IdentificadorConsultado, NombreConsultado, Resultado, Motivo, FechaHora
        FROM @Pendientes WHERE RowNum = @i;

        SET @i += 1;
    END
END
GO

-- =================================================================
-- Datos de ejemplo para pruebas manuales
-- =================================================================
INSERT INTO Entidades (Nombre, ConvenioActivo, ConvenioVigenciaInicio, ConvenioVigenciaFin, CuotaDiaria)
VALUES ('Entidad de Prueba', 1, '2026-01-01', '2026-12-31', 100);
-- Esta entidad quedará con Id = 1 en una tabla recién creada.

INSERT INTO Registros (Identificador, Nombre, Estado, NumeroRegistro, FechaEvento, FechaInscripcion)
VALUES ('8-123-456', 'Juan Pérez', 'Inscrito', 'REG-0001', '2020-05-10', '2020-05-15');

-- Prueba luego con:
-- POST /api/registros/consulta
-- { "entidadId": 1, "identificador": "8-123-456", "nombre": "Juan Pérez" }

-- Prueba de la validación de cuota: baja CuotaDiaria a un número pequeño
-- (p. ej. UPDATE Entidades SET CuotaDiaria = 1 WHERE Id = 1) e intenta
-- insertar dos logs con Resultado = 1 el mismo día para EntidadId = 1;
-- el segundo debe fallar con el mensaje del trigger.
