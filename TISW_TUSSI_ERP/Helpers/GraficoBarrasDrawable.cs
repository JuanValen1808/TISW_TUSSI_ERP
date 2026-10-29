namespace TISW_TUSSI_ERP.Helpers;

// Gráfico de barras agrupadas dibujado con Microsoft.Maui.Graphics (sin librerías externas).
// Se usa en los reportes contables (Flujo de Caja, Análisis por Cuenta, Antigüedad de Saldos).
public class SerieGrafico
{
    public string Nombre { get; init; } = string.Empty;
    public Color Color { get; init; } = Colors.SteelBlue;
    public IList<double> Valores { get; init; } = new List<double>();
}

public class GraficoBarrasDrawable : IDrawable
{
    public IList<string> Categorias { get; set; } = new List<string>();
    public IList<SerieGrafico> Series { get; set; } = new List<SerieGrafico>();

    private static readonly Color ColorTexto = Color.FromArgb("#94A3B8");
    private static readonly Color ColorGrilla = Color.FromArgb("#EEF2F7");

    public void Draw(ICanvas canvas, RectF r)
    {
        const float izq = 64f, der = 12f, arriba = 14f, abajo = 30f;
        var area = new RectF(r.X + izq, r.Y + arriba, r.Width - izq - der, r.Height - arriba - abajo);

        canvas.FontSize = 10;
        canvas.FontColor = ColorTexto;

        var valores = Series.SelectMany(s => s.Valores).ToList();
        double max = valores.Count == 0 ? 0 : valores.Max();
        int n = Categorias.Count;

        if (n == 0 || max <= 0 || area.Width < 20 || area.Height < 20)
        {
            canvas.FontSize = 12;
            canvas.DrawString("Sin datos para graficar en el período", r.X, r.Y, r.Width, r.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        max = MaximoRedondo(max);

        // Grilla horizontal + etiquetas del eje Y
        canvas.StrokeColor = ColorGrilla;
        canvas.StrokeSize = 1;
        for (int i = 0; i <= 4; i++)
        {
            float y = area.Bottom - area.Height * i / 4f;
            canvas.DrawLine(area.Left, y, area.Right, y);
            canvas.DrawString(Formato.MonedaCorta((decimal)(max * i / 4)), r.X, y - 8, izq - 10, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        int cantidadSeries = Math.Max(1, Series.Count);
        float grupo = area.Width / n;
        const float espacio = 4f;
        float anchoBarra = Math.Clamp((grupo * 0.7f - espacio * (cantidadSeries - 1)) / cantidadSeries, 4f, 28f);
        float anchoTotal = anchoBarra * cantidadSeries + espacio * (cantidadSeries - 1);

        for (int c = 0; c < n; c++)
        {
            float x0 = area.Left + grupo * c + (grupo - anchoTotal) / 2f;
            for (int k = 0; k < Series.Count; k++)
            {
                var serie = Series[k];
                double v = c < serie.Valores.Count ? Math.Max(0, serie.Valores[c]) : 0;
                float h = (float)(v / max * area.Height);
                if (h <= 0) continue;

                canvas.FillColor = serie.Color;
                canvas.FillRoundedRectangle(x0 + k * (anchoBarra + espacio), area.Bottom - h, anchoBarra, h, Math.Min(5f, h / 2f));
            }

            canvas.FontColor = ColorTexto;
            canvas.FontSize = 10;
            canvas.DrawString(Categorias[c], area.Left + grupo * c, area.Bottom + 8, grupo, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static double MaximoRedondo(double v)
    {
        var exp = Math.Pow(10, Math.Floor(Math.Log10(v)));
        var f = v / exp;
        double nf = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
        return nf * exp;
    }
}
