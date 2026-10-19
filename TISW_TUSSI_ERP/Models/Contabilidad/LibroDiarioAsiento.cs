using System;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class LibroDiarioAsiento
{
    public int IdAsiento { get; set; }
    public int NumeroAsiento { get; set; }
    public DateTime FechaAsiento { get; set; }
    public string GlosaDescripcion { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public DateTime CreatedAt { get; set; }
}
