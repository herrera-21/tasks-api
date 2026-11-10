using System.ComponentModel.DataAnnotations;

namespace TasksApi.Dtos;

/// <summary>
/// Datos que envía el cliente para crear una tarea.
/// </summary>
public class CreateTaskRequest
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
    /// Descripción opcional, máximo 1000 caracteres.
    /// </summary>
    /// <example>Para la oficina</example>
    [StringLength(1000, ErrorMessage = "La descripción no puede tener más de 1000 caracteres.")]
    public string? Description { get; set; }
}
