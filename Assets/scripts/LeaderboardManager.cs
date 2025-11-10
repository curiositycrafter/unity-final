using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public float totalTime;
    public int score;
}

[System.Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public TMP_Text leaderboardText;
    private string filePath;

    void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
    }

    public void SaveEntry(string playerName, float totalTime)
    {
        LeaderboardData data = LoadData();
        int score = Mathf.Max(1, 10000 - Mathf.FloorToInt(totalTime * 10));

        data.entries.Add(new LeaderboardEntry
        {
            playerName = playerName,
            totalTime = totalTime,
            score = score
        });

        data.entries.Sort((a, b) => a.totalTime.CompareTo(b.totalTime));
        if (data.entries.Count > 5)
            data.entries = data.entries.GetRange(0, 5);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    public LeaderboardData LoadData()
    {
        if (!File.Exists(filePath))
            return new LeaderboardData();

        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<LeaderboardData>(json);
    }

    public void DisplayLeaderboard()
    {
        LeaderboardData data = LoadData();

        // Center text on screen
        leaderboardText.alignment = TextAlignmentOptions.Center;
        leaderboardText.enableWordWrapping = false;
        leaderboardText.fontSize = 28;

        // Removed the unsupported emoji
        leaderboardText.text = "<b><size=34>Top 5 Players</size></b>\n\n";

        if (data.entries.Count == 0)
        {
            leaderboardText.text += "No records yet.";
            return;
        }

        int rank = 1;
        foreach (var e in data.entries)
        {
            leaderboardText.text += $"{rank}. {e.playerName} | {FormatTime(e.totalTime)} | Score: {e.score}\n";
            rank++;
        }
    }


    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}
