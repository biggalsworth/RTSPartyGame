using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{

    public GameObject MainMenu;

    public TextMeshProUGUI WarningText;

    public TMP_Dropdown size;

    public TMP_Dropdown mount;
    public TMP_Dropdown hill;
    public TMP_Dropdown water;

    public TextMeshProUGUI IPText;
    
    GameObject ActivePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        MainMenu.SetActive(true);
        ActivePanel = MainMenu;
    }

    // Update is called once per frame
    private void Update()
    {
        if(IPText != null)
        {
            IPText.text = "IP: " + ServerClient.instance.joinCode;
        }
    }

    public void SwitchPanel(GameObject Panel)
    {
        ActivePanel.SetActive(false);
        Panel.SetActive(true);
        ActivePanel = Panel;
    }


    public void ShowMainMenu()
    {
        ActivePanel.SetActive(false);
        MainMenu.SetActive(true);
        ActivePanel = MainMenu;
    }

    public void LeaveGame()
    {
        Application.Quit();
    }

    public void ReturnToMenu()
    {
        MatchSettings.instance.hosting = false;

        //Destroy(ServerHost.instance.gameObject);

        SceneLoader.instance.BeginScene("MainMenu");
    }

    public void JoinGame()
    {
        MatchSettings.instance.JoinCode = GameObject.Find("JoinCodeInput").GetComponent<TMP_InputField>().text;
        MatchSettings.instance.hosting = false;
        SceneLoader.instance.BeginScene("Lobby");

        if (ServerClient.instance)
            ServerClient.instance.canConnect = true;
    }

    public void CreateGame()
    {
        MatchSettings.instance.hosting = true;

        //if (ServerClient.instance)
        //    ServerClient.instance.canConnect = true;
        //
        //if (ServerHost.instance)
        //    ServerHost.instance.canConnect = true;

        SceneLoader.instance.BeginScene("Lobby");
    }


    //Match settings checks
    public void ValidateSettings()
    {
        WarningText.text = "";
        bool valid = true;


        valid = CheckBudget(GameObject.Find("BudgetInputField").GetComponent<TMP_InputField>().text);
        if (MatchSettings.instance.IsDigitsOnly(GameObject.Find("SeedInputField").GetComponent<TMP_InputField>().text) == false)
        {
            WarningText.text = "SEED IS INVALID";
            valid = false;
        }

        if (valid)
        {
            MatchSettings.instance.MapSeed = int.Parse(GameObject.Find("SeedInputField").GetComponent<TMP_InputField>().text);
            //MatchSettings.instance.JoinCode = GameObject.Find("JoinCodeInput").GetComponent<TMP_InputField>().text;
        }

        if (size && mount && hill && water)
        {
            MatchSettings.instance.SetSize(size.value);
            MatchSettings.instance.SetMountains(mount.value);
            MatchSettings.instance.SetHills(hill.value);
            MatchSettings.instance.SetWater(water.value);
        }

        MatchSettings.instance.hosting = true;

    }

    public bool CheckBudget(string value)
    {
        if (MatchSettings.instance.IsDigitsOnly(value) == false)
        {
            WarningText.text = "FIELD IS INVALID";
            return false;
        }
        else
        {
            MatchSettings.instance.SetBudget(value);
            return true;
        }
    }
}
