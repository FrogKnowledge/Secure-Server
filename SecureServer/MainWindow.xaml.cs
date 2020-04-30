using System;
using System.Net;
using System.Windows;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Security.Policy;
using Newtonsoft.Json;

namespace SecureServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int port = 8005; // порт для приема входящих запросов
        string connectionString;
        DataTable phonesTable;

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
                
                
                // получаем адреса для запуска сокета
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                
                // создаем сокет
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipPoint);

                // начинаем прослушивание
                listenSocket.Listen(10);
               
                    while (true)
                    {
                   
                        Socket handler = listenSocket.Accept();
                    try
                    {

                        // получаем сообщение
                        StringBuilder builder = new StringBuilder();
                        int bytes = 0; // количество полученных байтов
                        byte[] data = new byte[256]; // буфер для получаемых данных

                        do
                        {
                            bytes = handler.Receive(data);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (handler.Available > 0);
                        AutentificationData user = JsonConvert.DeserializeObject<AutentificationData>(builder.ToString());
                        DataTable dataTable = Select($"select Пароль from Сотрудники where Логин='{user.login}'");
                        if (dataTable.Rows[0].ItemArray[0].ToString() == JsonConvert.SerializeObject(new SHA512CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(user.password))))
                        {
                            string message = "Успешный вход";
                            data = Encoding.Unicode.GetBytes(message);
                            handler.Send(data);
                        }
                        else
                        {
                            string message = "Неверный логин или пароль";
                            data = Encoding.Unicode.GetBytes(message);
                            handler.Send(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = "Неверный логин или пароль";
                        byte[] data = Encoding.Unicode.GetBytes(message);
                        handler.Send(data);
                    }
                    // отправляем ответ

                    // закрываем сокет
                    handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                   
                    }
                
          
            });
        }
    }
}
