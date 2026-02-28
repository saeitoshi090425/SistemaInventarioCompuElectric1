using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SistemaInventarioCompuElectric1
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContenedorPrincipal.Content = new INVENTARIO.inventario_ventana_principal();
        }

        private void BotonInventario_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new INVENTARIO.inventario_ventana_principal();
        }

        private void BotonHistorial_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new HISTORIAL.historial_ventana_principal();
        }

        private void BotonCotizar_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new COTIZACION.cotizacion_ventana_principal();
        }

        private void BotonEstadisticas_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new ESTADISTICAS.estadisticas_ventana_principal();
        }
    }
}
