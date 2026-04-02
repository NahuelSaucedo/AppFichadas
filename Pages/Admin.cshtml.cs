using Microsoft.AspNetCore.Mvc.RazorPages;
using FichajApp.Services;

namespace FichajApp.Pages;

public class AdminModel : PageModel
{
    private readonly SupabaseService _supa;
    public AdminModel(SupabaseService supa) => _supa = supa;

    public List<FichadaVista> Fichadas { get; set; } = new();
    public int TotalEntradas { get; set; }
    public int TotalSalidas { get; set; }
    public int TotalInvalidas { get; set; }

    public async Task OnGetAsync()
    {
        // FechaHora ya viene en GMT-3 desde la query SQL (AT TIME ZONE)
        Fichadas = await _supa.GetFichadasAsync(200);
        TotalEntradas  = Fichadas.Count(f => f.Tipo == "ENTRADA" && f.Valido);
        TotalSalidas   = Fichadas.Count(f => f.Tipo == "SALIDA"  && f.Valido);
        TotalInvalidas = Fichadas.Count(f => !f.Valido);
    }
}
