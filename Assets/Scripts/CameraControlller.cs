using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;

    public float minimumSpeed = 0.8f;
    public float catchUpSpeed = 3f;

    public float desiredPlayerScreenY = 0.35f;
    public float smoothness = 3f;

    private float currentSpeed;

    void Start()
    {
        currentSpeed = minimumSpeed;
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

       
        transform.position +=
            Vector3.forward * currentSpeed * Time.deltaTime;
    }
}