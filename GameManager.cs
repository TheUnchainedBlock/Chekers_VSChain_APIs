using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;
using System;
using TMPro;

public class GameManager : MonoBehaviour 
{
	public static GameManager Instance { set; get; }

    public GameObject unconnected;
    public GameObject connected,rules,rules2;

    public GameObject mainMenu;
    public GameObject hostMenu;
    public GameObject connectMenu;

    public string gameaddress;



    public Button connect;
    public Button createWallet;
    public Button logout;
    public Button getTransactions;

    public InputField nameInput,addressbox,secretbox,addressbox2,credit,date,lastupdate,betinput,ruleinput;

    public TextMeshProUGUI t1, t2, t3, t4, t5,myip;

	public GameObject serverPrefab;
	public GameObject clientPrefab;

    public string vsCoin, bornOnDate, lastUpdate, address, secret,bet,rule,opponent;


    private void Start () 
	{
		Instance = this;
        connected.SetActive(false);
        unconnected.SetActive(true);
        rules.SetActive(false);
        hostMenu.SetActive(false);
        connectMenu.SetActive(false);
        mainMenu.SetActive(false);
        rules2.SetActive(false);

        gameaddress = "123";


        connect.onClick.AddListener(connectnow);

        createWallet.onClick.AddListener(creatwalletnow);

        logout.onClick.AddListener(logoutnow);

        //btn.onClick.AddListener(TaskOnClick);

        //API CALL
        StartCoroutine(postRequest("https://test.ws.vschain.net/wallet/", "{}",true));
        
        DontDestroyOnLoad (gameObject);
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
    public IEnumerator postRequest(string url, string json, Boolean wallet)
    {
        string type = "POST";
        if (!wallet)
            type = "PUT";
        var uwr = new UnityWebRequest(url, type);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        if (!wallet) {
            string authorization = authenticate(address, secret);
            uwr.SetRequestHeader("Authorization", authorization);
        }
        
        
        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            if (wallet) {
                callApi(uwr);
            }
            else
            {
                diffrentiate(uwr);
            }
            
        }
    }

    public void diffrentiate(UnityWebRequest uwr)
    {
        var data = JSON.Parse(uwr.downloadHandler.text);
        string one= data["data"]["players"][0]["address"].Value;
        string two = data["data"]["players"][1]["address"].Value;
        if (one.Equals(address))
        {
            opponent = two;
        }
        else
        {
            opponent = one;
        }

        Debug.Log("one:"+ address+" two: "+ opponent);

    }

    public void callApi(UnityWebRequest uwr)
    {
        var data = JSON.Parse(uwr.downloadHandler.text);

        vsCoin = data["data"]["vsCoin"].Value;
        bornOnDate = convertdate(data["data"]["bornOnDate"].Value);
        lastUpdate = convertdate(data["data"]["lastUpdate"].Value);
        address = data["data"]["address"].Value;
        secret = data["data"]["secret"].Value;

        
        
        addressbox.text = address;
        secretbox.text = secret;
        Debug.Log(address);

    }

    public string convertdate(string mili)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(Double.Parse(mili));

        var totalDays = ts.Days;
        var totalYears = Math.Truncate(totalDays / 365.2422);
        var totalMonths = Math.Truncate((totalDays % 365.2422) / 30);
        var remainingDays = Math.Truncate((totalDays % 365.2422) % 30);

        return ((remainingDays + 1).ToString() + "-" + (totalMonths + 1).ToString() +  "-" + (totalYears + 1970).ToString() + " " + ts.Hours.ToString() + ":" + ts.Minutes.ToString() + ":" + ts.Seconds.ToString());
        
    }
    public void ConnectButton()
	{
        mainMenu.SetActive(false);
        connectMenu.SetActive(true);
        connected.SetActive(false);
    }

	public void HostServerButton()
	{
		string hostAddress = "127.0.0.1";
        bet = betinput.text;
        rule = ruleinput.text;

		try
		{
			Server s = Instantiate(serverPrefab).GetComponent<Server>();
			s.Init();

			Client c = Instantiate(clientPrefab).GetComponent<Client>();
			c.clientName = nameInput.text;
			c.isHost = true;
			if (c.clientName == "")
				c.clientName = "Host";
			c.ConnectToServer(hostAddress, 6321); 
		}
		catch (System.Exception e)
		{
			Debug.Log (e.Message);
		}

        mainMenu.SetActive(false);
        connectMenu.SetActive(false);
        hostMenu.SetActive(true);
        connected.SetActive(false);
        rules.SetActive(false);

    }
	public void ConnectToServerButton()
	{
        //mainMenu.SetActive(true);
        //connectMenu.SetActive(false);
        //hostMenu.SetActive(false);
        //connected.SetActive(true);

        string hostAddress = GameObject.Find("HostInput").GetComponent<InputField>().text;
		if (hostAddress == "")
			hostAddress = "127.0.0.1";
		
		try
		{
			Client c = Instantiate(clientPrefab).GetComponent<Client>();
			c.clientName = nameInput.text;
			if (c.clientName == "")
				c.clientName = "Client";
			c.ConnectToServer(hostAddress, 6321); 
			
		}
		catch (System.Exception e)
		{
			Debug.Log(e.Message);
		}
	}
	public void BackButton()
	{
        mainMenu.SetActive(true);
        connectMenu.SetActive(false);
        hostMenu.SetActive(false);
        connected.SetActive(true);
        rules.SetActive(false);
        rules2.SetActive(false);


        Server s = FindObjectOfType<Server>();
		if (s != null)
			Destroy(s.gameObject);
	
		Client c = FindObjectOfType<Client>();
		if (c != null)
			Destroy(c.gameObject);
	}

    public void connectnow()
    {
        Debug.Log("You have clicked the button!");
        connected.SetActive(true);
        unconnected.SetActive(false);
        rules.SetActive(false);
        mainMenu.SetActive(true);

        addressbox2.text = address;
        credit.text = vsCoin;
        date.text = bornOnDate;
        lastupdate.text = lastUpdate;
    }

    public void logoutnow()
    {
        
        connected.SetActive(false);
        unconnected.SetActive(true);
        rules.SetActive(false);

    }

    public void creatwalletnow()
    {

        StartCoroutine(postRequest("https://ws.vschain.net/wallet/", "{}",true));
    }

    
    public void showBetRules()
    {
        mainMenu.SetActive(false);
        connectMenu.SetActive(false);
        hostMenu.SetActive(false);
        connected.SetActive(false);
        rules.SetActive(true);
    }
    public void showBetRules2()
    {
        mainMenu.SetActive(false);
        connectMenu.SetActive(false);
        hostMenu.SetActive(false);
        connected.SetActive(false);
        rules.SetActive(false);
        rules2.SetActive(true);
    }

    public void StartGame()
	{
		SceneManager.LoadScene ("Game");
	}

    public void StartGamewithconfirmation()
    {
        
        StartCoroutine(postRequest("https://test.ws.vschain.net/games/"+ gameaddress+"/", "{\"confirmed\":true}", false));
        SceneManager.LoadScene("Game");
    }
}
