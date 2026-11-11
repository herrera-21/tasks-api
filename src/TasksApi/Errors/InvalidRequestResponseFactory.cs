using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TasksApi.Errors;

/// <summary>
/// Arma la respuesta 400 cuando los datos de la petición no son válidos.
/// Los mensajes de las validaciones ([Required], [StringLength]) ya vienen en español.
/// Los errores de lectura del JSON vienen del framework en inglés y con nombres internos
/// del código, así que aquí se cambian por mensajes claros en español.
/// </summary>
public static class InvalidRequestResponseFactory
{
    private const string BodyField = "body";

    public static IActionResult Create(ActionContext context)
    {
        var problemFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = problemFactory.CreateValidationProblemDetails(
            context.HttpContext,
            new ModelStateDictionary(),
            statusCode: StatusCodes.Status400BadRequest,
            title: "Uno o más datos no son válidos.",
            detail: "Revisa el campo 'errors' para ver qué corregir.");

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            var (field, message) = Translate(key);
            var messages = message is null
                ? entry.Errors.Select(e => e.ErrorMessage).ToArray()
                : [message];

            problem.Errors[field] = problem.Errors.TryGetValue(field, out var existing)
                ? existing.Concat(messages).Distinct().ToArray()
                : messages;
        }

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" },
        };
    }

    // Devuelve el nombre del campo y el mensaje en español.
    // Si el mensaje es null, se dejan los mensajes originales (los de las validaciones).
    private static (string Field, string? Message) Translate(string key)
    {
        // Llave vacía: no se mandó body.
        if (key.Length == 0)
        {
            return (BodyField, "El cuerpo de la petición es obligatorio y debe ser un JSON.");
        }

        // "$": el JSON está mal escrito o no es un objeto.
        if (key == "$")
        {
            return (BodyField, "El JSON no es válido. Revisa que esté bien escrito y que sea un objeto { }.");
        }

        // "$.title", "$.isCompleted"...: un campo trae un tipo de dato incorrecto.
        if (key.StartsWith("$.", StringComparison.Ordinal))
        {
            var jsonName = key[2..].Split('.', '[')[0];
            var field = char.ToUpperInvariant(jsonName[0]) + jsonName[1..];
            return (field, $"El valor de '{jsonName}' no tiene el tipo correcto.");
        }

        return (key, null);
    }
}
