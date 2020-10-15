using UnityEngine;
using UnityEngine.UI;
using SYBOClientTest;
using SYBOClientTest.Packets;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Text text;
    [SerializeField]
    private Button connectButton;
    [SerializeField]
    private Button putButton;
    [SerializeField]
    private Button getButton;
    [SerializeField]
    private Button wrongCommandButton;
    [SerializeField]
    private Button disconectButton;


    public static UIManager _instance;
    public static UIManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != null)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        lastConectionState = true;
        Client.Instance.ConectionProtocol = new TCP();
    }

    private bool lastConectionState;
    private void Update()
    {
        if (lastConectionState != Client.Instance.Connected)
        {
            if (Client.Instance.Connected)
            {
                text.text = "Connected";
                connectButton.interactable = false;
                putButton.interactable = true;
                getButton.interactable = true;
                wrongCommandButton.interactable = true;
                disconectButton.interactable = true;
            }
            else
            {
                text.text = "Disconnected";
                connectButton.interactable = true;
                putButton.interactable = false;
                getButton.interactable = false;
                wrongCommandButton.interactable = false;
                disconectButton.interactable = false;
            }
            lastConectionState = Client.Instance.Connected;
        }
    }

    public void Response(string result)
    {
        Debug.Log(result);
    }

    public void ConnectedToServer()
    {
        ButtonsInteractable(false);
        if (Client.Instance.Connected)
        {
            Debug.Log("The client is already connected");
        }
        else
        {
            Client.Instance.ConnectToServer(ConnectionDone);
        }
    }

    public void SendRequestToServer()
    {
        Client.Instance.SendRequest(
            "CHICHA"
            , (string res) => {
                Debug.Log($"<color=#663399>The response was {res}.</color>");
            }
        );
    }

    public void SendBulkRequestToServer()
    {
        for (int i = 0; i < 20; i++)
        {
            int aux = i;
            Client.Instance.SendRequest(
                "CHICHA"
                , (string res) => {
                    Debug.Log($"<color=#663399>The response {aux} was {res}.</color>");
                }
            );
        }
    }


    public void SendWrongRequestToServer()
    {
        Client.Instance.SendRequest(
            "POLLO"
            , (string res) => {
                Debug.Log($"<color=#663399>The response was: {res}.</color>");
            }
        );
    }

    public void DisconectFromServer()
    {
        Client.Instance.DisconnectFromServer();
    }

    private void ConnectionDone()
    {
        if (!Client.Instance.Connected)
        {
            connectButton.interactable = true;
            putButton.interactable = false;
            getButton.interactable = false;
            disconectButton.interactable = false;
        }
        else
        {
            connectButton.interactable = false;
            putButton.interactable = true;
            getButton.interactable = true;
            disconectButton.interactable = true;
        }
    }

    private void ButtonsInteractable(bool state)
    {
        putButton.interactable = state;
        getButton.interactable = state;
        disconectButton.interactable = state;
    }
}