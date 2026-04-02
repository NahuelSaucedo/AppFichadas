using Npgsql;
using Dapper;
using FichajApp.Models;
using System.Text.Json;

namespace FichajApp.Services;

public class SupabaseService
{
    private readonly string _connStr;

    public SupabaseService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("Supabase")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DispositivoInfoHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }

    private NpgsqlConnection Conn() => new(_connStr);

    // ── EMPLEADOS ─────────────────────────────────────────────

    public async Task<Empleado?> BuscarPorDniAsync(string dni)
    {
        using var db = Conn();
        var rows = await db.QueryAsync<Empleado, Ubicacion?, Horario?, Empleado>(
            @"SELECT
                e.id, e.legajo, e.nombre, e.apellido, e.dni, e.activo,
                e.ubicacion_id, e.horario_id, e.created_at,
                u.id, u.nombre, u.direccion, u.latitud, u.longitud, u.radio_metros, u.created_at,
                h.id, h.nombre, h.hora_entrada, h.hora_salida, h.tolerancia_minutos, h.activo, h.created_at
              FROM empleados e
              LEFT JOIN ubicaciones u ON u.id = e.ubicacion_id
              LEFT JOIN horarios    h ON h.id = e.horario_id
              WHERE e.dni = @Dni AND e.activo = true",
            (emp, ubi, hor) => { emp.Ubicacion = ubi; emp.Horario = hor; return emp; },
            new { Dni = dni.Trim() },
            splitOn: "id,id");
        return rows.FirstOrDefault();
    }

    public async Task<List<Empleado>> GetEmpleadosAsync()
    {
        using var db = Conn();
        var rows = await db.QueryAsync<Empleado, Ubicacion?, Horario?, Empleado>(
            @"SELECT
                e.id, e.legajo, e.nombre, e.apellido, e.dni, e.activo,
                e.ubicacion_id, e.horario_id, e.created_at,
                u.id, u.nombre, u.direccion, u.latitud, u.longitud, u.radio_metros, u.created_at,
                h.id, h.nombre, h.hora_entrada, h.hora_salida, h.tolerancia_minutos, h.activo, h.created_at
              FROM empleados e
              LEFT JOIN ubicaciones u ON u.id = e.ubicacion_id
              LEFT JOIN horarios    h ON h.id = e.horario_id
              ORDER BY e.apellido, e.nombre",
            (emp, ubi, hor) => { emp.Ubicacion = ubi; emp.Horario = hor; return emp; },
            splitOn: "id,id");
        return rows.ToList();
    }

    // ── FICHADAS ──────────────────────────────────────────────

    /// Última fichada del empleado en el DÍA ARGENTINO actual
    public async Task<Fichada?> GetUltimaFichadaHoyAsync(Guid empleadoId)
    {
        // Calculamos inicio y fin del día en Argentina y los mandamos como UTC a Postgres
        var hoyArg = TimeHelper.HoyArgentina();
        var inicioUtc = TimeHelper.AHoraArgentina(DateTime.UtcNow).Date
            .Subtract(TimeHelper.Ahora() - DateTime.UtcNow); // roundtrip seguro

        // Forma más simple y correcta: comparar en el timezone de Postgres
        using var db = Conn();
        return await db.QueryFirstOrDefaultAsync<Fichada>(
            @"SELECT id, empleado_id, ubicacion_id, tipo, fecha_hora
              FROM fichadas
              WHERE empleado_id = @id
                AND (fecha_hora AT TIME ZONE 'America/Argentina/Buenos_Aires')::date = @hoy
              ORDER BY fecha_hora DESC
              LIMIT 1",
            new { id = empleadoId, hoy = hoyArg });
    }

    public async Task<Fichada> RegistrarFichadaAsync(Fichada f)
    {
        using var db = Conn();
        var jsonInfo = JsonSerializer.Serialize(f.DispositivoInfo,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var id = await db.ExecuteScalarAsync<long>(
            @"INSERT INTO fichadas (empleado_id, ubicacion_id, tipo, fecha_hora, dispositivo_info)
              VALUES (@EmpleadoId, @UbicacionId, @Tipo, @FechaHora, @Info::jsonb)
              RETURNING id",
            new { f.EmpleadoId, f.UbicacionId, f.Tipo, FechaHora = f.FechaHora, Info = jsonInfo });
        f.Id = id;
        return f;
    }

    public async Task<List<Fichada>> GetUltimasFichadasEmpleadoAsync(Guid empleadoId, int n = 3)
    {
        using var db = Conn();
        var rows = await db.QueryAsync<Fichada, string?, Fichada>(
            @"SELECT f.id, f.empleado_id, f.ubicacion_id, f.tipo, f.fecha_hora, f.created_at,
                     u.nombre as ub_nombre
              FROM fichadas f
              LEFT JOIN ubicaciones u ON u.id = f.ubicacion_id
              WHERE f.empleado_id = @id
              ORDER BY f.fecha_hora DESC LIMIT @n",
            (fichada, ubNombre) =>
            {
                if (ubNombre != null)
                    fichada.Ubicacion = new Ubicacion { Nombre = ubNombre };
                return fichada;
            },
            new { id = empleadoId, n },
            splitOn: "ub_nombre");
        return rows.ToList();
    }

    public async Task<List<FichadaVista>> GetFichadasAsync(int limit = 200)
    {
        using var db = Conn();
        var rows = await db.QueryAsync<FichadaVista>(
            @"SELECT f.id, f.tipo,
                     -- Convertir a GMT-3 directamente en Postgres
                     f.fecha_hora AT TIME ZONE 'America/Argentina/Buenos_Aires' AS fecha_hora,
                     e.nombre || ' ' || e.apellido AS empleado_nombre,
                     e.dni AS empleado_dni,
                     e.legajo AS empleado_legajo,
                     u.nombre AS ubicacion_nombre,
                     (f.dispositivo_info->>'distanciaMetros')::float AS distancia_metros,
                     (f.dispositivo_info->>'valido')::boolean AS valido
              FROM fichadas f
              LEFT JOIN empleados e ON e.id = f.empleado_id
              LEFT JOIN ubicaciones u ON u.id = f.ubicacion_id
              ORDER BY f.fecha_hora DESC LIMIT @limit",
            new { limit });
        return rows.ToList();
    }
}

// ── DTOs y handlers ───────────────────────────────────────────

public class FichadaVista
{
    public long Id { get; set; }
    public string Tipo { get; set; } = "";
    public DateTime FechaHora { get; set; }  // Ya viene convertida desde Postgres
    public string EmpleadoNombre { get; set; } = "";
    public string? EmpleadoDni { get; set; }
    public string? EmpleadoLegajo { get; set; }
    public string? UbicacionNombre { get; set; }
    public double? DistanciaMetros { get; set; }
    public bool Valido { get; set; }
}

public class DispositivoInfoHandler : SqlMapper.TypeHandler<DispositivoInfo?>
{
    public override DispositivoInfo? Parse(object value)
    {
        if (value is string s)
            return JsonSerializer.Deserialize<DispositivoInfo>(s,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return null;
    }
    public override void SetValue(System.Data.IDbDataParameter p, DispositivoInfo? v)
        => p.Value = v == null ? DBNull.Value : JsonSerializer.Serialize(v);
}

public class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value) => (TimeSpan)value;
    public override void SetValue(System.Data.IDbDataParameter p, TimeSpan v) => p.Value = v;
}
