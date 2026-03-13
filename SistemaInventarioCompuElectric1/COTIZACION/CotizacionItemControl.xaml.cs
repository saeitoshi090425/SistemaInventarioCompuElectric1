using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // ← AGREGAR ESTE USING
using System.Windows.Media.Imaging;


namespace SistemaInventarioCompuElectric1.COTIZACION
{
    public partial class CotizacionItemControl : UserControl
    {
        public CotizacionItemModel Item { get; set; }
        public string ImagenURL { get; set; }
        public event EventHandler<CotizacionItemModel> OnCantidadCambio;
        public event EventHandler<CotizacionItemModel> OnEliminar;

        public CotizacionItemControl(CotizacionItemModel item, string imagenURL = "")
        {
            InitializeComponent();
            Item = item;
            ImagenURL = imagenURL;
            ActualizarUI();
        }

        private void ActualizarUI()
        {
            txtNombre.Text = Item.Nombre;
            txtCodigo.Text = Item.Codigo;
            txtCantidad.Text = Item.Cantidad.ToString();
            txtSubtotal.Text = $"S/ {Item.Subtotal:F2}";
        }

        private void BtnMas_Click(object sender, RoutedEventArgs e)
        {
            Item.Cantidad++;
            ActualizarUI();
            OnCantidadCambio?.Invoke(this, Item);
        }

        private void BtnMenos_Click(object sender, RoutedEventArgs e)
        {
            if (Item.Cantidad > 1)
            {
                Item.Cantidad--;
                ActualizarUI();
                OnCantidadCambio?.Invoke(this, Item);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            OnEliminar?.Invoke(this, Item);
        }

        private void Imagen_Loaded(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                if (!string.IsNullOrEmpty(ImagenURL))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(ImagenURL, UriKind.Absolute);
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