using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;

public class LoginState : AState
{
    [SerializeField] private Button LoginButton;
    [SerializeField] private Canvas LoginCanvas;
    public override void Enter(AState from)
    {
        LoginCanvas.gameObject.SetActive(true);
        LoginButton.interactable = true;
    }

    public override void Exit(AState to)
    {
        LoginCanvas.gameObject.SetActive(false);
        //TODO: Populate PlayerData Class
    }

    public override string GetName()
    {
        return "Login";
    }

    public override void Tick()
    {
        
    }

    public void Login()
    {
        LoginButton.interactable = false;
        StartCoroutine(GuestLoginRoutine());
    }

    private IEnumerator GuestLoginRoutine()
    {
        bool gotResponse = false;
        LootLockerGuestSessionResponse response = null;
        LootLockerSDKManager.StartGuestSession((_response) => 
        {
            response = _response;
            gotResponse = true;
        });

        yield return new WaitWhile(() => gotResponse == false);

        if (response.success)
        {
            Debug.Log("Session Started!\nIdentifier: "+ response.player_identifier);
            PlayerData.instance.PlayerID = response.player_id.ToString();
            manager.SwitchState("Loadout");
        }
        else
        {
            Debug.LogError("Session Couldn't Started!");
        }
    }
}
