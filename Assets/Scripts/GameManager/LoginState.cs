using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using TMPro;
using Newtonsoft.Json;
using System.Linq;

public class LoginState : AState
{

    [SerializeField] private Canvas LoginCanvas;
    [SerializeField] private GameObject LoginTypeSelectionMenu;
    [Space]
    [Header("Login UI")]
    [SerializeField] private GameObject WhiteLoginCanvas;
    [SerializeField] private Button LoginButton;
    [SerializeField] private Button WhiteLabelLoginButton;
    [SerializeField] private TMP_InputField ExistingEmailField;
    [SerializeField] private TMP_InputField ExistingPasswordField;
    [SerializeField] private Toggle RememberMe;
    [Space]
    [Header("Register UI")]
    [SerializeField] private GameObject RegisterCanvas;
    [SerializeField] private Button RegisterButton;
    [SerializeField] private TMP_InputField RegisterUsernameField;
    [SerializeField] private TMP_InputField RegisterEmailField;
    [SerializeField] private TMP_InputField RegisterPasswordField;
    [Header("Logout")]
    [SerializeField] private Button LogoutButton;
    [SerializeField] private Button LogoutYesButton;
    [SerializeField] private Button LogoutNoButton;
    [SerializeField] private SettingPopup SettingPopUp;




    private bool needLogin = true;
    public override IEnumerator Enter(AState from)
    {
        LoginCanvas.gameObject.SetActive(true);
        yield return EnterStateRoutine();
    }

    private IEnumerator EnterStateRoutine()
    {
        //yield return CheckSessionValidRoutine();
        if (false && !needLogin) //TODO: enable this part!
        {
            yield return WhiteLabelSessionRoutine();
            StartCoroutine(manager.SwitchState("Loadout"));
            yield break;
        }
        LoginTypeSelectionMenu.SetActive(true);
        RegisterButton.interactable = true;
        LoginButton.interactable = true;
        WhiteLabelLoginButton.interactable = true;
        LogoutButton.interactable = true;
    }

    public override IEnumerator Exit()
    {
        bool done = false;
        PlayerData.NewSave();
        LootLockerSDKManager.GetEntirePersistentStorage(response => {
            Debug.Log(response);
            foreach (var kvp in response.payload)
            {
                ReadKVP(kvp.key, kvp.value);
            }
            done = true;
        });

        yield return new WaitUntil(() => done);
        PlayerData.instance.tutorialDone = true;
        Debug.Log("Done");
    }
    public override void Exit(AState to)
    {
        LoginCanvas.gameObject.SetActive(false);
        LoginTypeSelectionMenu.SetActive(false);
    }

