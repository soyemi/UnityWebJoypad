using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

public class SocketServer : MonoBehaviour {

    public string m_ipaddr = "192.168.1.3";
    public int m_port = 8885;
    private TcpListener m_server;

    private Thread m_thread;

    void Start()
    {
        m_server = new TcpListener(IPAddress.Parse(m_ipaddr), m_port);
        m_server.Start();

        Debug.Log("server start");


        m_thread = new Thread(ServerThread);
        m_thread.Start();
    }

    private void OnDestroy()
    {
        m_thread.Abort();
    }

    private void ServerThread()
    {
        TcpClient client = m_server.AcceptTcpClient();
        Debug.Log("a client connected");

        NetworkStream stream = client.GetStream();


        bool connected = false;
        while (true)
        {
            if (stream.DataAvailable)
            {
                Byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);

                string data = Encoding.UTF8.GetString(bytes);

                if(new Regex("^GET").IsMatch(data))
                {
                    string respo =
                        "HTTP/1.1 101 Switching Protocols" + Environment.NewLine +
                        "Connection: Upgrade" + Environment.NewLine +
                        "Upgrade: websocket" + Environment.NewLine +
                        "Sec-WebSocket-Accept: " +
                        Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + Environment.NewLine + Environment.NewLine;
                    byte[] response = Encoding.UTF8.GetBytes(respo);
                    stream.Write(response, 0, response.Length);

                    connected = true;
                }
                else
                {
                    if(connected)
                    {
                        var msg = DecodeMsg(bytes, bytes.Length);
                        Debug.Log("<< "+msg);


                        byte[] response = Encoding.UTF8.GetBytes("read:"+msg);
                        stream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        Debug.Log("close client");
                        client.Close();
                        break;
                    }
                }
            }
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
}
