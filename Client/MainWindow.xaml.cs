using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int port = 447; // порт сервера
        private static string address = "127.0.0.1"; // адрес сервера
        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += (x, y) => Send(x, y);
        }
        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {

            return true;
        }
        public void Send(object sender, EventArgs args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // подключаемся к удаленному хосту
            socket.Connect(ipPoint);
            using (var networkStream = new NetworkStream(socket, true))
            using (var sslStream = new SslStream(networkStream,false,ValidateServerCertificate))
            {
                sslStream.AuthenticateAsClient("SecureServer");
                byte[] messsage = Encoding.UTF8.GetBytes("Hello from the client.<EOF>");
                // Send hello message to the server. 
                sslStream.Write(messsage);
                sslStream.Flush();
                // Read message from the server.
                string serverMessage = ReadMessage(sslStream);
                MessageBox.Show(serverMessage);
            };
            socket.Close();

        }

        private static string ReadMessage(SslStream sslStream)
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
    }
}
