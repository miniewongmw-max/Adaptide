using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuBehaviour : MonoBehaviour
{
    public GameObject shopMenu;
    public GameObject settingsMenu;
    public GameObject modeSelectionMenu;

    private GameObject mainView;
    private GameObject modeView;
    private GameObject stageView;
    private GameObject shopView;
    private GameObject settingsView;
    private TMP_Text walletText;
    private TMP_Text stageTitle;
    private TMP_Text stageDescription;
    private TMP_Text shopFeedback;
    private StageCarousel3D carousel;
    private readonly GameObject[] views = new GameObject[5];

    private static readonly string[] PowerNames = { "Bubble Shield", "Speed Dash", "Pearl Magnet", "Invincibility" };
    private static readonly string[] PowerDescriptions =
    {
        "Blocks one obstacle hit",
        "Two tiles per forward swipe",
        "Pulls nearby pearls to you",
        "Ignores hazards for 10 seconds"
    };
    private static readonly int[] PowerCosts = { 15, 10, 50, 80 };

    private void Start()
    {
        Time.timeScale = 1f;
        BuildMenu();
    }

    private void BuildMenu()
    {
        Canvas canvas = OceanUI.CreateCanvas("Busy Reef Menu");
        RectTransform root = OceanUI.SafeRoot(canvas);
        OceanUI.AddOceanBackground(root);
        AdaptiveUILayout adaptive = root.gameObject.AddComponent<AdaptiveUILayout>();
        adaptive.scaler = canvas.GetComponent<CanvasScaler>();

        BuildTopBar(root);
        mainView = BuildMainView(root);
        modeView = BuildModeView(root);
        stageView = BuildStageView(root);
        shopView = BuildShopView(root);
        settingsView = BuildSettingsView(root);
        views[0] = mainView;
        views[1] = modeView;
        views[2] = stageView;
        views[3] = shopView;
        views[4] = settingsView;
        adaptive.carousel = carousel;
        ShowView(mainView);
        RefreshWallet();
    }

    private void BuildTopBar(RectTransform root)
    {
        TMP_Text logo = OceanUI.CreateText("BUSY REEF", root, 72f, OceanUI.Sand, TextAlignmentOptions.Left);
        OceanUI.SetRect(logo.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.7f, 0.98f), Vector2.zero, Vector2.zero);
        walletText = OceanUI.CreateText("PEARLS  0", root, 34f, OceanUI.Foam, TextAlignmentOptions.Right);
        OceanUI.SetRect(walletText.rectTransform, new Vector2(0.63f, 0.90f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
    }

    private GameObject BuildMainView(RectTransform root)
    {
        GameObject view = CreateView("Main", root);
        TMP_Text tagline = OceanUI.CreateText("A little turtle. A very busy ocean.", view.transform, 36f, OceanUI.Muted);
        OceanUI.SetRect(tagline.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f), Vector2.zero, Vector2.zero);
        CreateMenuButton(view.transform, "PLAY", 0.58f, OceanUI.Aqua, () => ShowView(modeView));
        CreateMenuButton(view.transform, "SHOP", 0.43f, OceanUI.Sand, () => { RefreshShop(); ShowView(shopView); });
        CreateMenuButton(view.transform, "SETTINGS", 0.28f, OceanUI.Coral, () => ShowView(settingsView));
        TMP_Text hint = OceanUI.CreateText("Swipe or use the touch pad | Collect pearls | Find your family", view.transform, 27f, OceanUI.Muted);
        OceanUI.SetRect(hint.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.16f), Vector2.zero, Vector2.zero);
        return view;
    }

    private GameObject BuildModeView(RectTransform root)
    {
        GameObject view = CreateView("Mode Select", root);
        AddSectionTitle(view.transform, "CHOOSE A MODE", "Each mode uses the same four-direction swipe controls");
        CreateModeCard(view.transform, "TUTORIAL", "Slow current | fewer obstacles | guided first run", 0.64f, OceanUI.Aqua, FishGameMode.Tutorial);
        CreateModeCard(view.transform, "STANDARD", "Endless reef | rising difficulty | high score", 0.46f, OceanUI.Sand, FishGameMode.Standard);
        CreateModeCard(view.transform, "TIME ATTACK", "60 seconds | fast routes | distance challenge", 0.28f, OceanUI.Coral, FishGameMode.TimeAttack);
        AddBack(view.transform, () => ShowView(mainView));
        return view;
    }

    private GameObject BuildStageView(RectTransform root)
    {
        GameObject view = CreateView("Stage Select", root);
        AddSectionTitle(view.transform, "DIVE LOCATION", "Drag the circular carousel or tap a reef");
        GameObject carouselObject = OceanUI.CreateObject("3D Circular Carousel", view.transform);
        OceanUI.SetRect(carouselObject.GetComponent<RectTransform>(), new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.70f), Vector2.zero, Vector2.zero);
        carousel = carouselObject.AddComponent<StageCarousel3D>();
        RectTransform[] cards = new RectTransform[3];
        Color[] colors = { OceanUI.Aqua, OceanUI.Sand, new Color32(76, 116, 177, 255) };
        for (int i = 0; i < cards.Length; i++)
        {
            Image card = OceanUI.CreatePanel(GameSession.StageNames[i], carouselObject.transform, colors[i]);
            cards[i] = card.rectTransform;
            OceanUI.SetCentered(cards[i], Vector2.zero, new Vector2(470f, 330f));
            card.gameObject.AddComponent<Button>();
            TMP_Text number = OceanUI.CreateText($"0{i + 1}", card.transform, 32f, OceanUI.Deep, TextAlignmentOptions.TopLeft);
            OceanUI.SetRect(number.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.30f, 0.94f), Vector2.zero, Vector2.zero);
            TMP_Text name = OceanUI.CreateText(GameSession.StageNames[i].ToUpperInvariant(), card.transform, 45f, OceanUI.Deep);
            OceanUI.SetRect(name.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
        }
        carousel.Configure(cards, GameSession.SelectedStage);
        carousel.SelectionChanged = index => { GameSession.SelectedStage = index; RefreshStageText(); };
        Button left = OceanUI.CreateButton("Previous", "<", view.transform, OceanUI.Panel, carousel.Previous);
        Button right = OceanUI.CreateButton("Next", ">", view.transform, OceanUI.Panel, carousel.Next);
        OceanUI.SetRect(left.GetComponent<RectTransform>(), new Vector2(0.02f, 0.43f), new Vector2(0.15f, 0.57f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(right.GetComponent<RectTransform>(), new Vector2(0.85f, 0.43f), new Vector2(0.98f, 0.57f), Vector2.zero, Vector2.zero);
        stageTitle = OceanUI.CreateText("", view.transform, 38f, OceanUI.Foam);
        stageDescription = OceanUI.CreateText("", view.transform, 27f, OceanUI.Muted);
        OceanUI.SetRect(stageTitle.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.30f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(stageDescription.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.23f), Vector2.zero, Vector2.zero);
        Button dive = OceanUI.CreateButton("Dive", "DIVE IN", view.transform, OceanUI.Coral, PlayGame);
        OceanUI.SetRect(dive.GetComponent<RectTransform>(), new Vector2(0.29f, 0.04f), new Vector2(0.71f, 0.13f), Vector2.zero, Vector2.zero);
        AddBack(view.transform, () => ShowView(modeView));
        RefreshStageText();
        return view;
    }

    private GameObject BuildShopView(RectTransform root)
    {
        GameObject view = CreateView("Shop", root);
        AddSectionTitle(view.transform, "PEARL SHOP", "Items activate automatically at the start of a future dive");
        for (int i = 0; i < PowerNames.Length; i++)
        {
            int index = i;
            float x0 = i % 2 == 0 ? 0.07f : 0.52f;
            float y0 = i < 2 ? 0.49f : 0.23f;
            Image card = OceanUI.CreatePanel(PowerNames[i], view.transform, new Color(0.04f, 0.38f, 0.46f, 0.94f));
            OceanUI.SetRect(card.rectTransform, new Vector2(x0, y0), new Vector2(x0 + 0.41f, y0 + 0.22f), Vector2.zero, Vector2.zero);
            TMP_Text title = OceanUI.CreateText(PowerNames[i].ToUpperInvariant(), card.transform, 30f, OceanUI.Sand);
            OceanUI.SetRect(title.rectTransform, new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);
            TMP_Text description = OceanUI.CreateText(PowerDescriptions[i], card.transform, 23f, OceanUI.Foam);
            OceanUI.SetRect(description.rectTransform, new Vector2(0.07f, 0.30f), new Vector2(0.93f, 0.67f), Vector2.zero, Vector2.zero);
            Button buy = OceanUI.CreateButton("Buy", $"{PowerCosts[i]} PEARLS", card.transform, OceanUI.Aqua, () => BuyPowerUp(index));
            OceanUI.SetRect(buy.GetComponent<RectTransform>(), new Vector2(0.12f, 0.05f), new Vector2(0.88f, 0.29f), Vector2.zero, Vector2.zero);
        }
        shopFeedback = OceanUI.CreateText("", view.transform, 27f, OceanUI.Foam);
        OceanUI.SetRect(shopFeedback.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.20f), Vector2.zero, Vector2.zero);
        AddBack(view.transform, () => ShowView(mainView));
        return view;
    }

    private GameObject BuildSettingsView(RectTransform root)
    {
        GameObject view = CreateView("Settings", root);
        AddSectionTitle(view.transform, "SETTINGS", "Comfortable controls for small and large screens");
        TMP_Text sound = OceanUI.CreateText("SOUND", view.transform, 38f, OceanUI.Foam);
        OceanUI.SetRect(sound.rectTransform, new Vector2(0.15f, 0.60f), new Vector2(0.45f, 0.70f), Vector2.zero, Vector2.zero);
        Button soundToggle = OceanUI.CreateButton("Sound Toggle", "ON / OFF", view.transform, OceanUI.Aqua, () => AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f);
        OceanUI.SetRect(soundToggle.GetComponent<RectTransform>(), new Vector2(0.50f, 0.60f), new Vector2(0.82f, 0.70f), Vector2.zero, Vector2.zero);
        TMP_Text note = OceanUI.CreateText("The interface follows your device safe area and rearranges automatically in portrait or landscape.", view.transform, 31f, OceanUI.Muted);
        OceanUI.SetRect(note.rectTransform, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.54f), Vector2.zero, Vector2.zero);
        AddBack(view.transform, () => ShowView(mainView));
        return view;
    }

    private GameObject CreateView(string name, RectTransform root)
    {
        GameObject view = OceanUI.CreateObject(name, root);
        OceanUI.SetRect(view.GetComponent<RectTransform>(), new Vector2(0f, 0.03f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
        return view;
    }

    private void AddSectionTitle(Transform parent, string titleText, string subtitleText)
    {
        TMP_Text title = OceanUI.CreateText(titleText, parent, 56f, OceanUI.Sand);
        TMP_Text subtitle = OceanUI.CreateText(subtitleText, parent, 27f, OceanUI.Muted);
        OceanUI.SetRect(title.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);
        OceanUI.SetRect(subtitle.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
    }

    private void CreateModeCard(Transform parent, string title, string description, float y, Color color, FishGameMode mode)
    {
        Button button = OceanUI.CreateButton(title, title, parent, color, () => SelectMode(mode));
        OceanUI.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.13f, y), new Vector2(0.87f, y + 0.14f), Vector2.zero, Vector2.zero);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        OceanUI.SetRect(label.rectTransform, new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        TMP_Text detail = OceanUI.CreateText(description, button.transform, 24f, OceanUI.Deep);
        OceanUI.SetRect(detail.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.42f), Vector2.zero, Vector2.zero);
    }

    private Button CreateMenuButton(Transform parent, string label, float y, Color color, Action action)
    {
        Button button = OceanUI.CreateButton(label, label, parent, color, action);
        OceanUI.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.21f, y), new Vector2(0.79f, y + 0.11f), Vector2.zero, Vector2.zero);
        return button;
    }

    private void AddBack(Transform parent, Action action)
    {
        Button back = OceanUI.CreateButton("Back", "< BACK", parent, OceanUI.Panel, action);
        OceanUI.SetRect(back.GetComponent<RectTransform>(), new Vector2(0.03f, 0.02f), new Vector2(0.22f, 0.10f), Vector2.zero, Vector2.zero);
        back.GetComponentInChildren<TMP_Text>().fontSize = 27f;
    }

    private void SelectMode(FishGameMode mode)
    {
        GameSession.Mode = mode;
        ShowView(stageView);
    }

    private void ShowView(GameObject selected)
    {
        foreach (GameObject view in views)
            if (view != null) view.SetActive(view == selected);
        selected?.SetActive(true);
        RefreshWallet();
    }

    private void RefreshStageText()
    {
        if (stageTitle == null) return;
        int index = Mathf.Clamp(GameSession.SelectedStage, 0, GameSession.StageNames.Length - 1);
        stageTitle.text = GameSession.StageNames[index].ToUpperInvariant();
        stageDescription.text = GameSession.StageDescriptions[index];
    }

    private void BuyPowerUp(int index)
    {
        bool bought = GameSession.BuyPowerUp(index, PowerCosts[index]);
        shopFeedback.text = bought ? $"{PowerNames[index]} packed for a future dive!" : "Collect more pearls before buying this item.";
        RefreshShop();
    }

    private void RefreshShop()
    {
        RefreshWallet();
        if (shopFeedback != null && string.IsNullOrEmpty(shopFeedback.text))
            shopFeedback.text = "Backpack items are consumed when a new run begins.";
    }

    private void RefreshWallet()
    {
        if (walletText != null) walletText.text = $"PEARLS  {GameSession.PearlWallet}";
    }

    public void SetShopMenu() => ShowView(shopView);
    public void SetSettingsMenu() => ShowView(settingsView);
    public void SetModeSelectionMenu() => ShowView(modeView);
    public void PlayGame() => SceneManager.LoadScene("Gameplay");
}
