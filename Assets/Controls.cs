using UnityEngine;

public class Controls : MonoBehaviour
{
    public static Controls instance;

    public ControlScheme input;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        input = new ControlScheme();
        input.Enable();

        ToggleMouse(false);
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ToggleInput(bool takeInput)
    {
        if (takeInput)
            input.Enable();
        else
            input.Disable();
    }


    public void ToggleMouse(bool show)
    {
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}
