# Arquitectura y decisiones

## Dependencias

~~~mermaid
flowchart LR
    C[Cliente HTTP o SignalR] --> API[ASP.NET Core API]
    API --> APP[Application]
    API --> INF[Infrastructure]
    INF --> APP
    INF --> DOM[Domain]
    APP --> DOM
    INF --> SQL[(SQL Server)]
~~~

- **Domain** contiene entidades, contratos comunes de auditoría y estado, y la política monetaria. No depende de EF Core.
- **Application** define casos de uso y contratos de persistencia. MediatR separa consultas y comandos mediante CQRS dentro del mismo proceso. `ValidationBehavior` ejecuta los validadores FluentValidation antes del handler.
- **Infrastructure** implementa el repositorio genérico EF Core y Unit of Work. La escritura del agregado se guarda en una sola unidad. Las consultas proyectan con LINQ, cuentan y paginan en SQL y usan seguimiento solo al cargar para escritura.
- **API** autentica y autoriza, valida el encabezado de idempotencia con un filtro, envía solicitudes a MediatR y devuelve resultados HTTP. El middleware de excepciones usa `ApiExceptionHandler` y ProblemDetails. Los errores inesperados se registran con identificador de solicitud y se responden de forma genérica.
- **Mapster** se registra como `IMapper` y configura explícitamente los campos editables de cinco catálogos. Los updates operan sobre la entidad seguida por EF. Affiliate conserva su escritor, que reutiliza valores normalizados al validar unicidad.

## Modelo relacional

~~~mermaid
erDiagram
    CONTRACTOR o|--o{ AFFILIATE : vincula
    AFFILIATE ||--o{ AUTHORIZATION : solicita
    CONTRACTOR o|--o{ AUTHORIZATION : participa
    INTERMEDIARY o|--o{ AUTHORIZATION : intermedia
    AUTHORIZATION ||--o{ AUTHORIZATION_DETAIL : contiene
    PROVIDER ||--o{ AUTHORIZATION_DETAIL : presta
    SERVICE_TYPE ||--o{ AUTHORIZATION_DETAIL : clasifica
    PROVIDER_TYPE ||--o{ PROVIDER : clasifica
~~~

Las ocho entidades usan claves GUID. La cardinalidad mínima de un detalle activo se exige en aplicación antes del guardado. Los montos se guardan como `decimal(19,2)` con restricciones de no negatividad y suma. SQL Server protege las claves foráneas y la unicidad del número de afiliado. La identificación del afiliado admite nulo y cuenta con un índice único filtrado. `DocumentType` vale 0 si falta identificación y admite Cédula, Pasaporte o RNC cuando existe.

El contexto asigna fechas UTC y el GUID autenticado a la auditoría. Los catálogos y los detalles con eliminación lógica se excluyen de consultas ordinarias. Las autorizaciones inactivas siguen consultables. El aviso 10622 de EF Core advierte que una futura consulta con un `INNER JOIN` a Affiliate puede perder resultados si el afiliado se filtra. Las proyecciones actuales se probaron por ID y listado con un afiliado eliminado.

## Integridad e idempotencia

`AuthorizationWriter` valida las referencias incorporadas o reemplazadas y prepara todos los cambios antes de mutar el agregado. Las referencias históricas conservadas pueden seguir vinculadas. `CoveragePolicy` redondea la parte ARS a dos decimales y asigna al afiliado el remanente. Los detalles omitidos de una actualización se eliminan lógicamente. `SaveChangesAsync` persiste el conjunto de cambios y SQL Server revierte la operación si una restricción falla.

La clave de creación se limita al GUID autenticado. `AuthorizationRequestHash` genera SHA-256 a partir de la solicitud original normalizada. Si coinciden clave, alcance y huella, se devuelve el ID existente aun si cambió posteriormente el agregado. Una huella distinta produce 409. El índice único filtrado arbitra carreras. La unidad de trabajo distingue la violación de ese índice, desacopla las entradas fallidas correspondientes y el handler recupera la fila ganadora. Solo la creación persistida notifica.

SQL Server genera `RowVersion` para Affiliate y Authorization. EF confronta la versión original al escribir y la unidad de trabajo traduce el conflicto a HTTP 409. Cambiar solo detalles también fuerza la escritura de la raíz. El contrato actual detecta carreras entre escrituras con contextos distintos. Una edición enviada tras una lectura anterior puede sobrescribir cambios si la versión del cliente nunca se envía.

## Seguridad y notificaciones

JWT Bearer valida firma, emisor, audiencia y vencimiento. La autenticación precede a la autorización. En producción se requiere autoridad OIDC HTTPS y algoritmos asimétricos permitidos. Desarrollo usa su propio esquema y User Secrets. La API valida y consume exactamente un GUID autenticado para auditoría e idempotencia.

SignalR difunde los cambios persistidos a conexiones autorizadas. El payload contiene ID, fecha y, cuando corresponde, estado. La lectura de tokens de query string se limita a WebSocket y SSE en la ruta del Hub. Un fallo de notificación se registra tras el guardado. La aplicación no ofrece entrega durable ni recuperación de eventos perdidos.

## Despliegue y decisiones de alcance

Docker usa imágenes de ASP.NET y SDK .NET 10 en etapas separadas. Compose espera el healthcheck de SQL Server, ejecuta el servicio de migración y arranca la API al finalizar. Los datos y secretos locales se guardan en volúmenes independientes. Las dos migraciones crean el esquema con datos de demostración y agregan las columnas, la restricción y el índice de idempotencia.

Se eligió EF Core frente a Dapper y Mapster frente a AutoMapper. SignalR cubre la integración inmediata sin una cola adicional. RabbitMQ, MongoDB, Redis y OpenTelemetry se dejaron fuera del alcance. Kubernetes y SonarQube quedaron fuera del alcance. Se incluyen un pipeline de Azure DevOps y una estructura declarativa mínima de GitOps por la discrepancia entre los apartados 5 y 9. La [guía de despliegue](../gitops/README.md) explica cómo configurarlos, promover y revertir versiones. Azure DevOps y la reconciliación de despliegues no se ejecutaron en infraestructura real. ETag e If-Match quedaron fuera por acuerdo. Si los resultados justifican mejoras futuras, pueden evaluarse paginación profunda, entrega durable u observabilidad adicional como cambios independientes.
