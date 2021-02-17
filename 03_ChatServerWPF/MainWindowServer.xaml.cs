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
            Debug.WriteLine("addmessaggeeee");
            // this.Dispatcher.Invoke(() => listChats.Items.Add(message));
            listChats.Items.Add(message);
            // listChats.SelectedIndex = listChats.Items.Count - 1;
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

        private async void stopServerButton_Click(object sender, RoutedEventArgs e)
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
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    
                    Debug.WriteLine( "Someone is connected bruhh" );

                    clientConnectionList.Add(tcpClient);

                    await Task.Run(() => ReceiveData(tcpClient, ParseStringToInt(port)));
                }
            });
        }

        private void ReceiveData(TcpClient tcpClient, int bufferSize)
        {
            Byte[] bytes = new byte[bufferSize];
            networkStream = tcpClient.GetStream();

            while (networkStream.CanRead)
            {
                using (networkStream = tcpClient.GetStream())
                {
                    int length;
                    while ((length = networkStream.Read(bytes, 0, bytes.Length)) != 0) {
                        var incommingData = new byte[length]; 							
                        Array.Copy(bytes, 0, incommingData, 0, length);  							
                        // Convert byte array to string message. 							
                        string clientMessage = Encoding.ASCII.GetString(incommingData); 							
                        Debug.WriteLine("client message received as: " + clientMessage);
                        AddMessage($"{clientMessage} has connected");
                    } 
                }
            }
            
            // string message = "";
            // byte[] buffer = new byte[buffersize];
            //
            // networkStream = tcpClient.GetStream();
            //
            // AddMessage("Connected!");
            //
            // while (networkStream.CanRead)
            // {
            //     int readBytes = networkStream.Read(buffer, 0, buffersize);
            //     message = Encoding.ASCII.GetString(buffer, 0, readBytes);
            //
            //     if (message == "bye")
            //         break;
            //
            //     AddMessage(message);
            // }
            //
            // // Verstuur een reactie naar de client (afsluitend bericht)
            // buffer = Encoding.ASCII.GetBytes("bye");
            // networkStream.Write(buffer, 0, buffer.Length);
            //
            // // cleanup:
            // networkStream.Close();
            // tcpClient.Close();
            //
            // AddMessage("Connection closed");
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