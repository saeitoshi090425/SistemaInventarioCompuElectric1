using SistemaInventarioCompuElectric1.INVENTARIO;
using SistemaInventarioCompuElectric1.SERVICIOS;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SistemaInventarioCompuElectric1.INVENTARIO
{
    public partial class AgregarProductoWindow : Window
    {
        private FirebaseService _firebaseService;
        public bool ProductoAgregado { get; private set; }

        public AgregarProductoWindow()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            ProductoAgregado = false;
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
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

                if (cmbCategoria.SelectedItem == null)
                {
                    txtEstado.Text = "Debe seleccionar una categoría";
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
                btnGuardar.IsEnabled = false;
                txtEstado.Text = "Guardando producto...";
                txtEstado.Foreground = System.Windows.Media.Brushes.Blue;

                // Obtener la categoría seleccionada
                ComboBoxItem itemSeleccionado = (ComboBoxItem)cmbCategoria.SelectedItem;
                string categoriaSeleccionada = itemSeleccionado.Content.ToString();

                // Determinar la categoría real para Firebase
                string categoriaReal;
                string nombreColeccion;

                if (categoriaSeleccionada == "Electrónica")
                {
                    categoriaReal = "Electronica";
                    nombreColeccion = "electronica";
                }
                else if (categoriaSeleccionada == "Robótica")
                {
                    categoriaReal = "Robotica";
                    nombreColeccion = "robotica";
                }
                else // Accesorios
                {
                    categoriaReal = "Accesorios";
                    nombreColeccion = "productos";
                }

                // Procesar la URL de la imagen
                string urlImagen = txtImagenURL.Text.Trim();

                // Si está vacío, usar una imagen por defecto o dejarlo vacío
                if (string.IsNullOrWhiteSpace(urlImagen))
                {
                    urlImagen = ""; // O puedes poner una URL por defecto
                }

                // Crear el nuevo producto
                var nuevoProducto = new ProductoModel
                {
                    nombre = txtNombre.Text.Trim(),
                    codigo = txtCodigo.Text.Trim(),
                    categoria = categoriaReal,
                    cantidad = cantidad,
                    precio = precio,
                    estante = txtEstante.Text.Trim(),
                    fila = txtFila.Text.Trim(),
                    imagenURL = urlImagen // Asegurar que se asigna correctamente
                };

                // Mostrar en Debug para verificar
                System.Diagnostics.Debug.WriteLine($"Guardando producto con imagenURL: '{nuevoProducto.imagenURL}'");

                // Guardar en Firebase
                bool resultado = await _firebaseService.AgregarProducto(nuevoProducto, nombreColeccion);

                if (resultado)
                {
                    ProductoAgregado = true;
                    MessageBox.Show($"✅ Producto '{txtNombre.Text}' agregado exitosamente a la categoría {categoriaSeleccionada}",
                                   "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    txtEstado.Text = "Error al guardar el producto";
                    txtEstado.Foreground = System.Windows.Media.Brushes.Red;
                    btnGuardar.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                txtEstado.Text = $"Error: {ex.Message}";
                txtEstado.Foreground = System.Windows.Media.Brushes.Red;
                btnGuardar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de cancelar? Los datos no se guardarán.",
                                        "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }


    }
}