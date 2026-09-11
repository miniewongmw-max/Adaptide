using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageCarousel3D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform[] cards;
    public float radiusX = 360f;
    public float radiusY = 95f;
    public Action<int> SelectionChanged;

    private int selectedIndex;
    private float currentAngle;
    private float targetAngle;
    private float dragStartAngle;
    private bool tutorialLocked;
    private const float Spacing = 120f;

    public int SelectedIndex => selectedIndex;

    public void Configure(RectTransform[] stageCards, int initialIndex)
    {
        cards = stageCards;
        selectedIndex = Mathf.Clamp(initialIndex, 0, cards.Length - 1);
        currentAngle = targetAngle = -selectedIndex * Spacing;
        for (int i = 0; i < cards.Length; i++)
        {
            int captured = i;
            Button button = cards[i].GetComponent<Button>();
            if (button != null) button.onClick.AddListener(() => Select(captured));
        }
        ApplyLayout(true);
    }

    private void Update()
    {
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 12f * Time.unscaledDeltaTime);
        ApplyLayout(false);
    }

    public void Previous() => Select((selectedIndex - 1 + cards.Length) % cards.Length);
    public void Next() => Select((selectedIndex + 1) % cards.Length);

    public void SetTutorialLocked(bool locked)
    {
        if (tutorialLocked == locked) return;
        tutorialLocked = locked;
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++) cards[i].gameObject.SetActive(!locked || i == 0);
        }
        if (locked)
        {
            selectedIndex = 0;
            currentAngle = targetAngle = 0f;
        }
    }

    public void Select(int index)
    {
        if (tutorialLocked) index = 0;
        selectedIndex = Mathf.Clamp(index, 0, cards.Length - 1);
        targetAngle = -selectedIndex * Spacing;
        SelectionChanged?.Invoke(selectedIndex);
    }

    public void OnBeginDrag(PointerEventData eventData) => dragStartAngle = targetAngle;

    public void OnDrag(PointerEventData eventData)
    {
        if (tutorialLocked) return;
        targetAngle = dragStartAngle + eventData.position.x - eventData.pressPosition.x;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (tutorialLocked) return;
        float delta = eventData.position.x - eventData.pressPosition.x;
        if (Mathf.Abs(delta) > 45f) Select(delta > 0 ? (selectedIndex - 1 + cards.Length) % cards.Length : (selectedIndex + 1) % cards.Length);
        else Select(selectedIndex);
    }

    private void ApplyLayout(bool immediate)
    {
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            float angle = (currentAngle + i * Spacing) * Mathf.Deg2Rad;
            float depth = (Mathf.Cos(angle) + 1f) * 0.5f;
            RectTransform card = cards[i];
            Vector2 targetPosition = new Vector2(Mathf.Sin(angle) * radiusX, (depth - 0.5f) * radiusY);
            float scale = Mathf.Lerp(0.64f, 1.08f, depth);
            card.anchoredPosition = immediate ? targetPosition : Vector2.Lerp(card.anchoredPosition, targetPosition, 16f * Time.unscaledDeltaTime);
            card.localScale = Vector3.one * scale;
            CanvasGroup group = card.GetComponent<CanvasGroup>();
            if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = Mathf.Lerp(0.38f, 1f, depth);
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        int[] order = new int[cards.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        for (int a = 0; a < order.Length - 1; a++)
        for (int b = a + 1; b < order.Length; b++)
        {
            float depthA = Mathf.Cos((currentAngle + order[a] * Spacing) * Mathf.Deg2Rad);
            float depthB = Mathf.Cos((currentAngle + order[b] * Spacing) * Mathf.Deg2Rad);
            if (depthA > depthB)
            {
                int swap = order[a];
                order[a] = order[b];
                order[b] = swap;
            }
        }
        for (int i = 0; i < order.Length; i++) cards[order[i]].SetSiblingIndex(i);
    }
}
