using FichajApp.Models;

namespace FichajApp.Services;

public static class GeoService
{
    private const double EarthRadiusMeters = 6371000;

    public static double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    public static (bool valido, double distancia, Ubicacion? punto) ValidarUbicacion(
        double lat, double lon, IEnumerable<Ubicacion> ubicaciones)
    {
        Ubicacion? masCercana = null;
        double menorDist = double.MaxValue;
        foreach (var u in ubicaciones)
        {
            var d = CalcularDistancia(lat, lon, u.Latitud, u.Longitud);
            if (d < menorDist) { menorDist = d; masCercana = u; }
        }
        bool valido = masCercana != null && menorDist <= masCercana.RadioMetros;
        return (valido, Math.Round(menorDist, 1), masCercana);
    }
}
