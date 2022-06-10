using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using TMPro;

public class LoginState : AState
{

    [SerializeField] private Canvas LoginCanvas;
    [SerializeField] private GameObject LoginTypeSelectionMenu;
    [Space]
    [Header("Login UI")]
    [SerializeField] private Button LoginButton;
    [SerializeField] private Button WhiteLabelLoginButton;
    [SerializeField] private TMP_InputField ExistingEmailField;
    [SerializeField] private TMP_InputField ExistingPasswordField;
    [SerializeField] private Toggle RememberMe;
    [Space]
    [Header("Register UI")]
    [SerializeField] private Button RegisterButton;
    [SerializeField] private InputField RegisterUsernameField;
    [SerializeField] private InputField RegisterEmailField;
    [SerializeField] private InputField RegisterPasswordField;
    [Header("Logout")]
    [SerializeField] private Button LogoutButton;
    [SerializeField] private Button LogoutYesButton;
    [SerializeField] private Button LogoutNoButton;
    [SerializeField] private SettingPopup SettingPopUp;




    private bool needLogin = true;
    public override void Enter(AState from)
    {
        LoginCanvas.gameObject.SetActive(true);
        StartCoroutine(EnterStateRoutine());
    }

    private IEnumerator EnterStateRoutine()
    {
        yield return CheckSessionValidRoutine();
        if (false && !needLogin) //TODO: enable this part!
        {
            yield return WhiteLabelSessionRoutine();
            manager.SwitchState("Loadout");
            yield break;
        }
        LoginTypeSelectionMenu.SetActive(true);
        RegisterButton.interactable = true;
        LoginButton.interactable = true;
        WhiteLabelLoginButton.interactable = true;
        LogoutButton.interactable = true;
    }

    public override void Exit(AState to)
    {
        LoginCanvas.gameObject.SetActive(false);
        LoginTypeSelectionMenu.SetActive(false);
        //TODO: Populate PlayerData Class
    }

    public override string GetName()
    {
        return "Login";
    }

    public override void Tick()
    {
        
    }

    public void LoginGuest()
    {
        LoginButton.interactable = false;
        StartCoroutine(GuestLoginRoutine());
    }

    public void LoginWhiteLabel()
    {
        WhiteLabelLoginButton.interactable = false;
        StartCoroutine(WhiteLabelLoginRoutine());
    }

    public void RegisterWhiteLabel()
    {
        RegisterButton.interactable = false;
        StartCoroutine(CreateAccountRoutine());
    }

    public void LogOut()
    {
        LogoutButton.interactable = false;
        LoginCanvas.gameObject.SetActive(true);
        StartCoroutine(LogOutRoutine());        
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

    private IEnumerator WhiteLabelLoginRoutine(bool isRegister = false)
    {
        string email = isRegister ? RegisterEmailField.text : ExistingEmailField.text;
        string password = isRegister ? RegisterPasswordField.text : ExistingPasswordField.text;
  
       
        bool gotResponse = false;
        LootLockerWhiteLabelLoginResponse loginResponse = null;
        LootLockerSDKManager.WhiteLabelLogin(email, password, RememberMe.isOn, response =>
        {
            loginResponse = response;
            gotResponse = true;
        });

        yield return new WaitWhile(() => gotResponse == false);
        if (!loginResponse.success)
        {
            Debug.Log("Error while logging in");
            FailedToLogin();
            yield break;
        }

        string token = loginResponse.SessionToken;
        yield return WhiteLabelSessionRoutine();
        if(!isRegister) manager.SwitchState("Loadout");
    }

    private void FailedToLogin()
    {
        //TODO: Create Pop-up about error
        LoginButton.interactable = true;
        WhiteLabelLoginButton.interactable = true;
    }

    private IEnumerator WhiteLabelSessionRoutine()
    {
        bool gotResponse = false;
        LootLockerSDKManager.StartWhiteLabelSession((response) =>
        {
            if (!response.success)
            {
                Debug.Log("Error while LootLocker Session");
                return;
            }
            else
            {
                Debug.Log("Session Started!\nIdentifier: " + response.player_id);
                PlayerData.instance.PlayerID = response.player_id.ToString();
                
            }
            gotResponse = true;

        });
        yield return new WaitWhile(() => gotResponse == false);
    }

    private IEnumerator CreateAccountRoutine()
    {
        string email = RegisterEmailField.text;
        string password = RegisterPasswordField.text;
        LootLockerWhiteLabelSignupResponse signUpResponse = null;
        LootLockerSDKManager.WhiteLabelSignUp(email, password, (response) =>
        {
            signUpResponse = response;
        });

        yield return new WaitWhile(() => signUpResponse == null);
        if (!signUpResponse.success)
        {
            Debug.Log("error while creating user");
            yield break;
        }
        Debug.Log("user created successfully");

        yield return WhiteLabelLoginRoutine(true);

        string userName = RegisterUsernameField.text;
        Debug.Log("Starting to change name to: " + userName);
        LootLockerHelper.ChangeUserName(userName);
        manager.SwitchState("Loadout");
    }

    private IEnumerator CheckSessionValidRoutine()
    {
        bool gotResponse = false;
        LootLockerSDKManager.CheckWhiteLabelSession(response =>
        {
            if (response)
            {
                Debug.Log("session is valid, you can start a game session");
                needLogin = false;

            }
            else
            {
                Debug.Log("session is NOT valid, we should show the login form");
                needLogin = true;
            }
            gotResponse = true;
        });
        yield return new WaitWhile(() => gotResponse == false);
    }

    private IEnumerator LogOutRoutine()
    {

        LootLockerSessionResponse endSessionResponse = null;
        LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
        LootLocker.LootLockerAPIManager.EndSession(sessionRequest, (response) =>
        {
            endSessionResponse = response;
        });
        
        yield return new WaitWhile(() => endSessionResponse == null);
        if (!endSessionResponse.success)
        {
            Debug.Log("Error while ending session");
            LoginCanvas.gameObject.SetActive(false);
            yield break;
        }
        Debug.Log("Session ended successfully");
        LogoutButton.interactable = true;
        manager.SwitchState("Login");
        SettingPopUp.gameObject.SetActive(false);
        SettingPopUp.transform.GetChild(1).gameObject.SetActive(false);
    }
}
