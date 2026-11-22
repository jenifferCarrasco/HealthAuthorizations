# HealthAuthorization

Solución .NET 10 para desarrollar una API REST que gestione autorizaciones de servicios de salud. La etapa inicial establece la estructura y las dependencias de Clean Architecture.

| Proyecto | Responsabilidad |
| --- | --- |
| `src/HealthAuthorization.API` | Hospedar la API ASP.NET Core y componer sus dependencias. |
| `src/HealthAuthorization.Application` | Alojar los casos de uso y los contratos de aplicación. |
| `src/HealthAuthorization.Domain` | Alojar el modelo y las reglas de negocio. |
| `src/HealthAuthorization.Infrastructure` | Alojar las implementaciones técnicas de los contratos de aplicación. |
| `tests/HealthAuthorization.UnitTests` | Alojar pruebas unitarias del dominio y la aplicación con xUnit. |
| `tests/HealthAuthorization.IntegrationTests` | Alojar pruebas de integración de la API con xUnit. |

## Desarrollo local

Instala el SDK de .NET 10 y ejecuta estos comandos desde la carpeta que contiene `HealthAuthorization.slnx`.

```sh
dotnet restore
dotnet build
dotnet test
dotnet run --project src/HealthAuthorization.API
```

La API inicia con el perfil HTTP local. Los endpoints y los casos de prueba se incorporarán en etapas posteriores.
