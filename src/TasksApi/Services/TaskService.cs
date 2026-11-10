using TasksApi.Dtos;
using TasksApi.Models;
using TasksApi.Repositories;

namespace TasksApi.Services;

public class TaskService(ITaskRepository repository) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await repository.GetAllAsync(cancellationToken);
        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetByIdAsync(id, cancellationToken);
        return task is null ? null : ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = CleanDescription(request.Description),
            IsCompleted = false, // Toda tarea nueva empieza pendiente.
            CreatedAt = DateTime.UtcNow,
        };

        var created = await repository.AddAsync(task, cancellationToken);
        return ToResponse(created);
    }

    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Title = request.Title.Trim();
        task.Description = CleanDescription(request.Description);
        task.IsCompleted = request.IsCompleted ?? false; // La validación ya exige que venga.
        task.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task, cancellationToken);
        return ToResponse(task);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        await repository.DeleteAsync(task, cancellationToken);
        return true;
    }

    // Quita espacios sobrantes; si la descripción queda vacía, se guarda como null.
    private static string? CleanDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    // Convierte la tarea de la base de datos en lo que se devuelve al cliente.
    private static TaskResponse ToResponse(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        IsCompleted = task.IsCompleted,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
    };
}
