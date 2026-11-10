using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TasksApi.Dtos;

namespace TasksApi.Tests;

/// <summary>
/// Pruebas de integración: llaman a los endpoints por HTTP como lo haría un cliente real.
/// Cada prueba usa su propia API y su propia base en memoria, así no se afectan entre sí.
/// </summary>
public class TasksEndpointsTests : IDisposable
{
    private const string BaseUrl = "/api/tasks";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TasksEndpointsTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ---------- Creación ----------

    [Fact]
    public async Task Post_ConDatosValidos_Devuelve201ConLaTareaYLocation()
    {
        var request = new CreateTaskRequest { Title = "  Comprar café  ", Description = "Para la oficina" };

        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Comprar café", created.Title); // Se quitan los espacios sobrantes.
        Assert.Equal("Para la oficina", created.Description);
        Assert.False(created.IsCompleted); // Toda tarea nueva empieza pendiente.
        Assert.Null(created.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);

        // El header Location apunta a la tarea recién creada.
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"{BaseUrl}/{created.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_LuegoGetPorId_DevuelveLaMismaTarea()
    {
        var createResponse = await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = "Leer un libro" });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Leer un libro", fetched.Title);
        Assert.Null(fetched.Description);
        Assert.Equal(DateTimeKind.Utc, fetched.CreatedAt.Kind); // Sigue en UTC al leerla de la base.
    }

    [Fact]
    public async Task GetAll_SinTareas_DevuelveListaVacia()
    {
        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task GetAll_ConTareas_DevuelveTodas()
    {
        await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = "Tarea 1" });
        await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = "Tarea 2" });

        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>(BaseUrl);

        Assert.NotNull(tasks);
        Assert.Equal(2, tasks.Count);
        Assert.Contains(tasks, t => t.Title == "Tarea 1");
        Assert.Contains(tasks, t => t.Title == "Tarea 2");
    }

    // ---------- Validación (400) ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_ConTituloVacio_Devuelve400ConErrorEnTitle(string title)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = title });

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Title", out _), "Se esperaba un error en el campo Title.");
    }

    [Fact]
    public async Task Post_SinTitulo_Devuelve400()
    {
        // JSON sin el campo title.
        var response = await PostRawJsonAsync("""{ "description": "sin título" }""");

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Title", out _), "Se esperaba un error en el campo Title.");
    }

    [Fact]
    public async Task Post_ConTituloDemasiadoLargo_Devuelve400()
    {
        var request = new CreateTaskRequest { Title = new string('a', 201) };

        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Title", out _), "Se esperaba un error en el campo Title.");
    }

    [Fact]
    public async Task Post_ConDescripcionDemasiadoLarga_Devuelve400()
    {
        var request = new CreateTaskRequest { Title = "Válido", Description = new string('a', 1001) };

        var response = await _client.PostAsJsonAsync(BaseUrl, request);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Description", out _), "Se esperaba un error en el campo Description.");
    }

    [Fact]
    public async Task Post_ConJsonMalEscrito_Devuelve400()
    {
        var response = await PostRawJsonAsync("""{ "title": "sin cerrar" """);

        await AssertValidationProblemAsync(response);
    }

    [Fact]
    public async Task Post_ConTituloInvalido_NoGuardaNada()
    {
        await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = "" });

        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>(BaseUrl);
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    // ---------- No encontrado (404) ----------

    [Fact]
    public async Task GetPorId_Inexistente_Devuelve404ProblemDetails()
    {
        var response = await _client.GetAsync($"{BaseUrl}/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Tarea no encontrada", body.RootElement.GetProperty("title").GetString());
        Assert.Contains("9999", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetPorId_ConIdNoNumerico_Devuelve404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Ayudantes ----------

    // Envía un texto tal cual como JSON (sirve para mandar JSON incompleto o mal escrito).
    private Task<HttpResponseMessage> PostRawJsonAsync(string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.PostAsync(BaseUrl, content);
    }

    // Revisa que la respuesta sea un 400 en formato ProblemDetails y devuelve el objeto "errors".
    private static async Task<JsonElement> AssertValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors), "Se esperaba el campo 'errors'.");

        // Clone() para poder usar el resultado después de liberar el documento.
        return errors.Clone();
    }
}
