using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TasksApi.Data;

/// <summary>
/// SQLite guarda las fechas como texto y olvida que estaban en UTC.
/// Este conversor se las vuelve a marcar como UTC al leerlas, para que el JSON salga con "Z".
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    // Al guardar: si la fecha no viene en UTC, se convierte.
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    // Al leer: se le indica que es UTC (el valor no cambia).
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
