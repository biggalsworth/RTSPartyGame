using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    PlayerInteractions playerInfo;

    public GameObject WinLosePanel;
    public TextMeshProUGUI WinLose;
    public TextMeshProUGUI playerFunds;

    [Header("Unit Stats")]
    public GameObject statsPanel;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Health;
    public TextMeshProUGUI Movement;

    public GameObject DeleteUnitButton;

    private void Start()
    {
        WinLosePanel.SetActive(false);
        playerInfo = GameObject.FindWithTag("Player").GetComponent<PlayerInteractions>();
    }

    public void Update()
    {
        if (GameObject.FindWithTag("Player") == null)
        {
            if(ServerClient.instance != null)
            {
                WinLosePanel.SetActive(true);
                WinLose.text = "Team " + (ServerClient.instance.GameState - 1) + " has lost!";
            }
            return;
        }

        playerFunds.text = "Team " + playerInfo.team + "\n" + "Funds: " + playerInfo.budget;

        if(playerInfo.selected != null)
        {
            if(!statsPanel.activeSelf)
                statsPanel.SetActive(true);

            if (!DeleteUnitButton.activeSelf)
                DeleteUnitButton.SetActive(true);

            Name.text = playerInfo.selected.GetComponent<UnitClass>().UnitName;
            Health.text = Mathf.Clamp(playerInfo.selected.GetComponent<UnitClass>().health, 0, playerInfo.selected.GetComponent<UnitClass>().maxHealth) + " / " + playerInfo.selected.GetComponent<UnitClass>().maxHealth;
            Movement.text = playerInfo.selected.GetComponent<UnitClass>().movesLeft + " / " + playerInfo.selected.GetComponent<UnitClass>().moveDistance;
        }
        else
        {
            if (statsPanel.activeSelf)
                statsPanel.SetActive(false);

            if (DeleteUnitButton.activeSelf)
                DeleteUnitButton.SetActive(false);
        }


        if(Input.GetKeyDown(KeyCode.Escape) && GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("OpenBuilds"))
        {
            GetComponent<Animator>().Play("CloseBuilds");
        }
    }

    public void DestroyUnit()
    {
        Destroy(playerInfo.selected);
    }

    public void LeaveGame()
    {
        ServerHost.instance.CloseGame();
        ServerClient.instance.Disconnect();
        Destroy(ServerHost.instance.gameObject);
        SceneManager.LoadScene(0);
    }
}
