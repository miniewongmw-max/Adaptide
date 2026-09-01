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
    private float furthestScoredZ;
    private readonly Queue<Vector3> moveQueue = new Queue<Vector3>();

    private void Start()
    {
        furthestScoredZ = transform.position.z;
        RefreshCharacter();
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
            GameManager.Instance?.NotifyPlayerMoved(direction);
            if (direction.z > 0f && transform.position.z > furthestScoredZ + 0.01f)
            {
                int newlyReachedRows = Mathf.Max(1, Mathf.RoundToInt((transform.position.z - furthestScoredZ) / tileSize));
                furthestScoredZ = transform.position.z;
                GameManager.Instance.AddScore(newlyReachedRows);
            }
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

    public void RefreshCharacter()
    {
        Transform oldSeal = transform.Find("Runtime Seal");
        if (oldSeal != null)
        {
            oldSeal.gameObject.SetActive(false);
            Destroy(oldSeal.gameObject);
        }
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
        if (GameSession.EquippedCharacter == 1)
        {
            BuildSealCharacter();
            return;
        }
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

    private void BuildSealCharacter()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        GameObject sealRoot = new GameObject("Runtime Seal");
        sealRoot.transform.SetParent(transform, false);
        Color body = new Color32(156, 197, 211, 255);
        CreateSealPart(sealRoot.transform, "Seal Body", PrimitiveType.Capsule, new Vector3(0f, 0.45f, 0f), new Vector3(0.72f, 0.55f, 0.95f), body);
        CreateSealPart(sealRoot.transform, "Seal Head", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0.42f), new Vector3(0.62f, 0.52f, 0.62f), new Color32(178, 216, 225, 255));
        CreateSealPart(sealRoot.transform, "Left Flipper", PrimitiveType.Sphere, new Vector3(-0.42f, 0.30f, 0f), new Vector3(0.42f, 0.12f, 0.62f), body, -24f);
        CreateSealPart(sealRoot.transform, "Right Flipper", PrimitiveType.Sphere, new Vector3(0.42f, 0.30f, 0f), new Vector3(0.42f, 0.12f, 0.62f), body, 24f);
        CreateSealPart(sealRoot.transform, "Nose", PrimitiveType.Sphere, new Vector3(0f, 0.57f, 0.72f), Vector3.one * 0.15f, new Color32(29, 55, 68, 255));
    }

    private void CreateSealPart(Transform parent, string partName, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, float zRotation = 0f)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = Quaternion.Euler(90f, 0f, zRotation);
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null) Destroy(partCollider);
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
        else renderer.material.color = color;
    }
}
