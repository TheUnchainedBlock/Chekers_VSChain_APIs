using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

public class CheckerBoard : MonoBehaviour 
{
	public static CheckerBoard Instance {set; get;}
	public Piece[,] pieces = new Piece[8, 8];
	public GameObject whitePiecePrefab;
	public GameObject blackPiecePrefab;

	public Transform chatMessageContainer;
	public GameObject messagePrefab;

	public GameObject highlightContainer;
	
	public CanvasGroup alertCanvas;
	private float lastAlert;
	private bool alertActive;
	private bool gameIsOver;
	private float winTime;
	

	private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
	private Vector3 pieceOffset = new Vector3(0.5f, 0.125f, 0.5f);

	public bool isWhite;
	private bool isWhiteTurn;
	private bool hasKilled;

	private Piece selectedPiece;
	private List<Piece> forcedPieces;

	private Vector2 mouseOver;
	private Vector2 startDrag;
	private Vector2 endDrag;

	private Client client;

	private void Start()
	{
		Instance = this;
		client = FindObjectOfType<Client>();

		foreach (Transform t in highlightContainer.transform)
		{
			t.position = Vector3.down * 100;
		}

		if (client)
		{
			isWhite = client.isHost;
			Alert(client.players[0].name + " versus " + client.players[1].name);
		}
		else
		{
			isWhite = true;
			Alert("White Player's turn");
			Transform c = GameObject.Find("Canvas").transform;
			foreach (Transform t in c)
			{
				t.gameObject.SetActive(false);
			}

			c.GetChild(0).gameObject.SetActive(true);
		}

		isWhiteTurn = true;
		GenerateBoard();
		forcedPieces = new List<Piece>();
	}

