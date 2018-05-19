using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;
using System.Text;

public class Server : MonoBehaviour 
{
	public int port = 6321;

	private List<ServerClient> clients;
	private List<ServerClient> disconnectList;

	private TcpListener server; // need to include System.Net.Sockets namespace to use TcpListener and TcpClient
	private bool serverStarted;

    string datetime_creation, lastUpdate, owner, state, collected, id;
    private Boolean started;




    public void Init()
	{
		DontDestroyOnLoad (gameObject); // don't want to destroy server when changing scene
		clients = new List<ServerClient>();
		disconnectList = new List<ServerClient>();
        started = false;

		try
		{
            string addr = LocalIPAddress();
            GameManager.Instance.myip.text = addr;
            IPAddress localAddr = IPAddress.Parse(addr);
            server = new TcpListener(IPAddress.Any, port); // need to include System.Net namespace to use IPAddress
			server.Start();

			StartListening();
			serverStarted = true;

		}
		catch (Exception e) // need to include System namespace to use Exception
		{
			Debug.Log ("Socket error: " + e.Message);
		}
	}
	private void Update()
	{
		if (!serverStarted) return;

		foreach (ServerClient c in clients)
		{
			// Is the clients still connected?
			if (!IsConnected(c.tcp))
			{
				c.tcp.Close();
				disconnectList.Add(c);
				continue;
			}
			else 
			{
				NetworkStream s = c.tcp.GetStream();
				if (s.DataAvailable)
				{
					StreamReader reader = new StreamReader(s, true); // need to include System.IO namespace to use StreamReader
					string data = reader.ReadLine();

					if (data != null)
						OnIncomingData(c, data);

				}
			}
		} // foreach

		for (int i = 0; i < disconnectList.Count - 1; i++)
		{
			// Tell our player somebody has disconnected

			clients.Remove(disconnectList[i]);
			disconnectList.RemoveAt(i);
		}
	}
    public string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    private void StartListening()
	{
		server.BeginAcceptTcpClient(AcceptTcpClient, server);
	}
	private void AcceptTcpClient(IAsyncResult ar)
	{
		TcpListener listener = (TcpListener)ar.AsyncState;

		string allUsers = "";
		foreach (ServerClient c in clients)
		{
			allUsers += c.clientName+ '|'; 
		}

		ServerClient sc = new ServerClient (listener.EndAcceptTcpClient(ar));
        
		clients.Add(sc);

		StartListening();
        //Broadcast("SWWA|"+GameManager.Instance.address, clients[clients.Count - 1]);
        Broadcast("SWHO|" + allUsers, clients[clients.Count - 1]);
	}

	private bool IsConnected (TcpClient c)
	{
		try
		{
			if (c != null && c.Client != null && c.Client.Connected)
			{
				if (c.Client.Poll(0, SelectMode.SelectRead))
					return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

				return true;
			}
			else
				return false;
		}
		catch
		{
			return false;
		}
	}

	/// Send to Server
	private void Broadcast (string data, List<ServerClient> cl)
	{
		foreach (ServerClient sc in cl)
		{
			try
			{
				StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
				writer.WriteLine (data);
				writer.Flush();
			}
			catch (Exception e)
			{
				Debug.Log ("Write error : " + e.Message);
			}
		} // foreach
	}
	private void Broadcast (string data, ServerClient c)
	{
		List<ServerClient> sc = new List<ServerClient> { c };
		Broadcast(data, sc);
	}

	/// Read from Server
	private void OnIncomingData(ServerClient c, string data)
	{		
		Debug.Log ("Server: " + data);
		string[] aData = data.Split('|');

		switch (aData[0])
		{
			case "CWHO":
                string[] bData = aData[1].ToString().Split('-');
                c.clientName = bData[0];
                c.address= bData[1];
                c.isHost = (aData[2] == "0") ? false : true;
				Broadcast("SCNN|" + c.clientName, clients);
				break;

			case "CMOV":
				data = data.Replace('C', 'S');
				Broadcast(data, clients);
				break;

            case "CMSG":
				Broadcast("SMSG|" + c.clientName + " : " + aData[1], clients);
				break;

            case "start":
                if (!started) {
                    
                    createGame(clients[0].address, clients[1].address, GameManager.Instance.bet, GameManager.Instance.rule);
                    GameManager.Instance.showBetRules2();
                    started = true;
                    
                }
                break;

        }
	}


    public void createGame(string hostaddress, string clientaddress, string bet, string rules)
    {
        string jsondata = "{\n\t\"players\":[\n\t\t\t\""+ hostaddress + "\",\n\t\t\t\""+ clientaddress + "\"\n\t\t],\n\t\"rules\":[" + rules + "],\n\t\"bet\":"+bet+"\n}";


        
        Debug.Log(jsondata);

        //API Call
        StartCoroutine(postRequest("https://test.ws.vschain.net/games/", jsondata));
        

    }
    //API Function
    string authenticate(string username, string password)
    {

        string auth = username + ":" + password;
        auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;
        Debug.Log(auth);
        return auth;
    }



    //API Function

    public IEnumerator postRequest(string url, string json)
    {
        Debug.Log(GameManager.Instance.address);
        Debug.Log(GameManager.Instance.secret);

        string authorization = authenticate(GameManager.Instance.address, GameManager.Instance.secret);


        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Authorization", authorization);
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Cache-Control", "no-cache");




        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);

            callApi(uwr);
        }
    }
    //API Function
    public void callApi(UnityWebRequest uwr)
    {
        var data = JSON.Parse(uwr.downloadHandler.text);
        Debug.Log(data);

        datetime_creation = data["data"]["datetime_creation"].Value;
        lastUpdate = data["data"]["lastUpdate"].Value;
        owner = data["data"]["owner"].Value;
        state = data["data"]["state"].Value;
        collected = data["data"]["collected"].Value;
        id = data["data"]["id"].Value;

        GameManager.Instance.t2.text = "Game ID: "+id;
        GameManager.Instance.t3.text = "Game Bet: "+GameManager.Instance.bet;
        GameManager.Instance.t4.text = "Collected: "+collected;
        GameManager.Instance.t5.text = "Creation Date:"+GameManager.Instance.convertdate(datetime_creation);
        GameManager.Instance.gameaddress = id;
        Broadcast("start|" + id + "|" + GameManager.Instance.bet + "|" + collected + "|" + datetime_creation, clients);


    }
    
}



public class ServerClient
{
	public string clientName;
    public string address;
	public TcpClient tcp;
	public bool isHost;

	public ServerClient (TcpClient tcp)
	{
		this.tcp = tcp;
	}
}
