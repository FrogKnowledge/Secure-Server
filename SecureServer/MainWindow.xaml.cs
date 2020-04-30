using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SecureServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int port = 447; // порт для приема входящих запросов

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;


        }

        public void GenerateServerX509Certificate(string Path, string SubjectOfSertification, int CountOfYears = 5)
        {
            var rsaKey = RSA.Create(2048);
            string subject = $"CN={SubjectOfSertification}";
            var certReq = new CertificateRequest(subject, rsaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            certReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certReq.PublicKey, false));
            var expirate = DateTimeOffset.Now.AddYears(CountOfYears);
            var caCert = certReq.CreateSelfSigned(DateTimeOffset.Now, expirate);
            var exportCert = new X509Certificate2(caCert.Export(X509ContentType.Cert), (string)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet).CopyWithPrivateKey((RSA)caCert.PrivateKey);
            File.WriteAllBytes(Path, exportCert.Export(X509ContentType.Pfx));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {

                /* if (new FileInfo("SrvCertificate.pfx").Exists == false)
                 {
                     GenerateServerX509Certificate("SrvCertificate.pfx", "SecureServer");
                 }
                 X509Certificate2 certificate = new X509Certificate2("SrvCertificate.crt");*/
                var rsaKey = RSA.Create(2048);
                string subject = $"CN=SecureServer";
                var certReq = new CertificateRequest(subject, rsaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                certReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certReq.PublicKey, false));
                var expirate = DateTimeOffset.Now.AddYears(5);
                var certificate = certReq.CreateSelfSigned(DateTimeOffset.Now, expirate);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                EndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);

                while (true)
                {
                    try
                    {
                        Socket handler = listenSocket.Accept();

                        using (var networkStream = new NetworkStream(handler, true))
                        using (var sslStream = new SslStream(networkStream))
                        {
                            sslStream.AuthenticateAsServer(certificate, clientCertificateRequired: false, checkCertificateRevocation: false);
                            // use sslStream.BeginRead/BeginWrite here
                            sslStream.ReadTimeout = 10000;
                            sslStream.WriteTimeout = 10000;
                            string messageData = ReadMessage(sslStream);
                            MessageBox.Show(messageData);
                            byte[] message = Encoding.UTF8.GetBytes("Hello from the server.<EOF>");
                            sslStream.Write(message);
                            sslStream.Close();
                        }

                        // закрываем сокет
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }


            });
        }

        private static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                // Read the client's test message.
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF or an empty message.
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
    }
}
