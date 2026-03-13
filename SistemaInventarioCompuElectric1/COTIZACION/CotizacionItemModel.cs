// COTIZACION/CotizacionItemModel.cs
namespace SistemaInventarioCompuElectric1.COTIZACION
{
    public class CotizacionItemModel
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public string Categoria { get; set; }
        public double PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public double Subtotal => PrecioUnitario * Cantidad;
    }
}