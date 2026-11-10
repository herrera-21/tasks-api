namespace TasksApi.Dtos;

/// <summary>
/// Lo que la API devuelve al cliente sobre una tarea.
/// </summary>
public class TaskResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
