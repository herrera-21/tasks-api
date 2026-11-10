using System.ComponentModel.DataAnnotations;

namespace TasksApi.Dtos;

/// <summary>
/// Datos que envía el cliente para actualizar una tarea.
/// Reemplaza todos los campos editables (PUT).
/// </summary>
public class UpdateTaskRequest
{
    // [Required] también rechaza textos vacíos o con solo espacios.
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede tener más de 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede tener más de 1000 caracteres.")]
    public string? Description { get; set; }

    // Es bool? para poder saber si el cliente no lo mandó.
    // Con bool normal, si falta se tomaría como false sin avisar.
    [Required(ErrorMessage = "El estado (isCompleted) es obligatorio.")]
    public bool? IsCompleted { get; set; }
}
