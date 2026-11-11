using TasksApi.Models;

namespace TasksApi.Repositories;

/// <summary>
/// Guarda y consulta tareas en la base de datos.
/// Las fechas y las validaciones se manejan en el servicio, no aquí.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
}
