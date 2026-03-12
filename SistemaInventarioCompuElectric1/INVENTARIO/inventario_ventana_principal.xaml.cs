// INVENTARIO/inventario_ventana_principal.xaml.cs
using SistemaInventarioCompuElectric1.SERVICIOS;
using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System.IO;
using Microsoft.Win32;
using ClosedXML.Excel;
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

            // Configurar ComboBox - SIN LIMPIAR LOS ITEMS YA DEFINIDOS EN XAML
            // Solo nos aseguramos de que "Todos" esté seleccionado
            if (cmbCategoria != null)
            {
                cmbCategoria.SelectedIndex = 0; // Seleccionar "Todos"
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

                // Verificar que se cargaron productos
                System.Diagnostics.Debug.WriteLine($"Productos cargados: {_todosLosProductos?.Count ?? 0}");

                // Mostrar los productos
                FiltrarYMostrarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCargando(false);
            }
        }

        private void FiltrarYMostrarProductos()
        {
            try
            {
                var productosPanel = FindName("ProductosPanel") as StackPanel;
                if (productosPanel == null) return;

                productosPanel.Children.Clear();

                var txtResultados = FindName("txtResultados") as TextBlock;

                if (_todosLosProductos == null || !_todosLosProductos.Any())
                {
                    productosPanel.Children.Add(new TextBlock
                    {
                        Text = "No hay productos disponibles",
                        Margin = new Thickness(10),
                        FontSize = 16,
                        Foreground = System.Windows.Media.Brushes.Gray
                    });

                    if (txtResultados != null)
                        txtResultados.Text = "0 productos";

                    ActualizarTotalCategoria();
                    return;
                }

                // Mapeo de categorías del ComboBox a las categorías reales en Firebase
                Dictionary<string, string> mapaCategorias = new Dictionary<string, string>
        {
            { "Electrónica", "Electronica" },
            { "Robótica", "Robotica" },
            { "Accesorios", "Accesorios" } // Importante: Usamos "Accesorios" como categoría real
        };

                System.Diagnostics.Debug.WriteLine("=== CATEGORÍAS DISPONIBLES EN LOS PRODUCTOS ===");
                var categoriasDisponibles = _todosLosProductos
                    .Where(p => p.categoria != null)
                    .Select(p => p.categoria)
                    .Distinct()
                    .ToList();

                foreach (var cat in categoriasDisponibles)
                {
                    System.Diagnostics.Debug.WriteLine($"- {cat}");
                }
                System.Diagnostics.Debug.WriteLine($"Total productos: {_todosLosProductos.Count}");
                System.Diagnostics.Debug.WriteLine($"Filtrando por categoría (ComboBox): '{_categoriaSeleccionada}'");

                var productosFiltrados = _todosLosProductos.AsEnumerable();

                // Filtrar por categoría
                if (_categoriaSeleccionada != "Todos")
                {
                    if (_categoriaSeleccionada == "Accesorios")
                    {
                        // Para Accesorios, buscamos productos con categoría "Accesorios"
                        productosFiltrados = productosFiltrados
                            .Where(p => p.categoria != null &&
                                       p.categoria.Equals("productos", StringComparison.OrdinalIgnoreCase));

                        System.Diagnostics.Debug.WriteLine("Buscando productos con categoría 'Accesorios'");
                    }
                    else
                    {
                        // Para Electrónica y Robótica, usamos el mapa
                        string categoriaReal = mapaCategorias[_categoriaSeleccionada];
                        System.Diagnostics.Debug.WriteLine($"Buscando productos con categoría real: '{categoriaReal}'");

                        productosFiltrados = productosFiltrados
                            .Where(p => p.categoria != null)
                            .Where(p => p.categoria.Equals(categoriaReal, StringComparison.OrdinalIgnoreCase));
                    }
                }

                // Filtrar por búsqueda (si hay texto)
                var txtBuscar = FindName("txtBuscar") as TextBox;
                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    var busqueda = txtBuscar.Text.ToLower().Trim();
                    productosFiltrados = productosFiltrados
                        .Where(p => (p.nombre != null && p.nombre.ToLower().Contains(busqueda)) ||
                                   (p.codigo != null && p.codigo.ToLower().Contains(busqueda)));
                }

                var listaFiltrada = productosFiltrados.ToList();

                System.Diagnostics.Debug.WriteLine($"Productos después de filtrar: {listaFiltrada.Count}");

                if (!listaFiltrada.Any())
                {
                    string mensaje = _categoriaSeleccionada == "Todos"
                        ? "No se encontraron productos"
                        : $"No se encontraron productos en categoría: {_categoriaSeleccionada}";

                    productosPanel.Children.Add(new TextBlock
                    {
                        Text = mensaje,
                        Margin = new Thickness(10),
                        FontSize = 16,
                        Foreground = System.Windows.Media.Brushes.Gray
                    });
                }
                else
                {
                    // Mostrar productos
                    foreach (var producto in listaFiltrada)
                    {
                        string categoriaMostrar = producto.categoria;

                        // Mapeo inverso para mostrar las categorías con acentos en la interfaz
                        if (producto.categoria == "Electronica")
                            categoriaMostrar = "Electrónica";
                        else if (producto.categoria == "Robotica")
                            categoriaMostrar = "Robótica";
                        // "Accesorios" ya se muestra como "Accesorios"

                        var itemControl = new ProductoItemControl(producto, categoriaMostrar);
                        itemControl.OnEditar += ItemControl_OnEditar;
                        itemControl.OnEliminar += ItemControl_OnEliminar;
                        productosPanel.Children.Add(itemControl);
                    }
                }

                // Mostrar contador
                if (txtResultados != null)
                {
                    txtResultados.Text = $"Mostrando {listaFiltrada.Count} de {_todosLosProductos.Count} productos";
                }

                // Actualizar total de la categoría
                ActualizarTotalCategoria();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en FiltrarYMostrarProductos: {ex.Message}");
                MessageBox.Show($"Error al mostrar productos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarTotalCategoria()
        {
            try
            {
                var txtTotalCategoria = FindName("txtTotalCategoria") as TextBlock;
                if (txtTotalCategoria == null || _todosLosProductos == null) return;

                // Mapeo de categorías del ComboBox a las categorías reales en Firebase
                Dictionary<string, string> mapaCategorias = new Dictionary<string, string>
        {
            { "Electrónica", "Electronica" },
            { "Robótica", "Robotica" },
            { "Accesorios", "productos" } // ← CORREGIDO: "Accesorios" mapea a "productos"
        };

                if (_categoriaSeleccionada == "Todos")
                {
                    txtTotalCategoria.Text = $"Total: {_todosLosProductos.Count} productos";
                }
                else
                {
                    string categoriaReal = mapaCategorias[_categoriaSeleccionada];
                    int totalCategoria = _todosLosProductos.Count(p =>
                        p.categoria != null &&
                        p.categoria.Equals(categoriaReal, StringComparison.OrdinalIgnoreCase));

                    txtTotalCategoria.Text = $"Total: {totalCategoria} productos";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarTotalCategoria: {ex.Message}");
            }
        }

        private void MostrarCargando(bool mostrar)
        {
            try
            {
                var cargando = FindName("CargandoIndicator") as TextBlock;
                if (cargando != null)
                    cargando.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;

                var productosPanel = FindName("ProductosPanel") as StackPanel;
                if (productosPanel != null)
                    productosPanel.Visibility = mostrar ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MostrarCargando: {ex.Message}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarYMostrarProductos();
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                if (comboBox?.SelectedItem is ComboBoxItem item)
                {
                    string nuevaCategoria = item.Content.ToString();
                    System.Diagnostics.Debug.WriteLine($"Categoría seleccionada: {nuevaCategoria}");

                    _categoriaSeleccionada = nuevaCategoria;
                    FiltrarYMostrarProductos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CmbCategoria_SelectionChanged: {ex.Message}");
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
                    MostrarCargando(true);
                    var eliminado = await _firebaseService.EliminarProducto(producto.categoria, producto.Id);

                    if (eliminado)
                    {
                        MessageBox.Show("Producto eliminado exitosamente", "Éxito",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarProductos(); // Recargar todos los productos
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    MostrarCargando(false);
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
            try
            {
                // Obtener los productos a exportar según la categoría seleccionada
                List<ProductoModel> productosAExportar;
                string nombreArchivo = "Inventario";

                if (_categoriaSeleccionada == "Todos")
                {
                    productosAExportar = _todosLosProductos;
                    nombreArchivo = "Inventario_Completo";
                }
                else
                {
                    string categoriaReal = _categoriaSeleccionada;

                    // MAPEO COMPLETO DE CATEGORÍAS
                    if (_categoriaSeleccionada == "Electrónica")
                        categoriaReal = "Electronica";
                    else if (_categoriaSeleccionada == "Robótica")
                        categoriaReal = "Robotica";
                    else if (_categoriaSeleccionada == "Accesorios")
                        categoriaReal = "productos"; // La categoría "Accesorios" viene de la colección "productos"

                    productosAExportar = _todosLosProductos
                        .Where(p => p.categoria != null &&
                               p.categoria.Equals(categoriaReal, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    nombreArchivo = $"Inventario_{_categoriaSeleccionada}";
                }

                if (!productosAExportar.Any())
                {
                    MessageBox.Show($"No hay productos para exportar en la categoría: {_categoriaSeleccionada}",
                                  "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    FileName = $"{nombreArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Guardar archivo Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Crear el libro de trabajo
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Inventario");

                        // Título
                        worksheet.Cell(1, 1).Value = $"REPORTE DE INVENTARIO - {_categoriaSeleccionada}";
                        worksheet.Range(1, 1, 1, 10).Merge();
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Fecha
                        worksheet.Cell(2, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                        worksheet.Range(2, 1, 2, 10).Merge();
                        worksheet.Cell(2, 1).Style.Font.Italic = true;

                        // Encabezados
                        string[] encabezados = { "NOMBRE", "CÓDIGO", "CATEGORÍA", "CANTIDAD",
                                        "PRECIO UNIT.", "VALOR TOTAL", "ESTANTE", "FILA",
                                        "IMAGEN URL", "ID" };

                        for (int i = 0; i < encabezados.Length; i++)
                        {
                            var cell = worksheet.Cell(4, i + 1);
                            cell.Value = encabezados[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(20, 60, 91);
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        // Variables para el cálculo total
                        decimal valorTotalInventario = 0;
                        int totalUnidades = 0;

                        // Datos
                        int fila = 5;
                        foreach (var producto in productosAExportar)
                        {
                            worksheet.Cell(fila, 1).Value = producto.nombre ?? "";
                            worksheet.Cell(fila, 2).Value = producto.codigo ?? "";

                            // Formatear categoría para mostrar
                            string categoriaMostrar = producto.categoria;
                            if (producto.categoria == "Electronica")
                                categoriaMostrar = "Electrónica";
                            else if (producto.categoria == "Robotica")
                                categoriaMostrar = "Robótica";
                            // "Accesorios" se mantiene igual

                            worksheet.Cell(fila, 3).Value = categoriaMostrar;

                            // Cantidad
                            worksheet.Cell(fila, 4).Value = producto.cantidad;
                            worksheet.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Precio unitario
                            worksheet.Cell(fila, 5).Value = producto.precio;
                            worksheet.Cell(fila, 5).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                            worksheet.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                            // VALOR TOTAL DEL PRODUCTO
                            decimal valorProducto = (decimal)(producto.cantidad * producto.precio);
                            worksheet.Cell(fila, 6).Value = valorProducto;
                            worksheet.Cell(fila, 6).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                            worksheet.Cell(fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            worksheet.Cell(fila, 6).Style.Font.Bold = true;

                            worksheet.Cell(fila, 7).Value = producto.estante ?? "";
                            worksheet.Cell(fila, 8).Value = producto.fila ?? "";
                            worksheet.Cell(fila, 9).Value = producto.imagenURL ?? "";
                            worksheet.Cell(fila, 10).Value = producto.Id ?? "";

                            // Acumular totales
                            valorTotalInventario += valorProducto;
                            totalUnidades += producto.cantidad;

                            // Bordes
                            for (int col = 1; col <= 10; col++)
                            {
                                worksheet.Cell(fila, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }

                            fila++;
                        }

                        // RESULTADOS FINALES
                        fila += 2;

                        // Título de resultados
                        worksheet.Cell(fila, 1).Value = "RESULTADOS DEL INVENTARIO";
                        worksheet.Range(fila, 1, fila, 4).Merge();
                        worksheet.Cell(fila, 1).Style.Font.Bold = true;
                        worksheet.Cell(fila, 1).Style.Font.FontSize = 14;
                        worksheet.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(20, 60, 91);
                        worksheet.Cell(fila, 1).Style.Font.FontColor = XLColor.White;

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Categoría:";
                        worksheet.Cell(fila, 2).Value = _categoriaSeleccionada;

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Total de productos (diferentes):";
                        worksheet.Cell(fila, 2).Value = productosAExportar.Count;
                        worksheet.Range(fila, 1, fila, 2).Style.Font.Bold = true;

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Total de unidades en stock:";
                        worksheet.Cell(fila, 2).Value = totalUnidades;
                        worksheet.Range(fila, 1, fila, 2).Style.Font.Bold = true;

                        fila += 2;
                        worksheet.Cell(fila, 1).Value = "VALOR TOTAL DEL INVENTARIO EN SOLES (S/):";
                        worksheet.Cell(fila, 2).Value = valorTotalInventario;
                        worksheet.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";
                        worksheet.Range(fila, 1, fila, 2).Style.Font.Bold = true;
                        worksheet.Range(fila, 1, fila, 2).Style.Font.FontSize = 14;
                        worksheet.Cell(fila, 2).Style.Font.FontColor = XLColor.Green;

                        // Valor en números
                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Valor en números:";
                        worksheet.Cell(fila, 2).Value = valorTotalInventario.ToString("N2");

                        // DESGLOSE DE PRECIOS
                        fila += 2;
                        worksheet.Cell(fila, 1).Value = "DESGLOSE DE PRECIOS:";
                        worksheet.Range(fila, 1, fila, 4).Merge();
                        worksheet.Cell(fila, 1).Style.Font.Bold = true;

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Precio más alto:";
                        decimal precioMax = (decimal)productosAExportar.Max(p => p.precio);
                        worksheet.Cell(fila, 2).Value = precioMax;
                        worksheet.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Precio más bajo:";
                        decimal precioMin = (decimal)productosAExportar.Min(p => p.precio);
                        worksheet.Cell(fila, 2).Value = precioMin;
                        worksheet.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";

                        fila += 1;
                        worksheet.Cell(fila, 1).Value = "Precio promedio:";
                        decimal precioPromedio = (decimal)productosAExportar.Average(p => p.precio);
                        worksheet.Cell(fila, 2).Value = precioPromedio;
                        worksheet.Cell(fila, 2).Style.NumberFormat.Format = "\"S/ \"#,##0.00";

                        // Ajustar columnas
                        worksheet.Columns().AdjustToContents();

                        // Guardar
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    // Calcular el valor total para mostrarlo en el mensaje
                    decimal valorTotalMsg = (decimal)productosAExportar.Sum(p => p.cantidad * p.precio);

                    MessageBox.Show($"✅ ARCHIVO EXCEL GENERADO EXITOSAMENTE\n\n" +
                                  $"📁 Categoría: {_categoriaSeleccionada}\n" +
                                  $"📦 Productos diferentes: {productosAExportar.Count}\n" +
                                  $"🔢 Unidades en stock: {productosAExportar.Sum(p => p.cantidad)}\n" +
                                  $"💰 VALOR TOTAL DEL INVENTARIO: S/ {valorTotalMsg:N2}\n\n" +
                                  $"📄 Archivo guardado en la ubicación seleccionada.",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar a Excel: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




    }



}