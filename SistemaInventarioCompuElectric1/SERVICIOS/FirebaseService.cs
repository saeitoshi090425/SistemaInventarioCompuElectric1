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
            // 1. Obtener la ruta de la carpeta FirebaseCredentials
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = ObtenerRaizProyecto(baseDirectory);
            string credentialsFolder = Path.Combine(projectRoot, "FirebaseCredentials");

            if (!Directory.Exists(credentialsFolder))
            {
                MessageBox.Show($"No se encontró la carpeta 'FirebaseCredentials' en:\n{credentialsFolder}",
                               "Error de configuración");
                return;
            }

            // 2. Obtener todos los archivos de credenciales
            var archivos = ObtenerArchivosCredenciales(credentialsFolder);

            if (archivos.Count == 0)
            {
                MessageBox.Show($"No se encontraron archivos de credenciales en:\n{credentialsFolder}",
                               "Error de configuración");
                return;
            }

            // 3. ¡AUTOMÁGICO! Seleccionar el archivo según el usuario
            _credentialsPath = SeleccionarArchivoAutomatico(archivos);

            // 4. Configurar Firebase
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
            _firestoreDb = FirestoreDb.Create(_projectId);

            // 5. Mostrar qué archivo se está usando (solo en debug)
            System.Diagnostics.Debug.WriteLine($"✅ Usando: {Path.GetFileName(_credentialsPath)}");
        }

        private string ObtenerRaizProyecto(string baseDirectory)
        {
            var directory = new DirectoryInfo(baseDirectory);

            // Subir hasta encontrar la carpeta con FirebaseCredentials o hasta 5 niveles
            for (int i = 0; i < 5; i++)
            {
                if (directory == null) break;

                // Si encontramos FirebaseCredentials en este nivel
                if (Directory.Exists(Path.Combine(directory.FullName, "FirebaseCredentials")))
                {
                    return directory.FullName;
                }

                // Si encontramos un archivo .csproj (señal de raíz)
                if (directory.GetFiles("*.csproj").Length > 0)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return baseDirectory;
        }

        private List<string> ObtenerArchivosCredenciales(string folder)
        {
            var archivos = new List<string>();

            // Buscar archivos .json
            archivos.AddRange(Directory.GetFiles(folder, "*.json"));

            // Buscar archivos sin extensión pero con nombres específicos
            foreach (var archivo in Directory.GetFiles(folder))
            {
                string nombre = Path.GetFileName(archivo);
                if ((nombre.Contains("firebase-admintoken") ||
                     nombre.Contains("firebase-admin") ||
                     nombre.Contains("firebase-adminsdk")) &&
                    !archivos.Contains(archivo))
                {
                    archivos.Add(archivo);
                }
            }

            return archivos;
        }

        private string SeleccionarArchivoAutomatico(List<string> archivos)
        {
            // ============== CONFIGURACIÓN ==============
            // Aquí defines qué archivo usa cada persona
            // ===========================================

            // Obtener nombre de usuario de Windows
            string usuarioWindows = Environment.UserName;

            // Diccionario de usuarios -> archivos (MODIFICA ESTO)
            var configUsuarios = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // PON AQUÍ TU NOMBRE DE USUARIO DE WINDOWS Y TU ARCHIVO
                { "juan", "firebase-adminsdk-fbsvc-f1d99bf37b.json" }, // TU archivo
                
                // PON AQUÍ EL NOMBRE DE USUARIO DE TU AMIGO Y SU ARCHIVO  
                { "ADMIN", "firebase-adminsdk-fbsvc-066b6e8b80.json" } // ARCHIVO DE TU AMIGO
            };

            // Buscar si hay un archivo asignado para este usuario
            if (configUsuarios.TryGetValue(usuarioWindows, out string archivoUsuario))
            {
                // Buscar el archivo exacto
                var archivoEncontrado = archivos.FirstOrDefault(a =>
                    Path.GetFileName(a).Equals(archivoUsuario, StringComparison.OrdinalIgnoreCase));

                if (archivoEncontrado != null)
                {
                    return archivoEncontrado;
                }

                // Si no encuentra el exacto, buscar por parte del nombre
                string parteNombre = archivoUsuario.Replace(".json", "");
                archivoEncontrado = archivos.FirstOrDefault(a =>
                    Path.GetFileName(a).Contains(parteNombre));

                if (archivoEncontrado != null)
                {
                    return archivoEncontrado;
                }
            }

            // ============== REGLAS AUTOMÁTICAS ==============
            // Si no hay configuración específica, usar reglas inteligentes
            // =================================================

            // Regla 1: Si solo hay un archivo, usarlo
            if (archivos.Count == 1)
                return archivos[0];

            // Regla 2: Buscar el archivo de "f1d99bf37b" (tuyo)
            var tuArchivo = archivos.FirstOrDefault(a =>
                Path.GetFileName(a).Contains("f1d99bf37b"));
            if (tuArchivo != null)
                return tuArchivo;

            // Regla 3: Buscar el archivo de "066b6e8b80" (de tu amigo)
            var archivoAmigo = archivos.FirstOrDefault(a =>
                Path.GetFileName(a).Contains("066b6e8b80"));
            if (archivoAmigo != null)
                return archivoAmigo;

            // Regla 4: Buscar cualquier archivo con "admin"
            var archivoAdmin = archivos.FirstOrDefault(a =>
                Path.GetFileName(a).Contains("admin"));
            if (archivoAdmin != null)
                return archivoAdmin;

            // Regla 5: Si nada funciona, usar el primero
            return archivos[0];
        }

        // Tus métodos existentes (ObtenerTodosLosProductos, etc.)
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