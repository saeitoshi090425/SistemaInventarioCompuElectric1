using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // ← AGREGAR ESTE USING
using System.Windows.Media.Imaging;

namespace SistemaInventarioCompuElectric1.COTIZACION
{
    public partial class ProductoCotizacionItem : UserControl
    {
        public INVENTARIO.ProductoModel Producto { get; set; }
        public event EventHandler<INVENTARIO.ProductoModel> OnProductoSeleccionado;

        public ProductoCotizacionItem(INVENTARIO.ProductoModel producto)
        {
            InitializeComponent();
            Producto = producto;
            DataContext = producto;

            txtNombre.Text = producto.nombre;
            txtCodigo.Text = producto.codigo;
            txtCantidadDisponible.Text = producto.cantidad.ToString();
            txtPrecio.Text = $"S/ {producto.precio:F2}";

            // Formatear categoría para mostrar
            string categoriaMostrar = producto.categoria;
            if (producto.categoria == "Electronica")
                categoriaMostrar = "Electrónica";
            else if (producto.categoria == "Robotica")
                categoriaMostrar = "Robótica";

            txtCategoria.Text = categoriaMostrar;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnProductoSeleccionado?.Invoke(this, Producto);
        }

        private void Imagen_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                if (!string.IsNullOrEmpty(Producto.imagenURL))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(Producto.imagenURL, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 150;
                        bitmap.EndInit();
                        image.Source = bitmap;
                        image.Stretch = Stretch.Uniform; // Ahora funciona con using System.Windows.Media
                        txtSinImagen.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cargando imagen: {ex.Message}");
                        image.Source = null;
                        txtSinImagen.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    txtSinImagen.Visibility = Visibility.Visible;
                }
            }
        }

        private void Imagen_Unloaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            if (image?.Source is BitmapImage bitmap)
            {
                bitmap.StreamSource?.Dispose();
                image.Source = null;
            }
        }
    }
}