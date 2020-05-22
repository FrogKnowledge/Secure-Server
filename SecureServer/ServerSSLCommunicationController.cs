using CommonTypes;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using X509.Crypto;


namespace SecureServer
{
    internal class ServerSSLCommunicationController
    {
        private readonly DatabaseAdapter database = new DatabaseAdapter();
        private readonly X509Certificate2 certificate = new X509Certificate2(CryptContextHelper.CreateX509Certificate("SecureServer", "12345678987654321", DateTime.Now.AddYears(5)), "12345678987654321");
        private readonly byte[] Deny = Encoding.UTF8.GetBytes("DENY<EOF>");
        public void StartServer(int port, IPAddress iPAddress)
        {
            Task.Run(() =>
            {
                database.OpenConnection("server=DESKTOP-KKQPCSA;Trusted_Connection=Yes;DataBase=Secure;");


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                EndPoint ipPoint = new IPEndPoint(iPAddress, port);
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);
                while (true)
                {
                    Socket handler = listenSocket.Accept();
                    HandleClient(handler);
                }


            });

        }

        private void HandleClient(Socket handler)
        {
            Task.Run(() =>
            {

                using (var networkStream = new NetworkStream(handler, true))
                using (var sslStream = new SslStream(networkStream))
                {
                    try
                    {
                        sslStream.AuthenticateAsServer(certificate, clientCertificateRequired: false, checkCertificateRevocation: false);
                        // use sslStream.BeginRead/BeginWrite here
                        // sslStream.ReadTimeout = 10000;
                        // sslStream.WriteTimeout = 10000;
                        while (sslStream != null)
                        {
                            string messageData = ReadMessage(sslStream);
                            string operation = new String(messageData.TakeWhile((x, y) => x == ':' ? false : true).ToArray());
                            messageData = messageData.Substring(operation.Length + 1, messageData.Length - operation.Length - 1);
                            byte[] data = Deny;
                            if (operation == "auth")
                            {
                                data = Autenticate(messageData, handler);
                                sslStream.Write(data);
                                sslStream.Flush();
                                continue;
                            }
                            string[] parameters = messageData.Split('.');
                            if (parameters.Length == 3)
                            {
                                if (Authorize(messageData, handler, operation))
                                {
                                    switch (operation)
                                    {
                                        case "FirstData":
                                            data = EmployeeData();
                                            sslStream.Write(data);
                                            sslStream.Flush();
                                            data = ProfessionData();
                                            sslStream.Write(data);
                                            sslStream.Flush();
                                            continue;
                                        case "AddEmployee":
                                            database.AddEmployee(parameters[1]);
                                            data = EmployeeData();
                                            break;
                                        case "EditEmployee":
                                            database.EditEmployee(parameters[1]);
                                            data = EmployeeData();
                                            break;
                                        case "DeleteEmployee":
                                            database.DeleteEmployee(Encoding.UTF8.GetString(Convert.FromBase64String(parameters[1])));
                                            data = EmployeeData();
                                            break;

                                    }
                                }
                            }
                            sslStream.Write(data);
                            sslStream.Flush();

                        }

                    }
                    catch (Exception ex)
                    {

                    }
                }

            });
        }
        public void Stop()
        {
            database.CloseConnection();
        }

        private string ReadMessage(SslStream sslStream)
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
                    messageData = messageData.Remove(messageData.ToString().IndexOf("<EOF>"), 5);
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();

        }
        private byte[] EmployeeData()
        {
            byte[] answer = null;
            EmployeeContainer[] employees = database.GetEmployees();
            answer = Encoding.UTF8.GetBytes("UpdEmployeeData." + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(employees))) + "<EOF>");

            return answer;
        }
        private byte[] ProfessionData()
        {
            byte[] answer = null;
            StringContainer[] professions = database.GetProfessions();
            answer = Encoding.UTF8.GetBytes("UpdProfessionData." + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(professions))) + "<EOF>");

            return answer;
        }
        private bool Authorize(String message, Socket client, string operation)
        {
            string[] parameters = message.Split('.');
            bool autorize = false;
            string body = operation + ':' + message.Substring(0, message.Length - 1 - parameters[2].Length);
            string[] auth = database.ClientAutorizationData(parameters[0]);
            var hmac = new HMACSHA512(Convert.FromBase64String(auth[1] ?? "null"));
            if ((auth[0] == "Administrator" || auth[0] == "Guard") && Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))) == parameters[2])
            {
                IPEndPoint ClientEP = (IPEndPoint)client.RemoteEndPoint;
                string IP = ClientEP.Address.ToString();
                if (IP != auth[2])
                {
                    database.DeleteToken(parameters[0]);
                    client.Dispose();
                    return autorize;
                }
                autorize = true;
            }
            return autorize;
        }
        private byte[] Autenticate(String message, Socket client)
        {
            AutentificationData user = JsonConvert.DeserializeObject<AutentificationData>(message);
            byte[] data;
            var csp = new SHA512CryptoServiceProvider();
            if (database.GetPasswordHash(user.login) == Convert.ToBase64String(csp.ComputeHash(Encoding.UTF8.GetBytes(user.password))))
            {
                IPEndPoint ClientEP = (IPEndPoint)client.RemoteEndPoint;
                string IP = ClientEP.Address.ToString();

                byte[][] tokenAndKey = GenerateToken();
                byte[] token = tokenAndKey[0];
                byte[] key = tokenAndKey[1];
                database.RefreshToken(user.login, Convert.ToBase64String(token), Convert.ToBase64String(key), IP);
                message = "OK." + database.GetRole(user.login) + '.' + Convert.ToBase64String(token) + '.' + Convert.ToBase64String(key) + "<EOF>";
                data = Encoding.UTF8.GetBytes(message);
            }
            else
            {
                data = Deny;
            }
            return data;
        }

        private byte[][] GenerateToken()
        {
            byte[][] tokenAndKey = new byte[2][];
            byte[] crypto = new byte[100];
            var csp = new SHA512CryptoServiceProvider();

            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(crypto);
            }

            byte[] rawData = csp.ComputeHash(crypto.Concat(Encoding.UTF8.GetBytes(DateTime.Now.ToString())).ToArray());
            tokenAndKey[0] = rawData.Take(40).ToArray();
            tokenAndKey[1] = rawData.Reverse().Take(24).ToArray();

            return tokenAndKey;
        }

    }

    internal struct AutentificationData
    {
        public string login;
        public string password;

    }
}

