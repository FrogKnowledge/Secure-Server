using System.Windows;
using System.Security;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int port = 8005; // порт сервера
        static string address = "127.0.0.1"; // адрес сервера
        public MainWindow()
        {
            InitializeComponent();
           

        }
        struct AutentificationData {
           public string login;
           public  string password;
        }
        public void Send(object sender,EventArgs args)
        {
            try
            {
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                AutentificationData data;
                data.login = Login.Text;
                data.password = Password.Password;
                socket.Connect(ipPoint);
                byte[] SendData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(data));
                socket.Send(SendData);

                // получаем ответ
                SendData = new byte[256]; // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байт

                do
                {
                    bytes = socket.Receive(SendData, SendData.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(SendData, 0, bytes));
                }
                while (socket.Available > 0);
                if(builder.ToString()== "Успешный вход")
                {
                    LoginForm.Visibility = Visibility.Hidden;
                }
                message.Text="ответ сервера: " + builder.ToString();
               
                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
          
        }
    }
}
