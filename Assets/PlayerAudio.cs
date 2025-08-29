using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAudio : MonoBehaviour
{
    public AudioClip click;

    AudioSource source;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        Controls.instance.input.Mouse.Click.performed += Clicked;
    }

    private void Clicked(InputAction.CallbackContext context)
    {
        source.PlayOneShot(click);
    }

}
