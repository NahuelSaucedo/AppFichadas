namespace FichajApp.Models;

public class Empleado
{
    public Guid Id { get; set; }
    public string Legajo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Dni { get; set; }
    public bool Activo { get; set; } = true;
    public Guid? UbicacionId { get; set; }
    public Guid? HorarioId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navegación (se completa manualmente en el servicio)
    public Ubicacion? Ubicacion { get; set; }
    public Horario? Horario { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string Iniciales => (Nombre.Length > 0 && Apellido.Length > 0)
        ? $"{Nombre[0]}{Apellido[0]}".ToUpper() : "??";
}

public class Ubicacion
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public int RadioMetros { get; set; } = 100;
    public DateTime CreatedAt { get; set; }
}

public class Horario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public TimeSpan HoraEntrada { get; set; }
    public TimeSpan HoraSalida  { get; set; }
    public int ToleranciaMInutos { get; set; } = 15;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public string HoraEntradaStr => $"{HoraEntrada:hh\\:mm}";
    public string HoraSalidaStr  => $"{HoraSalida:hh\\:mm}";

    // Soporta turnos que cruzan medianoche (ej: 22:00 → 06:00)
    public bool EstaEnRango(TimeSpan horaActual)
    {
        var inicio = HoraEntrada - TimeSpan.FromMinutes(ToleranciaMInutos);
        var fin    = HoraSalida  + TimeSpan.FromMinutes(ToleranciaMInutos);

        if (inicio < fin)
            // Turno normal: 08:00 → 17:00
            return horaActual >= inicio && horaActual <= fin;
        else
            // Turno nocturno: 22:00 → 06:00
            return horaActual >= inicio || horaActual <= fin;
    }
}

public class Fichada
{
    public long Id { get; set; }
    public Guid EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public Guid? UbicacionId { get; set; }
    public Ubicacion? Ubicacion { get; set; }
    public string Tipo { get; set; } = "ENTRADA";
    public DateTime FechaHora { get; set; }
    public DispositivoInfo? DispositivoInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DispositivoInfo
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double Precision { get; set; }
    public double DistanciaMetros { get; set; }
    public bool Valido { get; set; }
    public string? UserAgent { get; set; }
}

public class ValidacionFichada
{
    public bool Permitido { get; set; }
    public string? MotivoBloqueo { get; set; }
    public bool FueraDeUbicacion { get; set; }
    public bool FueraDeHorario { get; set; }
    public bool SalidaSinEntrada { get; set; }
    public bool SinUbicacionAsignada { get; set; }
    public bool SinHorarioAsignado { get; set; }
    public double DistanciaMetros { get; set; }
}
