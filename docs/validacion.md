# Validación de entrega

## Matriz de cumplimiento

| Apartado original | Resultado | Evidencia |
|---|---|---|
| 1. API REST sin interfaz gráfica | Cumplido | ASP.NET Core Web API. Swagger permite probar el contrato. |
| 2. Alta, actualización, detalle, listado filtrado e inactivación | Cumplido funcionalmente | Pruebas HTTP y SQL Server. El rendimiento de páginas profundas queda observado en las mediciones. |
| 2. Afiliados, contratantes, intermediarios, prestadores y tipos | Cumplido | Seis endpoints de catálogo y 16 filas iniciales. La prueba de Compose consultó `DEMO-001` con JWT. |
| 3. Modelo relacional de ocho entidades | Cumplido | Dos migraciones; clave GUID, relaciones, restricciones, índices y pruebas de integridad reales. |
| 4. Servicio obligatorio, monto explícito y distribución 70/30 | Cumplido | FluentValidation, `CoveragePolicy`, precisión decimal, restricciones monetarias y escenarios unitarios y HTTP. |
| 4. Referencias válidas e historial | Cumplido | Se rechazan altas y reemplazos con referencias indisponibles. Los detalles retirados y las autorizaciones inactivas se conservan. |
| 4. Integridad frente a errores, carreras y reintentos | Cumplido | Pruebas de rollback, conflicto `RowVersion`, unicidad e idempotencia con contextos SQL Server independientes. |
| 4. Evolución de la política de cobertura | Cumplido | El cálculo y el redondeo están centralizados en `CoveragePolicy`. |
| 5. .NET 10, Clean Architecture, CQRS, MediatR, EF Core, Mapster, FluentValidation, middleware, JWT y OpenAPI | Cumplido | Proyectos separados, flujo de aplicación por handlers, API autenticada y Swagger disponible en desarrollo. |
| 6. Seguridad, errores, diagnóstico y recuperación | Cumplido para el alcance probado | JWT, roles, ProblemDetails, logs y verificación tras reiniciar API y SQL Server. |
| 6. Rendimiento ante volumen creciente | Validado con observación | 320 respuestas correctas sobre una carga sintética representativa. La página 1500 mostró el costo del desplazamiento y las agregaciones. El documento no fija un umbral de latencia. |
| 7. Notificar altas y cambios de estado | Cumplido | Cliente SignalR real autenticado verificó ambos eventos y ausencia de duplicados para las operaciones probadas. Las demás tecnologías de integración eran opcionales. |
| 8. Pruebas y cobertura superior al 80 % | Cumplido | 122 pruebas aprobadas. Líneas 90,98 % sin migraciones y 95,04 % incluyéndolas. |
| 8. Duplicación inferior al 3 % | Cumplido para el código de producción sin migraciones | 1,39 % con el método descrito abajo. La medición que incluye los artefactos EF conserva un porcentaje mayor. |
| 8. Vulnerabilidades críticas conocidas y errores bloqueantes de análisis | Cumplido en el alcance auditado | NuGet no reportó avisos para las dependencias resueltas. Los analizadores del SDK terminaron con cero errores. |
| 9. Ejecutar localmente mediante Docker Desktop | Cumplido | Reconstrucción desde una exportación limpia de Git, SQL Server saludable, migrador con código 0, Swagger 200 y endpoint autenticado. |
| 5 y 9. Azure DevOps YAML y GitOps | Configuraciones preparadas | El apartado 5 los enumera como opcionales y el 9 los solicita. Se adjuntan el YAML y la estructura declarativa, validados localmente en los comandos de pruebas, la construcción Docker y la resolución de Compose. Las tareas de Azure y el despliegue externo no se ejecutaron. |

## Pruebas y cobertura

Release estándar terminó con **0 errores y 0 advertencias**. Ejecutadas 67 pruebas unitarias y 55 de integración, **122 aprobadas, 0 fallidas y 0 omitidas**. Se usó una base de pruebas nueva con ambas migraciones. No se usaron bases de desarrollo. Las pruebas HTTP usan tanto dobles como SQL Server real, según el objetivo del escenario.

| Código medido | Líneas | Ramas |
|---|---:|---:|
| API | 74,47 % | 51,01 % |
| Application | 98,03 % | 84,17 % |
| Domain | 88,89 % | 90,00 % |
| Infrastructure sin migraciones | 94,74 % | 79,17 % |
| Infrastructure con migraciones | 98,77 % | 79,17 % |
| Total sin migraciones | **90,98 %** | **67,34 %** |
| Total con migraciones | **95,04 %** | **67,34 %** |

Coverlet produjo un reporte OpenCover por suite. El script combina cobertura por archivo y línea. Para las ramas combina por clase, método, ubicación y camino. La vista sin migraciones excluye su carpeta para aislar la lógica escrita de los artefactos de esquema. La cifra con migraciones mantiene ese código, que se ejercita generando SQL y validando la instantánea. Las ramas se reportan por separado del umbral de líneas. [Datos de cobertura](evidencia/cobertura.csv)

## Duplicación y análisis estático

Se compararon ventanas exactas de **10 líneas C# consecutivas** normalizadas por sangría. Se omitieron líneas en blanco y comentarios de línea completa. Cada línea implicada se contó una vez. Dos coincidencias del mismo archivo solo se consideran si las ventanas no se solapan. El denominador comprende todas las líneas normalizadas del alcance. Este método no detecta clones con nombres o texto modificados y no es directamente comparable con la detección por tokens de otros analizadores.

| Alcance | Líneas repetidas / evaluadas | Duplicación |
|---|---:|---:|
| Producción sin migraciones | 60 / 4.304 | 1,39 % |
| Producción con migraciones | 1.636 / 6.224 | 26,29 % |
| Producción y pruebas sin migraciones | 94 / 7.244 | 1,30 % |
| Producción y pruebas con migraciones | 1.670 / 9.164 | 18,22 % |

