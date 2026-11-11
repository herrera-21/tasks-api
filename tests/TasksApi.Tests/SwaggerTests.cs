using System.Net;
using System.Text.Json;

namespace TasksApi.Tests;

/// <summary>
/// Revisa que la documentación Swagger esté disponible y describa los 5 endpoints.
/// Todas las pruebas usan la misma API porque solo leen, no cambian datos.
/// </summary>
public class SwaggerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerJson_Devuelve200ConLosEndpointsDeTareas()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths");

        // /api/tasks tiene GET (listar) y POST (crear).
        var tasks = paths.GetProperty("/api/tasks");
        Assert.True(tasks.TryGetProperty("get", out _));
        Assert.True(tasks.TryGetProperty("post", out _));

        // /api/tasks/{id} tiene GET (ver), PUT (actualizar) y DELETE (borrar).
        var taskById = paths.GetProperty("/api/tasks/{id}");
        Assert.True(taskById.TryGetProperty("get", out _));
        Assert.True(taskById.TryGetProperty("put", out _));
        Assert.True(taskById.TryGetProperty("delete", out _));
    }

    [Fact]
    public async Task SwaggerJson_IncluyeLosComentariosDelCodigo()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");

        using var doc = JsonDocument.Parse(json);
        var post = doc.RootElement.GetProperty("paths").GetProperty("/api/tasks").GetProperty("post");

        // El resumen sale del comentario /// del controlador.
        Assert.Equal("Crea una tarea nueva.", post.GetProperty("summary").GetString());

        // Las respuestas declaradas aparecen documentadas.
        var responses = post.GetProperty("responses");
        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
    }

    [Fact]
    public async Task SwaggerUi_Devuelve200()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Raiz_RedirigeASwagger()
    {
        // Sin seguir redirecciones, para ver la respuesta original.
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.ToString());
    }
}
