# Convenciones de C# en HealthAuthorization

- En código nuevo o refactorizado, usa clases tradicionales sin `sealed` y conserva la visibilidad que corresponda. Si quitar `sealed` afecta una regla técnica, explica el caso antes de modificarlo.
- Usa propiedades públicas `get; set;` en los contratos. Evita `record`, `record struct` y constructores primarios.
- Recibe las dependencias por constructores explícitos y guárdalas en campos privados `readonly`. Prefiere sintaxis explícita cuando facilite la lectura.
- Coloca cada comando o consulta en el mismo archivo que su handler. Mantén los validadores FluentValidation en archivos independientes y usa MediatR.
- En los Controllers, inyecta `IMediator` mediante un constructor explícito y guárdalo en un campo privado `readonly`. Envía Commands y Queries con `_mediator.Send`.
- En listados paginados, hereda de `PaginationFilter` y centraliza las reglas comunes con `PaginationFilterValidator<TFilter>`. Recibe el Query con `[FromQuery]` en el Controller y define los campos buscables y filtros propios en el Handler con expresiones traducibles a SQL.
- Usa `IRepositoryAsync<TEntity>` como único repositorio genérico e `IUnitOfWork` para guardar cambios. Escribe los mensajes personalizados de validación y excepciones en español.
- Limita las correcciones de estilo al alcance solicitado y verifica con `dotnet build` y `dotnet test`.
