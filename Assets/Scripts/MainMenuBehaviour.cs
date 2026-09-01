using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuBehaviour : MonoBehaviour
{
    public GameObject shopMenu, settingsMenu, modeSelectionMenu;
    private readonly List<RectTransform> pages = new List<RectTransform>();
    private readonly List<Image> navTiles = new List<Image>();
    private RectTransform pageArea;
    private TMP_Text shopWalletText, homeText, stageTitle, stageDescription, feedback, characterFeedback, touchLabel;
    private GameObject powerShop, characterShop;
    private StageCarousel3D carousel;
    private Button stagePrevious, stageNext;
    private Coroutine slideRoutine;
    private int currentPage = 2;
    private RectTransform navigationBar;
    private RectTransform navigationBleed;
    private Image fullPageBackdrop;
    private bool lastLandscape;
    private int slideFromPage = 2;
    private bool built;
    private bool gameplayHost;
    private Canvas hubCanvas;
    private const int SealCost = 120;
    public bool IsPlayPage => currentPage == 2;
    private static readonly string[] PowerNames = { "Bubble Shield", "Speed Dash", "Pearl Magnet", "Invincibility" };
    private static readonly string[] PowerInfo = { "Blocks one obstacle", "Moves forward twice", "Pulls nearby pearls", "Ignores hazards briefly" };
    private static readonly int[] PowerCosts = { 15, 10, 50, 80 };

    private void Start()
    {
        if (built) return;
        Time.timeScale = 1f;
        BuildHub(false);
    }

    public void BuildOnGameplay()
    {
        if (built) return;
        gameplayHost = true;
        BuildHub(true);
    }

    private void BuildHub(bool overGameplay)
    {
        built = true;
        Canvas canvas = OceanUI.CreateCanvas("Busy Reef Bottom Hub", !overGameplay);
        hubCanvas = canvas;
        if (overGameplay) canvas.sortingOrder = 60;
        RectTransform root = OceanUI.SafeRoot(canvas);
        if (!overGameplay) OceanUI.AddOceanBackground(root);
        AdaptiveUILayout adaptive = root.gameObject.AddComponent<AdaptiveUILayout>();
        adaptive.scaler = canvas.GetComponent<CanvasScaler>();
        GameObject backdropObject = OceanUI.CreateObject("Full Screen Page Backdrop", canvas.transform);
        fullPageBackdrop = backdropObject.AddComponent<Image>();
        fullPageBackdrop.raycastTarget = false;
        OceanUI.Stretch(backdropObject.GetComponent<RectTransform>(), 0f);
        backdropObject.transform.SetAsFirstSibling();

        pageArea = OceanUI.CreateObject("Sliding Pages", root).GetComponent<RectTransform>();
        OceanUI.SetRect(pageArea, new Vector2(0f, 0.145f), new Vector2(1f, 0.91f), Vector2.zero, Vector2.zero);
        pageArea.gameObject.AddComponent<RectMask2D>();
        pages.Add(BuildStagePage());
        pages.Add(BuildShopPage());
        pages.Add(BuildHomePage());
        pages.Add(BuildCharacterPage());
        pages.Add(BuildSettingsPage());
        adaptive.carousel = carousel;
        BuildNavigation(root);
        Canvas.ForceUpdateCanvases();
        lastLandscape = Screen.width > Screen.height;
        ApplyHubOrientation();
        PositionPages();
        SetRestingPageVisibility();
        RefreshAll();
    }

    private void Update()
    {
        if (!built) return;
        bool landscape = Screen.width > Screen.height;
        if (landscape == lastLandscape) return;
        lastLandscape = landscape;
        ApplyHubOrientation();
        Canvas.ForceUpdateCanvases();
        PositionPages();
    }

    private RectTransform Page(string name)
    {
        RectTransform page = OceanUI.CreateObject(name, pageArea).GetComponent<RectTransform>();
        OceanUI.Stretch(page, 0f);
        return page;
    }

    private void AddTitle(Transform parent, string title, string subtitle)
    {
        TMP_Text a = OceanUI.CreateText(title, parent, 52f, OceanUI.Sand);
        TMP_Text b = OceanUI.CreateText(subtitle, parent, 25f, OceanUI.Muted);
        OceanUI.SetRect(a.rectTransform, new Vector2(.06f, .86f), new Vector2(.94f, .99f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(b.rectTransform, new Vector2(.06f, .79f), new Vector2(.94f, .88f), Vector2.zero, Vector2.zero);
    }

    private RectTransform BuildHomePage()
    {
        RectTransform page = Page("Play Page");
        TMP_Text title = OceanUI.CreateText("BUSY REEF", page, 76f, OceanUI.Sand);
        OceanUI.SetRect(title.rectTransform, new Vector2(.06f, .68f), new Vector2(.94f, .92f), Vector2.zero, Vector2.zero);
        TMP_Text sub = OceanUI.CreateText("CROSS THE OCEAN. FIND YOUR FAMILY.", page, 31f, OceanUI.Foam);
        OceanUI.SetRect(sub.rectTransform, new Vector2(.08f, .58f), new Vector2(.92f, .70f), Vector2.zero, Vector2.zero);
        homeText = OceanUI.CreateText("", page, 28f, OceanUI.Muted);
        OceanUI.SetRect(homeText.rectTransform, new Vector2(.08f, .40f), new Vector2(.92f, .54f), Vector2.zero, Vector2.zero);
        TMP_Text begin = OceanUI.CreateText("TOUCH ANYWHERE TO BEGIN", page, 43f, OceanUI.Sand);
        OceanUI.SetRect(begin.rectTransform, new Vector2(.12f, .18f), new Vector2(.88f, .32f), Vector2.zero, Vector2.zero);
        return page;
    }

    private RectTransform BuildStagePage()
    {
        RectTransform page = Page("Scene Selection Page");
        AddOpaquePageBackground(page, new Color32(3, 45, 66, 255));
        AddTitle(page, "SCENE & MODE", "Choose a reef, then choose how to play");
        GameObject holder = OceanUI.CreateObject("3D Circular Carousel", page);
        OceanUI.SetRect(holder.GetComponent<RectTransform>(), new Vector2(.06f, .34f), new Vector2(.94f, .76f), Vector2.zero, Vector2.zero);
        carousel = holder.AddComponent<StageCarousel3D>();
        RectTransform[] cards = new RectTransform[3];
        Color[] colors = { OceanUI.Aqua, OceanUI.Sand, new Color32(76, 116, 177, 255) };
        for (int i = 0; i < 3; i++)
        {
            Image card = OceanUI.CreatePanel(GameSession.StageNames[i], holder.transform, colors[i]);
            card.gameObject.AddComponent<Button>();
            cards[i] = card.rectTransform;
            OceanUI.SetCentered(cards[i], Vector2.zero, new Vector2(430f, 270f));
            TMP_Text name = OceanUI.CreateText(GameSession.StageNames[i].ToUpperInvariant(), card.transform, 39f, OceanUI.Deep);
            OceanUI.SetRect(name.rectTransform, new Vector2(.06f, .15f), new Vector2(.94f, .85f), Vector2.zero, Vector2.zero);
        }
        carousel.Configure(cards, GameSession.SelectedStage);
        carousel.SelectionChanged = i =>
        {
            GameSession.SelectedStage = i;
            GameManager.Instance?.PreviewSelectedStage();
            RefreshAll();
        };
        stagePrevious = OceanUI.CreateButton("Previous", "<", page, OceanUI.Panel, carousel.Previous);
        stageNext = OceanUI.CreateButton("Next", ">", page, OceanUI.Panel, carousel.Next);
        OceanUI.SetRect(stagePrevious.GetComponent<RectTransform>(), new Vector2(.02f, .47f), new Vector2(.14f, .61f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(stageNext.GetComponent<RectTransform>(), new Vector2(.86f, .47f), new Vector2(.98f, .61f), Vector2.zero, Vector2.zero);
        stageTitle = OceanUI.CreateText("", page, 31f, OceanUI.Foam);
        stageDescription = OceanUI.CreateText("", page, 23f, OceanUI.Muted);
        OceanUI.SetRect(stageTitle.rectTransform, new Vector2(.08f, .27f), new Vector2(.92f, .35f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(stageDescription.rectTransform, new Vector2(.08f, .19f), new Vector2(.92f, .28f), Vector2.zero, Vector2.zero);
        ModeButton(page, "TUTORIAL", FishGameMode.Tutorial, .04f, OceanUI.Aqua);
        ModeButton(page, "STANDARD", FishGameMode.Standard, .35f, OceanUI.Sand);
        ModeButton(page, "TIME ATTACK", FishGameMode.TimeAttack, .66f, OceanUI.Coral);
        return page;
    }

    private void ModeButton(Transform parent, string label, FishGameMode mode, float x, Color color)
    {
        Button b = OceanUI.CreateButton(label, label, parent, color, () =>
        {
            GameSession.Mode = mode;
            if (mode == FishGameMode.Tutorial) GameSession.SelectedStage = 0;
            GameManager.Instance?.PreviewSelectedStage();
            RefreshAll();
        });
        OceanUI.SetRect(b.GetComponent<RectTransform>(), new Vector2(x, .05f), new Vector2(x + .29f, .15f), Vector2.zero, Vector2.zero);
        b.GetComponentInChildren<TMP_Text>().fontSize = 23f;
    }

    private RectTransform BuildShopPage()
    {
        RectTransform page = Page("Shop Page");
        AddOpaquePageBackground(page, new Color32(3, 45, 66, 255));
        AddTitle(page, "PEARL SHOP", "Power-ups and characters have separate categories");
        Button powers = OceanUI.CreateButton("Power Category", "POWER-UPS", page, OceanUI.Aqua, () => ShopCategory(true));
        Button chars = OceanUI.CreateButton("Character Category", "CHARACTERS", page, OceanUI.Sand, () => ShopCategory(false));
        OceanUI.SetRect(powers.GetComponent<RectTransform>(), new Vector2(.10f, .63f), new Vector2(.49f, .72f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(chars.GetComponent<RectTransform>(), new Vector2(.51f, .63f), new Vector2(.90f, .72f), Vector2.zero, Vector2.zero);
        Image shopWallet = OceanUI.CreatePanel("Shop Wallet", page, new Color(0.02f, 0.20f, 0.29f, 0.96f));
        OceanUI.SetRect(shopWallet.rectTransform, new Vector2(.67f, .74f), new Vector2(.93f, .82f), Vector2.zero, Vector2.zero);
        shopWalletText = OceanUI.CreateText("", shopWallet.transform, 28f, OceanUI.Sand, TextAlignmentOptions.Right);
        powerShop = OceanUI.CreateObject("Power-up Stock", page);
        OceanUI.SetRect(powerShop.GetComponent<RectTransform>(), new Vector2(.04f, .12f), new Vector2(.96f, .61f), Vector2.zero, Vector2.zero);
        for (int i = 0; i < 4; i++) BuildPowerCard(powerShop.transform, i);
        characterShop = OceanUI.CreateObject("Character Stock", page);
        OceanUI.SetRect(characterShop.GetComponent<RectTransform>(), new Vector2(.04f, .12f), new Vector2(.96f, .61f), Vector2.zero, Vector2.zero);
        Image seal = OceanUI.CreatePanel("Seal", characterShop.transform, new Color(.08f, .42f, .51f, .96f));
        OceanUI.SetRect(seal.rectTransform, new Vector2(.16f, .18f), new Vector2(.84f, .86f), Vector2.zero, Vector2.zero);
        TMP_Text sealName = OceanUI.CreateText("SEAL", seal.transform, 54f, OceanUI.Sand);
        OceanUI.SetRect(sealName.rectTransform, new Vector2(.05f, .61f), new Vector2(.95f, .92f), Vector2.zero, Vector2.zero);
        TMP_Text sealInfo = OceanUI.CreateText("A playful new ocean explorer", seal.transform, 28f, OceanUI.Foam);
        OceanUI.SetRect(sealInfo.rectTransform, new Vector2(.08f, .37f), new Vector2(.92f, .64f), Vector2.zero, Vector2.zero);
        Button buy = OceanUI.CreateButton("Buy Seal", $"BUY  {SealCost} PEARLS", seal.transform, OceanUI.Sand, BuySeal);
        OceanUI.SetRect(buy.GetComponent<RectTransform>(), new Vector2(.17f, .08f), new Vector2(.83f, .32f), Vector2.zero, Vector2.zero);
        feedback = OceanUI.CreateText("", page, 24f, OceanUI.Foam);
        OceanUI.SetRect(feedback.rectTransform, new Vector2(.08f, .04f), new Vector2(.92f, .13f), Vector2.zero, Vector2.zero);
        ShopCategory(true);
        return page;
    }

    private void BuildPowerCard(Transform parent, int i)
    {
        float x = i % 2 == 0 ? 0f : .51f, y = i < 2 ? .52f : .03f;
        Image card = OceanUI.CreatePanel(PowerNames[i], parent, new Color(.04f, .35f, .44f, .96f));
        OceanUI.SetRect(card.rectTransform, new Vector2(x, y), new Vector2(x + .49f, y + .45f), Vector2.zero, Vector2.zero);
        TMP_Text title = OceanUI.CreateText(PowerNames[i].ToUpperInvariant(), card.transform, 27f, OceanUI.Sand);
        TMP_Text info = OceanUI.CreateText(PowerInfo[i], card.transform, 21f, OceanUI.Foam);
        OceanUI.SetRect(title.rectTransform, new Vector2(.04f, .62f), new Vector2(.96f, .94f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(info.rectTransform, new Vector2(.06f, .30f), new Vector2(.94f, .65f), Vector2.zero, Vector2.zero);
        int item = i;
        Button buy = OceanUI.CreateButton("Buy", $"{PowerCosts[i]} PEARLS", card.transform, OceanUI.Aqua, () => BuyPower(item));
        OceanUI.SetRect(buy.GetComponent<RectTransform>(), new Vector2(.12f, .05f), new Vector2(.88f, .28f), Vector2.zero, Vector2.zero);
        buy.GetComponentInChildren<TMP_Text>().fontSize = 22f;
    }

    private RectTransform BuildCharacterPage()
    {
        RectTransform page = Page("Character Selection Page");
        AddOpaquePageBackground(page, new Color32(4, 52, 72, 255));
        AddTitle(page, "CHARACTERS", "Select who crosses the Busy Reef");
        CharacterCard(page, "TURTLE", "DEFAULT", 0, .07f, OceanUI.Aqua);
        CharacterCard(page, "SEAL", $"{SealCost} PEARLS", 1, .52f, OceanUI.Sand);
        characterFeedback = OceanUI.CreateText("", page, 29f, OceanUI.Foam);
        OceanUI.SetRect(characterFeedback.rectTransform, new Vector2(.08f, .15f), new Vector2(.92f, .28f), Vector2.zero, Vector2.zero);
        return page;
    }

    private void CharacterCard(Transform parent, string name, string note, int index, float x, Color color)
    {
        Image card = OceanUI.CreatePanel(name, parent, new Color(.04f, .31f, .41f, .96f));
        OceanUI.SetRect(card.rectTransform, new Vector2(x, .35f), new Vector2(x + .41f, .73f), Vector2.zero, Vector2.zero);
        TMP_Text icon = OceanUI.CreateText(index == 0 ? "T" : "S", card.transform, 78f, color);
        TMP_Text title = OceanUI.CreateText(name, card.transform, 32f, OceanUI.Foam);
        OceanUI.SetRect(icon.rectTransform, new Vector2(.1f, .43f), new Vector2(.9f, .94f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(title.rectTransform, new Vector2(.05f, .28f), new Vector2(.95f, .50f), Vector2.zero, Vector2.zero);
        Button choose = OceanUI.CreateButton("Choose", note, card.transform, color, () => SelectCharacter(index));
        OceanUI.SetRect(choose.GetComponent<RectTransform>(), new Vector2(.1f, .05f), new Vector2(.9f, .27f), Vector2.zero, Vector2.zero);
        choose.GetComponentInChildren<TMP_Text>().fontSize = 22f;
    }

    private RectTransform BuildSettingsPage()
    {
        RectTransform page = Page("Settings Page");
        AddOpaquePageBackground(page, new Color32(3, 39, 61, 255));
        AddTitle(page, "SETTINGS", "Controls adapt to portrait and landscape");
        Setting(page, "SOUND", "ON / OFF", .62f, () => AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f);
        Button touch = Setting(page, "TOUCH PAD", "", .45f, ToggleTouch);
        touchLabel = touch.GetComponentInChildren<TMP_Text>();
        TMP_Text note = OceanUI.CreateText("Tap anywhere to move forward. Swipe for a direction. The touch pad is OFF by default in desktop EXE builds.", page, 29f, OceanUI.Muted);
        OceanUI.SetRect(note.rectTransform, new Vector2(.10f, .18f), new Vector2(.90f, .39f), Vector2.zero, Vector2.zero);
        return page;
    }

    private Button Setting(Transform parent, string label, string value, float y, Action action)
    {
        TMP_Text name = OceanUI.CreateText(label, parent, 36f, OceanUI.Foam, TextAlignmentOptions.Left);
        OceanUI.SetRect(name.rectTransform, new Vector2(.12f, y), new Vector2(.48f, y + .11f), Vector2.zero, Vector2.zero);
        Button b = OceanUI.CreateButton(label + " Toggle", value, parent, OceanUI.Aqua, action);
        OceanUI.SetRect(b.GetComponent<RectTransform>(), new Vector2(.53f, y), new Vector2(.87f, y + .11f), Vector2.zero, Vector2.zero);
        return b;
    }

    private void BuildNavigation(RectTransform root)
    {
        GameObject bleedObject = OceanUI.CreateObject("Navigation Edge Fill", hubCanvas.transform);
        Color navigationColor = new Color32(3, 26, 41, 255);
        Image bleedImage = bleedObject.AddComponent<Image>();
        bleedImage.color = navigationColor;
        bleedImage.raycastTarget = false;
        navigationBleed = bleedObject.GetComponent<RectTransform>();
        bleedObject.transform.SetSiblingIndex(1);

        Image bar = OceanUI.CreatePanel("Five Button Navigation", root, navigationColor);
        bar.sprite = null;
        bar.type = Image.Type.Simple;
        bar.color = Color.clear;
        navigationBar = bar.rectTransform;
        OceanUI.SetRect(bar.rectTransform, new Vector2(.015f, .015f), new Vector2(.985f, .14f), Vector2.zero, Vector2.zero);
        string[] labels = { "SCENE", "SHOP", "PLAY", "CHARACTER", "SETTINGS" };
        for (int i = 0; i < 5; i++)
        {
            int page = i;
            Button b = OceanUI.CreateButton(labels[i], labels[i], bar.transform, i == 2 ? OceanUI.Aqua : OceanUI.Panel, () => Navigate(page));
            float x0 = i / 5f + .008f, x1 = (i + 1) / 5f - .008f;
            OceanUI.SetRect(b.GetComponent<RectTransform>(), new Vector2(x0, .10f), new Vector2(x1, .94f), Vector2.zero, Vector2.zero);
            b.GetComponentInChildren<TMP_Text>().fontSize = i == 3 ? 18f : 22f;
            navTiles.Add(b.GetComponent<Image>());
        }
    }

    private void Navigate(int index)
    {
        if (index == currentPage) return;
        slideFromPage = currentPage;
        currentPage = index;
        for (int i = 0; i < pages.Count; i++) pages[i].gameObject.SetActive(i == slideFromPage || i == currentPage);
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(Slide());
        RefreshAll();
    }

    private IEnumerator Slide()
    {
        bool landscape = Screen.width > Screen.height;
        float distance = Mathf.Max(1f, landscape ? pageArea.rect.height : pageArea.rect.width), elapsed = 0f;
        Vector2[] start = new Vector2[pages.Count];
        for (int i = 0; i < pages.Count; i++) start[i] = pages[i].anchoredPosition;
        while (elapsed < .26f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / .26f), 3f);
            for (int i = 0; i < pages.Count; i++)
            {
                Vector2 target = landscape ? new Vector2(0f, (currentPage - i) * distance) : new Vector2((i - currentPage) * distance, 0f);
                pages[i].anchoredPosition = Vector2.Lerp(start[i], target, t);
            }
            yield return null;
        }
        PositionPages();
        SetRestingPageVisibility();
        slideRoutine = null;
    }

    private void PositionPages()
    {
        bool landscape = Screen.width > Screen.height;
        float distance = Mathf.Max(1f, landscape ? pageArea.rect.height : pageArea.rect.width);
        for (int i = 0; i < pages.Count; i++)
            pages[i].anchoredPosition = landscape ? new Vector2(0f, (currentPage - i) * distance) : new Vector2((i - currentPage) * distance, 0f);
    }

    private void SetRestingPageVisibility()
    {
        for (int i = 0; i < pages.Count; i++) pages[i].gameObject.SetActive(i == currentPage);
    }

    private void ApplyHubOrientation()
    {
        if (navigationBar == null) return;
        bool landscape = Screen.width > Screen.height;
        if (landscape)
        {
            float boundary = LandscapeContentBoundary();
            OceanUI.SetRect(navigationBleed, new Vector2(0f, 0f), new Vector2(boundary, 1f), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(fullPageBackdrop.rectTransform, new Vector2(boundary, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(navigationBar, new Vector2(0f, .10f), new Vector2(.135f, .89f), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(pageArea, new Vector2(.14f, .02f), new Vector2(.995f, .91f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < navTiles.Count; i++)
            {
                float yMin = 1f - (i + 1) / 5f + .025f;
                float yMax = 1f - i / 5f - .025f;
                OceanUI.SetRect(navTiles[i].rectTransform, new Vector2(.07f, yMin), new Vector2(.93f, yMax), Vector2.zero, Vector2.zero);
            }
        }
        else
        {
            float boundary = PortraitContentBoundary();
            OceanUI.SetRect(navigationBleed, new Vector2(0f, 0f), new Vector2(1f, boundary), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(fullPageBackdrop.rectTransform, new Vector2(0f, boundary), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(navigationBar, new Vector2(0f, 0f), new Vector2(1f, .14f), Vector2.zero, Vector2.zero);
            OceanUI.SetRect(pageArea, new Vector2(0f, .145f), new Vector2(1f, .91f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < navTiles.Count; i++)
            {
                float xMin = i / 5f + .008f;
                float xMax = (i + 1) / 5f - .008f;
                OceanUI.SetRect(navTiles[i].rectTransform, new Vector2(xMin, .10f), new Vector2(xMax, .94f), Vector2.zero, Vector2.zero);
            }
        }
    }

    private float LandscapeContentBoundary()
    {
        Rect safe = Screen.safeArea;
        if (Screen.width <= 0) return .15f;
        return Mathf.Clamp01((safe.xMin + safe.width * .14f) / Screen.width);
    }

    private float PortraitContentBoundary()
    {
        Rect safe = Screen.safeArea;
        if (Screen.height <= 0) return .16f;
        return Mathf.Clamp01((safe.yMin + safe.height * .145f) / Screen.height);
    }

    private void AddOpaquePageBackground(RectTransform page, Color color)
    {
        Image background = OceanUI.CreatePanel("Opaque Page Background", page, color);
        // Full-page color is rendered once at canvas level. This transparent
        // raycast layer keeps the page interactive without double-color overlap.
        background.color = Color.clear;
        OceanUI.Stretch(background.rectTransform, 0f);
        background.transform.SetAsFirstSibling();
    }

    private void ShopCategory(bool powers)
    {
        if (powerShop != null) powerShop.SetActive(powers);
        if (characterShop != null) characterShop.SetActive(!powers);
        if (feedback != null) feedback.text = powers ? "POWER-UPS ARE USED ON A FUTURE DIVE" : "THE SEAL STAYS UNLOCKED FOREVER";
    }

    private void BuyPower(int i)
    {
        feedback.text = GameSession.BuyPowerUp(i, PowerCosts[i]) ? PowerNames[i].ToUpperInvariant() + " ADDED" : "NOT ENOUGH PEARLS";
        RefreshAll();
    }

    private void BuySeal()
    {
        bool bought = GameSession.BuySkin(1, SealCost);
        if (bought) GameSession.EquipSkin(1);
        if (bought) FindAnyObjectByType<PlayerController>()?.RefreshCharacter();
        feedback.text = bought ? "SEAL UNLOCKED AND SELECTED" : "NOT ENOUGH PEARLS";
        RefreshAll();
    }

    private void SelectCharacter(int index)
    {
        if (!GameSession.OwnsSkin(index)) { characterFeedback.text = $"BUY THE SEAL IN SHOP FOR {SealCost} PEARLS"; return; }
        GameSession.EquipSkin(index);
        FindAnyObjectByType<PlayerController>()?.RefreshCharacter();
        characterFeedback.text = GameSession.EquippedCharacterName.ToUpperInvariant() + " SELECTED";
        RefreshAll();
    }

    private void ToggleTouch() { GameSession.ShowTouchControls = !GameSession.ShowTouchControls; RefreshAll(); }

    private void RefreshAll()
    {
        if (fullPageBackdrop != null)
        {
            bool opaquePage = currentPage == 0 || currentPage == 1 || currentPage == 3 || currentPage == 4;
            fullPageBackdrop.gameObject.SetActive(opaquePage);
            fullPageBackdrop.color = currentPage == 0 || currentPage == 1 ? new Color32(3, 45, 66, 255) :
                currentPage == 3 ? new Color32(4, 52, 72, 255) : new Color32(3, 39, 61, 255);
        }
        RefreshWallet();
        if (homeText != null) homeText.text = $"{GameSession.Mode.ToString().ToUpperInvariant()}  |  {GameSession.StageNames[GameSession.SelectedStage].ToUpperInvariant()}\n{GameSession.EquippedCharacterName.ToUpperInvariant()} SELECTED";
        if (touchLabel != null) touchLabel.text = GameSession.ShowTouchControls ? "ON" : "OFF";
        if (characterFeedback != null && string.IsNullOrEmpty(characterFeedback.text)) characterFeedback.text = "CURRENT: " + GameSession.EquippedCharacterName.ToUpperInvariant();
        for (int i = 0; i < navTiles.Count; i++) navTiles[i].color = i == currentPage ? OceanUI.Aqua : OceanUI.Panel;
        if (stageTitle != null)
        {
            int i = Mathf.Clamp(GameSession.SelectedStage, 0, 2);
            stageTitle.text = GameSession.StageNames[i].ToUpperInvariant() + "  |  " + GameSession.Mode.ToString().ToUpperInvariant();
            stageDescription.text = GameSession.StageDescriptions[i];
        }
        carousel?.SetTutorialLocked(GameSession.Mode == FishGameMode.Tutorial);
        if (stagePrevious != null) stagePrevious.gameObject.SetActive(GameSession.Mode != FishGameMode.Tutorial);
        if (stageNext != null) stageNext.gameObject.SetActive(GameSession.Mode != FishGameMode.Tutorial);
    }

    public void SetShopMenu() => Navigate(1);
    public void SetSettingsMenu() => Navigate(4);
    public void SetModeSelectionMenu() => Navigate(0);
    public void HideGameplayHub()
    {
        if (hubCanvas != null) hubCanvas.gameObject.SetActive(false);
    }

    public void ShowGameplayHub()
    {
        if (hubCanvas != null) hubCanvas.gameObject.SetActive(true);
        if (pageArea != null) pageArea.gameObject.SetActive(true);
        if (navigationBar != null) navigationBar.gameObject.SetActive(true);
        if (navigationBleed != null) navigationBleed.gameObject.SetActive(true);
        SetRestingPageVisibility();
        RefreshAll();
    }

    public void RefreshWallet()
    {
        if (shopWalletText != null) shopWalletText.text = $"PEARL  {GameSession.PearlWallet}";
    }

    public void PlayGame()
    {
        if (gameplayHost && GameManager.Instance != null)
        {
            HideGameplayHub();
            GameManager.Instance.StartGame();
            return;
        }
        SceneManager.LoadScene("Gameplay");
    }
}
