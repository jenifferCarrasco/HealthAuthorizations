# Etapa 11E · Pruebas manuales en Swagger

## 1. Levantar un entorno separado

En PowerShell, abre la raíz de la solución. Usa un nombre de proyecto Compose nuevo y un puerto distinto del entorno de trabajo. Compose creará volúmenes propios para ese proyecto. Dentro del contenedor la base seguirá llamándose `HealthAuthorizationDb`, en una instancia SQL Server separada.

~~~powershell
$env:API_PORT = '5045'
docker compose -p healthauthorization-11e-manual up --build -d
docker compose -p healthauthorization-11e-manual ps -a
docker compose -p healthauthorization-11e-manual logs migrate
~~~

Utiliza la configuración local de `.env` para la contraseña de SQL Server. Si aún no existe, copia `.env.example` a `.env` y escribe una contraseña solo en ese archivo local. Espera a que SQL aparezca como `healthy`, el servicio `migrate` termine con código 0 y la API aparezca como `Up`.

## 2. Crear los tokens

~~~powershell
docker compose -p healthauthorization-11e-manual run --rm --no-deps migrate user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name admin-local --claim "healthauthorization_user_id=11111111-1111-4111-8111-111111111111" --role Administrator --valid-for 1h --output token

docker compose -p healthauthorization-11e-manual run --rm --no-deps migrate user-jwts create --project src/HealthAuthorization.API --scheme DevelopmentJwt --issuer dotnet-user-jwts --audience http://localhost:5044 --name operator-local --claim "healthauthorization_user_id=22222222-2222-4222-8222-222222222222" --role Operator --valid-for 1h --output token

docker compose -p healthauthorization-11e-manual restart api
~~~

