using Google.Cloud.Firestore;
using SistemaInventarioCompuElectric1.INVENTARIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaInventarioCompuElectric1.SERVICIOS
{
    public class FirebaseService
    {
        private FirestoreDb _firestoreDb;
        private string _projectId = "compuelectric-inventario";
        private string _credentialsPath;
        private bool _usandoCredencialesTemporales = false;

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
            try
            {
                // 1. PRIMERO: Intentar cargar desde recursos embebidos
                if (CargarCredencialesDesdeRecursos())
                {
                    // Si cargó desde recursos, configurar Firebase
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
                    _firestoreDb = FirestoreDb.Create(_projectId);

                    string mensaje = _usandoCredencialesTemporales ?
                        "✅ Firebase inicializado correctamente (usando archivo temporal)" :
                        "✅ Firebase inicializado correctamente";

                    System.Diagnostics.Debug.WriteLine($"✅ Firebase inicializado: {_credentialsPath}");
                    return;
                }

                // 2. SEGUNDO: Si no pudo cargar desde recursos, intentar búsqueda normal
                _credentialsPath = BuscarArchivoCredenciales();

                if (string.IsNullOrEmpty(_credentialsPath) || !File.Exists(_credentialsPath))
                {
                    MostrarErrorConInstrucciones();
                    return;
                }

                // Configurar Firebase con el archivo encontrado
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
                _firestoreDb = FirestoreDb.Create(_projectId);

                System.Diagnostics.Debug.WriteLine($"✅ Firebase inicializado con: {_credentialsPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en inicialización: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Error crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public async void TestFirebaseConnection()
        {
            try
            {
                string result = "=== COLECCIONES EN FIREBASE ===\n";

                // Para C# 7.3, usamos GetAsyncEnumerator manualmente
                var collections = _firestoreDb.ListRootCollectionsAsync();
                var enumerator = collections.GetAsyncEnumerator();

                try
                {
                    while (await enumerator.MoveNextAsync())
                    {
                        var collection = enumerator.Current;
                        result += $"\n📁 Colección: {collection.Id}\n";

                        try
                        {
                            // Obtener documentos de la colección
                            var snapshot = await collection.GetSnapshotAsync();
                            result += $"   📊 Documentos: {snapshot.Documents.Count}\n";

                            // Mostrar primeros 3 documentos como ejemplo
                            int count = 0;
                            foreach (var doc in snapshot.Documents)
                            {
                                if (count >= 3) break;

                                result += $"   📄 ID: {doc.Id}\n";

                                // Mostrar campos del documento
                                var dictionary = doc.ToDictionary();
                                foreach (var kvp in dictionary)
                                {
                                    result += $"      • {kvp.Key}: {kvp.Value}\n";
                                }
                                count++;
                            }
                        }
                        catch (Exception ex)
                        {
                            result += $"   ❌ Error al leer documentos: {ex.Message}\n";
                        }
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                if (result == "=== COLECCIONES EN FIREBASE ===\n")
                {
                    result += "\n⚠️ No se encontraron colecciones en Firebase";
                }

                MessageBox.Show(result, "Test Firebase",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en TestFirebaseConnection: {ex.Message}\n\nStack: {ex.StackTrace}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool CargarCredencialesDesdeRecursos()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] recursos = assembly.GetManifestResourceNames();

                // Buscar el recurso JSON (sin mostrar MessageBox)
                string resourceName = recursos.FirstOrDefault(r => r.EndsWith(".json"));

                if (resourceName == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se encontró archivo JSON embebido");
                    return false;
                }

                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null) return false;

                    string tempPath = Path.GetTempFileName();
                    string jsonPath = Path.ChangeExtension(tempPath, "json");

                    if (File.Exists(tempPath)) File.Delete(tempPath);

                    using (var fileStream = File.Create(jsonPath))
                    {
                        resourceStream.CopyTo(fileStream);
                    }

                    _credentialsPath = jsonPath;
                    _usandoCredencialesTemporales = true;

                    System.Diagnostics.Debug.WriteLine($"✅ Credenciales cargadas: {resourceName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando credenciales: {ex.Message}");
                return false;
            }
        }

        private void MostrarErrorConInstrucciones()
        {
            string nombreEsperado = "compuelectric-inventario-firebase-adminsdk-fbsvc-ca8bf2fdf4.json";
            string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string mensaje = "⚠️ NO SE ENCONTRÓ EL ARCHIVO DE CREDENCIALES\n\n" +
                            "📌 INSTRUCCIONES PARA EMBEBER EL ARCHIVO:\n\n" +
                            "1. Crea una carpeta 'Resources' en el proyecto\n" +
                            "2. Copia tu archivo JSON como 'firebase-credenciales.json'\n" +
                            "3. En propiedades del archivo, pon 'Acción de compilación = Recurso incrustado'\n" +
                            "4. Recompila el proyecto\n\n" +
                            "📌 O ALTERNATIVAMENTE:\n" +
                            $"Copia el archivo al escritorio con el nombre:\n{nombreEsperado}\n\n" +
                            "🔍 Archivos JSON en tu escritorio:\n";

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
        }

        private string BuscarArchivoCredenciales()
        {
            string nombreExacto = "compuelectric-inventario-firebase-adminsdk-fbsvc-ca8bf2fdf4.json";

            // 1. Buscar en el Escritorio
            string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreExacto);
            if (File.Exists(desktopPath))
                return desktopPath;

            // 2. Buscar en el Escritorio cualquier JSON similar
            try
            {
                var archivosEscritorio = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.json");
                foreach (var archivo in archivosEscritorio)
                {
                    string nombre = Path.GetFileName(archivo);
                    if (nombre.Contains("compuelectric") || nombre.Contains("firebase") || nombre.Contains("admin"))
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

            return null;
        }



        // Tus métodos existentes (sin cambios)
        public async Task<List<ProductoModel>> ObtenerTodosLosProductos()
        {
            try
            {
                if (_firestoreDb == null)
                {
                    MessageBox.Show("Firebase no está inicializado correctamente", "Error");
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

        public async Task<bool> ActualizarProducto(ProductoModel producto, string nombreColeccion)
        {
            try
            {
                // Validar que Firebase esté inicializado
                if (_firestoreDb == null)
                {
                    MessageBox.Show("Firebase no está inicializado", "Error");
                    return false;
                }

                // Validar que tenga ID
                if (string.IsNullOrEmpty(producto.Id))
                {
                    MessageBox.Show("El producto no tiene ID válido", "Error");
                    return false;
                }

                // Mostrar en Debug los datos que se van a actualizar
                System.Diagnostics.Debug.WriteLine("=== DATOS A ACTUALIZAR EN FIREBASE ===");
                System.Diagnostics.Debug.WriteLine($"ID: {producto.Id}");
                System.Diagnostics.Debug.WriteLine($"Nombre: {producto.nombre}");
                System.Diagnostics.Debug.WriteLine($"Código: {producto.codigo}");
                System.Diagnostics.Debug.WriteLine($"Categoría: {producto.categoria}");
                System.Diagnostics.Debug.WriteLine($"Cantidad: {producto.cantidad}");
                System.Diagnostics.Debug.WriteLine($"Precio: {producto.precio}");
                System.Diagnostics.Debug.WriteLine($"Estante: {producto.estante}");
                System.Diagnostics.Debug.WriteLine($"Fila: {producto.fila}");
                System.Diagnostics.Debug.WriteLine($"ImagenURL: {producto.imagenURL}");
                System.Diagnostics.Debug.WriteLine($"Colección: {nombreColeccion}");

                // Obtener referencia al documento
                DocumentReference docRef = _firestoreDb.Collection(nombreColeccion).Document(producto.Id);

                // Crear un diccionario con los campos a actualizar
                var updates = new Dictionary<string, object>
        {
            { "nombre", producto.nombre },
            { "codigo", producto.codigo },
            { "categoria", producto.categoria },
            { "cantidad", producto.cantidad },
            { "precio", producto.precio },
            { "estante", producto.estante ?? "" },
            { "fila", producto.fila ?? "" },
            { "imagenURL", producto.imagenURL ?? "" }
        };

                // Actualizar el documento
                await docRef.UpdateAsync(updates);

                System.Diagnostics.Debug.WriteLine($"✅ Producto actualizado en colección: {nombreColeccion}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar producto: {ex.Message}");
                MessageBox.Show($"Error al actualizar producto: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
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