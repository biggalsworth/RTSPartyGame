using UnityEngine;

public class BaseLogic : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.Log("Team " + GetComponent<UnitClass>().team + " Has Lost");
        if(ServerClient.instance != null)
        {
            if (GetComponent<UnitClass>().data.health <= 0 && GetComponent<UnitClass>().team != MatchSettings.instance.team)
            {
                ServerClient.instance.MessageServer("lost\n" + GetComponent<UnitClass>().team);
            }
        }
    }
}
