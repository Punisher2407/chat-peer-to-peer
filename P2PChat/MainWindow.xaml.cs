using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using P2PChat.Protocols;

namespace P2PChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IPAddress> ipList;
        private IPAddress selectedIP;
        private Connection connection;

        public MainWindow()
        {
            InitializeComponent();

            txtbxMessage.IsEnabled = false;
            btnSend.IsEnabled = false;
            btnConnect.IsDefault = true;

            connection = new Connection(UpdateChat);
            ipList = connection.GetIPList();
            foreach(IPAddress ip in ipList)
            {
                cmboxUserIP.Items.Add(ip.ToString());
            }
            cmboxUserIP.SelectedIndex = 0;
            selectedIP = ipList[0];
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            connection.chooseIP = selectedIP;
            connection.ConnectionToChat(txtbxLogin.Text);
            btnConnect.IsEnabled = false;
            cmboxUserIP.IsEnabled = false;
            btnConnect.IsDefault = false;
            txtbxLogin.IsReadOnly = true;

            btnSend.IsDefault = true;
            txtbxMessage.IsEnabled = true;
            btnSend.IsEnabled = true;
        }

        private void cmboxUserIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIP = IPAddress.Parse(cmboxUserIP.SelectedItem.ToString());
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string currMessage = txtbxMessage.Text;
            connection.SendNormalMessage(currMessage);
            txtbxMessage.Text = "";
            txtboxChatWindow.ScrollToEnd();
        }

        private void UpdateChat(string text)
        {
            txtboxChatWindow.AppendText(text);
        }

        private void formMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connection.SendDisconnectMessage();
            System.Environment.Exit(0);
        }
    }
}
