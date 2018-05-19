using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;

public class Client : MonoBehaviour 
{
	public string clientName,clientwallet;
	public bool isHost,started;

	private bool socketReady;
	private TcpClient socket;
	private NetworkStream stream;
	private StreamWriter writer;
	private StreamReader reader;

	public List<GameClient> players = new List<GameClient>();
    public List<string> addresses = new List<string>();

    private void Start()
	{
		DontDestroyOnLoad(gameObject);
        started = false;
	}

	public bool ConnectToServer (string host, int port)
	{
		if (socketReady) return false;

		try
		{
			socket = new TcpClient(host, port);
			stream = socket.GetStream();
			writer = new StreamWriter(stream);
			reader = new StreamReader(stream);
			
			socketReady = true;

            
		}
		catch (Exception e)
		{
			Debug.LogError ("Socket error " + e.Message);

		}

		return socketReady;
	}

	private void Update()
	{
		if (socketReady)
		{
			if (stream.DataAvailable)
			{
				string data = reader.ReadLine();
				if (data != null)
					OnIncomingData(data);
			}
		}
	}

	// Send messages to the server
	public void Send (string data)
	{
		if (!socketReady) return;

		writer.WriteLine(data);
		writer.Flush();
	}

	// Read messages from the server
	private void OnIncomingData (string data)
	{
		Debug.Log ("Client: " + data);
		string[] aData = data.Split('|');

		switch (aData[0])
		{
			case "SWHO":
			 	for (int i = 1; i < aData.Length - 1; i++)
				 {
					 UserConnected(aData[i], false);
				 }
				 Send("CWHO|" + clientName +'-'+GameManager.Instance.address+ "|" + ((isHost) ? 1 : 0).ToString());
				break;

			case "SCNN":
				UserConnected(aData[1], false);
				break;

			case "SMOV":
				CheckerBoard.Instance.TryMove(int.Parse(aData[1]), int.Parse(aData[2]), int.Parse(aData[3]), int.Parse(aData[4]));
				break;

			case "SMSG":
				CheckerBoard.Instance.ChatMessage(aData[1]);
				break;
            case "start":
                if (!started)
                {
                    GameManager.Instance.t2.text = "Game ID: " + aData[1];
                    GameManager.Instance.t3.text = "Game Bet: " + aData[2];
                    GameManager.Instance.t4.text = "Collected: " + aData[3];
                    GameManager.Instance.t5.text = "Creation Date:" + GameManager.Instance.convertdate(aData[4]);
                    GameManager.Instance.gameaddress = aData[1];
                    GameManager.Instance.showBetRules2();
                    started = true;
                }
                break;
        }
	}

	private void UserConnected(string name, bool host)
	{
		GameClient gc = new GameClient();
		gc.name = name;

		players.Add(gc);
        Debug.Log(GameManager.Instance.address);
        addresses.Add(GameManager.Instance.address);


		if (players.Count == 2) {
            Send("start|");
            
            //GameManager.Instance.StartGame();
        }
			
	}

	private void OnApplicationQuit()
	{
		CloseSocket();
	}
	private void OnDisable()
	{
		CloseSocket();
	}
	private void CloseSocket()
	{
		if (!socketReady) return;

		writer.Close();
		reader.Close();
		socket.Close();
		socketReady = false;
	}

}

public class GameClient
{
	public string name;
	public bool isHost;
}