	private void Update()
	{
		if (gameIsOver) 
		{
			if (Time.time - winTime > 3.0f)
			{
				Server server = GameObject.FindObjectOfType<Server>();
				Client client = GameObject.FindObjectOfType<Client>();

                if (server)
                {
                    
                    if (isWhite)
                    {
                        StartCoroutine(postRequest("https://test.ws.vschain.net/games/"+GameManager.Instance.gameaddress, "{\"score\":[\""+ GameManager.Instance.address + "\",\""+ GameManager.Instance.opponent + "\"]}"));
                    }
                    else
                    {
                        StartCoroutine(postRequest("https://test.ws.vschain.net/games/" + GameManager.Instance.gameaddress, "{\"score\":[\"" + GameManager.Instance.opponent + "\",\"" + GameManager.Instance.address + "\"]}"));

                    }
                    Destroy(server.gameObject);
                }

                if (client)
                {
                    if (!isWhite)
                    {
                        //API Call
                        StartCoroutine(postRequest("https://test.ws.vschain.net/games/" + GameManager.Instance.gameaddress, "{\"score\":[\"" + GameManager.Instance.address + "\",\"" + GameManager.Instance.opponent + "\"]}"));
                    }
                    else
                    {
                        //API Call
                        StartCoroutine(postRequest("https://test.ws.vschain.net/games/" + GameManager.Instance.gameaddress, "{\"score\":[\"" + GameManager.Instance.opponent + "\",\"" + GameManager.Instance.address + "\"]}"));

                    }
                    Destroy(client.gameObject);
                }

				SceneManager.LoadScene("Menu");
			}

			return;
		}

		foreach (Transform t in highlightContainer.transform)
		{
			t.Rotate(Vector3.up * 90 * Time.deltaTime);
		}

		UpdateAlert();
		UpdateMouseOver();

		if ((isWhite)? isWhiteTurn : !isWhiteTurn)
		{
			int x = (int)mouseOver.x;
			int y = (int)mouseOver.y;
		
			if(selectedPiece != null)
				UpdatePieceDrag(selectedPiece);

			if(Input.GetMouseButtonDown(0))
				SelectPiece(x, y);

			if(Input.GetMouseButtonUp(0))
				TryMove((int)startDrag.x, (int)startDrag.y, x, y);
			
		}
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
        string type = "PUT";
        
        var uwr = new UnityWebRequest(url, type);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        string authorization = authenticate(GameManager.Instance.address, GameManager.Instance.secret);
        uwr.SetRequestHeader("Authorization", authorization);


        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            

        }
    }

    private void UpdateMouseOver()
	{
		if(!Camera.main)
		{
			Debug.Log("Unable to find main camera");
			return;
		}

		RaycastHit hit;
		if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
		{
			mouseOver.x = (int)(hit.point.x - boardOffset.x);
			mouseOver.y = (int)(hit.point.z - boardOffset.z);
		}
		else
		{
			mouseOver.x = -1;
			mouseOver.y = -1;
		}
	}

    private void UpdatePieceDrag(Piece p)
	{
		if(!Camera.main)
		{
			Debug.Log("Unable to find main camera");
			return;
		}

		RaycastHit hit;
		if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
		{
			p.transform.position = hit.point + Vector3.up;
		}
	}

	private void SelectPiece(int x, int y)
	{
		// Out of bounds
		if (x < 0 || x >= 8 || y < 0 || y >= 8)
			return;

		Piece p = pieces[x, y];
		if (p != null && p.isWhite == isWhite)
		{
			if (forcedPieces.Count == 0)
			{
				selectedPiece = p;
				startDrag = mouseOver;
			}
			else
			{
				// Look for the piece under our forcedPieces list
				if (forcedPieces.Find(fp => fp == p) == null)
					return;
					
				selectedPiece = p;
				startDrag = mouseOver;
			}
		}
	}

	public void TryMove(int startX, int startY, int endX, int endY)
	{
		forcedPieces = ScanForPossibleMove();

		// Multiplayer Support
		startDrag = new Vector2(startX, startY);
		endDrag = new Vector2(endX, endY);
		selectedPiece = pieces[startX, startY];
		
		// Out of bounds
		if(endX < 0 || endX >= 8 || endY < 0 || endY >= 8)
		{
			if(selectedPiece != null)
				MovePiece(selectedPiece, startX, startY);

			startDrag = Vector2.zero;
			selectedPiece = null;
			Highlight();
			return;
		}

		if(selectedPiece != null)
		{
			// If it has not moved
			if(endDrag == startDrag)
			{
				MovePiece(selectedPiece, startX, startY);
				startDrag = Vector2.zero;
				selectedPiece = null;
				Highlight();
				return;
			}

			// Check if it is a valid move
			if (selectedPiece.ValidMove(pieces, startX, startY, endX, endY))
			{
				// Did we kill anything?
				// If it is a jump
				if (Mathf.Abs(endX - startX) == 2)
				{
					Piece p = pieces[(startX + endX) / 2, (startY + endY) / 2];
					if (p != null)
					{
						pieces[(startX + endX) / 2, (startY + endY) / 2] = null;
						DestroyImmediate(p.gameObject);
						hasKilled = true;
					}
				}

				// Were we supposed to kill anything?
				if (forcedPieces.Count != 0 && !hasKilled)
				{
					MovePiece(selectedPiece, startX, startY);
					startDrag = Vector2.zero;
					selectedPiece = null;
					Highlight();
					return;		
				}

				pieces[endX, endY] = selectedPiece;
				pieces[startX, startY] = null;
				MovePiece(selectedPiece, endX, endY);

				EndTurn();
			}
			else
			{
				MovePiece(selectedPiece, startX, startY);
				startDrag = Vector2.zero;
				selectedPiece = null;
				Highlight();
				return;	
			}
		}

	}

	private void EndTurn()
	{
		int x = (int)endDrag.x;
		int y = (int)endDrag.y;
		
		// Promotions
		if (selectedPiece != null)
		{
			if (selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
			{
				selectedPiece.isKing = true;
				selectedPiece.GetComponentInChildren<Animator>().SetTrigger("FlipTrigger");
			}
			else if (!selectedPiece.isWhite && !selectedPiece.isKing && y == 0)
			{
				selectedPiece.isKing = true;
				selectedPiece.GetComponentInChildren<Animator>().SetTrigger("FlipTrigger");
			}
		}
		
		if (client) {
			string msg = "CMOV|";
			msg += startDrag.x.ToString() + "|";
			msg += startDrag.y.ToString() + "|";
			msg += endDrag.x.ToString() + "|";
			msg += endDrag.y.ToString();

			client.Send(msg);
		}

		selectedPiece = null;
		startDrag = Vector2.zero;

		if (ScanForPossibleMove(selectedPiece, x, y).Count != 0 && hasKilled)
			return;
		
		isWhiteTurn = !isWhiteTurn;
		
		if (!client) isWhite = !isWhite;
		
		hasKilled = false;
		CheckVictory();

		ScanForPossibleMove();
	}

	private void CheckVictory()
	{
		var ps = FindObjectsOfType<Piece>();
		bool hasWhite = false, hasBlack = false;
		for (int i = 0; i < ps.Length; i++)
		{
			if (ps[i].isWhite)
				hasWhite = true;
			else
				hasBlack = true;
		}

		if (!hasWhite)
			Victory(false);
		if (!hasBlack)
			Victory(true);
	}
	
	private void Victory (bool isWhite)
	{
		winTime = Time.time;

		if (isWhite)
			Alert("White team has won");
		else
			Alert("Black team has won");

		gameIsOver = true;
	}

	private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
	{
		forcedPieces = new List<Piece>();

		if (pieces[x, y].IsForceToMove(pieces, x, y))
			forcedPieces.Add(pieces[x, y]);

		Highlight();
		return forcedPieces;
	}
	private List<Piece> ScanForPossibleMove()
	{
		forcedPieces = new List<Piece>();

		// Check all the pieces
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				if (pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn)
					if(pieces[i, j].IsForceToMove(pieces, i, j))
						forcedPieces.Add(pieces[i, j]);
			}
		}

		Highlight();
		return forcedPieces;
	}

	private void Highlight()
	{
		foreach (Transform t in highlightContainer.transform)
		{
			t.position = Vector3.down * 100;
		}

		if(forcedPieces.Count > 0)
			highlightContainer.transform.GetChild(0).transform.position = forcedPieces[0].transform.position + Vector3.down * 0.1f;
		
		if(forcedPieces.Count > 1)
			highlightContainer.transform.GetChild(1).transform.position = forcedPieces[1].transform.position + Vector3.down * 0.1f;
	}

	private void GenerateBoard()
	{
		// Generate White team
		for (int y = 0; y < 3; y++)
		{
			bool oddRow = (y % 2 == 0);
			for (int x = 0; x < 8; x+=2)
			{
				// Generate our Piece
				GeneratePiece((oddRow) ? x : x + 1 , y);
			}
		}

		// Generate Black team
		for (int y = 7; y > 4; y--)
		{
			bool oddRow = (y % 2 == 0);
			for (int x = 0; x < 8; x+=2)
			{
				// Generate our Piece
				GeneratePiece((oddRow) ? x : x + 1 , y);
			}
		}
	}

	private void GeneratePiece(int x, int y)
	{
		bool isWhitePiece = (y > 3) ? false : true;
		GameObject go = Instantiate((isWhitePiece) ? whitePiecePrefab : blackPiecePrefab) as GameObject;
		go.transform.SetParent(transform);
		Piece p = go.GetComponent<Piece>();
		pieces[x, y] = p;
		MovePiece(p, x, y);
	}

	private void MovePiece(Piece p, int x, int y)
	{
		p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
	}

	public void Alert(string text)
	{
		alertCanvas.GetComponentInChildren<Text>().text = text;
		alertCanvas.alpha = 1;
		lastAlert = Time.time;
		alertActive = true;
	}

	public void UpdateAlert()
	{
		if (alertActive)
		{
			if (Time.time - lastAlert > 1.5f)
			{
				alertCanvas.alpha = 1 - ((Time.time - lastAlert) - 1.5f);

				if(Time.time - lastAlert > 2.5f)
				{
					alertActive = false;
				}
			}
		}
	}

	public void ChatMessage(string msg)
    {
		GameObject go = Instantiate(messagePrefab) as GameObject;
		go.transform.SetParent(chatMessageContainer);

		go.GetComponentInChildren<Text>().text = msg;
    }

	public void SendChatMessage()
	{
		InputField i = GameObject.Find("MessageInput").GetComponent<InputField>();

		if (i.text == "") return;

		client.Send("CMSG|" + i.text);

		i.text = "";
	}
}
