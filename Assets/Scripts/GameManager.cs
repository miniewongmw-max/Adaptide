using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool gameStarted;
    public bool gameOver;
    public Transform player;
    public Camera mainCamera;
    public TMP_Text tapToStartText;

    private PlayerController playerController;
    private TMP_Text scoreText;
    private TMP_Text pearlText;
    private TMP_Text timerText;
    private TMP_Text statusText;
    private TMP_Text powerText;
    private TMP_Text crabText;
    private Image inkCloud;
    private GameObject pauseOverlay;
    private GameObject gameOverOverlay;
    private Button pauseButton;
    private GameObject pearlChip;
    private TMP_Text resultText;
    private float remainingTime = 60f;
    private int score;
    private bool paused;
    private bool shieldReady;
    private float speedDashUntil;
    private float magnetUntil;
    private float invincibleUntil;
    private int crabStep = -1;
    private SeaObstacle crabObstacle;
    private Coroutine statusRoutine;
    private MainMenuBehaviour gameplayHub;
    private bool runPrepared;
    private int tutorialForwardMoves;
    private bool tutorialSideMove;
    private bool tutorialBuffSeen;
    private int tutorialObstaclesSeen;

    public bool HasSpeedDash => gameStarted && Time.time < speedDashUntil;
    public bool HasPearlMagnet => gameStarted && Time.time < magnetUntil;
    public bool IsInvincible => gameStarted && Time.time < invincibleUntil;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        playerController = player != null ? player.GetComponent<PlayerController>() : FindAnyObjectByType<PlayerController>();
        if (player == null && playerController != null) player = playerController.transform;
        if (mainCamera == null) mainCamera = Camera.main;
        BuildGameplayUI();
        ApplyStageAtmosphere();
        GameSession.BeginRun();
        shieldReady = GameSession.ShieldReady;
        UpdateHud();
        gameplayHub = gameObject.AddComponent<MainMenuBehaviour>();
        gameplayHub.BuildOnGameplay();
    }

    private void Update()
    {
        if (!gameStarted || gameOver || paused) return;

        if (GameSession.Mode == FishGameMode.TimeAttack)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                GameOver("Time is up");
            }
        }

        CheckPlayerOutsideCamera();
        UpdateHud();
    }

    private void BuildGameplayUI()
    {
        Canvas canvas = OceanUI.CreateCanvas("Busy Reef Gameplay HUD");
        RectTransform root = OceanUI.SafeRoot(canvas);
        AdaptiveUILayout adaptive = root.gameObject.AddComponent<AdaptiveUILayout>();
        adaptive.scaler = canvas.GetComponent<CanvasScaler>();

        Image gestureSurface = OceanUI.CreatePanel("Swipe Surface", root, Color.clear);
        OceanUI.Stretch(gestureSurface.rectTransform, 0f);
        GameplayGestureInput gestures = gestureSurface.gameObject.AddComponent<GameplayGestureInput>();
        gestures.player = playerController;

        Image pearlPanel = OceanUI.CreatePanel("Pearls", root, new Color(0.02f, 0.20f, 0.29f, 0.86f));
        pearlChip = pearlPanel.gameObject;
        OceanUI.SetRect(pearlPanel.rectTransform, new Vector2(0.025f, 0.90f), new Vector2(0.30f, 0.975f), Vector2.zero, Vector2.zero);
        pearlText = OceanUI.CreateText("PEARL  0", pearlPanel.transform, 36f, OceanUI.Sand, TextAlignmentOptions.Left);
        pearlChip.SetActive(false);

        Image scoreChip = OceanUI.CreatePanel("Score", root, new Color(0.02f, 0.20f, 0.29f, 0.86f));
        OceanUI.SetRect(scoreChip.rectTransform, new Vector2(0.70f, 0.90f), new Vector2(0.975f, 0.975f), Vector2.zero, Vector2.zero);
        scoreText = OceanUI.CreateText("0", scoreChip.transform, 50f, OceanUI.Foam, TextAlignmentOptions.Right);
        timerText = OceanUI.CreateText("", root, 28f, OceanUI.Coral, TextAlignmentOptions.Right);
        OceanUI.SetRect(timerText.rectTransform, new Vector2(0.67f, 0.855f), new Vector2(0.975f, 0.90f), Vector2.zero, Vector2.zero);

        pauseButton = OceanUI.CreateButton("Pause", "PAUSE", root, OceanUI.Panel, TogglePause);
        pauseButton.GetComponentInChildren<TMP_Text>().fontSize = 22f;
        OceanUI.SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(0.835f, 0.79f), new Vector2(0.975f, 0.855f), Vector2.zero, Vector2.zero);
        pauseButton.gameObject.SetActive(false);

        powerText = OceanUI.CreateText("", root, 24f, OceanUI.Foam, TextAlignmentOptions.Left);
        OceanUI.SetRect(powerText.rectTransform, new Vector2(0.03f, 0.84f), new Vector2(0.65f, 0.895f), Vector2.zero, Vector2.zero);

        BuildDpad(root, adaptive);

        statusText = OceanUI.CreateText("", root, 37f, OceanUI.Sand);
        OceanUI.SetRect(statusText.rectTransform, new Vector2(0.10f, 0.63f), new Vector2(0.90f, 0.74f), Vector2.zero, Vector2.zero);

        inkCloud = OceanUI.CreatePanel("Ink Cloud", root, new Color(0.03f, 0.01f, 0.08f, 0.88f));
        OceanUI.Stretch(inkCloud.rectTransform, 0f);
        inkCloud.raycastTarget = false;
        inkCloud.gameObject.SetActive(false);

        pauseOverlay = BuildModal(root, "CURRENT PAUSED", "Take a breath. Your turtle is safe here.", "RESUME", TogglePause);
        AddModalSecondaryButton(pauseOverlay.transform, "BOTTOM MENU", GoToMenu);
        pauseOverlay.SetActive(false);
        gameOverOverlay = BuildResultOverlay(root);
        gameOverOverlay.SetActive(false);

        crabText = OceanUI.CreateText("", root, 43f, OceanUI.Foam);
        OceanUI.SetRect(crabText.rectTransform, new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
        crabText.gameObject.SetActive(false);
    }

    private void BuildDpad(RectTransform root, AdaptiveUILayout adaptive)
    {
        GameObject dpadObject = OceanUI.CreateObject("Touch Direction Pad", root);
        RectTransform dpad = dpadObject.GetComponent<RectTransform>();
        OceanUI.SetCentered(dpad, Vector2.zero, new Vector2(420f, 420f));
        adaptive.dpad = dpad;
        dpadObject.SetActive(GameSession.ShowTouchControls);
        CreateMoveButton(dpad, "^", Vector3.forward, new Vector2(0f, 125f));
        CreateMoveButton(dpad, "v", Vector3.back, new Vector2(0f, -125f));
        CreateMoveButton(dpad, "<", Vector3.left, new Vector2(-125f, 0f));
        CreateMoveButton(dpad, ">", Vector3.right, new Vector2(125f, 0f));
        Image center = OceanUI.CreatePanel("Pad Center", dpad, new Color(0.13f, 0.65f, 0.68f, 0.35f));
        OceanUI.SetCentered(center.rectTransform, Vector2.zero, new Vector2(112f, 112f));
        center.raycastTarget = false;
    }

    private GameObject BuildResultOverlay(RectTransform root)
    {
        GameObject overlay = OceanUI.CreateObject("Dive Result", root);
        OceanUI.Stretch(overlay.GetComponent<RectTransform>(), 0f);
        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.08f, 0.13f, 0.58f);
        resultText = OceanUI.CreateText("", overlay.transform, 54f, OceanUI.Foam);
        OceanUI.SetRect(resultText.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
        Button retry = OceanUI.CreateButton("Retry", "RETRY", overlay.transform, OceanUI.Sand, Restart);
        OceanUI.SetRect(retry.GetComponent<RectTransform>(), new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.22f), Vector2.zero, Vector2.zero);
        Button hub = OceanUI.CreateButton("Hub", "BOTTOM MENU", overlay.transform, OceanUI.Panel, GoToMenu);
        OceanUI.SetRect(hub.GetComponent<RectTransform>(), new Vector2(0.34f, 0.045f), new Vector2(0.66f, 0.105f), Vector2.zero, Vector2.zero);
        hub.GetComponentInChildren<TMP_Text>().fontSize = 24f;
        return overlay;
    }

    private void CreateMoveButton(RectTransform parent, string label, Vector3 direction, Vector2 position)
    {
        Button button = OceanUI.CreateButton("Move " + label, label, parent, new Color(0.24f, 0.86f, 0.82f, 0.88f), null);
        OceanUI.SetCentered(button.GetComponent<RectTransform>(), position, new Vector2(118f, 118f));
        TouchMoveButton input = button.gameObject.AddComponent<TouchMoveButton>();
        input.player = playerController;
        input.direction = direction;
    }

    private GameObject BuildModal(RectTransform root, string titleValue, string bodyValue, string primaryLabel, UnityEngine.Events.UnityAction primaryAction)
    {
        GameObject overlay = OceanUI.CreateObject(titleValue, root);
        OceanUI.Stretch(overlay.GetComponent<RectTransform>(), 0f);
        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.08f, 0.13f, 0.72f);
        Image panel = OceanUI.CreatePanel("Panel", overlay.transform, OceanUI.Panel);
        OceanUI.SetRect(panel.rectTransform, new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.73f), Vector2.zero, Vector2.zero);
        TMP_Text title = OceanUI.CreateText(titleValue, panel.transform, 52f, OceanUI.Sand);
        OceanUI.SetRect(title.rectTransform, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
        TMP_Text body = OceanUI.CreateText(bodyValue, panel.transform, 29f, OceanUI.Foam);
        body.name = "Body";
        OceanUI.SetRect(body.rectTransform, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
        Button primary = OceanUI.CreateButton("Primary", primaryLabel, panel.transform, OceanUI.Aqua, () => primaryAction());
        OceanUI.SetRect(primary.GetComponent<RectTransform>(), new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.29f), Vector2.zero, Vector2.zero);
        return overlay;
    }

    private void AddModalSecondaryButton(Transform overlay, string label, UnityEngine.Events.UnityAction action)
    {
        Transform panel = overlay.Find("Panel");
        Button primary = panel.Find("Primary")?.GetComponent<Button>();
        if (primary != null) OceanUI.SetRect(primary.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.29f), Vector2.zero, Vector2.zero);
        Button secondary = OceanUI.CreateButton("Secondary", label, panel, OceanUI.Coral, () => action());
        OceanUI.SetRect(secondary.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);
        secondary.GetComponentInChildren<TMP_Text>().fontSize = 28f;
    }

    private string StartDescription()
    {
        string mode = GameSession.Mode switch
        {
            FishGameMode.Tutorial => "TUTORIAL | Slow current\nSwipe or use the arrows to move one tile. Avoid sea creatures and keep ahead of the camera.",
            FishGameMode.TimeAttack => "TIME ATTACK | 60 seconds\nMove forward quickly. Pearls are currency; starfish add bonus score.",
            _ => "STANDARD | Endless reef\nSurvive, travel farther, collect pearls and beat your best score."
        };
        return $"{GameSession.StageNames[Mathf.Clamp(GameSession.SelectedStage, 0, 2)]}\n\n{mode}";
    }

    private void ApplyStageAtmosphere()
    {
        if (mainCamera == null) return;
        Color[] colors =
        {
            new Color32(23, 139, 157, 255),
            new Color32(8, 91, 119, 255),
            new Color32(3, 25, 55, 255)
        };
        mainCamera.backgroundColor = colors[Mathf.Clamp(GameSession.SelectedStage, 0, colors.Length - 1)];
    }

    public void PreviewSelectedStage()
    {
        if (gameStarted || gameOver) return;
        ApplyStageAtmosphere();
        FindAnyObjectByType<MapManager>()?.ResetForSelectedRun();
        FindAnyObjectByType<CameraController>()?.PrepareForSelectedRun();
    }

    public void TryStartFromTap()
    {
        if (gameplayHub != null && !gameplayHub.IsPlayPage) return;
        StartGame();
    }

    public void StartGame()
    {
        if (gameStarted || gameOver) return;
        if (!runPrepared)
        {
            runPrepared = true;
            FindAnyObjectByType<MapManager>()?.ResetForSelectedRun();
            FindAnyObjectByType<CameraController>()?.PrepareForSelectedRun();
            playerController?.RefreshCharacter();
            ApplyStageAtmosphere();
        }
        gameStarted = true;
        gameOver = false;
        paused = false;
        gameplayHub?.HideGameplayHub();
        if (pearlChip != null) pearlChip.SetActive(true);
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        if (tapToStartText != null) tapToStartText.gameObject.SetActive(false);
        if (GameSession.SpeedDashReady) speedDashUntil = Time.time + 18f;
        if (GameSession.PearlMagnetReady) magnetUntil = Time.time + 20f;
        if (GameSession.InvincibilityReady) invincibleUntil = Time.time + 10f;
        ShowStatus(GameSession.Mode == FishGameMode.Tutorial ? "STEP 1  TAP TO GO FORWARD - SWIPE TO TURN" : "GO!", 3.2f);
    }

    public void AddScore(int amount)
    {
        if (!gameStarted || gameOver) return;
        score += Mathf.Max(0, amount);
        UpdateHud();
    }

    public void CollectPearls(int amount)
    {
        GameSession.CollectPearl(amount);
        ShowStatus(amount > 1 ? $"TREASURE! +{amount} PEARLS" : "+1 PEARL", 0.8f);
        UpdateHud();
    }

    public void GrantPowerUp(int index)
    {
        GameSession.GrantPowerUp(index);
        switch (index)
        {
            case 0: shieldReady = true; ShowStatus("BUBBLE SHIELD READY", 1.2f); break;
            case 1: speedDashUntil = Mathf.Max(speedDashUntil, Time.time) + 12f; ShowStatus("SPEED DASH", 1.2f); break;
            case 2: magnetUntil = Mathf.Max(magnetUntil, Time.time) + 15f; ShowStatus("PEARL MAGNET", 1.2f); break;
            case 3: invincibleUntil = Mathf.Max(invincibleUntil, Time.time) + 8f; ShowStatus("INVINCIBLE BUBBLE", 1.2f); break;
        }
        if (GameSession.Mode == FishGameMode.Tutorial && !tutorialBuffSeen)
        {
            tutorialBuffSeen = true;
            ShowStatus("STEP 2  BUFFS HELP: SHIELD, DASH, MAGNET, INVINCIBLE", 4f);
        }
    }

    public void NotifyPlayerMoved(Vector3 direction)
    {
        if (GameSession.Mode != FishGameMode.Tutorial || !gameStarted) return;
        if (direction.z > 0f) tutorialForwardMoves++;
        if (Mathf.Abs(direction.x) > 0f) tutorialSideMove = true;
        if (tutorialForwardMoves == 3 && !tutorialSideMove)
            ShowStatus("NOW SWIPE LEFT OR RIGHT TO CHANGE LANE", 2.8f);
        else if (tutorialForwardMoves >= 3 && tutorialSideMove && !tutorialBuffSeen)
            ShowStatus("STEP 2  FOLLOW THE PEARL PATTERN TO THE BUBBLE BUFF", 3.2f);
    }

    public void NotifyObstacleEncountered(SeaObstacleType type)
    {
        if (GameSession.Mode != FishGameMode.Tutorial) return;
        tutorialObstaclesSeen++;
        string effect = type switch
        {
            SeaObstacleType.Coral => "CORAL BLOCKS THE LANE - MOVE AROUND IT",
            SeaObstacleType.Squid => "SQUID INK BLOCKS YOUR VIEW",
            SeaObstacleType.Jellyfish => "JELLYFISH STUNS YOU",
            SeaObstacleType.Pufferfish => "PUFFERFISH BLOCKS A LANE",
            SeaObstacleType.Crab => "CRAB: TAP, TAP, THEN SWIPE",
            _ => "SHARKS END THE RUN"
        };
        ShowStatus($"STEP 3  {effect}", 3f);
        if (tutorialObstaclesSeen >= 4)
        {
            GameSession.MarkTutorialComplete();
            ShowStatus("TUTORIAL COMPLETE - KEEP MOVING AND BEAT YOUR SCORE!", 4f);
        }
    }

    public bool TryUseShield()
    {
        if (!shieldReady) return false;
        shieldReady = false;
        ShowStatus("BUBBLE SHIELD SAVED YOU!", 1.4f);
        return true;
    }

    public void ShowInkCloud()
    {
        if (IsInvincible) return;
        StartCoroutine(InkRoutine());
    }

    private IEnumerator InkRoutine()
    {
        inkCloud.gameObject.SetActive(true);
        ShowStatus("SQUID INK!", 1f);
        yield return new WaitForSeconds(2.4f);
        inkCloud.gameObject.SetActive(false);
    }

    public void BeginCrabEscape(PlayerController trappedPlayer, SeaObstacle obstacle)
    {
        if (crabStep >= 0) return;
        crabStep = 0;
        crabObstacle = obstacle;
        trappedPlayer.SetInputLocked(true);
        crabText.gameObject.SetActive(true);
        crabText.text = "CRAB GRAB!\nTAP | TAP | SWIPE\nTAP NOW";
    }

    public bool RegisterEscapeInput(bool tap)
    {
        if (crabStep < 0) return false;
        bool expectedTap = crabStep < 2;
        if (tap == expectedTap) crabStep++;
        else crabStep = 0;

        if (crabStep >= 3)
        {
            crabStep = -1;
            crabText.gameObject.SetActive(false);
            playerController?.SetInputLocked(false);
            if (crabObstacle != null) Destroy(crabObstacle.gameObject);
            ShowStatus("ESCAPED!", 1f);
        }
        else
        {
            crabText.text = crabStep == 0 ? "CRAB GRAB!\nTAP | TAP | SWIPE\nTAP NOW" : crabStep == 1 ? "GOOD!\nTAP AGAIN" : "NOW SWIPE!";
        }
        return true;
    }

    public void ShowStatus(string message, float duration)
    {
        if (statusRoutine != null) StopCoroutine(statusRoutine);
        statusRoutine = StartCoroutine(StatusRoutine(message, duration));
    }

    private IEnumerator StatusRoutine(string message, float duration)
    {
        statusText.text = message;
        yield return new WaitForSecondsRealtime(duration);
        statusText.text = "";
        statusRoutine = null;
    }

    private void UpdateHud()
    {
        if (scoreText == null) return;
        scoreText.text = score.ToString();
        if (pearlText != null) pearlText.text = $"PEARL  {GameSession.PearlWallet + GameSession.RunPearls}";
        timerText.text = GameSession.Mode == FishGameMode.TimeAttack ? $"TIME  {Mathf.CeilToInt(remainingTime)}" : "";
        string powers = "";
        if (shieldReady) powers += "SHIELD  ";
        if (HasSpeedDash) powers += "DASH  ";
        if (HasPearlMagnet) powers += "MAGNET  ";
        if (IsInvincible) powers += "INVINCIBLE";
        powerText.text = powers;
    }

    private void CheckPlayerOutsideCamera()
    {
        if (player == null || mainCamera == null) return;
        Vector3 viewport = mainCamera.WorldToViewportPoint(player.position);
        if (viewport.y < -0.03f) GameOver("Left behind by the current");
    }

    public void GameOver() => GameOver("The reef got too busy");

    public void GameOver(string reason)
    {
        if (gameOver) return;
        gameOver = true;
        gameStarted = false;
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        if (pearlChip != null) pearlChip.SetActive(false);
        playerController?.SetInputLocked(true);
        GameSession.BankRunPearls();
        if (GameSession.Mode == FishGameMode.Tutorial) GameSession.MarkTutorialComplete();
        string key = $"Fishfish.HighScore.{GameSession.Mode}";
        int best = Mathf.Max(score, PlayerPrefs.GetInt(key, 0));
        PlayerPrefs.SetInt(key, best);
        PlayerPrefs.Save();
        resultText.text = $"{reason.ToUpperInvariant()}\n\nSCORE\n{score}\n\nBEST  {best}     PEARLS  {GameSession.PearlWallet}";
        gameOverOverlay.SetActive(true);
    }

    private void TogglePause()
    {
        if (gameOver || !gameStarted) return;
        paused = !paused;
        pauseOverlay.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }
}