Conserva la audiencia del proyecto, aunque Swagger se publique en el puerto 5045. Abre [Swagger](http://localhost:5045/swagger/index.html). Pulsa **Authorize** y pega únicamente el token. Guarda ambos tokens localmente mientras dure la prueba.

## 3. Probar creación, cobertura y reintentos

1. Como **Administrator**, ejecuta `POST /api/service-types` con el JSON siguiente. Espera `201` y guarda el ID obtenido.

   ~~~json
   {"name": "Consulta para validar inactivación", "description": "Catálogo de prueba manual"}
   ~~~

2. Ejecuta `POST /api/authorizations` como **Administrator** u **Operator**. En `Idempotency-Key`, introduce un GUID nuevo obtenido con `[guid]::NewGuid()`. Sustituye el primer tipo de servicio por el ID del paso anterior. Los demás identificadores corresponden a datos de demostración.

   ~~~json
   {
     "affiliateId": "000000cb-0000-0000-0000-000000000000",
     "services": [
       {
         "providerId": "00000065-0000-0000-0000-000000000000",
         "serviceTypeId": "SUSTITUIR_POR_EL_ID_NUEVO",
         "claimedAmount": 100
       },
       {
         "providerId": "00000065-0000-0000-0000-000000000000",
         "serviceTypeId": "0000000e-0000-0000-0000-000000000000",
         "claimedAmount": 50
       }
     ]
   }
   ~~~

3. Espera `201`. Guarda el ID y consulta `GET /api/authorizations/{id}`. Debes ver dos detalles y los totales 150 reclamados, 105 ARS y 45 del afiliado. Guarda ambos IDs de detalle.
4. Con el mismo token, repite el POST con la misma clave y los mismos datos. Espera `201` con el mismo ID. Repite con la misma clave y un monto distinto. Espera `409`. Quita el encabezado o envía un GUID vacío. Espera `400`.
5. Usa `GET /api/authorizations` con `affiliateId`, `pageNumber=1` y `pageSize=10`. Comprueba que el resultado incluye la autorización de prueba.

## 4. Probar un catálogo inactivo

1. Como **Administrator**, inactiva el tipo creado en `PATCH /api/service-types/{id}/status` con `{"isActive": false}`. Espera `204`.
2. Repite el POST original con el mismo usuario, cuerpo y clave. Debe devolver el ID original. Envíalo con una clave nueva y espera `404` porque el tipo está inactivo.
3. En `PUT /api/authorizations/{id}`, conserva los dos detalles originales con sus IDs y montos. Envía este formato para cada servicio y usa los datos originales.

   ~~~json
   {
     "affiliateId": "000000cb-0000-0000-0000-000000000000",
     "services": [
       {
         "detailId": "SUSTITUIR_POR_EL_ID_DEL_PRIMER_DETALLE",
         "providerId": "00000065-0000-0000-0000-000000000000",
         "serviceTypeId": "SUSTITUIR_POR_EL_ID_DEL_TIPO_INACTIVO",
         "claimedAmount": 100
       },
       {
         "detailId": "SUSTITUIR_POR_EL_ID_DEL_SEGUNDO_DETALLE",
         "providerId": "00000065-0000-0000-0000-000000000000",
         "serviceTypeId": "0000000e-0000-0000-0000-000000000000",
         "claimedAmount": 50
       }
     ]
   }
   ~~~

4. Espera `204`. Agrega un tercer detalle sin `detailId` con el tipo inactivo y espera `404`. Consulta de nuevo y confirma que los dos detalles y los montos siguen intactos.

## 5. Probar permisos y recuperación

- Sin token, solicita `GET /api/authorizations` y espera `401`.
- Con **Operator**, intenta `PATCH /api/authorizations/{id}/status` con `{"isActive": false}`. Espera `403`. Repite como **Administrator** y espera `204`.
- Reinicia primero `docker compose -p healthauthorization-11e-manual restart api`. Consulta de nuevo con el token. Después reinicia `docker compose -p healthauthorization-11e-manual restart sqlserver`, espera el estado `healthy` y repite la consulta. Deben conservarse el ID, el estado y los montos. Repite el POST original con el usuario original y su clave, y espera el mismo ID.
- Swagger permite ejecutar las operaciones HTTP. La recepción de eventos WebSocket se verifica con la prueba `ConnectedSignalRClientReceivesOnlyEffectiveChanges` del proyecto de integración.

## 6. Verificar los datos en SQL Server

Abre `sqlcmd` dentro del contenedor aislado. La contraseña se toma de su entorno.

~~~powershell
docker compose -p healthauthorization-11e-manual exec sqlserver sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d HealthAuthorizationDb'
~~~

En la consola de SQL, reemplaza el marcador por el ID obtenido en Swagger.

~~~sql
DECLARE @authorizationId uniqueidentifier = 'SUSTITUIR_POR_EL_GUID';
SELECT Id, IsActive, CreatedDate, CreatedBy, LastUpdatedDate, LastUpdatedBy,
       IdempotencyScopeId, IdempotencyKey, DATALENGTH(IdempotencyRequestHash) AS HashBytes
FROM Authorizations WHERE Id = @authorizationId;
SELECT Id, ServiceTypeId, ClaimedAmount, ArsAmount, AffiliateAmount, IsDelete
FROM AuthorizationDetails WHERE AuthorizationId = @authorizationId;
GO
~~~

Comprueba el GUID del usuario de creación, el del usuario que actualizó por última vez, la clave de idempotencia y los 32 bytes de la huella. Cada detalle debe satisfacer `ClaimedAmount = ArsAmount + AffiliateAmount`. Usa `EXIT` para salir. Puedes detener el entorno con `docker compose -p healthauthorization-11e-manual stop`.

## Aviso EF Core 10622

`Affiliate` tiene filtro global de eliminación lógica y es el extremo obligatorio de la relación con `Authorization`. Una consulta futura que cargue esa navegación con un `INNER JOIN` puede perder filas si el afiliado está eliminado. En SQL Server real ya se verificó que las proyecciones actuales por ID y listado mantienen visibles las autorizaciones históricas. Al incorporar consultas que carguen esa navegación, comprueba la visibilidad con un afiliado eliminado. Las relaciones y los filtros siguen vigentes.

## Reproducir las pruebas automatizadas

Con Docker Desktop y PowerShell 7, ejecuta desde la raíz de la solución:

~~~powershell
dotnet tool restore
$stamp = Get-Date -Format yyyyMMddHHmmss
.\tests\Run-SqlServerTests.ps1 -CoverageDirectory "artifacts/coverage/stage11e-$stamp" -PerformanceDirectory "artifacts/sqlserver/stage11e-$stamp"
~~~

Este comando inicia su propio SQL Server, usa la base HealthAuthorizationIdempotencyTests y publica un puerto solo local. Los resultados incluyen la cobertura de Coverlet, las métricas del listado, los planes reales y la comprobación de recuperación.
