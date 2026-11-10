namespace TasksApi.Dtos;

/// <summary>
/// Datos que envía el cliente para actualizar una tarea.
/// Reemplaza todos los campos editables (PUT).
/// </summary>
public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }
}
