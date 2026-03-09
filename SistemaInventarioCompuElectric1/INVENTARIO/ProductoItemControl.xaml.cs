using SistemaInventarioCompuElectric1.INVENTARIO;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SistemaInventarioCompuElectric1.INVENTARIO
{
    public partial class ProductoItemControl : UserControl
    {
        public ProductoModel Producto { get; set; }
        public string Categoria { get; set; }
        public event EventHandler<ProductoModel> OnEditar;
        public event EventHandler<ProductoModel> OnEliminar;

        public ProductoItemControl(ProductoModel producto, string categoria)
        {
            InitializeComponent();
            Producto = producto;
            Categoria = categoria;
            DataContext = producto;

            // Asegurar que la categoría se muestre correctamente
            if (string.IsNullOrEmpty(producto.categoria))
            {
                producto.categoria = categoria;
            }
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
                        bitmap.DecodePixelWidth = 200; // Limitar tamaño para mejor rendimiento
                        bitmap.EndInit();
                        image.Source = bitmap;

                        // Ocultar el texto "Sin imagen"
                        var txtSinImagen = this.FindName("txtSinImagen") as TextBlock;
                        if (txtSinImagen != null)
                            txtSinImagen.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cargando imagen: {ex.Message}");
                        image.Source = null;

                        // Mostrar texto "Sin imagen"
                        var txtSinImagen = this.FindName("txtSinImagen") as TextBlock;
                        if (txtSinImagen != null)
                            txtSinImagen.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // No hay URL de imagen
                    var txtSinImagen = this.FindName("txtSinImagen") as TextBlock;
                    if (txtSinImagen != null)
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

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            OnEditar?.Invoke(this, Producto);
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            OnEliminar?.Invoke(this, Producto);
        }
    }
}