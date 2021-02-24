using System;
using System.Diagnostics;
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

        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() => listChats.Items.Add(message));
        }

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
                    Debug.WriteLine($"Client send this message - {connectionMessage}");
                }
            }
            catch (SocketException socketException)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

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
                
                while (incomingMessage.IndexOf("@") < 0)
                {
                    int bytes = await networkStream.ReadAsync(buffer, 0, bufferSize);
                    message = Encoding.ASCII.GetString(buffer, 0, bytes);
                    incomingMessage += message;
                }
                Debug.WriteLine(incomingMessage);

                if (incomingMessage.EndsWith("SERVERDISCONNECT@"))
                {
                    message = incomingMessage.Remove(incomingMessage.Length - serverDisconnectMessage.Length);
                    AddMessage(message);

                    updateDisplay();

                    networkStream.Close();
                    tcpClient.Close();
                    break;
                }

                if (incomingMessage.EndsWith("CLIENTDISCONNECTED@"))
                {
                    AddMessage("Disconnected!");
                    updateDisplay();
                    networkStream.Close();
                    tcpClient.Close();
                    break;
                }

                message = incomingMessage.Remove(incomingMessage.Length - 1);

                AddMessage(message);
            }
        }

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

        private async Task SendMessageToServer(string name, string message)
        {
            try
            {
                string fullMessage = name + ": " + message;
                fullMessage += "MESSAGE@";

                if (networkStream.CanWrite)
                {
                    byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(fullMessage);
                    await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    Debug.WriteLine($"Client send this message - {fullMessage}");
                }

                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (SocketException exception)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

        private int ParseStringToInt(string input)
        {
            int.TryParse(input, out int number);
            return number;
        }

        private bool NameValidation(string input)
        {
            var regex = new Regex("^[a-zA-Z0-9 ]*$");
            return regex.IsMatch(input);
        }

        private bool IpValidation(string input)
        {
            return IPAddress.TryParse(input, out var ip);
        }

        private bool PortValidation(string input)
        {
            const int maxPortNumber = 65535;
            return input.All(char.IsDigit) && ParseStringToInt(input) <= maxPortNumber;
        }

        private bool BufferValidation(string input)
        {
            int bufferSizeInt = ParseStringToInt(input);
            return input.All(char.IsDigit) && bufferSizeInt > 0;
        }
    }
}