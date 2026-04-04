using Avalonia;
using System;

namespace vTFMS.Views;

public partial class TsdRadarControl
{
    // =========================================================================
    // Constants — Lambert Conformal Conic standard parallels for US
    // =========================================================================

    private const double Phi1 = 33.0 * Math.PI / 180.0;
    private const double Phi2 = 45.0 * Math.PI / 180.0;

    // =========================================================================
    // Projection — Lambert Conformal Conic
    // =========================================================================

    private static (double x, double y) LccProject(
        double lat, double lon,
        double centerLat, double centerLon)
    {
        double phi = lat * Math.PI / 180.0;
        double lam = lon * Math.PI / 180.0;
        double phi0 = centerLat * Math.PI / 180.0;
        double lam0 = centerLon * Math.PI / 180.0;

        double n = Math.Log(Math.Cos(Phi1) / Math.Cos(Phi2)) /
                   Math.Log(Math.Tan(Math.PI / 4.0 + Phi2 / 2.0) /
                            Math.Tan(Math.PI / 4.0 + Phi1 / 2.0));

        double F = Math.Cos(Phi1) *
                   Math.Pow(Math.Tan(Math.PI / 4.0 + Phi1 / 2.0), n) / n;

        double rho = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi / 2.0), n);
        double rho0 = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi0 / 2.0), n);

        double theta = n * (lam - lam0);

        double x = rho * Math.Sin(theta);
        double y = rho0 - rho * Math.Cos(theta);

        return (x, y);
    }

    private static (double lat, double lon) LccInverse(
        double px, double py,
        double centerLat, double centerLon)
    {
        double phi0 = centerLat * Math.PI / 180.0;
        double lam0 = centerLon * Math.PI / 180.0;

        double n = Math.Log(Math.Cos(Phi1) / Math.Cos(Phi2)) /
                   Math.Log(Math.Tan(Math.PI / 4.0 + Phi2 / 2.0) /
                            Math.Tan(Math.PI / 4.0 + Phi1 / 2.0));

        double F = Math.Cos(Phi1) *
                   Math.Pow(Math.Tan(Math.PI / 4.0 + Phi1 / 2.0), n) / n;

        double rho0 = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi0 / 2.0), n);

        double rho = Math.Sign(n) * Math.Sqrt(
            px * px + (rho0 - py) * (rho0 - py));
        double theta = Math.Atan2(px, rho0 - py);

        double phi = 2.0 * Math.Atan(Math.Pow(F / rho, 1.0 / n))
                     - Math.PI / 2.0;
        double lam = lam0 + theta / n;

        return (phi * 180.0 / Math.PI, lam * 180.0 / Math.PI);
    }

    private Point LatLonToScreen(double lat, double lon,
                                  double width, double height)
    {
        double scale = Math.Min(width, height) * 0.45 * ZoomLevel;

        var (px, py) = LccProject(lat, lon, CenterLat, CenterLon);
        var (cx, cy) = LccProject(CenterLat, CenterLon, CenterLat, CenterLon);

        double x = width / 2.0 + (px - cx) * scale / (Math.PI / 180.0) / 57.0;
        double y = height / 2.0 - (py - cy) * scale / (Math.PI / 180.0) / 57.0;

        return new Point(x, y);
    }

    private bool IsOnScreen(Point pt, double width, double height)
    {
        return pt.X >= -20 && pt.X <= width + 20 &&
               pt.Y >= -20 && pt.Y <= height + 20;
    }
}