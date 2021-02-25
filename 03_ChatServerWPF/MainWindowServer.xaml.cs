using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

        public MainWindowServer()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Updates the client list to show what clients are connected!
        /// </summary>
        /// <param name="tcpClient"></param>
        private void UpdateClientList(TcpClient tcpClient)
        {
            if (clientConnectionList.Count > 0 && clientConnectionList.Contains(tcpClient))
            {
                Dispatcher.Invoke(() => listClients.Items.RemoveAt(clientConnectionList.IndexOf(tcpClient)));
            }
        }

        /// <summary>
        /// Clear the client list after te server is disconnected
        /// </summary>
        private void EmptyClientList()
        {
            Dispatcher.Invoke(() => listClients.Items.Clear());
        }

        /// <summary>
        /// Add a client to the client list
        /// </summary>
        /// <param name="message"></param>
        private void AddMessageToClientList(string message)
        {
            Dispatcher.Invoke((() => listClients.Items.Add(message)));
        }

        /// <summary>
        /// Add a message to the chatBox
        /// </summary>
        /// <param name="message"></param>
        private void AddMessageToChatBox(string message)
        {
            Dispatcher.Invoke((() => listChats.Items.Add(message)));
        }

        /// <summary>
        /// Start the server button. This is the event handler coming from the WPF XAML code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (serverName.Text.Length > 0)
            {
                if (BufferValidation(serverBufferSize.Text)
                    && IpValidation(serverIpAdress.Text)
                    && PortValidation(serverPort.Text))
                {
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

        /// <summary>
        /// Stop the server button. This is the event handler coming from the WPF XAML code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string disconnectingMessage = "Server is closingSERVERDISCONNECT@";

                await SendMessageToClients(disconnectingMessage);

                EmptyClientList();

                clientConnectionList.Clear();

                foreach (var client in clientConnectionList)
                {
                    client.Close();
                }

                AddMessageToChatBox("Server is closing");

                serverRunning = false;
                tcpListener.Stop();
                serverName.IsEnabled = true;
                serverPort.IsEnabled = true;
                serverBufferSize.IsEnabled = true;
                serverIpAdress.IsEnabled = true;
                btnStop.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Visible;
            }
            catch
            {
                MessageBox.Show("SERVER CANT BE STOPPED");
            }
        }

        /// <summary>
        /// Creates the server async. With adjustable buffersize.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private async Task CreateServerAsync(int port, string buffer)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                AddMessageToChatBox("Server is starting");
                serverRunning = true;
                serverName.IsEnabled = false;
                serverPort.IsEnabled = false;
                serverBufferSize.IsEnabled = false;
                serverIpAdress.IsEnabled = false;
                btnStop.Visibility = Visibility.Visible;
                btnStart.Visibility = Visibility.Hidden;
                AddMessageToChatBox($"Listening for clients on port: {port}");
                
                // While server is running, keeps wait for other client to connect.
                while (serverRunning)
                {
                    try
                    {
                        tcpClient = await tcpListener.AcceptTcpClientAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    clientConnectionList.Add(tcpClient);
                    await Task.Run(() => ReceiveData(tcpClient, ParseStringToInt(buffer)));
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Cant connect server with these values, check your ipadress or port!", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
        
        /// <summary>
        /// Checks for incoming data
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="bufferSize"></param>
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

                try
                {
                    // Checks if a message is fully send.
                    while (incomingMessage.IndexOf("@") < 0)
                    {
                        int bytes = await networkStream.ReadAsync(buffer, 0, bufferSize);
                        message = Encoding.ASCII.GetString(buffer, 0, bytes);
                        incomingMessage += message;
                    }
                }
                catch (IOException)
                {
                    break;
                }

                // this if statement constructing determines what to do with the message.
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
                    await SendDisconnectMessageToClient(tcpClient, "CLIENTDC@");
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

        /// <summary>
        /// Send a disconnect message to the client that wants to disconnect.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendDisconnectMessageToClient(TcpClient tcpClient, string message)
        {
            networkStream = tcpClient.GetStream();
            if (networkStream.CanRead)
            {
                byte[] serverMessageByteArray = Encoding.ASCII.GetBytes(message);
                await networkStream.WriteAsync(serverMessageByteArray, 0, serverMessageByteArray.Length);
            }
        }

        /// <summary>
        /// Sends a message to all the clients currently connected
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Parses a string to a int.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private int ParseStringToInt(string input)
        {
            int number;
            int.TryParse(input, out number);
            return number;
        }

        /// <summary>
        /// Validates the port
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool PortValidation(string input)
        {
            const int maxPortNumber = 65535;
            return input.All(char.IsDigit) && ParseStringToInt(input) <= maxPortNumber && ParseStringToInt(input) > 0;
        }

        /// <summary>
        /// Validates the IPAdress. Done by using the IPAdress class.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool IpValidation(string input)
        {
            return IPAddress.TryParse(input, out var ip);
        }

        /// <summary>
        /// Valides the buffer
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool BufferValidation(string input)
        {
            int bufferSizeInt = ParseStringToInt(input);
            return input.All(char.IsDigit) && bufferSizeInt > 0;
        }

        /// <summary>
        /// On screen closes, closes the connection. To prevent crash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void close_server(object sender, CancelEventArgs e)
        {
            if (serverRunning && tcpListener.Server.Connected)
            {
                string disconnectingMessage = "Server is closingSERVERDISCONNECT@";

                await Task.Run(() => SendMessageToClients(disconnectingMessage));

                tcpListener.Stop();
            }
        }
    }
}