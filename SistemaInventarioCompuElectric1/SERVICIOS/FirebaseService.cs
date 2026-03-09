// SERVICIOS/FirebaseService.cs
using Google.Cloud.Firestore;
using SistemaInventarioCompuElectric1.INVENTARIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaInventarioCompuElectric1.SERVICIOS
{
    public class FirebaseService
    {
        private FirestoreDb _firestoreDb;
        private string _projectId = "compuelectric-inventario";

        public FirebaseService()
        {
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var jsonFiles = Directory.GetFiles(currentDirectory, "*.json");

                string credentialsPath = null;
                foreach (var file in jsonFiles)
                {
                    if (file.Contains("firebase-adminsdk") || file.Contains("serviceAccount"))
                    {
                        credentialsPath = file;
                        break;
                    }
                }

                if (credentialsPath == null)
                {
                    MessageBox.Show("No se encontró el archivo de credenciales de Firebase.",
                                   "Error de configuración");
                    return;
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                _firestoreDb = FirestoreDb.Create(_projectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar Firebase: {ex.Message}", "Error");
            }
        }

        public async Task<List<ProductoModel>> ObtenerTodosLosProductos()
        {
            try
            {
                if (_firestoreDb == null)
                {
                    return new List<ProductoModel>();
                }

                var productos = new List<ProductoModel>();

                // 1. LEER DE COLECCIÓN "electronica"
                try
                {
                    CollectionReference electronicaRef = _firestoreDb.Collection("electronica");
                    QuerySnapshot electronicaSnapshot = await electronicaRef.GetSnapshotAsync();

                    foreach (DocumentSnapshot document in electronicaSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel();
                            producto.Id = document.Id;
                            producto.categoria = "Electronica"; // Asignar categoría manualmente

                            if (document.ContainsField("nombre"))
                                producto.nombre = document.GetValue<string>("nombre");
                            if (document.ContainsField("codigo"))
                                producto.codigo = document.GetValue<string>("codigo");
                            if (document.ContainsField("cantidad"))
                                producto.cantidad = document.GetValue<int>("cantidad");
                            if (document.ContainsField("precio"))
                                producto.precio = document.GetValue<double>("precio");
                            if (document.ContainsField("estante"))
                                producto.estante = document.GetValue<string>("estante");
                            if (document.ContainsField("fila"))
                                producto.fila = document.GetValue<string>("fila");
                            if (document.ContainsField("imagenURL"))
                                producto.imagenURL = document.GetValue<string>("imagenURL");

                            productos.Add(producto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en electronica: {ex.Message}");
                }

                // 2. LEER DE COLECCIÓN "robotica"
                try
                {
                    CollectionReference roboticaRef = _firestoreDb.Collection("robotica");
                    QuerySnapshot roboticaSnapshot = await roboticaRef.GetSnapshotAsync();

                    foreach (DocumentSnapshot document in roboticaSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel();
                            producto.Id = document.Id;
                            producto.categoria = "Robotica"; // Asignar categoría manualmente

                            if (document.ContainsField("nombre"))
                                producto.nombre = document.GetValue<string>("nombre");
                            if (document.ContainsField("codigo"))
                                producto.codigo = document.GetValue<string>("codigo");
                            if (document.ContainsField("cantidad"))
                                producto.cantidad = document.GetValue<int>("cantidad");
                            if (document.ContainsField("precio"))
                                producto.precio = document.GetValue<double>("precio");
                            if (document.ContainsField("estante"))
                                producto.estante = document.GetValue<string>("estante");
                            if (document.ContainsField("fila"))
                                producto.fila = document.GetValue<string>("fila");
                            if (document.ContainsField("imagenURL"))
                                producto.imagenURL = document.GetValue<string>("imagenURL");

                            productos.Add(producto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en robotica: {ex.Message}");
                }

                // 3. LEER DE COLECCIÓN "productos" (si tiene productos)
                try
                {
                    CollectionReference productosRef = _firestoreDb.Collection("productos");
                    QuerySnapshot productosSnapshot = await productosRef.GetSnapshotAsync();

                    foreach (DocumentSnapshot document in productosSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel();
                            producto.Id = document.Id;

                            // Intentar leer categoría del documento, si no existe asignar "Accesorios"
                            if (document.ContainsField("categoria"))
                                producto.categoria = document.GetValue<string>("categoria");
                            else
                                producto.categoria = "Accesorios";

                            if (document.ContainsField("nombre"))
                                producto.nombre = document.GetValue<string>("nombre");
                            if (document.ContainsField("codigo"))
                                producto.codigo = document.GetValue<string>("codigo");
                            if (document.ContainsField("cantidad"))
                                producto.cantidad = document.GetValue<int>("cantidad");
                            if (document.ContainsField("precio"))
                                producto.precio = document.GetValue<double>("precio");
                            if (document.ContainsField("estante"))
                                producto.estante = document.GetValue<string>("estante");
                            if (document.ContainsField("fila"))
                                producto.fila = document.GetValue<string>("fila");
                            if (document.ContainsField("imagenURL"))
                                producto.imagenURL = document.GetValue<string>("imagenURL");

                            productos.Add(producto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en productos: {ex.Message}");
                }

                return productos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error");
                return new List<ProductoModel>();
            }
        }

        // Método para obtener productos por categoría (ahora más simple)
        public async Task<List<ProductoModel>> ObtenerProductosPorCategoria(string categoria)
        {
            // Como ya tenemos todos los productos, filtramos por categoría
            var todos = await ObtenerTodosLosProductos();
            return todos.FindAll(p => p.categoria != null &&
                                     p.categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
        }

        // Método para agregar producto (ahora a la colección correspondiente)
        public async Task<bool> AgregarProducto(ProductoModel producto, string categoria)
        {
            try
            {
                string nombreColeccion = categoria.ToLower();
                CollectionReference coleccionRef = _firestoreDb.Collection(nombreColeccion);
                await coleccionRef.AddAsync(producto);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar producto: {ex.Message}", "Error");
                return false;
            }
        }

        // Método para eliminar producto (de la colección específica)
        public async Task<bool> EliminarProducto(string categoria, string id)
        {
            try
            {
                string nombreColeccion = categoria.ToLower();
                DocumentReference docRef = _firestoreDb.Collection(nombreColeccion).Document(id);
                await docRef.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error");
                return false;
            }
        }

        // Versión simplificada si no sabemos la categoría (buscamos en todas)
        public async Task<bool> EliminarProductoPorId(string id)
        {
            try
            {
                // Buscar en electronica
                var electronicaDoc = await _firestoreDb.Collection("electronica").Document(id).GetSnapshotAsync();
                if (electronicaDoc.Exists)
                {
                    await electronicaDoc.Reference.DeleteAsync();
                    return true;
                }

                // Buscar en robotica
                var roboticaDoc = await _firestoreDb.Collection("robotica").Document(id).GetSnapshotAsync();
                if (roboticaDoc.Exists)
                {
                    await roboticaDoc.Reference.DeleteAsync();
                    return true;
                }

                // Buscar en productos
                var productosDoc = await _firestoreDb.Collection("productos").Document(id).GetSnapshotAsync();
                if (productosDoc.Exists)
                {
                    await productosDoc.Reference.DeleteAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar producto: {ex.Message}", "Error");
                return false;
            }
        }
    }
}