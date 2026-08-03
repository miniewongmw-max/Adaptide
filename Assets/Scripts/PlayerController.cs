using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float tileSize = 1f;
    public float moveDuration = 0.12f;

    public float minX = -4f;
    public float maxX = 4f;

    public MapManager mapManager;

    [Header("Check Obstacles")]
    public LayerMask obstacleLayer;
    public float obstacleCheckHeight = 0.5f;
    public float obstacleCheckRadius = 0.3f;

    private bool isMoving;
    private Queue<Vector3> moveQueue = new Queue<Vector3>();

    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!GameManager.Instance.gameStarted)
        {
            return;
        }

        if (GameManager.Instance.gameOver)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            AddMove(Vector3.forward);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            AddMove(Vector3.back);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            AddMove(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            AddMove(Vector3.right);
        }
    }

    void AddMove(Vector3 direction)
    {
        Vector3 futurePosition =
            transform.position + direction * tileSize;

        // 只限制左右边界
        if (futurePosition.x < minX ||
            futurePosition.x > maxX)
        {
            return;
        }

        // Check the destination tile for obstacles
        Vector3 checkPosition =
            futurePosition + Vector3.up * 0.75f;

        Vector3 halfExtents =
            new Vector3(0.4f, 0.75f, 0.4f);

        bool obstacleFound = Physics.CheckBox(
            checkPosition,
            halfExtents,
            Quaternion.identity,
            obstacleLayer,
            QueryTriggerInteraction.Collide
        );

        if (obstacleFound)
        {
            Debug.Log("Movement blocked by obstacle");
            return;
        }

        moveQueue.Enqueue(direction);

        if (!isMoving)
        {
            StartCoroutine(MovePlayer());
        }
    }

    IEnumerator MovePlayer()
    {
        isMoving = true;

        while (moveQueue.Count > 0)
        {
            if (GameManager.Instance.gameOver)
            {
                moveQueue.Clear();
                break;
            }

            Vector3 direction = moveQueue.Dequeue();

            Vector3 startPosition = transform.position;
            Vector3 targetPosition =
                startPosition + direction * tileSize;

            float timer = 0f;

            while (timer < moveDuration)
            {
                if (GameManager.Instance.gameOver)
                {
                    moveQueue.Clear();
                    isMoving = false;
                    yield break;
                }

                timer += Time.deltaTime;

                float progress = timer / moveDuration;

                transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

                yield return null;
            }

            transform.position = targetPosition;

            CheckBackRow();

            if (GameManager.Instance.gameOver)
            {
                moveQueue.Clear();
                isMoving = false;
                yield break;
            }
        }

        isMoving = false;
    }

    void CheckBackRow()
    {
        if (mapManager == null)
        {
            return;
        }

        float backRowZ = mapManager.GetBackRowZ();

        // 玩家走到当前地图最后一排
        if (transform.position.z <= backRowZ + 0.01f)
        {
            GameManager.Instance.GameOver();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            Destroy(other.gameObject);
            Debug.Log("Collected!");
        }
    }
}