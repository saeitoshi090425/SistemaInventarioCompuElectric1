using Google.Cloud.Firestore;
using SistemaInventarioCompuElectric1.INVENTARIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaInventarioCompuElectric1.SERVICIOS
{
    public class FirebaseService
    {
        private FirestoreDb _firestoreDb;
        private string _projectId = "compuelectric-inventario";
        private string _credentialsPath;

        public FirebaseService()
        {
            try
            {
                InicializarFirebase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar Firebase: {ex.Message}", "Error");
            }
        }

        private void InicializarFirebase()
        {
            // 1. Obtener la ruta de la raíz del proyecto
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = ObtenerRaizProyecto(baseDirectory);

            // 2. Buscar el archivo de credenciales específico
            string nombreArchivoCredenciales = "compuelectric-inventario-firebase-adminsdk-fbsvc-ca8bf2fdf4.json";
            _credentialsPath = Path.Combine(projectRoot, nombreArchivoCredenciales);

            // 3. Verificar que el archivo existe
            if (!File.Exists(_credentialsPath))
            {
                MessageBox.Show($"No se encontró el archivo de credenciales:\n{_credentialsPath}\n\n" +
                               $"Asegúrate de que el archivo '{nombreArchivoCredenciales}' esté en la raíz del proyecto.",
                               "Error de configuración");
                return;
            }

            // 4. Configurar Firebase
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
            _firestoreDb = FirestoreDb.Create(_projectId);

            // 5. Mostrar que se cargó correctamente
            System.Diagnostics.Debug.WriteLine($"✅ Firebase inicializado correctamente con: {nombreArchivoCredenciales}");
        }

        private string ObtenerRaizProyecto(string baseDirectory)
        {
            var directory = new DirectoryInfo(baseDirectory);

            // Subir hasta encontrar un archivo .csproj o hasta 5 niveles
            for (int i = 0; i < 5; i++)
            {
                if (directory == null) break;

                // Si encontramos un archivo .csproj (señal de raíz)
                if (directory.GetFiles("*.csproj").Length > 0)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            // Si no encontramos .csproj, usar el directorio base
            return baseDirectory;
        }

        // Tus métodos existentes (sin cambios)
        public async Task<List<ProductoModel>> ObtenerTodosLosProductos()
        {
            try
            {
                if (_firestoreDb == null)
                {
                    return new List<ProductoModel>();
                }

                var productos = new List<ProductoModel>();

                // Electronica
                try
                {
                    var electronicaRef = _firestoreDb.Collection("electronica");
                    var electronicaSnapshot = await electronicaRef.GetSnapshotAsync();

                    foreach (var document in electronicaSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel
                            {
                                Id = document.Id,
                                categoria = "Electronica",
                                nombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : "",
                                codigo = document.ContainsField("codigo") ? document.GetValue<string>("codigo") : "",
                                cantidad = document.ContainsField("cantidad") ? document.GetValue<int>("cantidad") : 0,
                                precio = document.ContainsField("precio") ? document.GetValue<double>("precio") : 0,
                                estante = document.ContainsField("estante") ? document.GetValue<string>("estante") : "",
                                fila = document.ContainsField("fila") ? document.GetValue<string>("fila") : "",
                                imagenURL = document.ContainsField("imagenURL") ? document.GetValue<string>("imagenURL") : ""
                            };
                            productos.Add(producto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en electronica: {ex.Message}");
                }

                // Robotica
                try
                {
                    var roboticaRef = _firestoreDb.Collection("robotica");
                    var roboticaSnapshot = await roboticaRef.GetSnapshotAsync();

                    foreach (var document in roboticaSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel
                            {
                                Id = document.Id,
                                categoria = "Robotica",
                                nombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : "",
                                codigo = document.ContainsField("codigo") ? document.GetValue<string>("codigo") : "",
                                cantidad = document.ContainsField("cantidad") ? document.GetValue<int>("cantidad") : 0,
                                precio = document.ContainsField("precio") ? document.GetValue<double>("precio") : 0,
                                estante = document.ContainsField("estante") ? document.GetValue<string>("estante") : "",
                                fila = document.ContainsField("fila") ? document.GetValue<string>("fila") : "",
                                imagenURL = document.ContainsField("imagenURL") ? document.GetValue<string>("imagenURL") : ""
                            };
                            productos.Add(producto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en robotica: {ex.Message}");
                }

                // Productos
                try
                {
                    var productosRef = _firestoreDb.Collection("productos");
                    var productosSnapshot = await productosRef.GetSnapshotAsync();

                    foreach (var document in productosSnapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var producto = new ProductoModel
                            {
                                Id = document.Id,
                                categoria = document.ContainsField("categoria") ? document.GetValue<string>("categoria") : "Accesorios",
                                nombre = document.ContainsField("nombre") ? document.GetValue<string>("nombre") : "",
                                codigo = document.ContainsField("codigo") ? document.GetValue<string>("codigo") : "",
                                cantidad = document.ContainsField("cantidad") ? document.GetValue<int>("cantidad") : 0,
                                precio = document.ContainsField("precio") ? document.GetValue<double>("precio") : 0,
                                estante = document.ContainsField("estante") ? document.GetValue<string>("estante") : "",
                                fila = document.ContainsField("fila") ? document.GetValue<string>("fila") : "",
                                imagenURL = document.ContainsField("imagenURL") ? document.GetValue<string>("imagenURL") : ""
                            };
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

        public async Task<List<ProductoModel>> ObtenerProductosPorCategoria(string categoria)
        {
            var todos = await ObtenerTodosLosProductos();
            return todos.FindAll(p => p.categoria != null &&
                                     p.categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
        }

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

        public async Task<bool> EliminarProductoPorId(string id)
        {
            try
            {
                var colecciones = new[] { "electronica", "robotica", "productos" };

                foreach (var coleccion in colecciones)
                {
                    var doc = await _firestoreDb.Collection(coleccion).Document(id).GetSnapshotAsync();
                    if (doc.Exists)
                    {
                        await doc.Reference.DeleteAsync();
                        return true;
                    }
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