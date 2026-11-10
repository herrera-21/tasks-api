using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TasksApi.Data;
using TasksApi.Errors;
using TasksApi.Repositories;
using TasksApi.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no está configurada.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

// Todas las respuestas de error usan el formato estándar ProblemDetails (RFC 9457).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers(options =>
    {
        // Evita el mensaje extra "The request field is required." cuando el JSON viene mal escrito.
        // Los campos obligatorios se marcan a mano con [Required] en los DTOs.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Respuesta 400 cuando los datos no pasan la validación o el JSON no se puede leer.
        options.InvalidModelStateResponseFactory = context =>
        {
            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext,
                context.ModelState,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Uno o más datos no son válidos.",
                detail: "Revisa el campo 'errors' para ver qué corregir.");

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" },
            };
        };
    });

var app = builder.Build();

// Atrapa errores no controlados (500) usando GlobalExceptionHandler.
app.UseExceptionHandler();

// Da cuerpo ProblemDetails a errores sin contenido, como una ruta que no existe (404) o un método no permitido (405).
app.UseStatusCodePages();

// Crea el esquema de la base de datos al iniciar si no existe.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expone Program para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program { }
