using Microsoft.AspNetCore.Mvc;
using TasksApi.Dtos;
using TasksApi.Services;

namespace TasksApi.Controllers;

/// <summary>
/// Endpoints HTTP de las tareas. Solo recibe la petición, llama al servicio y arma la respuesta.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Devuelve todas las tareas. Si no hay ninguna, devuelve una lista vacía.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Devuelve una tarea por su id, o 404 si no existe.
    /// </summary>
    // ":int" hace que /api/tasks/abc no entre aquí (responde 404 en vez de error).
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    /// Crea una tarea nueva. Responde 201 con la tarea creada y su dirección en el header Location.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Reemplaza los datos de una tarea. Responde 200 con la tarea actualizada, o 404 si no existe.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
    /// Borra una tarea. Responde 204 sin contenido, o 404 si no existe.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
