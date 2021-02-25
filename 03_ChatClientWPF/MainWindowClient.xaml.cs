using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace _03_ChatClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowClient : Window
    {
        TcpClient tcpClient;
        NetworkStream networkStream;

        public MainWindowClient()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Add a message to the chatBox
        /// </summary>
        /// <param name="message"></param>
        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
        }

        /// <summary>
        /// Updates the display after server message
        /// </summary>
        private void updateDisplay()
        {
            Dispatcher.Invoke(() => btnConnect.Visibility = Visibility.Visible);
            Dispatcher.Invoke(() => btnDisconnect.Visibility = Visibility.Hidden);
            Dispatcher.Invoke(() => clientName.IsEnabled = true);
            Dispatcher.Invoke(() => clientIp.IsEnabled = true);
            Dispatcher.Invoke(() => clientPort.IsEnabled = true);
            Dispatcher.Invoke(() => clientBufferSize.IsEnabled = true);
            Dispatcher.Invoke(() => btnSend.IsEnabled = false);
            Dispatcher.Invoke(() => txtMessage.IsEnabled = false);
        }

        /// <summary>
        /// This is the handler to connect. This is the event handler coming from the WPF XAML code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NameValidation(clientName.Text) && IpValidation(clientIp.Text)
                                                    && PortValidation(clientPort.Text) &&
                                                    BufferValidation(clientBufferSize.Text))
                {
                    int port = ParseStringToInt(clientPort.Text);
                    await CreateConnectionAsync(clientName.Text, clientIp.Text, port, clientBufferSize.Text);
                }
                else
                {
                    MessageBox.Show("Input value is not correct", "Invalid input");
                }
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
            }
        }

        /// <summary>
        /// TcpClient connects to the tcpListener
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task CreateConnectionAsync(string name, string ip, int port, string bufferSize)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(ip, port);

                btnConnect.Visibility = Visibility.Hidden;
                btnDisconnect.Visibility = Visibility.Visible;
                clientName.IsEnabled = false;
                clientIp.IsEnabled = false;
                clientPort.IsEnabled = false;
                clientBufferSize.IsEnabled = false;
                btnSend.IsEnabled = true;
                txtMessage.IsEnabled = true;

                networkStream = tcpClient.GetStream();

                await Task.Run(() => UpdateClientListBox(name));
                await Task.Run(() => ReceiveData(ParseStringToInt(bufferSize)));
            }
            catch (SocketException e)
            {
                MessageBox.Show("Could not create a connection with the server", "Connection error");
            }
        }

        /// <summary>
        /// Message to update to client box in the server. Also the server knows your are connected
        /// </summary>
        /// <param name="name"></param>
        private async void UpdateClientListBox(string name)
        {
            try
            {
                string connectionMessage = name;
                connectionMessage += "CONNECT@";

                if (networkStream.CanWrite)
                {
                    byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(connectionMessage);
                    await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

        /// <summary>
        /// Checks for incoming data from the server
        /// </summary>
        /// <param name="bufferSize"></param>
        private async void ReceiveData(int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            NetworkStream networkStream = tcpClient.GetStream();

            string serverDisconnectMessage = "SERVERDISCONNECT@";
            string clientDisconnectMessage = "CLIENTDISCONNECTED@";

            while (networkStream.CanRead)
            {
                string incomingMessage = "";
                string message = "";

                try
                {
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
                if (incomingMessage.EndsWith("SERVERDISCONNECT@"))
                {
                    message = incomingMessage.Remove(incomingMessage.Length - serverDisconnectMessage.Length);
                    AddMessage(message);
                    updateDisplay();
                    break;
                }

                if (incomingMessage.EndsWith("CLIENTDC@"))
                {
                    AddMessage("Disconnected!");
                    updateDisplay();
                    break;
                }

                message = incomingMessage.Remove(incomingMessage.Length - 1);

                AddMessage(message);
            }

            networkStream.Close();
            tcpClient.Close();
            updateDisplay();
        }

        /// <summary>
        /// Disconnect handler. This is the event handler coming from the WPF XAML code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await DisconnectClient();
            }
            catch
            {
                MessageBox.Show("YOU CANNOT DISCONNECT");
            }
        }

        /// <summary>
        /// Disconnect you from the server and lets the server know you disconnected
        /// </summary>
        /// <returns></returns>
        private async Task DisconnectClient()
        {
            string disconnectMessage = clientName.Text + ": disconnected";
            disconnectMessage += "DISCONNECT@";

            networkStream = tcpClient.GetStream();

            if (networkStream.CanRead)
            {
                byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(disconnectMessage);
                await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
            }
        }

        /// <summary>
        /// Message handler. This is the event handler coming from the WPF XAML code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SendMessageToServer(clientName.Text, txtMessage.Text);
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
            }
        }

        /// <summary>
        /// Send message to server with the right message marker.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendMessageToServer(string name, string message)
        {
            try
            {
                if (message.Length != 0)
                {
                    string fullMessage = name + ": " + message;
                    fullMessage += "MESSAGE@";

                    if (networkStream.CanWrite)
                    {
                        byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(fullMessage);
                        await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    }

                    txtMessage.Clear();
                    txtMessage.Focus();
                }
                else
                {
                    MessageBox.Show("Fill in a message", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (SocketException exception)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

        /// <summary>
        /// Parses a string to a int.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private int ParseStringToInt(string input)
        {
            int.TryParse(input, out int number);
            return number;
        }

        /// <summary>
        /// Validates the name from a client.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool NameValidation(string input)
        {
            var regex = new Regex("^[a-zA-Z0-9 ]*$");
            return regex.IsMatch(input);
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
        private async void close_client(object sender, CancelEventArgs e)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                await DisconnectClient();
            }
        }
    }
}