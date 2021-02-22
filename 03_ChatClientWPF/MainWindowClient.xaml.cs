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

                AddMessage("Connected");

                btnConnect.Visibility = Visibility.Hidden;
                btnDisconnect.Visibility = Visibility.Visible;
                clientName.IsEnabled = false;
                clientIp.IsEnabled = false;
                clientPort.IsEnabled = false;
                clientBufferSize.IsEnabled = false;
                btnSend.IsEnabled = true;
                txtMessage.IsEnabled = true;

                await Task.Run(() => updateClientListBox(tcpClient, name));
                // TODO implement Task.Run for Receivedata
            }
            catch (SocketException e)
            {
                MessageBox.Show("Could not create a connection with the server", "Connection error");
            }
        }

        private async Task updateClientListBox(TcpClient tcpClient, string name)
        {
            try
            {
                string connectionMessage = "@CONNECT";
                networkStream = tcpClient.GetStream();
                if (networkStream.CanWrite)
                {
                    connectionMessage += name;
                    byte[] clientMessageByteArray = Encoding.ASCII.GetBytes(connectionMessage);
                    networkStream.Write(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    Debug.WriteLine("Client send this message - connect");
                }
            }
            catch (SocketException socketException)
            {
                MessageBox.Show("Er gaat iets fout.", "Error");
            }
        }

        private void ReceiveData()
        {
            int bufferSize = 1024;
            string message = "";
            byte[] buffer = new byte[bufferSize];

            networkStream = tcpClient.GetStream();

            AddMessage("Connected!");

            while (true)
            {
                int readBytes = networkStream.Read(buffer, 0, bufferSize);
                message = Encoding.ASCII.GetString(buffer, 0, readBytes);

                if (message == "bye")
                    break;

                AddMessage(message);
            }

            // Verstuur een reactie naar de client (afsluitend bericht)
            buffer = Encoding.ASCII.GetBytes("bye");
            networkStream.Write(buffer, 0, buffer.Length);

            // cleanup:
            networkStream.Close();
            tcpClient.Close();

            AddMessage("Connection closed");
        }

        private async void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                networkStream.Close();
                tcpClient.Close();
                btnConnect.Visibility = Visibility.Visible;
                btnDisconnect.Visibility = Visibility.Hidden;
                clientName.IsEnabled = true;
                clientIp.IsEnabled = true;
                clientPort.IsEnabled = true;
                clientBufferSize.IsEnabled = true;
                btnSend.IsEnabled = false;
                txtMessage.IsEnabled = false;
                MessageBox.Show("Disconnect, TODO implement correctly");
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
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
                    networkStream.Write(clientMessageByteArray, 0, clientMessageByteArray.Length);
                    Debug.WriteLine("Client send this message - while connected");
                }

                AddMessage(name + ": " + message);
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
    }
}