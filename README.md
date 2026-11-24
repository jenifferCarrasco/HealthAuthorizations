# HealthAuthorization

API REST para registrar, consultar, modificar e inactivar autorizaciones de servicios de salud. Cada autorización contiene servicios con montos reclamados. La API calcula la distribución 70/30, conserva el historial y protege la creación frente a reintentos duplicados.

## Funcionalidades y tecnologías

- Autorizaciones con uno o varios servicios, consultas paginadas por afiliado, contratante, intermediario, fechas y estado, y notificaciones SignalR al crear o cambiar el estado.
- Seis catálogos con datos iniciales, búsqueda y eliminación lógica. Auditoría por usuario y concurrencia optimista con SQL Server.
- .NET 10, ASP.NET Core Web API, EF Core 10, SQL Server, MediatR, FluentValidation, Mapster, JWT Bearer, OpenAPI, Swagger UI, xUnit, Coverlet y Docker Compose.

| Proyecto | Responsabilidad |
|---|---|
| `src/HealthAuthorization.Domain` | Entidades y política de cobertura. |
| `src/HealthAuthorization.Application` | Comandos, consultas, validación, contratos y reglas de aplicación. |
| `src/HealthAuthorization.Infrastructure` | EF Core, SQL Server, repositorio, auditoría y migraciones. |
| `src/HealthAuthorization.API` | HTTP, autenticación, errores, OpenAPI y SignalR. |
| `tests/HealthAuthorization.UnitTests` | Dominio y aplicación con dobles de persistencia. |
| `tests/HealthAuthorization.IntegrationTests` | HTTP, seguridad, EF Core y SQL Server aislado. |

## Ejecutar con Visual Studio y SQL Server local

Necesitas Visual Studio con soporte para .NET 10 y la carga de trabajo ASP.NET, el SDK de .NET 10 y una instancia de SQL Server o SQL Server Express. Clona el repositorio y abre `HealthAuthorization.slnx`. Desde su raíz, configura la conexión de desarrollo con User Secrets. Ajusta el nombre de la instancia si usas otra.

~~~powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet user-secrets set 'ConnectionStrings:HealthAuthorization' 'Server=localhost\SQLEXPRESS;Database=HealthAuthorizationDb;Integrated Security=true;Encrypt=true;TrustServerCertificate=true' --project src/HealthAuthorization.API
dotnet ef database update --project src/HealthAuthorization.Infrastructure --startup-project src/HealthAuthorization.API --context HealthAuthorizationDbContext
~~~

