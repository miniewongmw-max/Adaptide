using UnityEngine;
using UnityEngine.EventSystems;

public class GameplayGestureInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public PlayerController player;
    private Vector2 start;
    private int pointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId != int.MinValue) return;
        pointerId = eventData.pointerId;
        start = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        pointerId = int.MinValue;
        Vector2 delta = eventData.position - start;
        float threshold = Mathf.Max(45f, Screen.dpi > 0f ? Screen.dpi * 0.18f : 55f);

        if (delta.magnitude < threshold)
        {
            if (GameManager.Instance != null && GameManager.Instance.RegisterEscapeInput(true)) return;
            if (GameManager.Instance != null && !GameManager.Instance.gameStarted)
            {
                GameManager.Instance.TryStartFromTap();
                return;
            }
            player?.QueueMove(Vector3.forward);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.RegisterEscapeInput(false)) return;
        if (player == null) return;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            player.QueueMove(delta.x > 0f ? Vector3.right : Vector3.left);
        else
            player.QueueMove(delta.y > 0f ? Vector3.forward : Vector3.back);
    }
}
