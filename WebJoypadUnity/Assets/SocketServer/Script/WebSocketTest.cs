using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebSocketTest : MonoBehaviour {

    private WebSocketServer m_Server;
	// Use this for initialization
	void Start () {
        m_Server = new WebSocketServer("0.0.0.0",8885);
        m_Server.Start();
	}

    private void OnDestroy()
    {
        m_Server.Close();
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void OnGUI()
    {
        if (GUILayout.Button("Send"))
        {
            m_Server.SendMessage("server send");
        }
    }
}
