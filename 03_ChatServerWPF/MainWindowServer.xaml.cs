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
        private bool serverRunning;
        private List<TcpClient> clientConnectionList = new List<TcpClient>();
        private bool test = true;

        public MainWindowServer()
        {
            InitializeComponent();
        }

        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
        }

        private void UpdateClientList(TcpClient tcpClient)
        {
            Dispatcher.Invoke(() => listClients.Items.RemoveAt(clientConnectionList.IndexOf(tcpClient)));
        }

        private void AddMessageToClientList(string message)
        {
            Dispatcher.Invoke((() => listClients.Items.Add(message)));
        }

        private void AddMessageToChatBox(string message)
        {
            Dispatcher.Invoke((() => listChats.Items.Add(message)));
        }

        private async void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serverName.Text.Length > 0)
                {
                    if (BufferValidation(serverBufferSize.Text)
                        && IpValidation(serverIpAdress.Text)
                        && PortValidation(serverPort.Text))
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
                else
                {
                    MessageBox.Show("Fill in a server name", "Invalid input");
                }
            }
            catch (SocketException socketException)
            {
                MessageBox.Show(socketException.Message);
            }
        }

        private async void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string disconnectingMessage = "Server is closingSERVERDISCONNECT@";

                await Task.Run(() => SendMessageToClients(disconnectingMessage));


                AddMessage("Server is closing");

                Debug.WriteLine("Serverrunnigset to false");
                serverRunning = false;
                serverName.IsEnabled = true;
                serverPort.IsEnabled = true;
                serverBufferSize.IsEnabled = true;
                serverIpAdress.IsEnabled = true;
                btnStop.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Visible;
                test = false;
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
            serverIpAdress.IsEnabled = false;
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

            Debug.WriteLine("Out while loop van serverrunning!");
            tcpListener.Stop();
        }

        private async void ReceiveData(TcpClient tcpClient, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            NetworkStream networkStream = tcpClient.GetStream();

            string connectIncoming = "CONNECT@";
            string messageIncoming = "MESSAGE@";
            string disconnectIncoming = "DISCONNECT@";

            while (networkStream.CanRead)
            {
                string incomingMessage = "";
                string message = "";

                while (incomingMessage.IndexOf("@") < 0)
                {
                    int bytes = await networkStream.ReadAsync(buffer, 0, bufferSize);
                    message = Encoding.ASCII.GetString(buffer, 0, bytes);
                    incomingMessage += message;
                }

                if (incomingMessage.EndsWith(messageIncoming))
                {
                    message = incomingMessage.Remove(incomingMessage.Length - messageIncoming.Length);

                    AddMessageToChatBox(message);
                    await SendMessageToClients(message + "@");
                }
                else if (incomingMessage.EndsWith(disconnectIncoming))
                {
                    message = incomingMessage.Remove(incomingMessage.Length - disconnectIncoming.Length);

                    AddMessageToChatBox(message);
                    await SendMessageToClients(message + "@");
                    await SendDisconnectMessageToClient(tcpClient, "CLIENTDISCONNECTED@");
                    UpdateClientList(tcpClient);
                    clientConnectionList.Remove(tcpClient);
                }
                else if (incomingMessage.EndsWith(connectIncoming))
                {
                    message = incomingMessage.Remove(incomingMessage.Length - connectIncoming.Length);
                    AddMessageToClientList(message);
                    await SendMessageToClients(message + ": connected!@");
                }
            }
        }

        private async Task SendDisconnectMessageToClient(TcpClient tcpClient, string message)
        {
            try
            {
                NetworkStream networkStream = tcpClient.GetStream();
                if (networkStream.CanRead)
                {
                    byte[] serverMessageByteArray = Encoding.ASCII.GetBytes(message);
                    await networkStream.WriteAsync(serverMessageByteArray, 0, serverMessageByteArray.Length);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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

        private bool IpValidation(string input)
        {
            return IPAddress.TryParse(input, out var ip);
        }

        private bool BufferValidation(string input)
        {
            int bufferSizeInt = ParseStringToInt(input);
            return input.All(char.IsDigit) && bufferSizeInt > 0;
        }
    }
}