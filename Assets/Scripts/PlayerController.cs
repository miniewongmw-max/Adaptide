using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private bool inputLocked;
    private float stunnedUntil;
    private float nextMagnetScan;
    private readonly Queue<Vector3> moveQueue = new Queue<Vector3>();

    private void Start()
    {
        ApplyTurtlePalette();
    }

    private void Update()
    {
        if (!CanAcceptInput()) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) QueueMove(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) QueueMove(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) QueueMove(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) QueueMove(Vector3.right);

        if (GameManager.Instance != null && GameManager.Instance.HasPearlMagnet && Time.time >= nextMagnetScan)
        {
            nextMagnetScan = Time.time + 0.18f;
            foreach (CollectibleItem item in FindObjectsByType<CollectibleItem>())
            {
                if (item.kind == CollectibleKind.Pearl && Vector3.Distance(transform.position, item.transform.position) < 3.2f)
                    item.Collect();
            }
        }
    }

    private bool CanAcceptInput()
    {
        return GameManager.Instance != null && GameManager.Instance.gameStarted && !GameManager.Instance.gameOver && !inputLocked && Time.time >= stunnedUntil;
    }

    public void QueueMove(Vector3 direction)
    {
        if (!CanAcceptInput() || direction == Vector3.zero || moveQueue.Count >= 3) return;
        moveQueue.Enqueue(direction);
        if (direction == Vector3.forward && GameManager.Instance.HasSpeedDash && moveQueue.Count < 3)
            moveQueue.Enqueue(direction);
        if (!isMoving) StartCoroutine(MovePlayer());
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        if (locked) moveQueue.Clear();
    }

    public void Stun(float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration);
        moveQueue.Clear();
    }

    private IEnumerator MovePlayer()
    {
        isMoving = true;
        while (moveQueue.Count > 0)
        {
            if (!CanAcceptInput())
            {
                moveQueue.Clear();
                break;
            }

            Vector3 direction = moveQueue.Dequeue();
            Vector3 target = transform.position + direction * tileSize;
            if (target.x < minX || target.x > maxX) continue;

            SeaObstacle obstacle = FindObstacle(target);
            if (obstacle != null && obstacle.Interact(this))
            {
                moveQueue.Clear();
                continue;
            }

            Vector3 start = transform.position;
            float timer = 0f;
            while (timer < moveDuration)
            {
                if (GameManager.Instance == null || GameManager.Instance.gameOver || inputLocked)
                {
                    moveQueue.Clear();
                    isMoving = false;
                    yield break;
                }
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / moveDuration);
                transform.position = Vector3.Lerp(start, target, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            transform.position = target;
            if (direction.z > 0f) GameManager.Instance.AddScore(1);
            CheckBackRow();
        }
        isMoving = false;
    }

    private SeaObstacle FindObstacle(Vector3 futurePosition)
    {
        Vector3 checkPosition = futurePosition + Vector3.up * 0.75f;
        Vector3 halfExtents = new Vector3(0.4f, 0.75f, 0.4f);
        int mask = obstacleLayer.value == 0 ? Physics.AllLayers : obstacleLayer.value;
        Collider[] hits = Physics.OverlapBox(checkPosition, halfExtents, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            SeaObstacle obstacle = hit.GetComponentInParent<SeaObstacle>();
            if (obstacle != null) return obstacle;
        }
        return null;
    }

    private void CheckBackRow()
    {
        if (mapManager == null || GameManager.Instance == null) return;
        if (transform.position.z <= mapManager.GetBackRowZ() + 0.01f)
            GameManager.Instance.GameOver("The reef moved on without you");
    }

    private void OnTriggerEnter(Collider other)
    {
        CollectibleItem item = other.GetComponentInParent<CollectibleItem>();
        if (item != null)
        {
            item.Collect();
            return;
        }
        if (other.CompareTag("Collectible"))
        {
            GameManager.Instance?.CollectPearls(1);
            Destroy(other.gameObject);
        }
    }

    private void ApplyTurtlePalette()
    {
        Color[] palettes =
        {
            new Color32(101, 190, 129, 255),
            new Color32(255, 134, 104, 255),
            new Color32(82, 206, 220, 255),
            new Color32(116, 92, 174, 255)
        };
        Color color = palettes[Mathf.Clamp(GameSession.EquippedSkin, 0, palettes.Length - 1)];
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
            else if (renderer.material.HasProperty("_Color")) renderer.material.color = color;
        }
    }
}
