# Configuración declarativa de despliegue

Esta carpeta puede trasladarse a un repositorio de despliegue independiente. Contiene el estado deseado de la API en Compose y una plantilla para fijar la imagen mediante digest. La revisión de Git controla qué versión se autoriza. La reconciliación descrita aquí se ejecuta manualmente. No hay un controlador GitOps ni un despliegue activo.

## Preparar la versión

1. Crea un pipeline de Azure DevOps que use `azure-pipelines.yml`. La ejecución es manual y la publicación está deshabilitada por defecto. Compila y prueba en Release, publica TRX y cobertura y construye la imagen de la API.
2. Para habilitar publicación, autoriza una conexión de servicio tipo Docker Registry. Ejecuta el pipeline con `publishImage=true`, el nombre de la conexión y `registryHost` sin protocolo. Se publica una etiqueta con el SHA del commit. Copia el digest que devuelve el registro.
3. En el repositorio de despliegue, copia `release.env.example` a `release.env` y sustituye `API_IMAGE` por la referencia inmutable `registro/repositorio@sha256:…`. Revisa el cambio de versión con el proceso normal de Git. El archivo de versión solo contiene configuración no sensible.
4. Proporciona externamente `ConnectionStrings__HealthAuthorization`, `Jwt__Authority` y `Jwt__Audience`. Si corresponde, configura `Jwt__RoleClaimType`. Prepara la base SQL Server con las migraciones de la misma versión antes de promocionar. La plantilla ejecuta solo la API y deja el puerto en loopback para un proxy HTTPS.
5. Un operador con acceso al entorno y al registro aplica el estado revisado.

~~~sh
docker compose --env-file release.env -f compose.yml config --quiet
docker compose --env-file release.env -f compose.yml pull
docker compose --env-file release.env -f compose.yml up -d
~~~

Valida una consulta autenticada de negocio. Para revertir, restaura en Git la referencia anterior y repite `pull` y `up -d`. Antes de revertir una imagen, comprueba su compatibilidad con el esquema vigente. El procedimiento conserva la base de datos y su historial de migraciones.

## Alcance verificado

El YAML del pipeline y la configuración declarativa se entregan preparados. No se han ejecutado tareas en un agente de Azure DevOps ni se han configurado conexiones de servicio, registros, cuentas, infraestructura externa o despliegues automáticos. La pipeline deja las pruebas de SQL Server real al script aislado del repositorio, que se ejecutó localmente durante la validación de entrega.
