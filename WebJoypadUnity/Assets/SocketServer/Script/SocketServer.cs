using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class SocketServer : MonoBehaviour {

    public string m_ipaddr = "127.0.0.1";
    public int m_port = 8885;
    private Socket m_socketServer;
    private Thread m_thread;

    private List<Thread> m_clientThread = new List<Thread>();
	void Start () {
        InitSocketServer();

    }
	void Update () {
		
	}

    private void OnDestroy()
    {

        Debug.Log("on destroy");
        foreach(var thread in m_clientThread)
        {
            thread.Abort();
        }
        m_clientThread.Clear();

        m_socketServer.Close();

        m_thread.Abort();

        Debug.Log("server close");
    }

    void InitSocketServer()
    {
        IPAddress ip = IPAddress.Parse(m_ipaddr);
        m_socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_socketServer.Bind(new IPEndPoint(ip, m_port));
        m_socketServer.Listen(10);

        Debug.Log("server start");

        m_thread = new Thread(ListenClientConnection);
        m_thread.Start();

        
    }

    void ListenClientConnection()
    {
        while (true)
        {
            Socket clientSocket = m_socketServer.Accept();
            Debug.Log(clientSocket.Connected);
            //clientSocket.Send(Encoding.ASCII.GetBytes("Server say hello"));
            //Thread receiveThread = new Thread(ReceiveMessage);
            //receiveThread.Start(clientSocket);

            clientSocket.Send(Encoding.ASCII.GetBytes("Server say hello"));

            Debug.Log("client connected");


            
        }
    }

    void ReceiveMessage(object clientSocket)
    {
        Socket socket = clientSocket as Socket;
        byte[] data = new byte[1024];
        while (true)
        {
            try
            {
                int receiveNumber = socket.Receive(data);
                Debug.Log(receiveNumber+":"+Encoding.ASCII.GetString(data, 0, receiveNumber));
            }
            catch(Exception e)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                break;
            }
        }

        Debug.Log("client close");
    }

}
