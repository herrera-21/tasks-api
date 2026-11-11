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

    // ---------- Actualización (PUT) ----------

    [Fact]
    public async Task Put_ConDatosValidos_Devuelve200ConLaTareaActualizada()
    {
        var created = await CreateTaskAsync("Título original", "Descripción original");
        var request = new UpdateTaskRequest { Title = "  Título nuevo  ", Description = "Descripción nueva", IsCompleted = true };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Título nuevo", updated.Title); // Se quitan los espacios sobrantes.
        Assert.Equal("Descripción nueva", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.Equal(created.CreatedAt, updated.CreatedAt); // La fecha de creación no cambia.
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, updated.UpdatedAt.Value.Kind);
    }

    [Fact]
    public async Task Put_LuegoGetPorId_DevuelveLosCambiosGuardados()
    {
        var created = await CreateTaskAsync("Antes");
        var request = new UpdateTaskRequest { Title = "Después", IsCompleted = true };

        await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"{BaseUrl}/{created.Id}");

        Assert.NotNull(fetched);
        Assert.Equal("Después", fetched.Title);
        Assert.True(fetched.IsCompleted);
        Assert.NotNull(fetched.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, fetched.UpdatedAt.Value.Kind); // Sigue en UTC al leerla de la base.
    }

    [Fact]
    public async Task Put_SinDescripcion_BorraLaDescripcionAnterior()
    {
        // PUT reemplaza todo: si no se manda la descripción, queda vacía (null).
        var created = await CreateTaskAsync("Con descripción", "Se va a borrar");
        var request = new UpdateTaskRequest { Title = "Con descripción", IsCompleted = false };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);

        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(updated);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task Put_PuedeVolverAPendiente()
    {
        var created = await CreateTaskAsync("Ida y vuelta");
        await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new UpdateTaskRequest { Title = "Ida y vuelta", IsCompleted = true });

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new UpdateTaskRequest { Title = "Ida y vuelta", IsCompleted = false });

        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(updated);
        Assert.False(updated.IsCompleted);
    }

    [Fact]
    public async Task Put_Inexistente_Devuelve404ProblemDetails()
    {
        var request = new UpdateTaskRequest { Title = "No existe", IsCompleted = false };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/9999", request);

        await AssertNotFoundProblemAsync(response, 9999);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Put_ConTituloVacio_Devuelve400ConErrorEnTitle(string title)
    {
        var created = await CreateTaskAsync("Válido");
        var request = new UpdateTaskRequest { Title = title, IsCompleted = false };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Title", out _), "Se esperaba un error en el campo Title.");
    }

    [Fact]
    public async Task Put_SinIsCompleted_Devuelve400ConErrorEnIsCompleted()
    {
        var created = await CreateTaskAsync("Válido");

        // JSON sin el campo isCompleted.
        var content = new StringContent("""{ "title": "Sin estado" }""", Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"{BaseUrl}/{created.Id}", content);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("IsCompleted", out _), "Se esperaba un error en el campo IsCompleted.");
    }

    [Fact]
    public async Task Put_ConTituloDemasiadoLargo_Devuelve400()
    {
        var created = await CreateTaskAsync("Válido");
        var request = new UpdateTaskRequest { Title = new string('a', 201), IsCompleted = false };

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", request);

        var errors = await AssertValidationProblemAsync(response);
        Assert.True(errors.TryGetProperty("Title", out _), "Se esperaba un error en el campo Title.");
    }

    [Fact]
    public async Task Put_ConDatosInvalidos_NoCambiaLaTarea()
    {
        var created = await CreateTaskAsync("Original");

        await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new UpdateTaskRequest { Title = "", IsCompleted = true });
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"{BaseUrl}/{created.Id}");

        Assert.NotNull(fetched);
        Assert.Equal("Original", fetched.Title);
        Assert.False(fetched.IsCompleted);
        Assert.Null(fetched.UpdatedAt);
    }

    // ---------- Borrado (DELETE) ----------

    [Fact]
    public async Task Delete_Existente_Devuelve204YLuegoGetDevuelve404()
    {
        var created = await CreateTaskAsync("Para borrar");

        var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty(await deleteResponse.Content.ReadAsByteArrayAsync()); // 204 no lleva cuerpo.

        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        await AssertNotFoundProblemAsync(getResponse, created.Id);
    }

    [Fact]
    public async Task Delete_SoloBorraLaTareaIndicada()
    {
        var toDelete = await CreateTaskAsync("Se borra");
        var toKeep = await CreateTaskAsync("Se queda");

        await _client.DeleteAsync($"{BaseUrl}/{toDelete.Id}");
        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>(BaseUrl);

        Assert.NotNull(tasks);
        var remaining = Assert.Single(tasks);
        Assert.Equal(toKeep.Id, remaining.Id);
    }

    [Fact]
    public async Task Delete_Inexistente_Devuelve404ProblemDetails()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/9999");

        await AssertNotFoundProblemAsync(response, 9999);
    }

    [Fact]
    public async Task Delete_DosVeces_LaSegundaDevuelve404()
    {
        var created = await CreateTaskAsync("Borrar dos veces");

        await _client.DeleteAsync($"{BaseUrl}/{created.Id}");
        var secondResponse = await _client.DeleteAsync($"{BaseUrl}/{created.Id}");

        await AssertNotFoundProblemAsync(secondResponse, created.Id);
    }

    // ---------- Ayudantes ----------

    // Crea una tarea por la API y devuelve lo que respondió.
    private async Task<TaskResponse> CreateTaskAsync(string title, string? description = null)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new CreateTaskRequest { Title = title, Description = description });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        return created!;
    }

    // Revisa que la respuesta sea un 404 en formato ProblemDetails para el id indicado.
    private static async Task AssertNotFoundProblemAsync(HttpResponseMessage response, int id)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Tarea no encontrada", body.RootElement.GetProperty("title").GetString());
        Assert.Contains(id.ToString(), body.RootElement.GetProperty("detail").GetString());
    }

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
