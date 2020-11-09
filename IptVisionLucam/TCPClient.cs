using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Ipt;

namespace IptVision
{
    class TCPClient
    {
        public delegate void SendMessageNotify(object sender, SendMessageArgs e);
        public event SendMessageNotify SendMessage;

        string m_cmd = "";
        public TCPClient()
        {
        }
        public void Send(String cmd)
        {
            m_cmd = cmd;
            Thread t = new Thread(new ThreadStart(ClientThreadStart));
            t.Start();
        }
        private void ClientThreadStart()
        {
            try
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //                clientSocket.Connect(new IPEndPoint(IPAddress.Parse("192.168.10.201"), 19450));
                clientSocket.Connect(new IPEndPoint(IPAddress.Parse("192.168.10.201"), 19450));
  //              clientSocket.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.1"), 19450));
                //            clientSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 19450));
                clientSocket.Send(Encoding.UTF8.GetBytes(m_cmd));
                switch (m_cmd.ElementAt(0))
                {
                    case '?':
                        byte[] buffer = new byte[1024];
                        clientSocket.Receive(buffer);
                        SendMessage(this, new SendMessageArgs(Encoding.UTF8.GetString(buffer)));
                        break;
                }
                clientSocket.Close();
            }
            catch(Exception ex)
            {
                SendMessage(this, new SendMessageArgs(ex.Message));
            }
        }
    }
}
