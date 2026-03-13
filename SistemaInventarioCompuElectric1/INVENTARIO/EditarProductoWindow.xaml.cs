using SistemaInventarioCompuElectric1.SERVICIOS;
using System;
using System.Windows;

namespace SistemaInventarioCompuElectric1.INVENTARIO
{
    public partial class EditarProductoWindow : Window
    {
        private FirebaseService _firebaseService;
        private ProductoModel _productoOriginal;
        public bool ProductoActualizado { get; private set; }

        public EditarProductoWindow(ProductoModel producto)
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            _productoOriginal = producto;
            ProductoActualizado = false;
            CargarDatosProducto();
        }

        private void CargarDatosProducto()
        {
            // Cargar los datos del producto en los campos
            txtId.Text = _productoOriginal.Id;
            txtNombre.Text = _productoOriginal.nombre;
            txtCodigo.Text = _productoOriginal.codigo;
            txtCantidad.Text = _productoOriginal.cantidad.ToString();
            txtPrecio.Text = _productoOriginal.precio.ToString();
            txtEstante.Text = _productoOriginal.estante;
            txtFila.Text = _productoOriginal.fila;
            txtImagenURL.Text = _productoOriginal.imagenURL;

            // Seleccionar la categoría correcta en el ComboBox
            string categoriaActual = _productoOriginal.categoria;

            if (categoriaActual == "Electronica")
                cmbCategoria.SelectedIndex = 0; // Electrónica
            else if (categoriaActual == "Robotica")
                cmbCategoria.SelectedIndex = 1; // Robótica
            else if (categoriaActual == "Accesorios")
                cmbCategoria.SelectedIndex = 2; // Accesorios
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    txtEstado.Text = "El nombre del producto es obligatorio";
                    txtNombre.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCodigo.Text))
                {
                    txtEstado.Text = "El código del producto es obligatorio";
                    txtCodigo.Focus();
                    return;
                }

                // Validar cantidad
                if (!int.TryParse(txtCantidad.Text, out int cantidad))
                {
                    txtEstado.Text = "La cantidad debe ser un número válido";
                    txtCantidad.Focus();
                    return;
                }

                // Validar precio
                if (!double.TryParse(txtPrecio.Text, out double precio))
                {
                    txtEstado.Text = "El precio debe ser un número válido";
                    txtPrecio.Focus();
                    return;
                }

                // Deshabilitar botón mientras se guarda
                btnActualizar.IsEnabled = false;
                txtEstado.Text = "Actualizando producto...";
                txtEstado.Foreground = System.Windows.Media.Brushes.Blue;

                // Determinar la categoría para la colección
                string nombreColeccion;
                if (_productoOriginal.categoria == "Electronica")
                    nombreColeccion = "electronica";
                else if (_productoOriginal.categoria == "Robotica")
                    nombreColeccion = "robotica";
                else
                    nombreColeccion = "productos"; // Para Accesorios

                // Crear el producto actualizado
                var productoActualizado = new ProductoModel
                {
                    Id = _productoOriginal.Id, // Mantener el mismo ID
                    nombre = txtNombre.Text.Trim(),
                    codigo = txtCodigo.Text.Trim(),
                    categoria = _productoOriginal.categoria, // Mantener la misma categoría
                    cantidad = cantidad,
                    precio = precio,
                    estante = txtEstante.Text.Trim(),
                    fila = txtFila.Text.Trim(),
                    imagenURL = txtImagenURL.Text.Trim()
                };

                // Mostrar en Debug para verificar
                System.Diagnostics.Debug.WriteLine($"Actualizando producto ID: {productoActualizado.Id}");

                // Actualizar en Firebase (necesitas crear este método)
                bool resultado = await _firebaseService.ActualizarProducto(productoActualizado, nombreColeccion);

                if (resultado)
                {
                    ProductoActualizado = true;
                    MessageBox.Show($"✅ Producto '{txtNombre.Text}' actualizado exitosamente",
                                   "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    txtEstado.Text = "Error al actualizar el producto";
                    txtEstado.Foreground = System.Windows.Media.Brushes.Red;
                    btnActualizar.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                txtEstado.Text = $"Error: {ex.Message}";
                txtEstado.Foreground = System.Windows.Media.Brushes.Red;
                btnActualizar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de cancelar? Los cambios no se guardarán.",
                                        "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }
}