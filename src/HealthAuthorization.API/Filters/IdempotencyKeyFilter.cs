using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace HealthAuthorization.API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotencyKeyFilter : ActionFilterAttribute
{
    public const string HeaderName = "Idempotency-Key";
    private readonly string _argumentName;

    public IdempotencyKeyFilter(string argumentName)
    {
        _argumentName = argumentName;
        // Valida el encabezado antes de que ApiController genere su 400 automático.
        Order = -2001;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var values = context.HttpContext.Request.Headers[HeaderName];
        if (values.Count == 1 && Guid.TryParse(values[0], out var key) && key != Guid.Empty)
        {
            context.ActionArguments[_argumentName] = key;
            return;
        }

        context.ModelState.Remove(_argumentName);
        context.ModelState.Remove(HeaderName);
        var message = values.Count == 0
            ? "El encabezado Idempotency-Key es obligatorio."
            : "El encabezado Idempotency-Key debe contener un único GUID válido y distinto de cero.";
        context.ModelState.AddModelError(HeaderName, message);
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>();
        context.Result = options.Value.InvalidModelStateResponseFactory(context);
    }
}
