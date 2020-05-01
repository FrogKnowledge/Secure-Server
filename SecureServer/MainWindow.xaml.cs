using System;
using System.Windows;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using X509.Crypto;


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


        public DataTable Select(string selectSQL) // функция подключения к базе данных и обработка запросов
        {
            DataTable dataTable = new DataTable("Secure");                // создаём таблицу в приложении
                                                                            // подключаемся к базе данных
            SqlConnection sqlConnection = new SqlConnection("server=DESKTOP-KKQPCSA;Trusted_Connection=Yes;DataBase=Secure;");
            sqlConnection.Open();                                           // открываем базу данных
            SqlCommand sqlCommand = sqlConnection.CreateCommand();          // создаём команду
            sqlCommand.CommandText = selectSQL;                             // присваиваем команде текст
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand); // создаём обработчик
            sqlDataAdapter.Fill(dataTable);                                 // возращаем таблицу с результатом
            return dataTable;
        }
        struct AutentificationData
        {
            public string login;
            public string password;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var certificate = new X509Certificate2(CryptContextHelper.CreateX509Certificate("SecureServer", "12345678987654321", DateTime.Now.AddYears(5)), "12345678987654321");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                EndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);
                while (true)
                {

                    Socket handler = listenSocket.Accept();
                    try
                    {
                        using (var networkStream = new NetworkStream(handler, true))
                        using (var sslStream = new SslStream(networkStream))
                        {
                            sslStream.AuthenticateAsServer(certificate, clientCertificateRequired: false, checkCertificateRevocation: false);
                            // use sslStream.BeginRead/BeginWrite here
                           // sslStream.ReadTimeout = 10000;
                           // sslStream.WriteTimeout = 10000;
                            string messageData = ReadMessage(sslStream);
                            AutentificationData user = JsonConvert.DeserializeObject<AutentificationData>(messageData);
                            DataTable dataTable = Select($"select Пароль from Сотрудники where Логин='{user.login}'");
                            byte[] data;
                            if (dataTable.Rows[0].ItemArray[0].ToString() == JsonConvert.SerializeObject(new SHA512CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(user.password))))
                            {
                                string message = "Успешный вход<EOF>";
                                data = Encoding.UTF8.GetBytes(message);
                            }
                            else
                            {
                                string message = "Неверный логин или пароль<EOF>";
                                data = Encoding.UTF8.GetBytes(message);
                            }
                            sslStream.Write(data);
                            sslStream.Close();
                        }

                    }
                    catch (Exception ex)
                    {

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
                    messageData=messageData.Remove(messageData.ToString().IndexOf("<EOF>"), 5);
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();

        }
    }
}
