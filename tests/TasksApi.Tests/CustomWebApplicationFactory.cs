using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TasksApi.Data;

namespace TasksApi.Tests;

/// <summary>
/// Levanta la API completa en memoria para las pruebas.
/// Cambia la base de datos real (tasks.db) por una SQLite en memoria,
/// así las pruebas no tocan ni ensucian el archivo real.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // La base en memoria solo existe mientras esta conexión siga abierta.
    // Por eso se abre una vez y se comparte durante toda la prueba.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Quita la configuración de base de datos que puso Program.cs...
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // ...y la reemplaza por la conexión en memoria.
            // Program.cs llama a EnsureCreated(), que crea las tablas en esta base.
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // Al cerrar la conexión, la base en memoria se borra.
            _connection.Dispose();
        }
    }
}
