using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void BeginScene(string name)
    {
        SceneManager.LoadScene(name);
    }
    public void BeginScene(int index)
    {
        SceneManager.LoadScene(index);
    }
}
