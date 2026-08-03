using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool gameStarted = false;
    public bool gameOver = false;

    public Transform player;
    public Camera mainCamera;
    public TMP_Text tapToStartText;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!gameStarted)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartGame();
            }

            return;
        }

        if (gameOver)
        {
            return;
        }

        CheckPlayerOutsideCamera();
    }

    void StartGame()
    {
        gameStarted = true;

        if (tapToStartText != null)
        {
            tapToStartText.gameObject.SetActive(false);
        }

        Debug.Log("Game Started");
    }

    void CheckPlayerOutsideCamera()
    {
        if (player == null || mainCamera == null)
        {
            return;
        }

        Vector3 viewportPosition =
            mainCamera.WorldToViewportPoint(player.position);

        // 玩家已经掉出画面底部
        if (viewportPosition.y < 0f)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;

        Debug.Log("Game Over");
    }
}