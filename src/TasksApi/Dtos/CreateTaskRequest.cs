namespace TasksApi.Dtos;

/// <summary>
/// Datos que envía el cliente para crear una tarea.
/// </summary>
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
}
