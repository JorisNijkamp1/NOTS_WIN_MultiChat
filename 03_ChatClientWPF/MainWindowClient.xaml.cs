﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        // Stap 3:
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thread;

        public MainWindowClient()
        {
            InitializeComponent();
        }

        private void AddMessage(string message)
        {
            this.Dispatcher.Invoke(() => listChats.Items.Add(message));
        }
        
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await CreateConnectionAsync(clientName.Text, clientIp.Text, clientPort.Text, clientBufferSize.Text);
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
            }
        }

        private async Task CreateConnectionAsync(string name, string ip, string port, string bufferSize)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(ip, ParseStringToInt(port));
                
                AddMessage("Connected");
                
                clientName.IsEnabled = false;
                clientIp.IsEnabled = false;
                clientPort.IsEnabled = false;
                clientBufferSize.IsEnabled = false;
                btnSend.IsEnabled = true;
                txtMessage.IsEnabled = true;
                //TODO 2 lines below execute when connect proberly
                btnConnect.Visibility = Visibility.Hidden;
                btnDisconnect.Visibility = Visibility.Visible;
            }
            catch (SocketException e)
            {
                MessageBox.Show("Could not create a connection with the server", "Connection error");
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
                MessageBox.Show("TODO implement Disconnect");
            }
            catch
            {
                MessageBox.Show("YOU CANNOT CONNECT");
            }
        }
        
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = txtMessage.Text;

            byte[] buffer = Encoding.ASCII.GetBytes(message);
            networkStream.Write(buffer, 0, buffer.Length);

            AddMessage(message);
            txtMessage.Clear();
            txtMessage.Focus();
        }
        
        private int ParseStringToInt(string input)
        {
            int number;
            int.TryParse(input, out number);
            return number;
        }
    }
}