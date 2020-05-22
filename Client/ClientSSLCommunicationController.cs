using CommonTypes;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    internal class ClientSSLCommunicationController
    {
        private SslStream SslStream;
        private MainWindow main;

        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        public void TryConnect(IPAddress iPAddress, int port, string login, string password, MainWindow main)
        {
            Task.Run(() =>
            {
                this.main = main;
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    IPEndPoint ipPoint = new IPEndPoint(iPAddress, port);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ipPoint);
                    using (var networkStream = new NetworkStream(socket, true))
                    using (var sslStream = new SslStream(networkStream, false, ValidateServerCertificate))
                    {
                        this.SslStream = sslStream;
                        sslStream.AuthenticateAsClient("SecureServer");
                        AutentificationData data;
                        data.login = login;
                        data.password = password;
                        byte[] SendData = Encoding.UTF8.GetBytes("auth:" + JsonConvert.SerializeObject(data) + "<EOF>");
                        sslStream.Write(SendData);
                        sslStream.Flush();
                        string serverMessage = ReadMessage();

                        if (main.DisplayRoleInterface(serverMessage))
                        {
                            StartHandlingAnswer();
                        }

                    };

                    socket.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            });
        }
        private void StartHandlingAnswer()
        {
            string serverMessage;
            while (true)
            {
                serverMessage = ReadMessage();
                string[] parameters = serverMessage.Split('.');
                if (parameters.Length != 0)
                {
                    switch (parameters[0])
                    {
                        case "UpdEmployeeData":
                            var newEmployeeData = JsonConvert.DeserializeObject<ObservableCollection<EmployeeContainer>>(Encoding.UTF8.GetString(Convert.FromBase64String(parameters[1])));
                            main.Dispatcher.Invoke(() =>
                            {
                                main.Employees.Clear();
                                for (int i = 0; i < newEmployeeData.Count; i++)
                                {
                                    main.Employees.Add(newEmployeeData[i]);

                                }
                            });
                            break;
                        case "UpdProfessionData":
                            var newProfessionData = JsonConvert.DeserializeObject<ObservableCollection<StringContainer>>(Encoding.UTF8.GetString(Convert.FromBase64String(parameters[1])));
                            main.Dispatcher.Invoke(() =>
                            {
                                main.Professions.Clear();
                                for (int i = 0; i < newProfessionData.Count; i++)
                                {
                                    main.Professions.Add(newProfessionData[i]);
                                }
                            });
                            break;
                    }
                }
            }
        }
        public void Send(string operation, string sendData, byte[] token, byte[] key)
        {
            Task.Run(() =>
            {
                var hmac = new HMACSHA512(key);
                Encoding enc = Encoding.UTF8;
                string body = operation + ':' + Convert.ToBase64String(token) + '.' + Convert.ToBase64String(enc.GetBytes(sendData));
                string signature = Convert.ToBase64String(hmac.ComputeHash(enc.GetBytes(body)));
                string package = body + '.' + signature + "<EOF>";
                SslStream?.Write(enc.GetBytes(package));
                SslStream?.Flush();
            });
        }
        private string ReadMessage()
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {

                bytes = SslStream.Read(buffer, 0, buffer.Length);
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    messageData.Remove(messageData.ToString().IndexOf("<EOF>"), 5);
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();

        }

        private struct AutentificationData
        {
            public string login;
            public string password;
        }
    }
}
