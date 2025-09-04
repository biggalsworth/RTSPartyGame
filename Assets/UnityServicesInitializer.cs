using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UnityServicesInitializer : MonoBehaviour
{
    async void Awake()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}