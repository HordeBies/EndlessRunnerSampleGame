using UnityEngine;
using LootLocker.Requests;
using System.Collections;
// Prefill the info on the player data, as they will be used to populate the leadboard.
public class Leaderboard : MonoBehaviour
{
	public RectTransform entriesRoot;
	public int entriesCount;

	public bool forcePlayerDisplay;
	public bool displayPlayer = true;

	public void Open()
	{
		gameObject.SetActive(true);
		StartCoroutine(Populate());
	}

	public void Close()
	{
		gameObject.SetActive(false);
	}

	public IEnumerator Populate()
	{
		for (int i = 0; i < entriesCount; ++i)
		{
			entriesRoot.GetChild(i).gameObject.SetActive(false);
		}

		bool done = false;
		LootLockerLeaderboardMember[] members = null;
		LootLockerSDKManager.GetScoreListMain(
			ThemeDatabase.GetThemeData(PlayerData.instance.themes[PlayerData.instance.usedTheme]).ThemeID,
			entriesCount,
			0,
			(_response) =>
			{
				members = _response.items;
				done = true;
			}
		);
		yield return new WaitWhile(() => done == false);
		done = false;
		LootLockerLeaderboardMember member = null;
		LootLockerSDKManager.GetMemberRank(
			ThemeDatabase.GetThemeData(PlayerData.instance.themes[PlayerData.instance.usedTheme]).ThemeID.ToString(),
			PlayerData.instance.PlayerID,
			(_response) =>
			{
				member = new LootLockerLeaderboardMember() { member_id = _response.member_id, player = _response.player, rank = _response.rank, score = _response.score };
				done = true;
			}
		);
		yield return new WaitWhile(() => done == false);

        for (int i = 0; i < entriesCount && i < members.Length; ++i)
		{
			HighscoreUI hs = entriesRoot.GetChild(i).GetComponent<HighscoreUI>();
			hs.gameObject.SetActive(true);
			hs.playerName.text = members[i].player.name;
			hs.number.text = members[i].rank.ToString();
			hs.score.text = members[i].score.ToString();
		}

		if(member.rank > entriesCount)
        {
			var hs = entriesRoot.GetChild(entriesRoot.childCount - 1).GetComponent<HighscoreUI>();
			hs.playerName.text = member.player.name;
			hs.number.text = member.rank.ToString();
			hs.score.text = member.score.ToString();
        }

		//int idx = member.rank - 1 < entriesCount ? member.rank - 1 : entriesCount;
		//entriesRoot.GetChild(idx).gameObject.SetActive(false);
		//playerEntry.transform.SetSiblingIndex(idx);
		//playerEntry.number.text = member.rank.ToString();
		//playerEntry.score.text = member.score.ToString();


		//// Find all index in local page space.
		//int localStart = 0;
		//int place = -1;
		//int localPlace = -1;

		//if (displayPlayer)
		//{
		//	place = PlayerData.instance.GetScorePlace(int.Parse(playerEntry.score.text));
		//	localPlace = place - localStart;
		//}

		//if (localPlace >= 0 && localPlace < entriesCount && displayPlayer)
		//{
		//	playerEntry.gameObject.SetActive(true);
		//	playerEntry.transform.SetSiblingIndex(localPlace);
		//}

		//if (!forcePlayerDisplay || PlayerData.instance.highscores.Count < entriesCount)
		//	entriesRoot.GetChild(entriesRoot.transform.childCount - 1).gameObject.SetActive(false);

		//int currentHighScore = localStart;

		//for (int i = 0; i < entriesCount; ++i)
		//{
		//	HighscoreUI hs = entriesRoot.GetChild(i).GetComponent<HighscoreUI>();

		//          if (hs == playerEntry || hs == null)
		//	{
		//		// We skip the player entry.
		//		continue;
		//	}

		//    if (PlayerData.instance.highscores.Count > currentHighScore)
		//    {
		//        hs.gameObject.SetActive(true);
		//        hs.playerName.text = PlayerData.instance.highscores[currentHighScore].name;
		//        hs.number.text = (localStart + i + 1).ToString();
		//        hs.score.text = PlayerData.instance.highscores[currentHighScore].score.ToString();

		//        currentHighScore++;
		//    }
		//    else
		//        hs.gameObject.SetActive(false);
		//}

		//// If we force the player to be displayed, we enable it even if it was disabled from elsewhere
		//if (forcePlayerDisplay) 
		//	playerEntry.gameObject.SetActive(true);

		//playerEntry.number.text = (place + 1).ToString();
	}
}
