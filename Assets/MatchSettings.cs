using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchSettings : MonoBehaviour
{
    public static MatchSettings instance;

    public bool hosting;

    public string JoinCode;

    public int team;

    public int Budget = 1000;

    public int MapSeed = 189909;

    public Vector2 size = new Vector2(50, 50);

    [SerializeField]
    [Header("Generation")]
    public int MountainChance = 5;
    public int HillChance = 10;
    public int WaterChance = 20;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SetBudget(string _budget)
    {
        if (IsDigitsOnly(_budget))
            int.TryParse(_budget, out Budget);
    }
    internal bool IsDigitsOnly(string str)
    {
        foreach (char c in str)
        {
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }


    public void SetCode(string value)
    {
        JoinCode = value;
    }


    public void SetSize(int value)
    {
        switch (value)
        {
            //None
            case 0:
                size = new Vector2(30, 30);
                break;

            //Low
            case 1:
                size = new Vector2(60, 60);
                break;

            //Medium
            case 2:
                size = new Vector2(100, 100);
                break;
        }
    }



    public int SetChance(int value)
    {
        int chance = 0;
        switch (value)
        {
            //None
            case 0:
                chance = 0;
                break;

            //Low
            case 1:
                chance = 5;
                break;

            //Medium
            case 2:
                chance = 8;
                break;

            //High
            case 3:
                chance = 15;
                break;

        }

        return chance;
    }


    public void SetMountains(int value)
    {
        MountainChance = SetChance(value);
    }
    public void SetHills(int value)
    {
        HillChance = SetChance(value);
    }
    public void SetWater(int value)
    {
        WaterChance = SetChance(value);
    }
}
