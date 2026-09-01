using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;

    public float minimumSpeed = 0.8f;
    public float catchUpSpeed = 3f;
    public float maxDistanceAhead = 5f;
    [Range(0.4f, 0.6f)]
    public float desiredPlayerScreenY = 0.50f;
    public float smoothness = 3f;
    public float cameraDistance = 11f;
    [Tooltip("Keep at zero to place the character in the phone screen centre.")]
    public float focusAheadOfPlayer = 0f;
    public Vector3 isometricEuler = new Vector3(48f, -30f, 0f);

    private float currentSpeed;
    private Camera cameraComponent;
    private bool lastPortrait;
    private float focusZ;

    void Start()
    {
        ConfigureForMode();
        currentSpeed = minimumSpeed;
        cameraComponent = GetComponent<Camera>();
        ApplyOrientation();
        SnapToFocus();
    }

    void ConfigureForMode()
    {
        float stageBoost = Mathf.Clamp(GameSession.SelectedStage, 0, 2) * 0.12f;
        switch (GameSession.Mode)
        {
            case FishGameMode.Tutorial:
                minimumSpeed = 0f;
                catchUpSpeed = 2.1f;
                break;
            case FishGameMode.TimeAttack:
                minimumSpeed = 1.0f + stageBoost;
                catchUpSpeed = 3.8f;
                break;
            default:
                minimumSpeed = 0.72f + stageBoost;
                catchUpSpeed = 3.0f;
                break;
        }
    }

    void ApplyOrientation()
    {
        if (cameraComponent == null) return;
        lastPortrait = Screen.height > Screen.width;
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = lastPortrait ? 7.25f : 4.75f;
        transform.rotation = Quaternion.Euler(isometricEuler);
    }

    public void PrepareForSelectedRun()
    {
        if (cameraComponent == null) cameraComponent = GetComponent<Camera>();
        ConfigureForMode();
        currentSpeed = minimumSpeed;
        ApplyOrientation();
        SnapToFocus();
    }

    void SnapToFocus()
    {
        if (player == null) return;
        focusZ = player.position.z + focusAheadOfPlayer;
        Vector3 focus = new Vector3(0f, player.position.y, focusZ);
        transform.position = focus - transform.forward * cameraDistance;
    }

    void Update()
{
    if (lastPortrait != (Screen.height > Screen.width))
    {
        ApplyOrientation();
    }
    if (player != null && (GameManager.Instance == null ||
        !GameManager.Instance.gameStarted || GameManager.Instance.gameOver))
    {
        // Keep the preview and ready-state character centred every frame. This
        // also prevents scene Start-order differences from restoring an old
        // serialized camera transform after the initial snap.
        SnapToFocus();
        return;
    }

    if (GameManager.Instance == null)
    {
        return;
    }

    if (player == null)
    {
        return;
    }

        Vector3 playerViewportPosition =
            cameraComponent.WorldToViewportPoint(player.position);

        float targetSpeed = minimumSpeed;

        if (playerViewportPosition.y > desiredPlayerScreenY)
        {
            float difference =
                playerViewportPosition.y - desiredPlayerScreenY;

            targetSpeed = Mathf.Lerp(
                minimumSpeed,
                catchUpSpeed,
                difference * 3f
            );
        }

        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            smoothness * Time.deltaTime
        );

        focusZ += currentSpeed * Time.deltaTime;
        focusZ = Mathf.Max(focusZ, player.position.z + focusAheadOfPlayer);

        // Keep the reef centred like a Crossy Road camera. Lateral player hops do
        // not drag the whole board sideways.
        Vector3 focus = new Vector3(0f, player.position.y, focusZ);
        Vector3 target = focus - transform.forward * cameraDistance;
        transform.position = Vector3.Lerp(transform.position, target, smoothness * Time.deltaTime);
    }
}
