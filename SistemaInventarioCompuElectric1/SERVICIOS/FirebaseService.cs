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
            // Buscar el archivo en ubicaciones SEGURAS fuera del proyecto
            _credentialsPath = BuscarArchivoCredenciales();

            // Verificar que el archivo existe
            if (string.IsNullOrEmpty(_credentialsPath) || !File.Exists(_credentialsPath))
            {
                string nombreEsperado = "compuelectric-inventario-firebase-adminsdk-fbsvc-ca8bf2fdf4.json";
                string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string mensaje = "⚠️ NO SE ENCONTRÓ EL ARCHIVO DE CREDENCIALES\n\n" +
                                $"Buscando específicamente: {nombreEsperado}\n\n" +
                                "📌 SOLUCIÓN INMEDIATA:\n" +
                                $"1. Tu archivo está en: {escritorio}\n" +
                                $"2. Debe llamarse EXACTAMENTE: {nombreEsperado}\n" +
                                $"3. Verifica que el nombre sea idéntico (incluyendo mayúsculas/minúsculas)\n\n" +
                                $"📂 Ruta completa buscada:\n{Path.Combine(escritorio, nombreEsperado)}\n\n" +
                                "🔍 Archivos encontrados en tu escritorio:\n";

                // Listar archivos JSON en el escritorio para ayudar
                try
                {
                    var archivosJson = Directory.GetFiles(escritorio, "*.json");
                    foreach (var archivo in archivosJson)
                    {
                        mensaje += $"   • {Path.GetFileName(archivo)}\n";
                    }
                }
                catch { }

                MessageBox.Show(mensaje, "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Configurar Firebase
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
            _firestoreDb = FirestoreDb.Create(_projectId);

            // Mostrar que se cargó correctamente
            MessageBox.Show($"✅ Firebase inicializado correctamente\n\nArchivo: {Path.GetFileName(_credentialsPath)}",
                           "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            System.Diagnostics.Debug.WriteLine($"✅ Firebase inicializado correctamente con: {Path.GetFileName(_credentialsPath)}");
            System.Diagnostics.Debug.WriteLine($"📍 Ruta: {_credentialsPath}");
        }

        private string BuscarArchivoCredenciales()
        {
            string nombreExacto = "compuelectric-inventario-firebase-adminsdk-fbsvc-ca8bf2fdf4.json";

            // 1. Buscar en el Escritorio con el nombre exacto
            string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreExacto);
            if (File.Exists(desktopPath))
                return desktopPath;

            // 2. Buscar en el Escritorio cualquier archivo JSON que contenga "firebase" o "admin"
            try
            {
                var archivosEscritorio = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.json");

                // Buscar por nombre exacto (sin ruta)
                foreach (var archivo in archivosEscritorio)
                {
                    if (Path.GetFileName(archivo).Equals(nombreExacto, StringComparison.OrdinalIgnoreCase))
                        return archivo;
                }

                // Buscar por parte del nombre
                foreach (var archivo in archivosEscritorio)
                {
                    string nombre = Path.GetFileName(archivo);
                    if (nombre.Contains("compuelectric") ||
                        nombre.Contains("firebase") ||
                        nombre.Contains("admin") ||
                        nombre.Contains("fbsvc"))
                    {
                        return archivo;
                    }
                }
            }
            catch { }

            // 3. Buscar en Mis Documentos
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nombreExacto);
            if (File.Exists(documentsPath))
                return documentsPath;

            // 4. Buscar en AppData/Roaming
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SistemaInventario", nombreExacto);
            if (File.Exists(appDataPath))
                return appDataPath;

            return null;
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