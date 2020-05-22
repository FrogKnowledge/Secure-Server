using System.Net;
using System.Windows;

namespace SecureServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly int port = 447;
        private readonly ServerSSLCommunicationController communicationController = new ServerSSLCommunicationController();

        public MainWindow()
        {
            InitializeComponent();
            communicationController.StartServer(port, IPAddress.Loopback);
        }

        ~MainWindow()
        {
            communicationController.Stop();
        }
    }
}
