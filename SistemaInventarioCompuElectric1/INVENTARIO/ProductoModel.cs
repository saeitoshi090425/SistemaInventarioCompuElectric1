// INVENTARIO/ProductoModel.cs
using Google.Cloud.Firestore;

namespace SistemaInventarioCompuElectric1.INVENTARIO
{
    [FirestoreData]
    public class ProductoModel
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public int cantidad { get; set; }

        [FirestoreProperty]
        public string categoria { get; set; }

        [FirestoreProperty]
        public string codigo { get; set; }

        [FirestoreProperty]
        public string estante { get; set; }

        [FirestoreProperty]
        public string fila { get; set; }

        [FirestoreProperty]
        public string imagenURL { get; set; }

        [FirestoreProperty]
        public string nombre { get; set; }

        [FirestoreProperty]
        public double precio { get; set; }
    }
}