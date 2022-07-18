using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LootLockerHelper 
{
    public static void ChangeUserName(string userName,Action<PlayerNameResponse> callback)
    {
        LootLockerSDKManager.SetPlayerName(userName, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Succesfully set player name");
            }
            else
            {
                Debug.LogError("Could not set player name " + response.Error);
            }
            callback?.Invoke(response);
        });
    }
}
