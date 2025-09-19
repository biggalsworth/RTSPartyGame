using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    PlayerInteractions playerInfo;

    [Header("Game Info")]
    public GameObject BuildingControls;
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
        if (GameObject.FindWithTag("Player") == null || GameObject.FindWithTag("Player").activeSelf == false)
        {
            if(ServerClient.instance != null)
            {
                WinLosePanel.SetActive(true);
                //WinLose.text = "Team " + (ServerClient.instance.GameState - 1) + " has lost!";
                WinLose.text = "Team " + (ServerClient.instance.GameState) + " has lost!";
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

        BuildingControls.SetActive(playerInfo.currBuild != null);


        if (Input.GetKeyDown(KeyCode.Escape) && GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("OpenBuilds"))
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
        StartCoroutine(Leaving());
    }

    IEnumerator Leaving()
    {
        ServerClient.instance.Disconnect();

        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        ServerHost.instance.CloseGame();

        yield return new WaitForSeconds(0.2f);

        Destroy(ServerHost.instance.gameObject);
        SceneManager.LoadScene(0);
    }
}
