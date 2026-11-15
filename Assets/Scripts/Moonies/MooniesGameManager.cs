using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonies
{
    public class MooniesGameManager : MonoBehaviour
    {
        public static MooniesGameManager Instance { get; private set; }

        [Header("Optional: assign the JSON file or load it from Resources/MooniesMissions")]
        [SerializeField] private TextAsset missionData;

        public IReadOnlyList<MooniesMission> Missions => missions;
        public MissionResources Resources { get; private set; } = new MissionResources();
        public int CurrentIndex { get; private set; }
        public int Score { get; private set; }
        public int CorrectAnswers { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }
        public bool HasAnswered { get; private set; }
        public bool HintUsed { get; private set; }
        public bool RescueUsed { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsFinished { get; private set; }
        public int SessionPhase { get; private set; } = -1;
        public MissionAttempt CurrentAttempt => currentAttempt;
        public string CrewName { get; private set; } = "MOONIES Crew";
        public MooniesMission CurrentMission => sessionMissions != null && CurrentIndex >= 0 && CurrentIndex < sessionMissions.Count ? sessionMissions[CurrentIndex] : null;
        public int AnsweredActs => attempts.Count;
        public event Action StateChanged;

        private List<MooniesMission> missions;
        private List<MooniesMission> sessionMissions;
        private MooniesUI ui;
        private readonly List<MissionAttempt> attempts = new List<MissionAttempt>();
        private MissionAttempt currentAttempt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(MooniesUI gameUI, TextAsset jsonOverride = null)
        {
            ui = gameUI;
            TextAsset json = jsonOverride != null ? jsonOverride : missionData;
            if (json == null) json = ResourcesLoadMissions();

            if (json == null)
            {
                Debug.LogError("MOONIES: MooniesMissions.json was not found. Place it in a Resources folder.");
                missions = new List<MooniesMission>();
                if (ui != null) ui.ShowFatalError("The mission file is missing. Check that MooniesMissions.json is inside a Resources folder.");
                return;
            }

            try
            {
                MooniesDatabase database = JsonUtility.FromJson<MooniesDatabase>(json.text);
                missions = database != null && database.missions != null
                    ? new List<MooniesMission>(database.missions)
                    : new List<MooniesMission>();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                missions = new List<MooniesMission>();
            }

            if (missions.Count != 30)
                Debug.LogWarning("MOONIES: Expected 30 acts, but loaded " + missions.Count + ".");
        }

        private TextAsset ResourcesLoadMissions()
        {
            return UnityEngine.Resources.Load<TextAsset>("MooniesMissions");
        }

        public void StartMission(string crewName)
        {
            if (missions == null || missions.Count == 0) return;
            BeginCampaign(crewName, new List<MooniesMission>(missions), true, -1);
        }

        public bool StartPhase(string crewName, int phase)
        {
            if (missions == null || missions.Count == 0 || phase < 0) return false;
            List<MooniesMission> phaseMissions = missions.FindAll(item => item != null && item.phase == phase);
            if (phaseMissions.Count == 0) return false;
            if (HasStarted && !IsFinished && SessionPhase == phase)
            {
                CrewName = string.IsNullOrWhiteSpace(crewName) ? CrewName : crewName.Trim();
                if (ui != null) ui.ShowMission(this);
                return true;
            }
            BeginCampaign(crewName, phaseMissions, !HasStarted, phase);
            return true;
        }

        private void BeginCampaign(string crewName, List<MooniesMission> selectedMissions, bool resetStats, int phase)
        {
            CrewName = string.IsNullOrWhiteSpace(crewName) ? "MOONIES Crew" : crewName.Trim();
            sessionMissions = selectedMissions;
            SessionPhase = phase;
            CurrentIndex = 0;
            if (resetStats)
            {
                Score = 0;
                CorrectAnswers = 0;
                CurrentStreak = 0;
                BestStreak = 0;
                Resources = new MissionResources();
                attempts.Clear();
            }
            HasStarted = true;
            IsFinished = false;
            HasAnswered = false;
            HintUsed = false;
            RescueUsed = false;
            currentAttempt = null;
            PresentCurrentMission();
        }

        public void PresentCurrentMission()
        {
            if (CurrentMission == null) return;
            HasAnswered = false;
            HintUsed = false;
            currentAttempt = null;
            if (ui != null) ui.ShowMission(this);
            StateChanged?.Invoke();
        }

        public void Choose(int choiceIndex)
        {
            if (HasAnswered || CurrentMission == null) return;
            MooniesChoice[] choices = CurrentMission.choices;
            if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Length) return;

            HasAnswered = true;
            MooniesChoice selected = choices[choiceIndex];
            if (selected.isCorrect)
            {
                CorrectAnswers++;
                CurrentStreak++;
                BestStreak = Mathf.Max(BestStreak, CurrentStreak);
                int bonus = CurrentStreak > 0 && CurrentStreak % 3 == 0 ? 25 : 0;
                Score += 100 + bonus;
                Resources.Add(selected.gain, +1);
                if (ui != null) ui.ShowFeedback(true, selected.why, CurrentMission.learn, CurrentMission.fact, bonus, null, choiceIndex);
            }
            else
            {
                CurrentStreak = 0;
                Score += 15;
                Resources.Add(selected.damage, -1);
                if (ui != null) ui.ShowFeedback(false, selected.why, CurrentMission.learn, CurrentMission.fact, 0, selected.damage, choiceIndex);
            }

            currentAttempt = new MissionAttempt
            {
                actNumber = attempts.Count + 1,
                correct = selected.isCorrect,
                selectedOption = choiceIndex
            };
            attempts.Add(currentAttempt);
            StateChanged?.Invoke();
            if (ui != null) ui.RefreshHud(this);
        }

        public void RequestHint()
        {
            if (HasAnswered || HintUsed || CurrentMission == null) return;
            HintUsed = true;
            Score = Mathf.Max(0, Score - 10);
            if (ui != null) ui.ShowHint(CurrentMission.hint);
            if (ui != null) ui.RefreshHud(this);
        }

        public void UseHoustonRescue()
        {
            if (RescueUsed) return;
            ResourceType lowest = Resources.Lowest();
            Resources.Recover(lowest, 22);
            Score = Mathf.Max(0, Score - 80);
            RescueUsed = true;
            if (ui != null) ui.ShowRescue(lowest, HasAnswered);
            if (ui != null) ui.RefreshHud(this);
            StateChanged?.Invoke();
        }

        public void NextAct()
        {
            if (!HasAnswered) return;
            if (sessionMissions == null || CurrentIndex >= sessionMissions.Count - 1)
            {
                FinishMission();
                return;
            }
            CurrentIndex++;
            PresentCurrentMission();
        }

        public void FinishMission()
        {
            if (IsFinished) return;
            IsFinished = true;
            int average = Resources.Average();
            string rank;
            string medal;
            string summary;
            int accuracy = attempts.Count == 0 ? 0 : Mathf.RoundToInt((float)CorrectAnswers / attempts.Count * 100f);

            if (accuracy >= 90 && average >= 70)
            {
                rank = "LUNAR COMMANDER";
                medal = "🏆";
                summary = "Outstanding work. You protected the crew, managed resources, and returned with valuable scientific results.";
            }
            else if (accuracy >= 75 && average >= 45)
            {
                rank = "ELITE CREW";
                medal = "🥇";
                summary = "Great work. You showed sound judgment and handled crises well. Some decisions left their mark, but the mission made it home.";
            }
            else if (accuracy >= 55)
            {
                rank = "LUNAR EXPLORER";
                medal = "🥈";
                summary = "You completed the campaign and learned from your mistakes. Review the explanations and try again to improve your safety margin.";
            }
            else
            {
                rank = "SPACE CADET";
                medal = "🌙";
                summary = "The mission is over, and the experience still counts. Every mistake in space teaches something: review the report and try a new strategy.";
            }

            ResourceType lowest = Resources.Lowest();
            string debrief = "Your most depleted resource was " + MooniesText.ResourceName(lowest) + " (" + Resources.Get(lowest) + "%). Correct answers: " + CorrectAnswers + " of " + attempts.Count + ". Score: " + Score + ". Mission Control recommends replaying the campaign and watching decisions that affect " + MooniesText.ResourceName(lowest).ToLowerInvariant() + ".";
            if (ui != null) ui.ShowResults(rank, medal, summary, Score, CorrectAnswers, BestStreak, accuracy, debrief, Resources);
        }

        public int TotalActs => sessionMissions == null ? 0 : sessionMissions.Count;
    }
}
