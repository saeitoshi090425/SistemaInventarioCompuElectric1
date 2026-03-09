// INVENTARIO/inventario_ventana_principal.xaml.cs
using SistemaInventarioCompuElectric1.SERVICIOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SistemaInventarioCompuElectric1.INVENTARIO
{
    public partial class inventario_ventana_principal : UserControl
    {
        private FirebaseService _firebaseService;
        private List<ProductoModel> _todosLosProductos;
        private string _categoriaSeleccionada = "Todos";

        public inventario_ventana_principal()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();

            // Configurar ComboBox
            var cmbCategoria = FindName("cmbCategoria") as ComboBox;
            if (cmbCategoria != null)
            {
                cmbCategoria.Items.Clear();
                cmbCategoria.Items.Add(new ComboBoxItem { Content = "Todos", IsSelected = true });
                cmbCategoria.Items.Add(new ComboBoxItem { Content = "Electrónica" });
                cmbCategoria.Items.Add(new ComboBoxItem { Content = "Robótica" });
                cmbCategoria.Items.Add(new ComboBoxItem { Content = "Accesorios" });
                cmbCategoria.SelectionChanged += CmbCategoria_SelectionChanged;
            }

            CargarProductos();
        }

        private async void CargarProductos()
        {
            try
            {
                MostrarCargando(true);
                _todosLosProductos = await _firebaseService.ObtenerTodosLosProductos();
                FiltrarYMostrarProductos();
                ActualizarTotalCategoria(); // Actualizar el total
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}");
            }
            finally
            {
                MostrarCargando(false);
            }
        }

        private void FiltrarYMostrarProductos()
        {
            var productosPanel = FindName("ProductosPanel") as StackPanel;
            if (productosPanel == null) return;

            productosPanel.Children.Clear();

            if (_todosLosProductos == null || !_todosLosProductos.Any())
            {
                productosPanel.Children.Add(new TextBlock
                {
                    Text = "No hay productos disponibles",
                    Margin = new Thickness(10),
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
                return;
            }

            var productosFiltrados = _todosLosProductos.AsEnumerable();

            // Filtrar por categoría
            if (_categoriaSeleccionada != "Todos")
            {
                productosFiltrados = productosFiltrados
                    .Where(p => p.categoria != null &&
                               p.categoria.Equals(_categoriaSeleccionada, StringComparison.OrdinalIgnoreCase));
            }

            // Filtrar por búsqueda
            var txtBuscar = FindName("txtBuscar") as TextBox;
            if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                var busqueda = txtBuscar.Text.ToLower().Trim();
                productosFiltrados = productosFiltrados
                    .Where(p => (p.nombre != null && p.nombre.ToLower().Contains(busqueda)) ||
                               (p.codigo != null && p.codigo.ToLower().Contains(busqueda)));
            }

            var listaFiltrada = productosFiltrados.ToList();

            if (!listaFiltrada.Any())
            {
                productosPanel.Children.Add(new TextBlock
                {
                    Text = _categoriaSeleccionada == "Todos"
                        ? "No se encontraron productos"
                        : $"No se encontraron productos en categoría: {_categoriaSeleccionada}",
                    Margin = new Thickness(10),
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
                return;
            }

            // Mostrar productos
            foreach (var producto in listaFiltrada)
            {
                var itemControl = new ProductoItemControl(producto, producto.categoria);
                itemControl.OnEditar += ItemControl_OnEditar;
                itemControl.OnEliminar += ItemControl_OnEliminar;
                productosPanel.Children.Add(itemControl);
            }

            // Mostrar contador
            var txtResultados = FindName("txtResultados") as TextBlock;
            if (txtResultados != null)
            {
                txtResultados.Text = $"Mostrando {listaFiltrada.Count} de {_todosLosProductos.Count} productos";
            }

            ActualizarTotalCategoria(); // Actualizar cuando cambian los filtros
        }

        // NUEVO MÉTODO: Actualizar el total de productos por categoría
        private void ActualizarTotalCategoria()
        {
            var txtTotalCategoria = FindName("txtTotalCategoria") as TextBlock;
            if (txtTotalCategoria == null || _todosLosProductos == null) return;

            if (_categoriaSeleccionada == "Todos")
            {
                txtTotalCategoria.Text = $"Total: {_todosLosProductos.Count} productos";
            }
            else
            {
                int totalCategoria = _todosLosProductos.Count(p =>
                    p.categoria != null &&
                    p.categoria.Equals(_categoriaSeleccionada, StringComparison.OrdinalIgnoreCase));

                txtTotalCategoria.Text = $"Total: {totalCategoria} productos";
            }
        }

        private void MostrarCargando(bool mostrar)
        {
            var cargando = FindName("CargandoIndicator") as TextBlock;
            if (cargando != null)
                cargando.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;

            var productosPanel = FindName("ProductosPanel") as StackPanel;
            if (productosPanel != null)
                productosPanel.Visibility = mostrar ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarYMostrarProductos();
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                _categoriaSeleccionada = item.Content.ToString();
                FiltrarYMostrarProductos();
                ActualizarTotalCategoria(); // Actualizar el total al cambiar categoría
            }
        }

        private async void ItemControl_OnEliminar(object sender, ProductoModel producto)
        {
            var result = MessageBox.Show($"¿Estás seguro de eliminar el producto '{producto.nombre}'?",
                                         "Confirmar eliminación",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var eliminado = await _firebaseService.EliminarProducto(producto.categoria, producto.Id);

                    if (eliminado)
                    {
                        MessageBox.Show("Producto eliminado exitosamente", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarProductos();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar producto: {ex.Message}");
                }
            }
        }

        private void ItemControl_OnEditar(object sender, ProductoModel producto)
        {
            MessageBox.Show($"Función de editar para: {producto.nombre}", "Editar",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Añadir_nuevo_producto_click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de agregar producto en desarrollo", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exportar_inventario_excel(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de exportar a Excel en desarrollo", "Información",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}