    private void ReadKVP(string key, string value)
    {
        switch (key)
        {
            case "Coin":
                PlayerData.instance.coins = int.Parse(value);
                break;
            case "Premium":
                PlayerData.instance.premium = int.Parse(value);
                break;
            case "Magnet":
                if (int.Parse(value) > 0) PlayerData.instance.consumables[Consumable.ConsumableType.COIN_MAG] = int.Parse(value);
                break;
            case "X2":
                if (int.Parse(value) > 0) PlayerData.instance.consumables[Consumable.ConsumableType.SCORE_MULTIPLAYER] = int.Parse(value);
                break;
            case "Invincible":
                if (int.Parse(value) > 0) PlayerData.instance.consumables[Consumable.ConsumableType.INVINCIBILITY] = int.Parse(value);
                break;
            case "Life":
                if(int.Parse(value) > 0) PlayerData.instance.consumables[Consumable.ConsumableType.EXTRALIFE] = int.Parse(value);
                break;
            case "TrashCat":
                if (bool.Parse(value)) PlayerData.instance.characters.Add("Trash Cat");
                else PlayerData.instance.characters.Remove("Trash Cat");
                break;
            case "RubbishRaccoon":
                if (bool.Parse(value)) PlayerData.instance.characters.Add("Rubbish Raccoon");
                else PlayerData.instance.characters.Remove("Rubbish Raccoon");
                break;
            case "DayTheme":
                if (bool.Parse(value)) PlayerData.instance.themes.Add("Day");
                else PlayerData.instance.characters.Remove("Day");
                break;
            case "NightTheme":
                if (bool.Parse(value)) PlayerData.instance.themes.Add("NightTime");
                else PlayerData.instance.characters.Remove("NightTime");
                break;
            default:
                break;
        }
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
            if (!response.seen_before) yield return PopulateCloudData();
            Debug.Log("Session Started!\nIdentifier: "+ response.player_identifier);
            PlayerData.instance.PlayerID = response.player_id.ToString();
            StartCoroutine(manager.SwitchState("Loadout"));
        }
        else
        {
            Debug.LogError("Session Couldn't Started!");
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.text);
            StartCoroutine(FailedToLogin(responseDict["message"]));
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
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string,string>>(loginResponse.text);
            StartCoroutine(FailedToLogin(responseDict["message"]));
            yield break;
        }

        string token = loginResponse.SessionToken;
        yield return WhiteLabelSessionRoutine();
        if (!isRegister)
        {
            WhiteLoginCanvas.SetActive(false);
            StartCoroutine(manager.SwitchState("Loadout"));
        }
        }

    private IEnumerator FailedToLogin(string error,float duration = 5f)
    {
        //TODO: Create Pop-up about error
        PopupManager.CreatePopup(PopupType.Error, error, duration);
        
        yield return new WaitForSeconds(2f);
        
        LoginButton.interactable = true;
        WhiteLabelLoginButton.interactable = true;
    }

    private IEnumerator FailedToRegister(string error, float duration = 5f)
    {
        PopupManager.CreatePopup(PopupType.Error, error, duration);

        yield return new WaitForSeconds(2f);

        RegisterButton.interactable = true;

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
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(signUpResponse.text);
            var error = responseDict["message"];
            StartCoroutine(FailedToRegister(error));
            yield break;
        }
        PopupManager.CreatePopup(PopupType.Info, "User Created Successfully");
        Debug.Log("user created successfully");

        RegisterCanvas.SetActive(false);

        yield return WhiteLabelLoginRoutine(true);

        yield return PopulateCloudData();

        string userName = RegisterUsernameField.text;
        Debug.Log("Starting to change name to: " + userName);
        LootLockerHelper.ChangeUserName(userName,null);
        StartCoroutine(manager.SwitchState("Loadout"));
    }

    private IEnumerator PopulateCloudData()
    {
        //Currencies
        var coin = StartCoroutine(RegisterKVP("Coin", "0"));
        var premium = StartCoroutine(RegisterKVP("Premium", "0"));

        //Collectables
        var magnet = StartCoroutine(RegisterKVP("Magnet", "0"));
        var x2 = StartCoroutine(RegisterKVP("X2", "0"));
        var invincible = StartCoroutine(RegisterKVP("Invincible", "0"));
        var life = StartCoroutine(RegisterKVP("Life", "0"));

        //Characters
        var trashCat = StartCoroutine(RegisterKVP("TrashCat", "true"));
        var rubbishRaccoon = StartCoroutine(RegisterKVP("RubbishRaccoon", "false"));

        //Accessories
        //TODO: Add Accessories

        //Themes
        var dayTheme = StartCoroutine(RegisterKVP("DayTheme", "true"));
        var nightTheme = StartCoroutine(RegisterKVP("NightTheme", "false"));

        yield return coin;
        yield return premium;
        yield return magnet;
        yield return x2;
        yield return invincible;
        yield return life;
        yield return trashCat;
        yield return rubbishRaccoon;
        yield return dayTheme;
        yield return nightTheme;

    }

    private IEnumerator RegisterKVP(string key, string value)
    {
        bool done = false;
        LootLockerSDKManager.UpdateOrCreateKeyValue(key, value, response =>
        {
            done = true;
        });
        yield return new WaitUntil(() => done);
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
        StartCoroutine(manager.SwitchState("Login"));
        SettingPopUp.gameObject.SetActive(false);
        SettingPopUp.transform.GetChild(1).gameObject.SetActive(false);
    }
}