La primera medición dio 4,58 % de duplicación en producción sin migraciones. Los validadores de creación y actualización ahora comparten las reglas de sus DTO de catálogo. La repetición restante medida en producción corresponde a los comandos de cambio de estado. Las instantáneas y los diseñadores de EF Core explican gran parte de la diferencia al incluir migraciones.

El análisis ampliado con `EnableNETAnalyzers=true`, `RunAnalyzersDuringBuild=true` y `AnalysisMode=All` terminó con **0 errores y 486 advertencias**. Entre las más frecuentes están CA2007 sobre `ConfigureAwait`, CA1062 sobre parámetros públicos y CA1852 sobre sellado de tipos. El perfil ampliado se ejecutó como diagnóstico. Las dos ocurrencias CA2025 y la ocurrencia CA2016 dejaron de aparecer. CA1062 pasó de 56 a 53 con guardias en tres puntos públicos de la API. No se suprimieron advertencias. La compilación estándar no emitió advertencias. La advertencia EF Core 10622 se observó al migrar y se explica en [Arquitectura](arquitectura.md).

Una ejecución inicial coincidió con la recompilación de diagnóstico en el mismo directorio de salida y falló al cargar `Microsoft.SqlServer.Server`. En esa primera ejecución, el build ampliado también reportó 37 advertencias MSB3061 por archivos bloqueados. La repetición secuencial completó los 122 casos y el análisis ampliado de esa revisión contó 481 advertencias de código, sin bloqueos. El cierre volvió a ejecutar ambos pasos en secuencia. No se modificó código para repetirla.

## Dependencias y secretos

La auditoría consultó NuGet con `NuGetAuditMode=all` y `dotnet list package --vulnerable --include-transitive`. El catálogo consultado no reportó paquetes vulnerables para los seis proyectos. El alcance es el conjunto de paquetes NuGet resueltos, sin un escaneo de las capas de imágenes Docker.

Se revisaron los archivos versionados y los árboles de los **15 commits** alcanzables por las referencias locales. Se buscaron nombres de archivos sensibles, firmas de claves privadas, JWT literales, formatos de tokens conocidos y asignaciones de contraseñas. Los candidatos correspondieron a marcadores, variables de entorno, tokens generados en memoria o código de pruebas. No se encontraron credenciales reales con esos patrones. La auditoría no cubre referencias remotas que no estén presentes localmente ni formatos que no coincidan con los patrones. El entorno usa User Secrets o variables de entorno. Los ejemplos versionados contienen valores vacíos o datos de demostración.

## Rendimiento y recuperación

La medición de la Etapa 11E empleó la API Release en loopback, SQL Server 2022 Developer CU28 en Docker, 12 procesadores visibles y unos 6,1 GiB de memoria visible para SQL. Se agregaron 30.000 autorizaciones, 90.000 detalles y 300 afiliados sintéticos. Se hizo una solicitud de calentamiento por escenario. Los tiempos miden la solicitud HTTP completa, incluida la salida del cliente. El P95 usa el rango `ceil(0,95 × N)`. Los 320 intentos medidos fueron exitosos.

| Escenario | Solicitudes | Mediana | P95 |
|---|---:|---:|---:|
| Sin filtros, 10 por página | 30 | 19,82 ms | 44,84 ms |
| Sin filtros, 50 por página | 30 | 23,29 ms | 44,72 ms |
| Sin filtros, 100 por página | 30 | 32,11 ms | 38,73 ms |
| Afiliado | 30 | 19,25 ms | 23,41 ms |
| Fecha | 30 | 17,58 ms | 24,45 ms |
| Inactivas | 30 | 16,89 ms | 21,50 ms |
| Afiliado, estado y fecha | 30 | 22,43 ms | 31,53 ms |
| Página 1500 con tamaño 20 | 30 | 794,81 ms | 1.125,22 ms |
| Ocho lectores concurrentes | 80 | 32,50 ms | 74,36 ms |

El plan real de la página profunda recorrió 30.000 filas del índice frente a 20 en la primera página. El motor calculó los totales por detalle antes de descartar las filas desplazadas. El contrato no ofrece filtro por prestador. Los números no representan una garantía de latencia para producción. [Métricas con tamaños de muestra, medias, mínimos, máximos y errores](evidencia/rendimiento.csv)

Tras reiniciar por separado la API y SQL Server, se recuperaron las consultas con JWT y coincidieron los datos de la autorización testigo, incluidos el estado, montos, detalle, referencias, auditoría y metadatos de idempotencia. Durante el arranque de SQL Server se observaron rechazos transitorios de conexión. Tras volver a estar disponible, el reintento idempotente devolvió 201 con el mismo ID.

## Reproducción

~~~powershell
dotnet tool restore
$stamp = Get-Date -Format yyyyMMddHHmmss
$coverage = "artifacts/coverage/delivery-$stamp"
.\tests\Run-SqlServerTests.ps1 -CoverageDirectory $coverage -PerformanceDirectory "artifacts/sqlserver/delivery-$stamp"
.\tests\Measure-Coverage.ps1 $coverage

dotnet restore HealthAuthorization.slnx --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet list HealthAuthorization.slnx package --include-transitive --vulnerable --no-restore
dotnet build HealthAuthorization.slnx -c Release --no-restore --no-incremental -p:EnableNETAnalyzers=true -p:RunAnalyzersDuringBuild=true -p:AnalysisMode=All
~~~

Ejecuta el análisis estático después de que termine Coverlet. Las pruebas y los analizadores comparten directorios de compilación. La [guía de Swagger](../tests/Etapa11E-Swagger.md) documenta la validación manual.
