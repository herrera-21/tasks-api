using TasksApi.Dtos;
using TasksApi.Models;
using TasksApi.Services;

namespace TasksApi.Tests.Unit;

/// <summary>
/// Pruebas unitarias de TaskService: revisan sus reglas sin HTTP ni base de datos.
/// Usan un repositorio falso (FakeTaskRepository) que guarda todo en una lista.
/// </summary>
public class TaskServiceTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repository);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_QuitaEspaciosSobrantesDelTituloYLaDescripcion()
    {
        var request = new CreateTaskRequest { Title = "  Comprar café  ", Description = "  Para la oficina  " };

        var created = await _service.CreateAsync(request);

        Assert.Equal("Comprar café", created.Title);
        Assert.Equal("Para la oficina", created.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ConDescripcionVaciaONula_LaGuardaComoNull(string? description)
    {
        var request = new CreateTaskRequest { Title = "Tarea", Description = description };

        var created = await _service.CreateAsync(request);

        Assert.Null(created.Description);
        Assert.Null(_repository.Tasks.Single().Description);
    }

    [Fact]
    public async Task CreateAsync_LaTareaEmpiezaPendienteYSinFechaDeActualizacion()
    {
        var created = await _service.CreateAsync(new CreateTaskRequest { Title = "Nueva" });

        Assert.False(created.IsCompleted);
        Assert.Null(created.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_PoneLaFechaDeCreacionActualEnUtc()
    {
        var before = DateTime.UtcNow;

        var created = await _service.CreateAsync(new CreateTaskRequest { Title = "Nueva" });

        var after = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.InRange(created.CreatedAt, before, after);
    }

    [Fact]
    public async Task CreateAsync_GuardaLaTareaYDevuelveElIdAsignado()
    {
        var created = await _service.CreateAsync(new CreateTaskRequest { Title = "Nueva" });

        var saved = Assert.Single(_repository.Tasks);
        Assert.Equal(saved.Id, created.Id);
        Assert.Equal("Nueva", saved.Title);
    }

    // ---------- GetAllAsync / GetByIdAsync ----------

    [Fact]
    public async Task GetAllAsync_SinTareas_DevuelveListaVacia()
    {
        var tasks = await _service.GetAllAsync();

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task GetAllAsync_DevuelveTodasLasTareasConSusDatos()
    {
        var createdAt = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        _repository.Seed(new TaskItem { Title = "Uno", Description = "Primera", IsCompleted = true, CreatedAt = createdAt });
        _repository.Seed(new TaskItem { Title = "Dos", CreatedAt = createdAt });

        var tasks = await _service.GetAllAsync();

        Assert.Equal(2, tasks.Count);

        // Revisa que se copien todos los campos de la tarea a la respuesta.
        var first = tasks[0];
        Assert.Equal(1, first.Id);
        Assert.Equal("Uno", first.Title);
        Assert.Equal("Primera", first.Description);
        Assert.True(first.IsCompleted);
        Assert.Equal(createdAt, first.CreatedAt);
        Assert.Null(first.UpdatedAt);

        Assert.Equal("Dos", tasks[1].Title);
    }

    [Fact]
    public async Task GetByIdAsync_Existente_DevuelveLaTarea()
    {
        var seeded = _repository.Seed(new TaskItem { Title = "Buscada", CreatedAt = DateTime.UtcNow });

        var task = await _service.GetByIdAsync(seeded.Id);

        Assert.NotNull(task);
        Assert.Equal("Buscada", task.Title);
    }

    [Fact]
    public async Task GetByIdAsync_Inexistente_DevuelveNull()
    {
        var task = await _service.GetByIdAsync(999);

        Assert.Null(task);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_Existente_CambiaLosDatosYGuarda()
    {
        var createdAt = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
        var seeded = _repository.Seed(new TaskItem { Title = "Antes", Description = "Vieja", CreatedAt = createdAt });
        var request = new UpdateTaskRequest { Title = "  Después  ", Description = "  Nueva  ", IsCompleted = true };

        var updated = await _service.UpdateAsync(seeded.Id, request);

        Assert.NotNull(updated);
        Assert.Equal("Después", updated.Title);
        Assert.Equal("Nueva", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.Equal(createdAt, updated.CreatedAt); // La fecha de creación no cambia.
        Assert.Equal(1, _repository.UpdateCalls);
    }

    [Fact]
    public async Task UpdateAsync_PoneLaFechaDeActualizacionActualEnUtc()
    {
        var seeded = _repository.Seed(new TaskItem { Title = "Tarea", CreatedAt = DateTime.UtcNow });
        var before = DateTime.UtcNow;

        var updated = await _service.UpdateAsync(seeded.Id, new UpdateTaskRequest { Title = "Tarea", IsCompleted = false });

        var after = DateTime.UtcNow;
        Assert.NotNull(updated?.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, updated.UpdatedAt.Value.Kind);
        Assert.InRange(updated.UpdatedAt.Value, before, after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task UpdateAsync_ConDescripcionVaciaONula_BorraLaDescripcion(string? description)
    {
        var seeded = _repository.Seed(new TaskItem { Title = "Tarea", Description = "Se va a borrar", CreatedAt = DateTime.UtcNow });

        var updated = await _service.UpdateAsync(seeded.Id, new UpdateTaskRequest { Title = "Tarea", Description = description, IsCompleted = false });

        Assert.NotNull(updated);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_SinIsCompleted_LoTomaComoPendiente()
    {
        // En la API esto no llega (la validación lo rechaza con 400),
        // pero el servicio igual se protege y no se cae.
        var seeded = _repository.Seed(new TaskItem { Title = "Tarea", IsCompleted = true, CreatedAt = DateTime.UtcNow });

        var updated = await _service.UpdateAsync(seeded.Id, new UpdateTaskRequest { Title = "Tarea", IsCompleted = null });

        Assert.NotNull(updated);
        Assert.False(updated.IsCompleted);
    }

    [Fact]
    public async Task UpdateAsync_Inexistente_DevuelveNullYNoGuardaNada()
    {
        var updated = await _service.UpdateAsync(999, new UpdateTaskRequest { Title = "No existe", IsCompleted = true });

        Assert.Null(updated);
        Assert.Equal(0, _repository.UpdateCalls);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_Existente_DevuelveTrueYLaBorra()
    {
        var seeded = _repository.Seed(new TaskItem { Title = "Para borrar", CreatedAt = DateTime.UtcNow });

        var deleted = await _service.DeleteAsync(seeded.Id);

        Assert.True(deleted);
        Assert.Empty(_repository.Tasks);
        Assert.Equal(1, _repository.DeleteCalls);
    }

    [Fact]
    public async Task DeleteAsync_Inexistente_DevuelveFalseYNoBorraNada()
    {
        _repository.Seed(new TaskItem { Title = "Se queda", CreatedAt = DateTime.UtcNow });

        var deleted = await _service.DeleteAsync(999);

        Assert.False(deleted);
        Assert.Single(_repository.Tasks);
        Assert.Equal(0, _repository.DeleteCalls);
    }
}
