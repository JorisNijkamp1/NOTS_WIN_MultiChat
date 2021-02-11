using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _03_ChatServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Stap 3:
        TcpClient tcpClient;
        NetworkStream networkStream;
        private Boolean serverRunning;

        private List<TcpClient> clientConnectionList = new List<TcpClient>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddMessage(string message)
        {
            this.Dispatcher.Invoke(() => listChats.Items.Add(message));
        }
        
        private async void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!serverRunning)
                {
                    await CreateServerAsync(
                        serverPort.Text,
                        serverBufferSize.Text
                    );
                }
            }
            catch
            {
                MessageBox.Show("Server is not available");
            }
        }

        private async void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serverRunning = false;
                serverName.IsEnabled = true;
                serverPort.IsEnabled = true;
                serverBufferSize.IsEnabled = true;
                btnStop.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Visible;
                btnSend.IsEnabled = true;
            }
            catch
            {
                MessageBox.Show("Server is not available");
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

            TcpListener tcpListener = new TcpListener(IPAddress.Any, Int32.Parse(port));
            tcpListener.Start();
            
            AddMessage($"Listening for clients on port: {port}");

            while (serverRunning)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                clientConnectionList.Add(tcpClient);
                // await Task.Run(() => ReceiveData(tcpClient, Int32.Parse(buffer)));
            }
            tcpListener.Stop();
        }
        
        private void ReceiveData(TcpClient tcpClient, int buffersize)
        {
            string message = "";
            byte[] buffer = new byte[buffersize];

            networkStream = tcpClient.GetStream();
            
            AddMessage("Connected!");
            
            while (true)
            {
                int readBytes = networkStream.Read(buffer, 0, buffersize);
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
    }
}
