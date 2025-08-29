using UnityEngine;

public class IconManager : MonoBehaviour
{
    public Material TeamA;
    public Material TeamB;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(MatchSettings.instance.team == 0)
        {
            GetComponent<MeshRenderer>().material = TeamA;
        }
        else
        {
            GetComponent<MeshRenderer>().material = TeamB;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
