namespace TasksApi.Models;

/// <summary>
/// Entidad de dominio que representa una tarea.
/// Se llama TaskItem para no chocar con System.Threading.Tasks.Task.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
