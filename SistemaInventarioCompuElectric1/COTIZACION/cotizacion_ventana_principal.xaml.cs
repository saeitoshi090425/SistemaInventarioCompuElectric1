using SistemaInventarioCompuElectric1.INVENTARIO;
using SistemaInventarioCompuElectric1.SERVICIOS;
using System;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace SistemaInventarioCompuElectric1.COTIZACION
{
    public partial class cotizacion_ventana_principal : UserControl
    {
        private FirebaseService _firebaseService;
        private List<ProductoModel> _todosLosProductos;
        private List<CotizacionItemModel> _itemsCotizacion;
        private string _categoriaSeleccionada = "Todas";

        public cotizacion_ventana_principal()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            _itemsCotizacion = new List<CotizacionItemModel>();

            // Mensaje de depuración para verificar que los controles se inicializaron
            System.Diagnostics.Debug.WriteLine("cotizacion_ventana_principal inicializado");
        }

        // Event Handler para Loaded
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("UserControl_Loaded ejecutado");
            System.Diagnostics.Debug.WriteLine($"ProductosPanel en Loaded: {ProductosPanel != null}");

            if (ProductosPanel == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: ProductosPanel es null en Loaded");
                MessageBox.Show("Error de inicialización: No se encontró el panel de productos",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await CargarProductos();
        }

        // Event Handler para TextChanged del buscador
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ProductosPanel != null && _todosLosProductos != null)
            {
                FiltrarYMostrarProductos();
            }
        }

        // Event Handler para SelectionChanged del ComboBox
        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategoria.SelectedItem is ComboBoxItem item && ProductosPanel != null && _todosLosProductos != null)
            {
                _categoriaSeleccionada = item.Content.ToString();
                FiltrarYMostrarProductos();
            }
        }

        // Event Handler para botón Limpiar
        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            _itemsCotizacion.Clear();
            ActualizarCotizacion();
        }

        // Event Handler para botón Generar PDF
        // Event Handler para botón Generar PDF
        private void BtnGenerarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_itemsCotizacion.Any())
                {
                    MessageBox.Show("No hay productos en la cotización", "Información",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Configurar el diálogo para guardar el archivo
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    FileName = $"Cotizacion_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Title = "Guardar cotización como PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerarPDF(saveFileDialog.FileName);

                    MessageBox.Show($"✅ PDF generado exitosamente\n\n" +
                                  $"Archivo guardado en:\n{saveFileDialog.FileName}",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para generar el PDF
        // Método para generar el PDF
        private void GenerarPDF(string filePath)
        {
            // Crear el documento (tamaño carta)
            Document document = new Document(PageSize.LETTER, 36, 36, 36, 36);

            try
            {
                // Crear el escritor PDF
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // FUENTES
                Font tituloFont = FontFactory.GetFont("Arial", 24, Font.BOLD, new BaseColor(20, 60, 91)); // #143c5b
                Font subtituloFont = FontFactory.GetFont("Arial", 14, Font.BOLD, new BaseColor(20, 60, 91));
                Font normalFont = FontFactory.GetFont("Arial", 11, Font.NORMAL, BaseColor.BLACK);
                Font boldFont = FontFactory.GetFont("Arial", 11, Font.BOLD, BaseColor.BLACK);
                Font headerFont = FontFactory.GetFont("Arial", 12, Font.BOLD, BaseColor.WHITE);
                Font totalFont = FontFactory.GetFont("Arial", 16, Font.BOLD, new BaseColor(0, 128, 0)); // Verde

                // TÍTULO
                Paragraph titulo = new Paragraph("COTIZACIÓN", tituloFont);
                titulo.Alignment = Element.ALIGN_CENTER;
                titulo.SpacingAfter = 20f;
                document.Add(titulo);

                // INFORMACIÓN DE LA EMPRESA CON LOGO
                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 30f, 70f });

                // Celda izquierda - LOGO
                PdfPCell cellLogo = new PdfPCell();
                cellLogo.Border = Rectangle.NO_BORDER;
                cellLogo.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellLogo.HorizontalAlignment = Element.ALIGN_LEFT;

                try
                {
                    // Ruta del logo (ajusta según la ubicación de tu proyecto)
                    string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"IMG\logo_letrasa_azules_fondoblanco.png");

                    // Si no está en la carpeta de salida, busca en la carpeta del proyecto
                    if (!File.Exists(logoPath))
                    {
                        string projectPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        logoPath = System.IO.Path.Combine(projectPath, @"..\..\IMG\logo_letrasa_azules_fondoblanco.png");
                        logoPath = System.IO.Path.GetFullPath(logoPath);
                    }

                    if (File.Exists(logoPath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);
                        logo.ScaleToFit(150f, 80f); // Ajustar tamaño del logo
                        cellLogo.AddElement(logo);
                    }
                    else
                    {
                        cellLogo.AddElement(new Paragraph("COMPUELECTRIC", boldFont));
                        System.Diagnostics.Debug.WriteLine($"Logo no encontrado en: {logoPath}");
                    }
                }
                catch (Exception ex)
                {
                    cellLogo.AddElement(new Paragraph("COMPUELECTRIC", boldFont));
                    System.Diagnostics.Debug.WriteLine($"Error cargando logo: {ex.Message}");
                }

                infoTable.AddCell(cellLogo);

                // Celda derecha - Información de la empresa
                PdfPCell cellInfo = new PdfPCell();
                cellInfo.Border = Rectangle.NO_BORDER;
                cellInfo.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellInfo.HorizontalAlignment = Element.ALIGN_RIGHT;

                cellInfo.AddElement(new Paragraph("COMPUELECTRIC", boldFont));
                cellInfo.AddElement(new Paragraph("RUC: 10104724553", normalFont));
                cellInfo.AddElement(new Paragraph("Av. México 107 15311, Lima – Comas", normalFont));
                cellInfo.AddElement(new Paragraph("Tel: +51 977 679 527", normalFont));

                infoTable.AddCell(cellInfo);

                document.Add(infoTable);
                document.Add(new Paragraph(" "));

                // Fecha y N° Cotización (fila separada)
                PdfPTable fechaTable = new PdfPTable(2);
                fechaTable.WidthPercentage = 100;
                fechaTable.SetWidths(new float[] { 50f, 50f });

                PdfPCell cellFechaLabel = new PdfPCell(new Phrase("Fecha:", boldFont));
                cellFechaLabel.Border = Rectangle.NO_BORDER;
                cellFechaLabel.HorizontalAlignment = Element.ALIGN_LEFT;
                fechaTable.AddCell(cellFechaLabel);

                PdfPCell cellFechaValue = new PdfPCell(new Phrase($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}", normalFont));
                cellFechaValue.Border = Rectangle.NO_BORDER;
                cellFechaValue.HorizontalAlignment = Element.ALIGN_RIGHT;
                fechaTable.AddCell(cellFechaValue);

                PdfPCell cellNroLabel = new PdfPCell(new Phrase("N° Cotización:", boldFont));
                cellNroLabel.Border = Rectangle.NO_BORDER;
                cellNroLabel.HorizontalAlignment = Element.ALIGN_LEFT;
                fechaTable.AddCell(cellNroLabel);

                PdfPCell cellNroValue = new PdfPCell(new Phrase($"COT-{DateTime.Now:yyyyMMdd-HHmmss}", normalFont));
                cellNroValue.Border = Rectangle.NO_BORDER;
                cellNroValue.HorizontalAlignment = Element.ALIGN_RIGHT;
                fechaTable.AddCell(cellNroValue);

                document.Add(fechaTable);
                document.Add(new Paragraph(" "));

                // TÍTULO DE LA TABLA
                Paragraph tablaTitulo = new Paragraph("PRODUCTOS COTIZADOS", subtituloFont);
                tablaTitulo.SpacingBefore = 15f;
                tablaTitulo.SpacingAfter = 10f;
                document.Add(tablaTitulo);

                // TABLA DE PRODUCTOS (SIN CÓDIGO)
                PdfPTable table = new PdfPTable(5); // Reducido a 5 columnas (sin código)
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 35f, 25f, 15f, 15f, 15f }); // Ajustar anchos

                // ENCABEZADOS (SIN CÓDIGO)
                string[] headers = { "PRODUCTO", "CATEGORÍA", "CANTIDAD", "P.UNIT", "SUBTOTAL" };
                BaseColor headerBg = new BaseColor(20, 60, 91); // #143c5b

                foreach (string header in headers)
                {
                    PdfPCell headerCell = new PdfPCell(new Phrase(header, headerFont));
                    headerCell.BackgroundColor = headerBg;
                    headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    headerCell.Padding = 8;
                    table.AddCell(headerCell);
                }

                // DATOS DE PRODUCTOS (SIN CÓDIGO)
                double totalGeneral = 0;

                foreach (var item in _itemsCotizacion)
                {
                    // Producto (ahora ocupa más espacio)
                    PdfPCell cellProducto = new PdfPCell(new Phrase(item.Nombre, normalFont));
                    cellProducto.Padding = 5;
                    table.AddCell(cellProducto);

                    // Categoría
                    string categoriaMostrar = item.Categoria;
                    if (item.Categoria == "Electronica")
                        categoriaMostrar = "Electrónica";
                    else if (item.Categoria == "Robotica")
                        categoriaMostrar = "Robótica";

                    PdfPCell cellCategoria = new PdfPCell(new Phrase(categoriaMostrar, normalFont));
                    cellCategoria.HorizontalAlignment = Element.ALIGN_CENTER;
                    cellCategoria.Padding = 5;
                    table.AddCell(cellCategoria);

                    // Cantidad
                    PdfPCell cellCantidad = new PdfPCell(new Phrase(item.Cantidad.ToString(), normalFont));
                    cellCantidad.HorizontalAlignment = Element.ALIGN_CENTER;
                    cellCantidad.Padding = 5;
                    table.AddCell(cellCantidad);

                    // Precio Unitario
                    PdfPCell cellPrecio = new PdfPCell(new Phrase($"S/ {item.PrecioUnitario:F2}", normalFont));
                    cellPrecio.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cellPrecio.Padding = 5;
                    table.AddCell(cellPrecio);

                    // Subtotal
                    PdfPCell cellSubtotal = new PdfPCell(new Phrase($"S/ {item.Subtotal:F2}", boldFont));
                    cellSubtotal.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cellSubtotal.Padding = 5;
                    table.AddCell(cellSubtotal);

                    totalGeneral += item.Subtotal;
                }

                document.Add(table);
                document.Add(new Paragraph(" "));

                // TOTALES
                PdfPTable totalTable = new PdfPTable(2);
                totalTable.WidthPercentage = 40;
                totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalTable.SetWidths(new float[] { 50f, 50f });

                // Fila Subtotal
                PdfPCell cellSubtotalLabel = new PdfPCell(new Phrase("SUBTOTAL:", boldFont));
                cellSubtotalLabel.Border = Rectangle.NO_BORDER;
                cellSubtotalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellSubtotalLabel.Padding = 5;
                totalTable.AddCell(cellSubtotalLabel);

                PdfPCell cellSubtotalValue = new PdfPCell(new Phrase($"S/ {totalGeneral:F2}", normalFont));
                cellSubtotalValue.Border = Rectangle.NO_BORDER;
                cellSubtotalValue.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellSubtotalValue.Padding = 5;
                totalTable.AddCell(cellSubtotalValue);

                // Fila Total
                PdfPCell cellTotalLabel = new PdfPCell(new Phrase("TOTAL:", boldFont));
                cellTotalLabel.Border = Rectangle.NO_BORDER;
                cellTotalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellTotalLabel.Padding = 5;
                totalTable.AddCell(cellTotalLabel);

                PdfPCell cellTotalValue = new PdfPCell(new Phrase($"S/ {totalGeneral:F2}", totalFont));
                cellTotalValue.Border = Rectangle.NO_BORDER;
                cellTotalValue.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellTotalValue.Padding = 5;
                totalTable.AddCell(cellTotalValue);

                document.Add(totalTable);
                document.Add(new Paragraph(" "));

                // NOTA AL PIE
            

                // FIRMA
                Paragraph firma = new Paragraph("COMPUELECTRIC", boldFont);
                firma.Alignment = Element.ALIGN_CENTER;
                firma.SpacingBefore = 20f;
                document.Add(firma);

                document.Close();
                writer.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al generar PDF: {ex.Message}");
            }
            finally
            {
                if (document.IsOpen())
                    document.Close();
            }
        }
        // Método para cargar productos desde Firebase
        private async System.Threading.Tasks.Task CargarProductos()
        {
            try
            {
                MostrarCargando(true);
                _todosLosProductos = await _firebaseService.ObtenerTodosLosProductos();

                System.Diagnostics.Debug.WriteLine($"Productos cargados: {_todosLosProductos?.Count ?? 0}");

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

        // Método para filtrar y mostrar productos
        // Método para filtrar y mostrar productos
        private void FiltrarYMostrarProductos()
        {
            // Verificación de seguridad mejorada
            if (ProductosPanel == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: ProductosPanel es null en FiltrarYMostrarProductos");
                return;
            }

            // Limpiar el panel
            ProductosPanel.Children.Clear();

            // Verificar si hay productos
            if (_todosLosProductos == null)
            {
                System.Diagnostics.Debug.WriteLine("_todosLosProductos es null");
                if (txtContadorProductos != null)
                    txtContadorProductos.Text = "0 productos disponibles";
                return;
            }

            if (!_todosLosProductos.Any())
            {
                if (txtContadorProductos != null)
                    txtContadorProductos.Text = "0 productos disponibles";
                return;
            }

            try
            {
                var productosFiltrados = _todosLosProductos.AsEnumerable();

                // Filtrar por categoría
                if (_categoriaSeleccionada != "Todas")
                {
                    string categoriaReal;

                    // Mapeo de categorías del ComboBox a las categorías reales en Firebase
                    if (_categoriaSeleccionada == "Electrónica")
                        categoriaReal = "Electronica";
                    else if (_categoriaSeleccionada == "Robótica")
                        categoriaReal = "Robotica";
                    else if (_categoriaSeleccionada == "Accesorios")
                        categoriaReal = "productos"; // ← IMPORTANTE: "Accesorios" en ComboBox = "productos" en Firebase
                    else
                        categoriaReal = _categoriaSeleccionada;

                    System.Diagnostics.Debug.WriteLine($"Filtrando por categoría: '{_categoriaSeleccionada}' -> Real: '{categoriaReal}'");

                    productosFiltrados = productosFiltrados
                        .Where(p => p.categoria != null &&
                               p.categoria.Equals(categoriaReal, StringComparison.OrdinalIgnoreCase));
                }

                // Filtrar por búsqueda
                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    var busqueda = txtBuscar.Text.ToLower().Trim();
                    productosFiltrados = productosFiltrados
                        .Where(p => (p.nombre != null && p.nombre.ToLower().Contains(busqueda)) ||
                                   (p.codigo != null && p.codigo.ToLower().Contains(busqueda)));
                }

                var listaFiltrada = productosFiltrados.ToList();

                if (txtContadorProductos != null)
                    txtContadorProductos.Text = $"{listaFiltrada.Count} productos disponibles";

                // Mostrar productos
                foreach (var producto in listaFiltrada)
                {
                    var itemControl = new ProductoCotizacionItem(producto);
                    itemControl.OnProductoSeleccionado += ItemControl_OnProductoSeleccionado;
                    ProductosPanel.Children.Add(itemControl);
                }

                System.Diagnostics.Debug.WriteLine($"Mostrados {listaFiltrada.Count} productos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en FiltrarYMostrarProductos: {ex.Message}");
                MessageBox.Show($"Error al filtrar productos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event Handler cuando se selecciona un producto
        // Event Handler cuando se selecciona un producto
        private void ItemControl_OnProductoSeleccionado(object sender, ProductoModel producto)
        {
            try
            {
                // Verificar si el producto ya está en la cotización
                var itemExistente = _itemsCotizacion.FirstOrDefault(i => i.Id == producto.Id);

                if (itemExistente != null)
                {
                    // Si ya existe, aumentar cantidad
                    itemExistente.Cantidad++;
                }
                else
                {
                    // Determinar la categoría para mostrar en la cotización
                    string categoriaMostrar = producto.categoria;
                    if (producto.categoria == "Electronica")
                        categoriaMostrar = "Electrónica";
                    else if (producto.categoria == "Robotica")
                        categoriaMostrar = "Robótica";
                    else if (producto.categoria == "productos")
                        categoriaMostrar = "Accesorios";

                    // Si no existe, agregar nuevo item
                    _itemsCotizacion.Add(new CotizacionItemModel
                    {
                        Id = producto.Id,
                        Nombre = producto.nombre,
                        Codigo = producto.codigo,
                        Categoria = categoriaMostrar, // Guardamos la categoría para mostrar
                        PrecioUnitario = producto.precio,
                        Cantidad = 1
                    });
                }

                ActualizarCotizacion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al seleccionar producto: {ex.Message}");
            }
        }
        // Método para actualizar la cotización
        // Método para actualizar la cotización (SIN IGV)
        private void ActualizarCotizacion()
        {
            try
            {
                if (CotizacionPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: CotizacionPanel es null");
                    return;
                }

                CotizacionPanel.Children.Clear();

                if (!_itemsCotizacion.Any())
                {
                    if (txtSinProductos != null)
                        txtSinProductos.Visibility = Visibility.Visible;

                    if (txtSubtotal != null)
                        txtSubtotal.Text = "S/ 0.00";

                    if (txtTotal != null)
                        txtTotal.Text = "S/ 0.00";

                    // Ocultar la fila del IGV
                   

                    return;
                }

                if (txtSinProductos != null)
                    txtSinProductos.Visibility = Visibility.Collapsed;

                double total = 0;

                foreach (var item in _itemsCotizacion)
                {
                    // Buscar la imagen del producto en la lista original
                    var productoOriginal = _todosLosProductos?.FirstOrDefault(p => p.Id == item.Id);
                    string imagenURL = productoOriginal?.imagenURL ?? "";

                    var itemControl = new CotizacionItemControl(item, imagenURL);
                    itemControl.OnCantidadCambio += ItemControl_OnCantidadCambio;
                    itemControl.OnEliminar += ItemControl_OnEliminar;
                    CotizacionPanel.Children.Add(itemControl);

                    total += item.Subtotal;
                }

                // SIN IGV - el total es igual al subtotal
                if (txtSubtotal != null)
                    txtSubtotal.Text = $"S/ {total:F2}";

                // Ocultar el IGV
               

                if (txtTotal != null)
                    txtTotal.Text = $"S/ {total:F2}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarCotizacion: {ex.Message}");
            }
        }

        // Event Handler cuando cambia la cantidad en la cotización
        private void ItemControl_OnCantidadCambio(object sender, CotizacionItemModel item)
        {
            ActualizarCotizacion();
        }

        // Event Handler cuando se elimina un item de la cotización
        private void ItemControl_OnEliminar(object sender, CotizacionItemModel item)
        {
            _itemsCotizacion.Remove(item);
            ActualizarCotizacion();
        }

        // Método para mostrar/ocultar indicador de carga
        private void MostrarCargando(bool mostrar)
        {
            try
            {
                if (CargandoIndicator != null)
                    CargandoIndicator.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;

                if (ProductosPanel != null)
                    ProductosPanel.Visibility = mostrar ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MostrarCargando: {ex.Message}");
            }
        }
    }
}