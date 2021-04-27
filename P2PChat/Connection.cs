using P2PChat.Clients_Message;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace P2PChat.Protocols
{
    class Connection
    {
        public delegate void UpdateWindowChat(string text);

        private const int UDP_PORT = 48887;
        private const int TCP_PORT = 48888;
        private string ownLogin;
        private List<Client> clients = new List<Client>();
        public IPAddress chooseIP;
        public UpdateWindowChat updateChat;
        private StringBuilder chatHistory;
        private readonly SynchronizationContext synchronizationContext;

        public Connection(UpdateWindowChat del)
        {
            updateChat = del;
            chatHistory = new StringBuilder();
            synchronizationContext = SynchronizationContext.Current;
        }

        public void ConnectionToChat(string login)
        {
            IPEndPoint srcIP = new IPEndPoint(chooseIP, UDP_PORT);
            IPEndPoint destIP = new IPEndPoint(MakeBroadcastAdr(chooseIP), UDP_PORT);
            UdpClient udpClient = new UdpClient(srcIP);
            udpClient.EnableBroadcast = true;

            ownLogin = login;
            byte[] connectMessBytes = Encoding.UTF8.GetBytes(login);

            try
            {
                udpClient.Send(connectMessBytes, connectMessBytes.Length, destIP);
                udpClient.Close();

                string connectMess = $"{DateTime.Now} : [{chooseIP}] {login} подключился к чату\n";
                chatHistory.Append(connectMess);
                updateChat($"{DateTime.Now} : [{chooseIP}] Вы ({login}) подключились к чату\n");

                Task recieveUdpBroadcast = new Task(ReceiveBroadcast);
                recieveUdpBroadcast.Start();

                Task recieveTCP = new Task(ReceiveTCP);
                recieveTCP.Start();
            }
           catch
           {
               MessageBox.Show("Sending Error!", "BAD", MessageBoxButton.OKCancel);
           }
        }

        private void ReceiveBroadcast()
        {
            IPEndPoint srcIP = new IPEndPoint(chooseIP, UDP_PORT);
            IPEndPoint destIP = new IPEndPoint(IPAddress.Any, UDP_PORT);
            UdpClient udpReceiver = new UdpClient(srcIP);

            while(true)
            {
                byte[] receivedData = udpReceiver.Receive(ref destIP);
                string clientLogin = Encoding.UTF8.GetString(receivedData);

                Client newClient = new Client(clientLogin, destIP.Address, TCP_PORT);
                newClient.EstablishConnection();
                clients.Add(newClient);
                newClient.SendMessage(new Message(Message.CONNECT, ownLogin));

                string infoMessage = $"{DateTime.Now} : [{newClient.IP}] {newClient.login} подключился к чату\n";

                synchronizationContext.Post(delegate { updateChat(infoMessage); }, null);
                

                Task.Factory.StartNew(() => ListenClient(newClient));
            }
        }

        private void ReceiveTCP()
        {
            TcpListener tcpListener = new TcpListener(chooseIP, TCP_PORT);
            tcpListener.Start();

            while(true)
            {
                TcpClient tcpNewClient = tcpListener.AcceptTcpClient();
                Client newClient = new Client(tcpNewClient, TCP_PORT);

                Task.Factory.StartNew(() => ListenClient(newClient));
            }

        }

        private void ListenClient(Client client)
        {
            while (true)
            {
                
                    Message tcpMessage = client.ReceiveMessage();
                    string infoMessage;

                    switch (tcpMessage.code)
                    {
                        case Message.CONNECT:
                            client.login = tcpMessage.data;
                            clients.Add(client);
                            GetHistoryMessageToConnect(client);
                            break;

                        case Message.MESSAGE:
                            infoMessage = $"{DateTime.Now} : [{client.IP}] {client.login} : {tcpMessage.data}\n";
                            synchronizationContext.Post(delegate { updateChat(infoMessage); chatHistory.Append(infoMessage); }, null);
                            break;

                        case Message.DISCONNECT:
                            infoMessage = $"{DateTime.Now} : [{client.IP}] {client.login} покинул чат\n";
                            synchronizationContext.Post(delegate { updateChat(infoMessage); chatHistory.Append(infoMessage); }, null);
                            clients.Remove(client);
                            return;

                        case Message.GET_HISTORY:
                            SendHistoryMessage(client);
                            break;

                        case Message.SHOW_HISTORY:
                            synchronizationContext.Post(delegate { updateChat(tcpMessage.data); chatHistory.Append(tcpMessage.data); }, null);
                            break;

                        default:
                            MessageBox.Show("Неверный формат сообщения", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                
            }
        }

        public void SendHistoryMessage(Client client)
        {
            Message historyMessage = new Message(Message.SHOW_HISTORY, chatHistory.ToString());
            client.SendMessage(historyMessage);
        }

        public void GetHistoryMessageToConnect(Client client)
        {
            Message historyMessage = new Message(Message.GET_HISTORY, "");
            client.SendMessage(historyMessage);
        }

        public void SendDisconnectMessage()
        {
            string disconnectStr = $"{ownLogin} покинул чат";
            Message disconnectMessage = new Message(Message.DISCONNECT, disconnectStr);
            SendMessageToAllClients(disconnectMessage);
        }

        public void SendNormalMessage(string mes)
        {
            if (mes != "")
            {
                Message normalMessage = new Message(Message.MESSAGE, mes);
                SendMessageToAllClients(normalMessage);
            }
        }

        public void SendMessageToAllClients(Message tcpMessage)
        {
            foreach (var user in clients)
            {
                try
                {
                        user.SendMessage(tcpMessage);
                }
                catch
                {
                    MessageBox.Show($"Не удалось отправить сообщение пользователю {user.login}.",
                        "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (tcpMessage.code == Message.MESSAGE)
            {
                string infoMessage = $"{DateTime.Now} : [{chooseIP}] Вы : {tcpMessage.data}\n";

                updateChat(infoMessage);

                infoMessage = $"{DateTime.Now} : [{chooseIP}] {ownLogin} : {tcpMessage.data}\n";
                chatHistory.Append(infoMessage);
            }

        }

        private IPAddress MakeBroadcastAdr(IPAddress ip)
        {
            string broadcastAddr = ip.ToString();
            broadcastAddr = broadcastAddr.Substring(0, broadcastAddr.LastIndexOf('.') + 1) + "255";

            return IPAddress.Parse(broadcastAddr);
        }
        public List<IPAddress> GetIPList()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] ipAddrs = Dns.GetHostAddresses(hostName);
            List<IPAddress> ipList = new List<IPAddress>();
            foreach (var item in ipAddrs)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    ipList.Add(item);
            }
            return ipList;
        }
    }
}