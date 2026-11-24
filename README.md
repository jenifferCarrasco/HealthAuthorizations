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

La API inicia con el perfil HTTP local. En desarrollo, Swagger UI está disponible en `/swagger`.

## Ejecutar con Docker Desktop

Usa Docker Compose v2 y contenedores Linux. La imagen de SQL Server utiliza `linux/amd64`. La configuración es para una demostración local. Solo publica la API en la interfaz de loopback del anfitrión y SQL Server queda en la red interna de Compose.

Desde la raíz de la solución, crea tu archivo local de configuración:

```powershell
Copy-Item .env.example .env
```

Edita `.env` y asigna una contraseña propia a `MSSQL_SA_PASSWORD`. Si Visual Studio ya usa el puerto 5044, cambia `API_PORT` a un puerto libre. Después ejecuta:

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps -a
docker compose logs migrate
```

Compose espera a que SQL Server responda, ejecuta las migraciones existentes con el servicio `migrate` y arranca la API si ese servicio termina con código `0`. Las migraciones crean `HealthAuthorizationDb` en el SQL Server del contenedor y cargan los catálogos de demostración. El volumen `sql-data` conserva los datos. La instancia SQL Server Express configurada para Visual Studio sigue disponible por separado. La contraseña inicial de `sa` se establece al inicializar el volumen de SQL Server.

Abre Swagger en [http://localhost:5044/swagger](http://localhost:5044/swagger), o usa el puerto que configuraste. Para generar los tokens, usa el SDK incluido en la imagen de `migrate`. Estos comandos funcionan sin instalar .NET en el anfitrión:

```powershell
docker compose run --rm --no-deps migrate user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name admin-local --claim "healthauthorization_user_id=11111111-1111-4111-8111-111111111111" --role Administrator --valid-for 1h --output token
docker compose run --rm --no-deps migrate user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name operator-local --claim "healthauthorization_user_id=22222222-2222-4222-8222-222222222222" --role Operator --valid-for 1h --output token
docker compose restart api
```

Conserva la audiencia `http://localhost:5044` en esos comandos aunque cambies el puerto de acceso a Swagger. La audiencia identifica al destinatario del token y la API usa ese valor para validarlo. La clave se guarda en el volumen `jwt-secrets` y la API lo monta en modo de solo lectura. En Swagger, pulsa **Authorize**, pega el token sin el prefijo `Bearer` y prueba `GET /api/authorizations`. Para crear una autorización, envía también un GUID en `Idempotency-Key`.

Para detener el entorno y conservar la base y la clave local, ejecuta `docker compose down`. La configuración de Compose utiliza `Development`. Un despliegue de producción debe utilizar la configuración del proveedor externo descrita más abajo.

## Autenticación para pruebas locales

Desde la raíz de la solución, genera un token para cada rol. Los GUID de ejemplo identifican de forma estable a cada usuario de prueba.

```powershell
dotnet user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name admin-local --claim "healthauthorization_user_id=11111111-1111-4111-8111-111111111111" --role Administrator --valid-for 1h --output token
dotnet user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name operator-local --claim "healthauthorization_user_id=22222222-2222-4222-8222-222222222222" --role Operator --valid-for 1h --output token
```

En Swagger, abre **Authorize** y pega el token sin el prefijo `Bearer`. Con un token de `Operator` puedes consultar los catálogos y consultar, crear y actualizar autorizaciones. Las escrituras de catálogos y los cambios de estado de autorizaciones requieren `Administrator`. Un token ausente o inválido devuelve `401` y un usuario autenticado sin permiso recibe `403`.

`dotnet user-jwts` guarda la clave local en User Secrets. Los tokens y las claves deben permanecer fuera del repositorio. El esquema `DevelopmentJwt` solo se registra en `Development`.

## Proveedor de identidad externo

En producción, configura `Jwt__Authority` con la autoridad OIDC sobre HTTPS y `Jwt__Audience` con la audiencia de los tokens de acceso de esta API. El claim de roles predeterminado es `roles`. Si el emisor usa otro nombre, configura `Jwt__RoleClaimType`. El proveedor debe emitir los valores de rol `Administrator` y `Operator` en el claim configurado.

`healthauthorization_user_id` es un claim **personalizado**. El proveedor externo debe incluir exactamente un valor que sea un GUID estable, distinto de cero y único entre las identidades admitidas. La API usa ese valor para auditoría e idempotencia. Las claves de idempotencia históricas del alcance anónimo se conservan. Las solicitudes autenticadas usan el alcance de su propio usuario.

## Notificaciones de autorizaciones

Los clientes SignalR pueden conectarse a `/hubs/authorizations` y suscribirse a `AuthorizationCreated` y `AuthorizationStatusChanged` antes de iniciar la conexión. Los eventos incluyen el identificador de la autorización y la fecha UTC. El evento de cambio de estado incluye también `isActive`.

El Hub admite conexiones autenticadas con rol `Administrator` u `Operator` y las cierra al expirar su token. Los clientes pueden pasar el token mediante `accessTokenFactory` en JavaScript o `AccessTokenProvider` en .NET. El servidor admite `access_token` en la query string solo para los transportes WebSocket y Server-Sent Events de la ruta del Hub. Usa HTTPS en producción y evita registrar ese parámetro en proxies o infraestructura.

Las notificaciones se emiten después de guardar el cambio por REST y se difunden a todos los clientes admitidos por el Hub. Un fallo del envío se registra y la respuesta REST conserva el resultado del guardado. Los clientes desconectados no reciben los eventos anteriores al reconectarse. La implementación no persiste ni reintenta las notificaciones.
