
namespace TISW_TUSSI_ERP.Models.Ventas;

public class Caja
{
    public int IdCaja { get; set; }
    public string NombreCaja { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Activa { get; set; }
}

public class CajaTurno
{
    public int IdTurno { get; set; }
    public int IdCaja { get; set; }
    public int IdUsuarioApertura { get; set; }
    public int? IdUsuarioCierre { get; set; }

    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }

    public decimal MontoInicial { get; set; }
    public decimal? MontoFinalEsperado { get; set; }
    public decimal? MontoFinalReal { get; set; }
    public decimal? Diferencia { get; set; }

    public string Estado { get; set; } = "ABIERTA";
    public string Observaciones { get; set; } = "";

    public bool EstaAbierta => Estado == "ABIERTA";
}
