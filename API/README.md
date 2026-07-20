# Registro Consulta API

API REST (.NET 8 / ASP.NET Core) que implementa `POST /api/registros/consulta`.

## Contrato actual

La entidad que consulta se identifica mediante
`entidadId` en el body. La búsqueda del registro requiere `identificador` y
`nombre` juntos (no se permite buscar solo por identificador).

## Mapeo de la lógica de negocio a la implementación

| Paso | Dónde vive | Código de error si falla |
|------|-----------|---------------------------|
| (1) entidadId válido + convenio activo/vigente | `EntidadAuthService` | 401 (entidadId no existe) / 403 (convenio inactivo o fuera de vigencia) |
| (2) entidadId, identificador y nombre requeridos | `ConsultaRequestValidator` (FluentValidation) | 400 |
| (3) Cuota diaria por entidad | `QuotaService` | 429 |
| (4) Consulta al repositorio, por identificador y nombre | `RegistroRepository` | 404 si no hay match, 200 con los datos si existe |
| (5) Log de cada intento | `LogConsultaRepository`, invocado desde `RegistroConsultaService` en cada rama | — |
| Errores no previstos | `try/catch` en el Service + middleware global en `Program.cs` | 500 |

El `Controller` no contiene lógica de negocio: solo traduce el `ConsultaResultStatus`
devuelto por el Service a la respuesta HTTP.

