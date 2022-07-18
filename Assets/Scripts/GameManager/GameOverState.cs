using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif
using System.Collections.Generic;
using LootLocker.Requests;
/// <summary>
/// state pushed on top of the GameManager when the player dies.
/// </summary>
public class GameOverState : AState
{
    public TrackManager trackManager;
    public Canvas canvas;
    public MissionUI missionPopup;

	public AudioClip gameOverTheme;

	public Leaderboard miniLeaderboard;
	public Leaderboard fullLeaderboard;

    public GameObject addButton;

    bool ready;
    public override IEnumerator Enter(AState from)
    {
        canvas.gameObject.SetActive(true);
        StartCoroutine(SetupGameOverState());

        if (PlayerData.instance.AnyMissionComplete())
            StartCoroutine(missionPopup.Open());
        else
            missionPopup.gameObject.SetActive(false);

		CreditCoins();

		if (MusicPlayer.instance.GetStem(0) != gameOverTheme)
		{
            MusicPlayer.instance.SetStem(0, gameOverTheme);
			StartCoroutine(MusicPlayer.instance.RestartAllStems());
        }

        yield return null;
    }
    public IEnumerator SetupGameOverState()
    {
        yield return SubmitScoreRoutine();
        yield return miniLeaderboard.Populate();
    }

	public override void Exit(AState to)
    {
        canvas.gameObject.SetActive(false);
        FinishRun();
    }
    public override IEnumerator Exit()
    {
        yield return null;
    }

    public override string GetName()
    {
        return "GameOver";
    }

    public override void Tick()
    {

    }
    private int rank;
    private IEnumerator SubmitScoreRoutine()
    {
        bool done = false;
        LootLockerSDKManager.SubmitScore(
            PlayerData.instance.PlayerID,
            trackManager.score,
            ThemeDatabase.GetThemeData(PlayerData.instance.themes[PlayerData.instance.usedTheme]).ThemeID,
            (_response) =>
            {
                rank = _response.rank;
                Debug.Log("Score Uploaded!");
                done = true;
            }
        );

        yield return new WaitWhile(() => done == false);
    }
    public void OpenLeaderboard()
	{
		fullLeaderboard.forcePlayerDisplay = false;
		fullLeaderboard.displayPlayer = true;

		fullLeaderboard.Open();
    }

	public void GoToStore()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("shop", UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }


    public void GoToLoadout()
    {
        trackManager.isRerun = false;
        StartCoroutine(manager.SwitchState("Loadout"));
    }

    public void RunAgain()
    {
        trackManager.isRerun = false;
        StartCoroutine(manager.SwitchState("Game"));
    }

    protected void CreditCoins()
	{
		PlayerData.instance.Save(PlayerData.SaveType.Currency);

#if UNITY_ANALYTICS // Using Analytics Standard Events v0.3.0
        var transactionId = System.Guid.NewGuid().ToString();
        var transactionContext = "gameplay";
        var level = PlayerData.instance.rank.ToString();
        var itemType = "consumable";
        
        if (trackManager.characterController.coins > 0)
        {
            AnalyticsEvent.ItemAcquired(
                AcquisitionType.Soft, // Currency type
                transactionContext,
                trackManager.characterController.coins,
                "fishbone",
                PlayerData.instance.coins,
                itemType,
                level,
                transactionId
            );
        }

        if (trackManager.characterController.premium > 0)
        {
            AnalyticsEvent.ItemAcquired(
                AcquisitionType.Premium, // Currency type
                transactionContext,
                trackManager.characterController.premium,
                "anchovies",
                PlayerData.instance.premium,
                itemType,
                level,
                transactionId
            );
        }
#endif 
	}

	protected void FinishRun()
    {

        PlayerData.instance.InsertScore(trackManager.score, "Trash Cat" );

        CharacterCollider.DeathEvent de = trackManager.characterController.characterCollider.deathData;
        //register data to analytics
#if UNITY_ANALYTICS
        AnalyticsEvent.GameOver(null, new Dictionary<string, object> {
            { "coins", de.coins },
            { "premium", de.premium },
            { "score", de.score },
            { "distance", de.worldDistance },
            { "obstacle",  de.obstacleType },
            { "theme", de.themeUsed },
            { "character", de.character },
        });
#endif
        //TODO: CHECK THIS
        //PlayerData.instance.Save();

        trackManager.End();
    }

    //----------------

}