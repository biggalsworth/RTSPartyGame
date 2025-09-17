using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{

    public TextMeshProUGUI Connections;
    public TextMeshProUGUI IPText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(ServerClient.instance.connected)
            IPText.text = MatchSettings.instance.JoinCode;

        if(MatchSettings.instance.hosting)
        {
            Connections.text = ServerHost.instance.GetClientCount() + " players connected";
        }
        else
        {
            Connections.text = "Waiting for game to start";
        }
    }
    public void ReturnToMenu()
    {
        //if (MatchSettings.instance.hosting)
        ServerClient.instance.Disconnect();
        ServerHost.instance.CloseGame();

        MatchSettings.instance.hosting = false;
        
        Destroy(ServerHost.instance.gameObject);

        SceneLoader.instance.BeginScene("MainMenu");
    }

    public void CopyToClipboard(TextMeshProUGUI text)
    {
        GUIUtility.systemCopyBuffer = text.text;
    }

}
