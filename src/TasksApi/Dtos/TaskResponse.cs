namespace TasksApi.Dtos;

/// <summary>
/// Lo que la API devuelve al cliente sobre una tarea.
/// </summary>
public class TaskResponse
{
    /// <summary>
    /// Id único de la tarea.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Título de la tarea.
    /// </summary>
    /// <example>Comprar café</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la tarea (null si no tiene).
    /// </summary>
    /// <example>Para la oficina</example>
    public string? Description { get; set; }

    /// <summary>
    /// Indica si la tarea está completada.
    /// </summary>
    /// <example>false</example>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Fecha y hora de creación, en UTC.
    /// </summary>
    /// <example>2026-11-10T21:30:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de la última actualización, en UTC (null si nunca se actualizó).
    /// </summary>
    /// <example>2026-11-10T22:00:00Z</example>
    public DateTime? UpdatedAt { get; set; }
}
