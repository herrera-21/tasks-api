using TasksApi.Models;
using TasksApi.Repositories;

namespace TasksApi.Tests.Unit;

/// <summary>
/// Repositorio falso para las pruebas unitarias: guarda las tareas en una lista en memoria,
/// sin base de datos. Además cuenta cuántas veces se llamó a actualizar y borrar,
/// para poder revisar que el servicio no los llame cuando no debe.
/// </summary>
public class FakeTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];
    private int _nextId = 1;

    public int UpdateCalls { get; private set; }

    public int DeleteCalls { get; private set; }

    /// <summary>Lo que hay "guardado" en este momento.</summary>
    public IReadOnlyList<TaskItem> Tasks => _tasks;

    /// <summary>Mete una tarea directo, como si ya existiera en la base.</summary>
    public TaskItem Seed(TaskItem task)
    {
        task.Id = _nextId++;
        _tasks.Add(task);
        return task;
    }

    public Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TaskItem> result = _tasks.OrderBy(t => t.Id).ToList();
        return Task.FromResult(result);
    }

    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id));
    }

    public Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        // Igual que la base de datos: le asigna el siguiente id.
        return Task.FromResult(Seed(task));
    }

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        // La tarea de la lista ya es el mismo objeto que cambió el servicio; solo se cuenta la llamada.
        UpdateCalls++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        DeleteCalls++;
        _tasks.Remove(task);
        return Task.CompletedTask;
    }
}