Selecciona `HealthAuthorization.API` como proyecto de inicio. Ejecuta el perfil HTTP en Visual Studio o usa `dotnet run --project src/HealthAuthorization.API`. Abre [Swagger](http://localhost:5044/swagger). La opción `TrustServerCertificate=true` se usa aquí para desarrollo local.

## Ejecutar con Docker Compose

Necesitas Docker Desktop con Compose v2 y contenedores Linux para `linux/amd64`. Las imágenes incluyen el runtime y el SDK necesarios. Copia el archivo de ejemplo y configura una contraseña propia para `MSSQL_SA_PASSWORD`.

~~~powershell
Copy-Item .env.example .env
docker compose config --quiet
docker compose up --build -d
docker compose ps -a
docker compose logs migrate
~~~

SQL Server debe aparecer como `healthy` y el servicio `migrate` debe terminar con código `0`. Compose aplica las migraciones antes de iniciar la API. Publica Swagger en [http://localhost:5044/swagger](http://localhost:5044/swagger) y mantiene SQL Server en la red interna. Usa `docker compose stop` para detener el entorno conservando sus volúmenes.

| Variable | Uso |
|---|---|
| `MSSQL_SA_PASSWORD` | Contraseña de inicialización de SQL Server en Compose. |
| `API_PORT` | Puerto local de Swagger, 5044 por defecto. |
| `ConnectionStrings__HealthAuthorization` | Alternativa a User Secrets para inyectar la conexión. Compose la configura para su SQL Server. |
| `ASPNETCORE_ENVIRONMENT` | Usa `Development` en el entorno de demostración. |
| `Jwt__Authority`, `Jwt__Audience`, `Jwt__RoleClaimType` | Autoridad OIDC HTTPS, audiencia y nombre opcional del claim de roles en producción. |

## Autenticación en Swagger

Genera ambos tokens desde la raíz. Con Visual Studio, usa los comandos siguientes. En Docker, sustituye el prefijo `dotnet` de cada comando por `docker compose run --rm --no-deps migrate` y después ejecuta `docker compose restart api`. Mantén la audiencia 5044 aunque cambies el puerto de acceso en Compose.

~~~powershell
dotnet user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name admin-local --claim "healthauthorization_user_id=11111111-1111-4111-8111-111111111111" --role Administrator --valid-for 1h --output token
dotnet user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name operator-local --claim "healthauthorization_user_id=22222222-2222-4222-8222-222222222222" --role Operator --valid-for 1h --output token
~~~

En Swagger, pulsa **Authorize** y pega el token. Si la API local ya se estaba ejecutando, reiníciala para cargar la clave. Los tokens de desarrollo se validan solo en `Development`. En producción, el proveedor OIDC externo debe emitir exactamente un claim personalizado `healthauthorization_user_id` con un GUID válido, estable y distinto de cero.

| Operación | Administrator | Operator |
|---|:---:|:---:|
| Consultar catálogos y autorizaciones | Sí | Sí |
| Crear y actualizar autorizaciones | Sí | Sí |
| Escribir catálogos y cambiar estado de autorizaciones | Sí | |
| Conectarse al Hub | Sí | Sí |

## Crear una autorización de ejemplo

La migración inicial carga 16 registros activos: un afiliado `DEMO-001`, un contratante, un intermediario, cuatro prestadores, cuatro tipos de prestador y cinco tipos de servicio. Las autorizaciones y los detalles se crean por la API. Los catálogos se consultan en sus endpoints.

Envía `POST /api/authorizations` con tu token y el encabezado `Idempotency-Key`. Para crear una nueva operación usa un GUID, por ejemplo con `[guid]::NewGuid()`.

~~~json
{
  "affiliateId": "000000cb-0000-0000-0000-000000000000",
  "contractorId": "000000c9-0000-0000-0000-000000000000",
  "intermediaryId": "000000ca-0000-0000-0000-000000000000",
  "services": [
    {
      "providerId": "00000065-0000-0000-0000-000000000000",
      "serviceTypeId": "0000000b-0000-0000-0000-000000000000",
      "claimedAmount": 100
    }
  ]
}
~~~

La respuesta `201 Created` incluye el ID y la cabecera `Location`. Sigue esa ruta para comprobar 100 reclamados, 70 de la ARS y 30 del afiliado. Reenviar la misma solicitud, clave y usuario devuelve el ID original. Cambiar el contenido con la misma clave devuelve `409`.

## Pruebas y resultados

~~~powershell
dotnet build HealthAuthorization.slnx -c Release
dotnet test HealthAuthorization.slnx -c Release --no-build
~~~

Las pruebas SQL Server se habilitan con el script siguiente. Requiere PowerShell 7, el SDK y Docker Desktop. Usa un contenedor distinto y una base exclusiva de pruebas.

~~~powershell
dotnet tool restore
$stamp = Get-Date -Format yyyyMMddHHmmss
$coverage = "artifacts/coverage/delivery-$stamp"
.\tests\Run-SqlServerTests.ps1 -CoverageDirectory $coverage
.\tests\Measure-Coverage.ps1 $coverage
~~~

La validación completa obtuvo 122 pruebas aprobadas y superó el 80 % de cobertura de líneas. La duplicación de producción sin migraciones quedó en 1,39 %, por debajo del objetivo del 3 %. La cobertura final de líneas fue 90,98 % sin migraciones y 95,04 % incluyéndolas. La cobertura de ramas fue 67,34 %. La medición con 30.000 autorizaciones sintéticas registró 320 respuestas exitosas y un P95 de 1.125,22 ms en la página 1500. Consulta el alcance, el método de medición y las limitaciones en el informe de validación.

## Documentación

- [Arquitectura, modelo y decisiones](docs/arquitectura.md)
- [Contratos y reglas de negocio](docs/api.md)
- [Matriz de cumplimiento y validación](docs/validacion.md)
- [Guía manual de Swagger](tests/Etapa11E-Swagger.md)
- [Pipeline de Azure DevOps](azure-pipelines.yml) y [plantilla declarativa de despliegue](gitops/README.md)
