namespace FichajApp.Services;

/// <summary>
/// Centraliza todas las operaciones de fecha/hora en GMT-3 (Argentina).
/// Usar SIEMPRE esta clase — nunca DateTime.Now, DateTime.UtcNow ni .ToLocalTime() directo.
/// </summary>
public static class TimeHelper
{
    private static readonly TimeZoneInfo Tz =
        TimeZoneInfo.FindSystemTimeZoneById(
            // Linux (Docker/Linux server): "America/Argentina/Buenos_Aires"
            // Windows: "Argentina Standard Time"
            OperatingSystem.IsWindows()
                ? "Argentina Standard Time"
                : "America/Argentina/Buenos_Aires"
        );

    /// Hora actual en Argentina (GMT-3)
    public static DateTime Ahora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    /// Convierte un DateTime UTC guardado en DB → hora Argentina
    public static DateTime AHoraArgentina(DateTime utc)
    {
        var kind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(kind, Tz);
    }

    /// Hora actual de Argentina como TimeSpan (para comparar con horarios)
    public static TimeSpan HoraActual() => Ahora().TimeOfDay;

    /// Fecha de hoy en Argentina (sin hora) — para comparar fichadas del día
    public static DateTime HoyArgentina() => Ahora().Date;
}
