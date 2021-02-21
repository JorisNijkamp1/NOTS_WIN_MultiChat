using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Dispatcher.Invoke((() => listChats.Items.RemoveAt(0)));
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
                await CreateServerAsync(
                    serverPort.Text,
                    serverBufferSize.Text
                );
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
                btnSend.IsEnabled = true;
            }
            catch
            {
                MessageBox.Show("SERVER CANT BE STOPPED");
            }
        }

        private async Task CreateServerAsync(string port, string buffer)
        {
            serverRunning = true;
            serverName.IsEnabled = false;
            serverPort.IsEnabled = false;
            serverBufferSize.IsEnabled = false;
            btnStop.Visibility = Visibility.Visible;
            btnStart.Visibility = Visibility.Hidden;
            btnSend.IsEnabled = true;

            tcpListener = new TcpListener(IPAddress.Any, ParseStringToInt(port));
            tcpListener.Start();

            AddMessage($"Listening for clients on port: {port}");

            Task.Run(async () =>
            {
                while (serverRunning)
                {
                    Debug.WriteLine("Waiting for client");
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    Debug.WriteLine("Someone is connected bruhh");

                    clientConnectionList.Add(tcpClient);

                    await Task.Run(() => ReceiveData(tcpClient, ParseStringToInt(port)));
                }
            });
        }

        private void ReceiveData(TcpClient tcpClient, int bufferSize)
        {
            Byte[] buffer = new byte[bufferSize];
            networkStream = tcpClient.GetStream();

            while (networkStream.CanRead)
            {
                using (networkStream = tcpClient.GetStream())
                {
                    int length;
                    while ((length = networkStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(buffer, 0, incommingData, 0, length);

                        // Convert byte array to string message.						
                        string clientMessage = Encoding.ASCII.GetString(incommingData);
                        
                        Debug.WriteLine("client message received as: " + clientMessage);
                        
                        if (clientMessage.StartsWith("@CONNECT"))
                        {
                            AddMessageToClientList(clientMessage);
                        }
                        else if (clientMessage.StartsWith("@MESSAGE"))
                        {
                            AddMessageToChatBox(clientMessage);
                        }
                    }
                }
            }

            buffer = Encoding.ASCII.GetBytes("bye");
            networkStream.Write(buffer, 0, buffer.Length);

            networkStream.Close();
            tcpClient.Close();
            AddMessage("Connection closed");
        }


        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = txtMessage.Text;

                byte[] buffer = Encoding.ASCII.GetBytes(message);
                networkStream.Write(buffer, 0, buffer.Length);

                AddMessage(message);
                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch
            {
                AddMessage("Message could not be send!");
            }
        }

        private int ParseStringToInt(string input)
        {
            int number;
            int.TryParse(input, out number);
            return number;
        }
    }
}