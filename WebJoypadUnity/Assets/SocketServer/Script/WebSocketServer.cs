using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;

public class WebSocketServer {

    private IPAddress m_ipaddr;
    private int m_port;
    private TcpListener m_listener;
    private ClientManager m_clientManager;


    public delegate void OnReceiveCallback(WebSocketServer server,string msg);
    public event OnReceiveCallback eventOnReceive = delegate { };

    public WebSocketServer(string ipaddr,int port)
    {
        m_ipaddr = IPAddress.Parse(ipaddr);
        m_port = port;

        m_listener = new TcpListener(m_ipaddr, m_port);
        m_clientManager = new ClientManager();

    }

    public void Start()
    {
        m_listener.Start();

        Debug.Log("server start");

        AccepterHelper acceptHelper = new AccepterHelper((msg)=> {
            eventOnReceive.Invoke(this, msg);
        });
        IAsyncResult result = m_listener.BeginAcceptSocket(new AsyncCallback(acceptHelper.AcceptClient), this);
    }

    public void Close()
    {
        m_clientManager.Close();
        m_listener.Server.Close();

        Debug.Log("server closed");

    }

    public void SendMessage(string msg)
    {
        m_clientManager.sendMessage(msg);
    }

    private class AccepterHelper
    {
        private Action<string> m_callback;
        public AccepterHelper(Action<string> callback)
        {
            m_callback = callback;
        }

        public void AcceptClient(IAsyncResult ar)
        {

            Debug.Log(ar.AsyncState);

            var socketserver = ar.AsyncState as WebSocketServer;
            var m_listener = socketserver.m_listener;

            Socket client = m_listener.EndAcceptSocket(ar);
            Debug.Log("accept " + client.RemoteEndPoint.ToString());

            ReceiveHelper receivehelper = new ReceiveHelper(m_callback);
            client.BeginReceive(receivehelper.m_buffer, 0, 1024, 0, new AsyncCallback(receivehelper.ReceiveTarget), client);
            socketserver.m_clientManager.AddClient(client);
            m_listener.BeginAcceptSocket(new AsyncCallback(AcceptClient),socketserver);
        }
    }


    private class ReceiveHelper
    {
        private Action<string> m_onreceive = null;
        public ReceiveHelper(Action<string> onreceive)
        {
            m_onreceive = onreceive;
        }

        public byte[] m_buffer = new byte[1024];
        public void ReceiveTarget(IAsyncResult ar)
        {
            Socket client = ar.AsyncState as Socket;
            int read = client.EndReceive(ar);
            Debug.Log("receive:"+read);
            if(read > 0)
            {
                string data = Encoding.UTF8.GetString(m_buffer, 0, read);
                if(new Regex("^GET").IsMatch(data))
                {
                    string respo =
                        "HTTP/1.1 101 Switching Protocols" + Environment.NewLine +
                        "Connection: Upgrade" + Environment.NewLine +
                        "Upgrade: websocket" + Environment.NewLine +
                        "Sec-WebSocket-Accept: " +
                        Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + Environment.NewLine + Environment.NewLine;
                    byte[] response = Encoding.UTF8.GetBytes(respo);

                    client.Send(response);
                }
                else
                {
                    string msg = DecodeMsg(m_buffer, read);
                    Debug.Log(">" + msg);
                    m_onreceive.Invoke(msg);
                }

                client.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveTarget), client);

            }
            else
            {

            }
        }



        public static string DecodeMsg(Byte[] buffer, int len)
        {
            if (buffer[0] != 0x81
                || (buffer[0] & 0x80) != 0x80
                || (buffer[1] & 0x80) != 0x80)
            {
                return null;
            }
            Byte[] mask = new Byte[4];
            int beginIndex = 0;
            int payload_len = buffer[1] & 0x7F;
            if (payload_len == 0x7E)
            {
                Array.Copy(buffer, 4, mask, 0, 4);
                payload_len = payload_len & 0x00000000;
                payload_len = payload_len | buffer[2];
                payload_len = (payload_len << 8) | buffer[3];
                beginIndex = 8;
            }
            else if (payload_len != 0x7F)
            {
                Array.Copy(buffer, 2, mask, 0, 4);
                beginIndex = 6;
            }

            for (int i = 0; i < payload_len; i++)
            {
                buffer[i + beginIndex] = (byte)(buffer[i + beginIndex] ^ mask[i % 4]);
            }
            return Encoding.UTF8.GetString(buffer, beginIndex, payload_len);
        }

        public static byte[] EncodeMsg(string content)
        {
            byte[] bts = null;
            byte[] temp = Encoding.UTF8.GetBytes(content);
            if (temp.Length < 126)
            {
                bts = new byte[temp.Length + 2];
                bts[0] = 0x81;
                bts[1] = (byte)temp.Length;
                Array.Copy(temp, 0, bts, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                bts = new byte[temp.Length + 4];
                bts[0] = 0x81;
                bts[1] = 126;
                bts[2] = (byte)(temp.Length & 0xFF);
                bts[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, bts, 4, temp.Length);
            }
            else
            {
                byte[] st = System.Text.Encoding.UTF8.GetBytes(string.Format("暂不处理超长内容").ToCharArray());
            }
            return bts;
        }
    }

    private class ClientManager
    {
        private List<Socket> m_clients = new List<Socket>();
        public ClientManager()
        {

        }

        public void AddClient(Socket client)
        {
            m_clients.Add(client);
        }

        public void Close()
        {
            foreach(var c in m_clients)
            {
                c.Close();
            }
            m_clients.Clear();
        }

        public void sendMessage(string msg)
        {
            var data = ReceiveHelper.EncodeMsg(msg);
            foreach (var c in m_clients)
            {
                c.Send(data);
            }
        }
    }

    
}
