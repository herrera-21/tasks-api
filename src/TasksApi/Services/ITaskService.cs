using TasksApi.Dtos;

namespace TasksApi.Services;

/// <summary>
/// Reglas de negocio de las tareas. El controlador habla con esta capa, no con el repositorio.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <returns>La tarea, o null si no existe.</returns>
    Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    /// <returns>La tarea actualizada, o null si no existe.</returns>
    Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    /// <returns>true si se borró, false si no existía.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
