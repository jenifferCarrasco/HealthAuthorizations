# Convenciones de C# en HealthAuthorization

- En código nuevo o refactorizado, usa clases tradicionales sin `sealed` y conserva la visibilidad que corresponda. Si quitar `sealed` afecta una regla técnica, explica el caso antes de modificarlo.
- Usa propiedades públicas `get; set;` en los contratos. Evita `record`, `record struct` y constructores primarios.
- Recibe las dependencias por constructores explícitos y guárdalas en campos privados `readonly`. Prefiere sintaxis explícita cuando facilite la lectura.
- Coloca cada comando o consulta en el mismo archivo que su handler. Mantén los validadores FluentValidation en archivos independientes y usa MediatR.
- Usa `IRepositoryAsync<TEntity>` como único repositorio genérico e `IUnitOfWork` para guardar cambios. Escribe los mensajes personalizados de validación y excepciones en español.
- Limita las correcciones de estilo al alcance solicitado y verifica con `dotnet build` y `dotnet test`.
