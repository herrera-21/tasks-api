# Tasks API

API REST para gestionar tareas (crear, listar, ver, actualizar y borrar), hecha en .NET 10 con SQLite.

## Stack

- **.NET 10 / ASP.NET Core**: tipado fuerte, validación integrada y buen rendimiento.
- **EF Core + SQLite**: base de datos en un solo archivo, sin instalar nada extra.
- **Swagger**: documentación y pruebas desde el navegador.
- **xUnit**: pruebas unitarias y de integración.
- **Docker** y **GitHub Actions** con Semgrep y Trivy.

## Estructura

```
src/TasksApi/
  Controllers/    Endpoints HTTP
  Services/       Reglas de negocio
  Repositories/   Acceso a la base de datos
  Dtos/           Datos que entran y salen de la API
  Models/         Entidad TaskItem
  Data/           AppDbContext (EF Core)
  Errors/         Manejo global de errores
tests/TasksApi.Tests/
  Unit/           Pruebas de TaskService
  *.cs            Pruebas de integración de los endpoints
```

## Cómo correrla

**Local** (requiere .NET 10 SDK):

```
dotnet run --project src/TasksApi
```

Abrir http://localhost:5233 (redirige a Swagger).

**Con Docker:**

```
docker compose up -d --build
```

Abrir http://localhost:8080. Las tareas se guardan en un volumen y no se pierden al reiniciar.
Para detenerla: `docker compose down` (agregar `-v` para borrar también los datos).

**Pruebas:**

```
dotnet test
```

## Endpoints

| Método | Ruta | Descripción | Respuestas |
|---|---|---|---|
| GET | `/api/tasks` | Lista todas las tareas | 200 |
| GET | `/api/tasks/{id}` | Obtiene una tarea | 200, 404 |
| POST | `/api/tasks` | Crea una tarea | 201, 400 |
| PUT | `/api/tasks/{id}` | Actualiza una tarea (reemplazo completo) | 200, 400, 404 |
| DELETE | `/api/tasks/{id}` | Borra una tarea | 204, 404 |

**Crear una tarea**

```
POST /api/tasks
{
  "title": "Comprar café",
  "description": "Para la oficina"
}
```

Respuesta `201 Created`:

```json
{
  "id": 1,
  "title": "Comprar café",
  "description": "Para la oficina",
  "isCompleted": false,
  "createdAt": "2026-11-11T17:30:00Z",
  "updatedAt": null
}
```

**Actualizar una tarea**: `title` e `isCompleted` son obligatorios.

```
PUT /api/tasks/1
{
  "title": "Comprar café",
  "isCompleted": true
}
```

**Errores**: se devuelven en formato [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457). Ejemplo de `400`:

```json
{
  "title": "Uno o más datos no son válidos.",
  "status": 400,
  "errors": {
    "Title": ["El título es obligatorio."]
  }
}
```

Reglas: el título es obligatorio (máximo 200 caracteres), la descripción es opcional (máximo 1000) y las fechas están en UTC.

## CI

En cada PR hacia `main` o `dev`, GitHub Actions:

1. Compila y corre las pruebas.
2. Analiza el código con **Semgrep** (C#, Dockerfile y secretos).
3. Construye la imagen Docker, prueba que la API responda y la revisa con **Trivy** (vulnerabilidades HIGH y CRITICAL).

Los resultados de Semgrep y Trivy se ven en la pestaña **Security** del repositorio.
