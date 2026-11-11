using Microsoft.AspNetCore.Mvc;
using TasksApi.Dtos;
using TasksApi.Services;

namespace TasksApi.Controllers;

/// <summary>
/// Endpoints HTTP de las tareas. Solo recibe la petición, llama al servicio y arma la respuesta.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Tags("Tareas")]
// Cualquier endpoint puede fallar con un error inesperado (500).
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class TasksController : ControllerBase
{
    private const string ProblemJson = "application/problem+json";

    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Lista todas las tareas.
    /// </summary>
    /// <remarks>Si no hay ninguna tarea, devuelve una lista vacía (<c>[]</c>).</remarks>
    /// <param name="cancellationToken">Se cancela si el cliente corta la petición.</param>
    /// <response code="200">Lista de tareas (puede venir vacía).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Obtiene una tarea por su id.
    /// </summary>
    /// <param name="id">Id de la tarea.</param>
    /// <param name="cancellationToken">Se cancela si el cliente corta la petición.</param>
    /// <response code="200">La tarea encontrada.</response>
    /// <response code="404">No existe una tarea con ese id.</response>
    // ":int" hace que /api/tasks/abc no entre aquí (responde 404 en vez de error).
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);

        if (task is null)
        {
            return TaskNotFound(id);
        }

        return Ok(task);
    }

    /// <summary>
    /// Crea una tarea nueva.
    /// </summary>
    /// <remarks>
    /// Solo <c>title</c> es obligatorio. Si no se manda <c>isCompleted</c>, la tarea empieza pendiente (<c>false</c>).
    /// Al título y la descripción se les quitan los espacios sobrantes.
    ///
    /// Ejemplo:
    ///
    ///     POST /api/tasks
    ///     {
    ///       "title": "Comprar café",
    ///       "description": "Para la oficina",
    ///       "isCompleted": false
    ///     }
    /// </remarks>
    /// <param name="request">Datos de la tarea a crear.</param>
    /// <param name="cancellationToken">Se cancela si el cliente corta la petición.</param>
    /// <response code="201">Tarea creada. El header Location trae su dirección.</response>
    /// <response code="400">Los datos no son válidos o el JSON está mal escrito.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Actualiza una tarea (reemplaza todos sus datos).
    /// </summary>
    /// <remarks>
    /// Es un reemplazo completo: si no se manda la descripción, queda vacía.
    /// <c>title</c> e <c>isCompleted</c> son obligatorios.
    ///
    /// Ejemplo:
    ///
    ///     PUT /api/tasks/1
    ///     {
    ///       "title": "Comprar café",
    ///       "description": "Para la oficina",
    ///       "isCompleted": true
    ///     }
    /// </remarks>
    /// <param name="id">Id de la tarea.</param>
    /// <param name="request">Datos nuevos de la tarea.</param>
    /// <param name="cancellationToken">Se cancela si el cliente corta la petición.</param>
    /// <response code="200">La tarea ya actualizada.</response>
    /// <response code="400">Los datos no son válidos o el JSON está mal escrito.</response>
    /// <response code="404">No existe una tarea con ese id.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateAsync(id, request, cancellationToken);

        if (updated is null)
        {
            return TaskNotFound(id);
        }

        return Ok(updated);
    }

    /// <summary>
    /// Borra una tarea.
    /// </summary>
    /// <param name="id">Id de la tarea.</param>
    /// <param name="cancellationToken">Se cancela si el cliente corta la petición.</param>
    /// <response code="204">Tarea borrada (la respuesta no trae contenido).</response>
    /// <response code="404">No existe una tarea con ese id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _taskService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return TaskNotFound(id);
        }

        return NoContent();
    }

    // Respuesta 404 igual para todos los endpoints, en formato ProblemDetails.
    private ObjectResult TaskNotFound(int id)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Tarea no encontrada",
            detail: $"No existe una tarea con id {id}.");
    }
}
