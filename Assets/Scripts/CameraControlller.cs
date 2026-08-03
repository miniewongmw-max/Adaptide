using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;

    public float minimumSpeed = 0.8f;
    public float catchUpSpeed = 3f;
    public float maxDistanceAhead = 5f;
    public float horizontalOffset = 3.1f;

    public float desiredPlayerScreenY = 0.35f;
    public float smoothness = 3f;

    private float currentSpeed;
    private Camera cameraComponent;

    void Start()
    {
        currentSpeed = minimumSpeed;
        cameraComponent = GetComponent<Camera>();

        if (player != null)
        {
            horizontalOffset =
                transform.position.x - player.position.x;
        }
    }

    void Update()
{
    if (GameManager.Instance == null)
    {
        return;
    }

    if (!GameManager.Instance.gameStarted ||
        GameManager.Instance.gameOver)
    {
        return;
    }

    if (player == null)
    {
        return;
    }

        Vector3 playerViewportPosition =
            Camera.main.WorldToViewportPoint(player.position);

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

       //Camera automatically moves forward
        transform.position +=
            Vector3.forward * currentSpeed * Time.deltaTime;

        // Prevent the player from getting too far ahead
        if (player.position.z - transform.position.z >
            maxDistanceAhead)
        {
            Vector3 cameraPosition = transform.position;

            cameraPosition.z =
                player.position.z - maxDistanceAhead;

            transform.position = cameraPosition;
        }

        // Follow the player's left and right movement
        Vector3 finalCameraPosition = transform.position;

        finalCameraPosition.x = Mathf.Lerp(
            finalCameraPosition.x,
            player.position.x + horizontalOffset,
            smoothness * Time.deltaTime
        );
        transform.position = finalCameraPosition;
    }
}