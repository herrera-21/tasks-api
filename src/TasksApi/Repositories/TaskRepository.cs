using Microsoft.EntityFrameworkCore;
using TasksApi.Data;
using TasksApi.Models;

namespace TasksApi.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Las tareas solo se van a leer, así que EF Core no necesita vigilar si cambian.
        return await db.Tasks
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // EF Core sí vigila esta tarea, para poder modificarla o borrarla después.
        return db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return task; // Ya trae el Id que le asignó la base de datos.
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        db.Tasks.Update(task);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
    }
}
