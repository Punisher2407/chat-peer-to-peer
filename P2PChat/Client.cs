using System.Text;
using System.Net;
using System.Net.Sockets;

namespace P2PChat.Clients_Message
{
    public class Client
    {
        public string login;
        public IPAddress IP;
        private IPEndPoint endIP;
        private TcpClient tcpClient;
        private int tcpPort;
        public NetworkStream messageStream;

        public Client(TcpClient TcpClient, int clientPort)
        {
            tcpClient = TcpClient;
            tcpPort = clientPort;
            IP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
            messageStream = tcpClient.GetStream();
        }

        public Client(string clientLogin, IPAddress clientIP, int clientPort)
        {
            login = clientLogin;
            IP = clientIP;
            tcpPort = clientPort;
            endIP = new IPEndPoint(IP, tcpPort);
        }

        public void EstablishConnection()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(endIP);
            messageStream = tcpClient.GetStream();
        }

        public void SendMessage(Message clientMess)
        {
            byte[] bMesssage = Encoding.UTF8.GetBytes((char)clientMess.code + clientMess.data);
            messageStream.Write(bMesssage, 0, bMesssage.Length);
        }
        
        public Message ReceiveMessage()
        {
            StringBuilder message = new StringBuilder();
            byte[] buff = new byte[1024];

            do
            {
                try
                {
                    int size = messageStream.Read(buff, 0, buff.Length);
                    message.Append(Encoding.UTF8.GetString(buff, 0, size));
                }
                catch
                {
                    return new Message(Message.DISCONNECT, "");
                }

            }
            while (messageStream.DataAvailable);

            Message receiveMessage = new Message(message[0], message.ToString().Substring(1));

            return receiveMessage;
        }

    }
}
