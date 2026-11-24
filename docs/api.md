# Contratos HTTP y reglas

Todos los endpoints de negocio requieren un JWT válido con rol Administrator u Operator. En desarrollo, Swagger está en `/swagger` y el documento OpenAPI en `/openapi/v1.json`.

| Método y ruta | Acceso | Resultado |
|---|---|---|
| `POST /api/authorizations` | Ambos roles | 201 con `id` y `Location`. Requiere `Idempotency-Key` con un GUID no vacío. |
| `PUT /api/authorizations/{id}` | Ambos roles | 204 al actualizar el agregado activo. |
| `GET /api/authorizations/{id}` | Ambos roles | 200 con `summary` y `details` visibles. |
| `GET /api/authorizations` | Ambos roles | 200 con `items`, `pageNumber`, `pageSize` y `totalCount`. |
| `PATCH /api/authorizations/{id}/status` | Administrator | 204. Cuerpo `{"isActive":false}` para inactivar. La política predeterminada impide reactivar. |

La creación y actualización reciben `affiliateId` y `services` obligatorios. `contractorId` e `intermediaryId` son opcionales. Cada servicio exige `providerId`, `serviceTypeId` y `claimedAmount`. Cero es válido si se envía explícitamente. Los montos negativos o con más de dos decimales se rechazan y el total debe ajustarse a la precisión monetaria admitida.

El PUT representa el conjunto actual completo de servicios. Conserva `detailId` para modificar un detalle existente y omítelo para uno nuevo. Los detalles existentes omitidos se marcan como eliminados. Debe quedar al menos un servicio. Un detalle ajeno, repetido o eliminado no puede actualizarse. Un agregado inactivo sigue visible y sus datos dejan de admitir actualizaciones.

El listado admite `affiliateId`, `contractorId`, `intermediaryId`, `isActive`, `createdFrom` inclusivo y `createdBefore` exclusivo. Ordena por fecha descendente y después por ID descendente. La paginación admite tamaños de 1 a 100.

## Catálogos

| Ruta | Campos de entrada |
|---|---|
| `/api/affiliates` | `affiliateNumber`, `firstName`, `lastName`, `birthDate`, `identification`, `documentType`, `contractorId`. |
| `/api/contractors`, `/api/intermediaries` | `name`, `identification`. |
| `/api/providers` | `name`, `identification`, `providerTypeId`. |
| `/api/provider-types`, `/api/service-types` | `name`, `description` opcional. |

Cada catálogo admite GET paginado y GET por ID para ambos roles. Administrator puede crear con POST, modificar con PUT, cambiar `isActive` con PATCH `/{id}/status` y ejecutar DELETE lógico. Los listados admiten búsqueda y estado. Providers también admite `providerTypeId`.

## Errores y eventos

- `400` para entrada inválida o encabezado de idempotencia inválido.
- `401` por token ausente o inválido y `403` por falta de permisos.
- `404` por ausencia del recurso o porque una referencia nueva no está disponible.
- `409` por concurrencia, conflicto de idempotencia o conflicto de negocio.
- `500` genérico ante una excepción inesperada.

El Hub `/hubs/authorizations` emite `AuthorizationCreated` y `AuthorizationStatusChanged`. Ambos roles pueden conectarse. Las notificaciones llevan `authorizationId` y `occurredAt`. El cambio de estado agrega `isActive`. No se emiten por repeticiones, fallos de escritura o cambios de estado sin efecto.

La [guía manual](../tests/Etapa11E-Swagger.md) contiene cuerpos de ejemplo, encabezados y consultas de verificación en SQL Server.
