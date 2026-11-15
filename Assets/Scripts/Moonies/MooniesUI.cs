using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Moonies
{
    /// <summary>
    /// Complete interface generated in code with Unity's classic UGUI system.
    /// </summary>
    public class MooniesUI : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.93f, 0.96f, 1f);
        private static readonly Color Muted = new Color(0.68f, 0.75f, 0.87f);
        private static readonly Color Cyan = new Color(0.38f, 0.88f, 0.93f);
        private static readonly Color Green = new Color(0.28f, 0.82f, 0.61f);
        private static readonly Color Red = new Color(0.95f, 0.36f, 0.45f);
        private static readonly Color Gold = new Color(1f, 0.77f, 0.32f);

        private MooniesGameManager manager;
        private Action returnToShip;
        private Font font;
        private Canvas canvas;
        private GameObject homeScreen;
        private GameObject gameScreen;
        private GameObject resultsScreen;
        private Text homeMessage;
        private InputField crewInput;
        private Text headerStatus;
        private Button returnToShipButton;
        private Text progressText;
        private Text scoreText;
        private readonly Dictionary<ResourceType, Image> meterFills = new Dictionary<ResourceType, Image>();
        private readonly Dictionary<ResourceType, Text> meterValues = new Dictionary<ResourceType, Text>();
        private Text phaseText;
        private Text missionTitle;
        private Text scenarioText;
        private Text questionText;
        private Text feedbackText;
        private Button nextButton;
        private Button hintButton;
        private Button rescueButton;
        private readonly List<Button> answerButtons = new List<Button>();
        private readonly List<int> answerIndices = new List<int>();
        private bool built;

        public void Initialize(MooniesGameManager gameManager, Action shipReturnAction = null)
        {
            manager = gameManager;
            returnToShip = shipReturnAction;
            if (!built) BuildInterface();
        }

        private void BuildInterface()
        {
            built = true;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("MOONIES Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.GetComponent<GraphicRaycaster>();

            // A subtle tint keeps the panels readable over the 3D scenes.
            CreatePanel(canvas.transform, "Backdrop Tint", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Color(0.015f, 0.025f, 0.06f, 0.27f));
            BuildHeader();
            BuildHud();
            BuildGamePanel();
            BuildHomeScreen();
            BuildResultsScreen();
            ShowHome();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BuildHeader()
        {
            GameObject header = CreatePanel(canvas.transform, "Header", new Vector2(0.035f, 0.92f), new Vector2(0.965f, 0.99f), Vector2.zero, Vector2.zero, new Color(0.025f, 0.055f, 0.12f, 0.84f));
            Text brand = CreateText(header.transform, "MOONIES  /  ARTEMIS LUNAR HORIZON", 27, FontStyle.Bold, Ink,
                new Vector2(0.025f, 0.08f), new Vector2(0.56f, 0.92f), TextAnchor.MiddleLeft);
            Text sub = CreateText(header.transform, "LUNAR EXPLORATION LEARNING SIMULATOR", 12, FontStyle.Bold, Cyan,
                new Vector2(0.57f, 0.08f), new Vector2(0.79f, 0.92f), TextAnchor.MiddleRight);
            headerStatus = sub;
            returnToShipButton = CreateButton(header.transform,"RETURN TO SHIP",new Color(0.12f,0.2f,0.34f),RequestReturnToShip);
            SetAnchors(returnToShipButton.GetComponent<RectTransform>(),new Vector2(.81f,.12f),new Vector2(.98f,.88f),Vector2.zero,Vector2.zero);
            returnToShipButton.GetComponent<LayoutElement>().ignoreLayout = true;
            returnToShipButton.gameObject.SetActive(returnToShip != null);
        }

        private void BuildHud()
        {
            GameObject hud = new GameObject("Mission HUD", typeof(RectTransform));
            hud.transform.SetParent(canvas.transform, false);
            SetAnchors(hud.GetComponent<RectTransform>(), new Vector2(0.035f, 0.805f), new Vector2(0.965f, 0.91f), Vector2.zero, Vector2.zero);

            ResourceType[] types =
            {
                ResourceType.Oxygen, ResourceType.Fuel, ResourceType.Power,
                ResourceType.Hull, ResourceType.Team, ResourceType.Science
            };
            Color[] colors = { new Color(0.3f,0.82f,0.93f), new Color(1f,0.66f,0.26f), new Color(0.72f,0.58f,1f), Green, new Color(0.36f,0.75f,0.95f), Gold };
            for (int i = 0; i < types.Length; i++)
            {
                float x0 = i / 6f;
                float x1 = (i + 1) / 6f;
                GameObject card = CreatePanel(hud.transform, "Meter " + types[i], new Vector2(x0, 0), new Vector2(x1, 1), new Vector2(4, 2), new Vector2(-4, -2), new Color(0.025f, 0.055f, 0.12f, 0.9f));
                Text label = CreateText(card.transform, MooniesText.ResourceName(types[i]).ToUpperInvariant(), 12, FontStyle.Bold, Muted,
                    new Vector2(0.08f, 0.47f), new Vector2(0.72f, 0.95f), TextAnchor.MiddleLeft);
                Text value = CreateText(card.transform, "100%", 18, FontStyle.Bold, Ink,
                    new Vector2(0.70f, 0.47f), new Vector2(0.94f, 0.95f), TextAnchor.MiddleRight);
                GameObject barBack = CreatePanel(card.transform, "Bar Background", new Vector2(0.08f, 0.16f), new Vector2(0.94f, 0.37f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.1f));
                GameObject barFill = CreatePanel(barBack.transform, "Bar Fill", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, colors[i]);
                Image fillImage = barFill.GetComponent<Image>();
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                meterFills[types[i]] = fillImage;
                meterValues[types[i]] = value;
            }
        }

        private void BuildGamePanel()
        {
            gameScreen = CreatePanel(canvas.transform, "Game Screen", new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.79f), Vector2.zero, Vector2.zero, new Color(0.025f, 0.045f, 0.10f, 0.93f));
            GameObject viewport = CreatePanel(gameScreen.transform, "Viewport", new Vector2(0.015f, 0.02f), new Vector2(0.985f, 0.98f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            ScrollRect scroll = gameScreen.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;

            GameObject content = new GameObject("Mission Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 22, 26);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.content = contentRect;

            phaseText = AddContentText(content.transform, "PHASE LABEL", 13, FontStyle.Bold, Cyan, 30);
            missionTitle = AddContentText(content.transform, "ACT 01", 34, FontStyle.Bold, Ink, 62);
            scenarioText = AddContentText(content.transform, "Scenario", 18, FontStyle.Normal, Muted, 105);
            questionText = AddContentText(content.transform, "Question", 21, FontStyle.Bold, Gold, 52);

            GameObject answers = new GameObject("Answers", typeof(RectTransform), typeof(VerticalLayoutGroup));
            answers.transform.SetParent(content.transform, false);
            LayoutElement answersLayout = answers.AddComponent<LayoutElement>();
            answersLayout.preferredHeight = 340;
            VerticalLayoutGroup answerLayout = answers.GetComponent<VerticalLayoutGroup>();
            answerLayout.spacing = 9;
            answerLayout.childControlWidth = true;
            answerLayout.childControlHeight = true;
            answerLayout.childForceExpandWidth = true;
            answerLayout.childForceExpandHeight = false;
            // Keep the container so the four answers can be rebuilt for each act.
            answersContainer = answers.transform;

            GameObject actionRow = new GameObject("Action Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionRow.transform.SetParent(content.transform, false);
            LayoutElement rowLayout = actionRow.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 58;
            HorizontalLayoutGroup horizontal = actionRow.GetComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
            hintButton = CreateButton(actionRow.transform, "REQUEST A HINT  (−10 PTS)", new Color(0.12f, 0.24f, 0.42f), () => manager.RequestHint());
            rescueButton = CreateButton(actionRow.transform, "HOUSTON RESCUE  (−80 PTS)", new Color(0.38f, 0.24f, 0.13f), () => manager.UseHoustonRescue());

            feedbackPanel = CreatePanel(content.transform, "Feedback Panel", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Color(0.07f, 0.11f, 0.20f, 1f));
            LayoutElement feedbackLayout = feedbackPanel.AddComponent<LayoutElement>();
            feedbackLayout.preferredHeight = 240;
            feedbackText = CreateText(feedbackPanel.transform, "Feedback", 17, FontStyle.Normal, Ink,
                new Vector2(0.025f, 0.14f), new Vector2(0.975f, 0.95f), TextAnchor.UpperLeft);
            nextButton = CreateButton(content.transform, "NEXT ACT  →", Green, () => manager.NextAct());
            nextButton.GetComponent<LayoutElement>().preferredHeight = 58;
            feedbackPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }

        private Transform answersContainer;
        private GameObject feedbackPanel;

        private void BuildHomeScreen()
        {
            homeScreen = CreatePanel(canvas.transform, "Home Screen", new Vector2(0.14f, 0.17f), new Vector2(0.86f, 0.83f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.04f, 0.10f, 0.96f));
            Text title = CreateText(homeScreen.transform, "MOONIES", 74, FontStyle.Bold, Cyan, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.91f), TextAnchor.MiddleCenter);
            Text subtitle = CreateText(homeScreen.transform, "ARTEMIS LUNAR HORIZON  /  30 ACTS · 8 PHASES", 19, FontStyle.Bold, Ink, new Vector2(0.05f, 0.59f), new Vector2(0.95f, 0.69f), TextAnchor.MiddleCenter);
            Text intro = CreateText(homeScreen.transform,
                "You are part of a lunar mission. Make decisions and manage oxygen, fuel, power, hull, crew, and science. Learn about astronomy and engineering as you work to bring everyone home.",
                20, FontStyle.Normal, Muted, new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.58f), TextAnchor.MiddleCenter);
            homeMessage = CreateText(homeScreen.transform, "Every decision has consequences. A hint costs 10 points; Houston rescue costs 80.", 15, FontStyle.Italic, Gold, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.35f), TextAnchor.MiddleCenter);

            Text nameLabel = CreateText(homeScreen.transform, "CREW CALLSIGN", 13, FontStyle.Bold, Cyan,
                new Vector2(0.18f, 0.16f), new Vector2(0.48f, 0.22f), TextAnchor.MiddleRight);
            GameObject inputObject = CreatePanel(homeScreen.transform, "Crew Name Input", new Vector2(0.50f, 0.155f), new Vector2(0.82f, 0.235f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.12f, 0.21f, 1f));
            crewInput = inputObject.AddComponent<InputField>();
            Text inputText = CreateText(inputObject.transform, "Input Text", 18, FontStyle.Normal, Ink, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAnchor.MiddleLeft);
            Text placeholder = CreateText(inputObject.transform, "Placeholder", 18, FontStyle.Italic, new Color(0.55f, 0.62f, 0.72f), new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAnchor.MiddleLeft);
            placeholder.text = "MOONIES Crew";
            crewInput.textComponent = inputText;
            crewInput.placeholder = placeholder;
            crewInput.characterLimit = 28;

            Button start = CreateButton(homeScreen.transform, "START MISSION  →", new Color(0.18f, 0.72f, 0.73f), StartClicked);
            SetAnchors(start.GetComponent<RectTransform>(), new Vector2(0.32f, 0.045f), new Vector2(0.68f, 0.135f), Vector2.zero, Vector2.zero);
            start.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private void BuildResultsScreen()
        {
            resultsScreen = CreatePanel(canvas.transform, "Results Screen", new Vector2(0.12f, 0.11f), new Vector2(0.88f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.04f, 0.10f, 0.97f));
            resultTitle = CreateText(resultsScreen.transform, "MISSION COMPLETE", 38, FontStyle.Bold, Cyan, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.93f), TextAnchor.MiddleCenter);
            resultRank = CreateText(resultsScreen.transform, "LUNAR COMMANDER", 31, FontStyle.Bold, Gold, new Vector2(0.04f, 0.66f), new Vector2(0.96f, 0.78f), TextAnchor.MiddleCenter);
            resultStats = CreateText(resultsScreen.transform, "Score · Correct · Streak", 20, FontStyle.Bold, Ink, new Vector2(0.05f, 0.53f), new Vector2(0.95f, 0.65f), TextAnchor.MiddleCenter);
            resultSummary = CreateText(resultsScreen.transform, "Summary", 18, FontStyle.Normal, Muted, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.51f), TextAnchor.MiddleCenter);
            resultDebrief = CreateText(resultsScreen.transform, "Debrief", 16, FontStyle.Italic, Ink, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.35f), TextAnchor.MiddleCenter);
            Button replay = CreateButton(resultsScreen.transform, "REPLAY MISSION", new Color(0.18f, 0.72f, 0.73f), StartClicked);
            SetAnchors(replay.GetComponent<RectTransform>(), new Vector2(0.17f, 0.055f), new Vector2(0.47f, 0.15f), Vector2.zero, Vector2.zero);
            replay.GetComponent<LayoutElement>().ignoreLayout = true;
            Button home = CreateButton(resultsScreen.transform, returnToShip != null ? "RETURN TO SHIP" : "BACK TO HOME", new Color(0.12f, 0.2f, 0.34f), RequestReturnToShip);
            SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.53f, 0.055f), new Vector2(0.83f, 0.15f), Vector2.zero, Vector2.zero);
            home.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private Text resultTitle;
        private Text resultRank;
        private Text resultStats;
        private Text resultSummary;
        private Text resultDebrief;

        public void ShowHome()
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            SetVisible(homeScreen, true);
            SetVisible(gameScreen, false);
            SetVisible(resultsScreen, false);
            if (headerStatus != null) headerStatus.text = "MISSION CONTROL  /  STANDING BY";
        }

        public void ShowFatalError(string message)
        {
            if (homeMessage != null) homeMessage.text = message;
            ShowHome();
        }

        private void StartClicked()
        {
            if (manager == null) return;
            manager.StartMission(crewInput != null ? crewInput.text : "MOONIES Crew");
        }

        public void ShowMission(MooniesGameManager game)
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            SetVisible(homeScreen, false);
            SetVisible(resultsScreen, false);
            SetVisible(gameScreen, true);
            MooniesMission mission = game.CurrentMission;
            if (mission == null) return;

            if (headerStatus != null) headerStatus.text = "CREW  /  " + game.CrewName.ToUpperInvariant();
            phaseText.text = "PHASE " + (mission.phase + 1).ToString("00") + "  /  " + mission.phaseName.ToUpperInvariant() + "     •     " + mission.tag;
            missionTitle.text = "ACT " + (game.CurrentIndex + 1).ToString("00") + "  ·  " + mission.title;
            scenarioText.text = mission.scenario;
            questionText.text = mission.question;
            feedbackPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
            hintButton.interactable = true;
            hintButton.GetComponentInChildren<Text>().text = "REQUEST A HINT  (−10 PTS)";
            rescueButton.interactable = !game.RescueUsed;
            rescueButton.GetComponentInChildren<Text>().text = game.RescueUsed ? "HOUSTON RESCUE: USED" : "HOUSTON RESCUE  (−80 PTS)";
            CreateAnswerButtons(mission);
            RefreshHud(game);
            if (game.HasAnswered && game.CurrentAttempt != null)
            {
                MooniesChoice selected = mission.choices[game.CurrentAttempt.selectedOption];
                int bonus = selected.isCorrect && game.CurrentStreak > 0 && game.CurrentStreak % 3 == 0 ? 25 : 0;
                ShowFeedback(selected.isCorrect,selected.why,mission.learn,mission.fact,bonus,
                    selected.isCorrect ? null : selected.damage,game.CurrentAttempt.selectedOption);
            }
            else if (game.HintUsed) ShowHint(mission.hint);
        }

        public void HideForShip()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void RequestReturnToShip()
        {
            if (returnToShip != null) returnToShip();
            else ShowHome();
        }

        private void CreateAnswerButtons(MooniesMission mission)
        {
            for (int i = answersContainer.childCount - 1; i >= 0; i--)
                Destroy(answersContainer.GetChild(i).gameObject);
            answerButtons.Clear();
            answerIndices.Clear();

            List<int> order = new List<int>();
            for (int i = 0; i < mission.choices.Length; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = order[i]; order[i] = order[j]; order[j] = temp;
            }

            for (int shown = 0; shown < order.Count; shown++)
            {
                int originalIndex = order[shown];
                string label = ((char)('A' + shown)) + "    " + mission.choices[originalIndex].text;
                Button button = CreateButton(answersContainer, label, new Color(0.075f, 0.12f, 0.22f, 1f), () => manager.Choose(originalIndex));
                button.GetComponent<LayoutElement>().preferredHeight = 74;
                button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                button.GetComponentInChildren<Text>().fontSize = 17;
                answerButtons.Add(button);
                answerIndices.Add(originalIndex);
            }
        }

        public void ShowFeedback(bool correct, string consequence, string learning, string fact, int bonus, ResourceDelta damage = null, int selectedIndex = -1)
        {
            if (feedbackPanel == null) return;
            feedbackPanel.SetActive(true);
            string impact = "";
            if (damage != null)
            {
                List<string> impacts = new List<string>();
                foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                {
                    int amount = damage.Get(type);
                    if (amount > 0) impacts.Add(MooniesText.ResourceName(type) + " −" + amount + "%");
                }
                if (impacts.Count > 0) impact = "\nResource impact: " + string.Join(" · ", impacts);
            }
            else if (bonus > 0) impact = "\nStreak: +" + bonus + " points.";

            feedbackText.text = (correct ? "CORRECT DECISION\n" : "NOT RECOMMENDED\n") + consequence + impact +
                                "\n\nLEARN\n" + learning +
                                (string.IsNullOrWhiteSpace(fact) ? "" : "\n\nHISTORICAL / SCIENTIFIC FACT\n" + fact);
            feedbackText.color = Ink;
            feedbackPanel.GetComponent<Image>().color = correct ? new Color(0.035f, 0.16f, 0.12f, 1f) : new Color(0.18f, 0.065f, 0.10f, 1f);
            for (int i = 0; i < answerButtons.Count; i++)
            {
                answerButtons[i].interactable = false;
                int original = answerIndices[i];
                bool isCorrect = manager.CurrentMission.choices[original].isCorrect;
                if (original == selectedIndex && !isCorrect)
                    answerButtons[i].GetComponent<Image>().color = new Color(0.42f, 0.10f, 0.15f, 1f);
                else
                    answerButtons[i].GetComponent<Image>().color = isCorrect ? new Color(0.08f, 0.34f, 0.25f, 1f) : new Color(0.055f, 0.075f, 0.12f, 0.85f);
            }
            hintButton.interactable = false;
            nextButton.gameObject.SetActive(true);
            nextButton.GetComponentInChildren<Text>().text = manager.CurrentIndex >= manager.TotalActs - 1 ? "VIEW FINAL DEBRIEF  →" : "NEXT ACT  →";
            Canvas.ForceUpdateCanvases();
        }

        public void ShowHint(string hint)
        {
            feedbackPanel.SetActive(true);
            feedbackPanel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.25f, 1f);
            feedbackText.text = "HOUSTON HINT  (−10 POINTS)\n\n" + hint + "\n\nThe hint does not lock your answers. Choose when you are ready.";
            hintButton.interactable = false;
            hintButton.GetComponentInChildren<Text>().text = "HINT USED";
        }

        public void ShowRescue(ResourceType recovered, bool hasAnswered)
        {
            feedbackPanel.SetActive(true);
            feedbackPanel.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.20f, 1f);
            feedbackText.text = "HOUSTON RESPONDS\n\nContingency protocol activated: " + MooniesText.ResourceName(recovered) + " +22%.\n\nRescue cost: 80 points. The mission continues.";
            rescueButton.interactable = false;
            rescueButton.GetComponentInChildren<Text>().text = "HOUSTON RESCUE: USED";
            if (hasAnswered) nextButton.gameObject.SetActive(true);
        }

        public void RefreshHud(MooniesGameManager game)
        {
            if (headerStatus != null)
                headerStatus.text = "ACT " + (game.CurrentIndex + 1).ToString("00") + " / " + game.TotalActs + "     ·     " + game.Score.ToString("0000") + " PTS";
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                int value = game.Resources.Get(type);
                if (meterFills.TryGetValue(type, out Image fill)) fill.fillAmount = value / 100f;
                if (meterValues.TryGetValue(type, out Text label)) label.text = value + "%";
            }
        }

        public void ShowResults(string rank, string medal, string summary, int score, int correct, int bestStreak, int accuracy, string debrief, MissionResources resources)
        {
            SetVisible(homeScreen, false);
            SetVisible(gameScreen, false);
            SetVisible(resultsScreen, true);
            resultTitle.text = "MISSION COMPLETE  " + medal;
            resultRank.text = rank;
            resultStats.text = "SCORE: " + score + "     ·     CORRECT: " + correct + "/" + manager.AnsweredActs + "     ·     ACCURACY: " + accuracy + "%     ·     BEST STREAK: " + bestStreak;
            resultSummary.text = manager.CrewName + ": " + summary;
            resultDebrief.text = debrief + "\n\nFINAL RESOURCES\n" +
                "Oxygen " + resources.oxygen + "%  ·  Fuel " + resources.fuel + "%  ·  Power " + resources.power + "%\n" +
                "Hull " + resources.hull + "%  ·  Crew " + resources.team + "%  ·  Science " + resources.science + "%";
        }

        private Text AddContentText(Transform parent, string name, int size, FontStyle style, Color color, float preferredHeight)
        {
            Text text = CreateText(parent, name, size, style, color, Vector2.zero, Vector2.one, TextAnchor.MiddleLeft);
            LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = 1;
            return text;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text CreateText(Transform parent, string name, int size, FontStyle style, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.lineSpacing = 1.08f;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject("Button - " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.16f);
            colors.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
            colors.disabledColor = new Color(0.5f, 0.55f, 0.65f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(action);
            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredHeight = 54;
            element.flexibleWidth = 1;
            Text text = CreateText(go.transform, "Label", 16, FontStyle.Bold, Ink,
                new Vector2(0.035f, 0.06f), new Vector2(0.965f, 0.94f), TextAnchor.MiddleCenter);
            text.text = label;
            return button;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null) target.SetActive(visible);
        }
    }
}
