using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FichajApp.Models;
using FichajApp.Services;

namespace FichajApp.Pages;

public class FicharModel : PageModel
{
    private readonly SupabaseService _supa;
    public FicharModel(SupabaseService supa) => _supa = supa;

    public Empleado? EmpleadoEncontrado { get; set; }
    public List<Fichada> UltimasFichadas { get; set; } = new();
    public Fichada? UltimaFichada { get; set; }
    public string? Resultado { get; set; }
    public bool Exito { get; set; }
    public string? ErrorDNI { get; set; }
    public string Paso { get; set; } = "inicio";
    public string TipoRegistrado { get; set; } = "";
    public ValidacionFichada? Validacion { get; set; }
    public DateTime AhoraArgentina { get; set; }

    public Task OnGetAsync()
    {
        AhoraArgentina = TimeHelper.Ahora();
        return Task.CompletedTask;
    }

    // Paso 1: buscar empleado por DNI
    public async Task<IActionResult> OnPostBuscarEmpleadoAsync([FromForm] string dni)
    {
        AhoraArgentina = TimeHelper.Ahora();
        var empleado = await _supa.BuscarPorDniAsync(dni);

        if (empleado == null)
        {
            ErrorDNI = $"No se encontro ningun empleado activo con DNI {dni}.";
            Paso = "inicio";
            return Page();
        }

        EmpleadoEncontrado = empleado;
        UltimasFichadas = await _supa.GetUltimasFichadasEmpleadoAsync(empleado.Id, 3);
        UltimaFichada = UltimasFichadas.FirstOrDefault();
        Paso = "confirmar";
        return Page();
    }

    // Paso 2: registrar con GPS
    public async Task<IActionResult> OnPostRegistrarAsync(
        [FromForm] string empleadoId,
        [FromForm] double lat,
        [FromForm] double lon,
        [FromForm] double precision,
        [FromForm] string tipo)
    {
        AhoraArgentina = TimeHelper.Ahora();

        if (!Guid.TryParse(empleadoId, out var empGuid))
            return RedirectToPage();

        var empleados = await _supa.GetEmpleadosAsync();
        var empleado = empleados.FirstOrDefault(e => e.Id == empGuid);
        if (empleado == null) return RedirectToPage();

        EmpleadoEncontrado = empleado;
        var validacion = new ValidacionFichada();

        // VALIDACION 1: Sin ubicacion asignada
        if (empleado.UbicacionId == null || empleado.Ubicacion == null)
        {
            validacion.Permitido = false;
            validacion.SinUbicacionAsignada = true;
            validacion.MotivoBloqueo = "Tu usuario no tiene una ubicacion asignada. Contacta al administrador.";
            return await MostrarResultado(empGuid, validacion, tipo);
        }

        var ubicacion = empleado.Ubicacion;

        // VALIDACION 2: Distancia GPS — UNICO BLOQUEO
        var distancia = Math.Round(GeoService.CalcularDistancia(lat, lon, ubicacion.Latitud, ubicacion.Longitud), 1);
        validacion.DistanciaMetros = distancia;

        if (distancia > ubicacion.RadioMetros)
        {
            validacion.Permitido = false;
            validacion.FueraDeUbicacion = true;
            validacion.MotivoBloqueo =
                $"Estas a <strong>{distancia} m</strong> de <strong>{ubicacion.Nombre}</strong>.<br>" +
                $"<small>Necesitas estar dentro de los {ubicacion.RadioMetros} m permitidos.</small>";
            return await MostrarResultado(empGuid, validacion, tipo);
        }

        // VALIDACION 3: No SALIDA sin ENTRADA previa hoy
        if (tipo == "SALIDA")
        {
            var ultimaHoy = await _supa.GetUltimaFichadaHoyAsync(empGuid);
            if (ultimaHoy == null || ultimaHoy.Tipo != "ENTRADA")
            {
                validacion.Permitido = false;
                validacion.SalidaSinEntrada = true;
                validacion.MotivoBloqueo =
                    "No podes registrar <strong>SALIDA</strong> sin haber registrado " +
                    "<strong>ENTRADA</strong> primero hoy.";
                return await MostrarResultado(empGuid, validacion, tipo);
            }
        }

        // TODO OK: registrar
        validacion.Permitido = true;
        var fichada = new Fichada
        {
            EmpleadoId  = empGuid,
            UbicacionId = ubicacion.Id,
            Tipo        = tipo == "SALIDA" ? "SALIDA" : "ENTRADA",
            FechaHora   = DateTime.UtcNow,
            DispositivoInfo = new DispositivoInfo
            {
                Latitud = lat,
                Longitud = lon,
                Precision = precision,
                DistanciaMetros = distancia,
                Valido = true,
                UserAgent = Request.Headers.UserAgent.ToString()
            }
        };

        await _supa.RegistrarFichadaAsync(fichada);
        fichada.Ubicacion = ubicacion;

        TipoRegistrado = fichada.Tipo;
        Resultado = $"Registrada en <strong>{ubicacion.Nombre}</strong> &middot; {distancia} m del punto";
        Exito = true;
        Validacion = validacion;
        return await MostrarResultado(empGuid, validacion, tipo, registrado: true);
    }

    private async Task<IActionResult> MostrarResultado(
        Guid empGuid, ValidacionFichada validacion, string tipo, bool registrado = false)
    {
        if (!registrado)
        {
            Resultado = validacion.MotivoBloqueo;
            Exito = false;
            TipoRegistrado = tipo;
        }
        Validacion = validacion;
        UltimasFichadas = await _supa.GetUltimasFichadasEmpleadoAsync(empGuid, 3);
        UltimaFichada = UltimasFichadas.FirstOrDefault();
        Paso = "resultado";
        return Page();
    }
}
