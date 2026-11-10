using System.ComponentModel.DataAnnotations;

namespace TasksApi.Dtos;

/// <summary>
/// Datos que envía el cliente para actualizar una tarea.
/// Reemplaza todos los campos editables (PUT).
/// </summary>
public class UpdateTaskRequest
{
    /// <summary>
    /// Título de la tarea. Obligatorio, máximo 200 caracteres.
    /// </summary>
    /// <example>Comprar café</example>
    // [Required] también rechaza textos vacíos o con solo espacios.
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede tener más de 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional, máximo 1000 caracteres. Si no se manda, queda vacía.
    /// </summary>
    /// <example>Para la oficina</example>
    [StringLength(1000, ErrorMessage = "La descripción no puede tener más de 1000 caracteres.")]
    public string? Description { get; set; }

    /// <summary>
    /// Indica si la tarea está completada. Obligatorio.
    /// </summary>
    /// <example>true</example>
    // Es bool? para poder saber si el cliente no lo mandó.
    // Con bool normal, si falta se tomaría como false sin avisar.
    [Required(ErrorMessage = "El estado (isCompleted) es obligatorio.")]
    public bool? IsCompleted { get; set; }
}
