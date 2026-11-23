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

## Notificaciones de autorizaciones

Los clientes SignalR pueden conectarse a `/hubs/authorizations` y suscribirse a `AuthorizationCreated` y `AuthorizationStatusChanged` antes de iniciar la conexión. Los eventos incluyen el identificador de la autorización y la fecha UTC. El evento de cambio de estado incluye también `isActive`.

Las notificaciones se emiten después de guardar el cambio por REST y se difunden a todos los clientes conectados al Hub. Un fallo del envío se registra y la respuesta REST conserva el resultado del guardado. Los clientes desconectados no reciben los eventos anteriores al reconectarse. La implementación no persiste ni reintenta las notificaciones.
