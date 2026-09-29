using TISW_TUSSI_ERP.Models.Dashboard;

namespace TISW_TUSSI_ERP.Views.Dashboard;

// Dibuja el gráfico de dona con Microsoft.Maui.Graphics (sin librerías externas).
public class DonutDrawable : IDrawable
{
    public IList<MedioPago> Segmentos { get; set; } = new List<MedioPago>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float grosor = 22f;
        float lado = Math.Min(dirtyRect.Width, dirtyRect.Height) - grosor;
        float x = dirtyRect.Center.X - lado / 2;
        float y = dirtyRect.Center.Y - lado / 2;

        canvas.StrokeSize = grosor;
        canvas.StrokeLineCap = LineCap.Butt;

        var total = Segmentos.Sum(s => s.Monto);
        if (total <= 0)
        {
            canvas.StrokeColor = Color.FromArgb("#E5E9F0");
            canvas.DrawEllipse(x, y, lado, lado);
            return;
        }

        float acumulado = 0f; // grados recorridos desde las 12 en punto, en sentido horario
        foreach (var s in Segmentos)
        {
            float barrido = (float)(s.Monto / total * 360m);
            canvas.StrokeColor = s.Color;

            if (barrido >= 359.9f)
            {
                canvas.DrawEllipse(x, y, lado, lado);
                break;
            }

            // En Graphics: 0° = 3 en punto y crece en sentido antihorario.
            float inicio = Normalizar(90f - acumulado);
            float fin = Normalizar(90f - acumulado - Math.Max(barrido - 1.5f, 0.5f)); // 1.5° de separación
            canvas.DrawArc(x, y, lado, lado, inicio, fin, true, false);
            acumulado += barrido;
        }
    }

    private static float Normalizar(float grados) => ((grados % 360f) + 360f) % 360f;
}
