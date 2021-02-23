using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace _03_ChatServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowServer : Window
    {
        TcpClient tcpClient;
        private TcpListener tcpListener;
        NetworkStream networkStream;
        private Boolean serverRunning;
        private List<TcpClient> clientConnectionList = new List<TcpClient>();

        protected delegate void UpdateDisplay(string message);

        public MainWindowServer()
        {
            InitializeComponent();
        }

        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
            // listChats.Items.Add(message);
            // listChats.SelectedIndex = listChats.Items.Count - 1;
        }

        private void AddMessageToClientList(string message)
        {
            if (clientConnectionList.Count == 1)
            {
                Dispatcher.Invoke((() => listClients.Items.Add(message)));
                Dispatcher.Invoke((() => listClients.Items.RemoveAt(0)));
            }
            else
            {
                Dispatcher.Invoke((() => listClients.Items.Add(message)));
            }
        }

        private void AddMessageToChatBox(string message)
        {
            if (clientConnectionList.Count == 1)
            {
                Dispatcher.Invoke((() => listChats.Items.Add(message)));
            }
            else
            {
                Dispatcher.Invoke((() => listChats.Items.Add(message)));
            }
        }

        private async void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PortValidation(serverPort.Text) && serverBufferSize.Text.All(char.IsDigit))
                {
                    AddMessage("Server is starting");
                    await CreateServerAsync(
                        ParseStringToInt(serverPort.Text),
                        serverBufferSize.Text
                    );
                }
                else
                {
                    MessageBox.Show("Invalid input", "Invalid input");
                }
            }
            catch
            {
                MessageBox.Show("SERVER CANNOT BE STARTED");
            }
        }

        private void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddMessage("Server is closing");
                serverRunning = false;
                tcpListener.Stop();
                serverName.IsEnabled = true;
                serverPort.IsEnabled = true;
                serverBufferSize.IsEnabled = true;
                btnStop.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Visible;
            }
            catch
            {
                MessageBox.Show("SERVER CANT BE STOPPED");
            }
        }

        private async Task CreateServerAsync(int port, string buffer)
        {
            serverRunning = true;
            serverName.IsEnabled = false;
            serverPort.IsEnabled = false;
            serverBufferSize.IsEnabled = false;
            btnStop.Visibility = Visibility.Visible;
            btnStart.Visibility = Visibility.Hidden;

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            AddMessage($"Listening for clients on port: {port}");

            while (serverRunning)
            {
                tcpClient = await tcpListener.AcceptTcpClientAsync();
                clientConnectionList.Add(tcpClient);
                //TODO stoppen en meerdere clients fixen!
                await Task.Run(() => ReceiveData(tcpClient, ParseStringToInt(buffer)));
            }
        }

        private async void ReceiveData(TcpClient tcpClient, int bufferSize)
        {
            Byte[] buffer = new byte[bufferSize];
            networkStream = tcpClient.GetStream();

            string connectIncoming = "@CONNECT";
            string messageIncoming = "@MESSAGE";
            string disconnectIncoming = "@DISCONNECT";

            while (networkStream.CanRead)
            {
                string incomingMessage = "";

                while (incomingMessage.IndexOf("@") < 0)
                {
                    Debug.WriteLine("Incoming message");
                    int bytes = await networkStream.ReadAsync(buffer, 0, bufferSize);
                    incomingMessage = Encoding.ASCII.GetString(buffer, 0, bytes);
                    
                }
                
                Debug.WriteLine("Incoming message" + incomingMessage);

                if (incomingMessage.EndsWith(connectIncoming))
                {
                    string message = incomingMessage.Remove(incomingMessage.Length - connectIncoming.Length);
                    AddMessageToClientList(message);
                    await SendMessageToClients(message + ": connected!@");
                }
                else if (incomingMessage.EndsWith(messageIncoming))
                {
                    string message = incomingMessage.Remove(incomingMessage.Length - messageIncoming.Length);
                    
                    AddMessageToChatBox(message);
                    message += "@";
                    await SendMessageToClients(message);
                }
                else if (incomingMessage.EndsWith(disconnectIncoming))
                {
                    string message = incomingMessage.Remove(incomingMessage.Length - disconnectIncoming.Length);
                
                    AddMessageToChatBox(message);
                    message += "@";
                    await SendMessageToClients(message);
                }
            }
        }

        private async Task SendMessageToClients(string message)
        {
            if (clientConnectionList.Count > 0)
            {
                foreach (var client in clientConnectionList)
                {
                    networkStream = client.GetStream();
                    if (networkStream.CanRead)
                    {
                        byte[] serverMessageByteArray = Encoding.ASCII.GetBytes(message);
                        await networkStream.WriteAsync(serverMessageByteArray, 0, serverMessageByteArray.Length);
                        Debug.WriteLine("Message send a client (must be multiple execute");
                    }
                }
            }
        }

        private int ParseStringToInt(string input)
        {
            int number;
            int.TryParse(input, out number);
            return number;
        }

        private bool PortValidation(string input)
        {
            const int maxPortNumber = 65535;
            return input.All(char.IsDigit) && ParseStringToInt(input) <= maxPortNumber;
        }
    }
}