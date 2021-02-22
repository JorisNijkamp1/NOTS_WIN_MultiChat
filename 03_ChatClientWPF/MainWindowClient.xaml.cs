using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NameValidation(clientName.Text) && IpValidation(clientIp.Text)
                                                    && PortValidation(clientPort.Text) &&
                                                    clientBufferSize.Text.All(char.IsDigit))
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
                Task.Run(() => ReceiveData(tcpClient, ParseStringToInt(bufferSize)));
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
                string connectionMessage = "@CONNECT";

                if (networkStream.CanWrite)
                {
                    connectionMessage += name;
                    byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(connectionMessage);
                    await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    Debug.WriteLine("Client send this message - connect");
                }
            }
            catch (SocketException socketException)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

        private void ReceiveData(TcpClient tcpClient, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];

            while (networkStream.CanRead)
            {
                using (networkStream = tcpClient.GetStream())
                {
                    string messageIncoming = "@MESSAGE";
                    string connectIncoming = "@CONNECT";
                    int length;
                    while ((length = networkStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        // Determine incoming bytes.
                        var incomingData = new byte[length];

                        Array.Copy(buffer, 0, incomingData, 0, length);

                        string clientMessage = Encoding.ASCII.GetString(incomingData);

                        if (clientMessage.StartsWith(messageIncoming))
                        {
                            AddMessage(clientMessage.Remove(0, messageIncoming.Length));
                        }
                        else if (clientMessage.StartsWith(connectIncoming))
                        {
                            AddMessage(clientMessage.Remove(0, connectIncoming.Length));
                        }
                    }
                }
            }
        }

        private async void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await DisconnectClient();
                btnConnect.Visibility = Visibility.Visible;
                btnDisconnect.Visibility = Visibility.Hidden;
                clientName.IsEnabled = true;
                clientIp.IsEnabled = true;
                clientPort.IsEnabled = true;
                clientBufferSize.IsEnabled = true;
                btnSend.IsEnabled = false;
                txtMessage.IsEnabled = false;
            }
            catch
            {
                MessageBox.Show("YOU CANNOT DISCONNECT");
            }
        }

        private async Task DisconnectClient()
        {
            string fullMessage = "@DISCONNECT";
            fullMessage += clientName.Text + ": disconnected";

            if (networkStream.CanRead)
            {
                byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(fullMessage);
                await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                tcpClient.Close();
                networkStream.Close();
                Debug.WriteLine("Client send this message - wanting to disconnect!");
            }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SendMessageToServer(clientName.Text, txtMessage.Text, clientBufferSize.Text);
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
            }
        }

        private async Task SendMessageToServer(string name, string message, string buffersize)
        {
            try
            {
                string fullMessage = "@MESSAGE";
                fullMessage += name + ": ";
                networkStream = tcpClient.GetStream();

                if (networkStream.CanRead)
                {
                    fullMessage += message;
                    byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(fullMessage);
                    await networkStream.WriteAsync(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    Debug.WriteLine("Client send this message - while connected");
                }
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
    }
}