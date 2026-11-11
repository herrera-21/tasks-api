using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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

// Documentación de la API (Swagger / OpenAPI).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tasks API",
        Version = "v1",
        Description = "API REST para gestionar tareas: crear, listar, ver, actualizar y borrar.",
    });

    // Usa los comentarios /// del código como descripción de cada endpoint y campo.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
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

// Swagger queda activo en todos los ambientes (también dentro del contenedor Docker),
// para que quien revise la prueba pueda ver y probar la API desde el navegador.
// JSON: /swagger/v1/swagger.json — Pantalla: /swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tasks API v1");
    options.DocumentTitle = "Tasks API - Swagger";
});

// Entrar a la raíz (/) lleva directo a Swagger.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Pasar de http a https solo en tu máquina.
// Dentro de Docker la API solo usa http (puerto 8080), así que ahí no aplica.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expone Program para WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program { }
