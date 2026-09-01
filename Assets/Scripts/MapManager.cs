using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("World Prefabs")]
    public GameObject tilePrefab;
    public GameObject borderTilePrefab;
    public Transform cameraTransform;

    public int width = 9;
    public int length = 20;

    // Player 后面先生成多少排
    public int rowsBehindPlayer = 5;

    public float recycleDistance = 5f;

    private List<Transform> rows = new List<Transform>();

    private float nextRowZ;

    [Header("Collectible Prefabs")]
    [Tooltip("Normal pearl currency prefab.")]
    public GameObject pearlPrefab;
    public GameObject starfishPrefab;
    public GameObject treasureChestPrefab;

    [Header("Buff Prefabs")]
    public GameObject bubbleShieldPrefab;
    public GameObject speedDashPrefab;
    public GameObject pearlMagnetPrefab;
    public GameObject invincibilityBubblePrefab;

    [Header("Animal And Obstacle Prefabs")]
    public GameObject coralPrefab;
    public GameObject squidPrefab;
    public GameObject crabPrefab;
    public GameObject jellyfishPrefab;
    public GameObject pufferfishPrefab;
    public GameObject sharkPrefab;

    [Header("Legacy Prefab Fallbacks")]
    [Tooltip("Kept so existing scene assignments continue working. New artwork should use the named slots above.")]
    public List<GameObject> collectiblePrefabs;
    [Range(0f, 1f)]
    public float collectibleSpawnChance = 0.15f;
    public float collectibleHeight = 0.5f;

    [Tooltip("Kept so existing scene assignments continue working. New artwork should use the named slots above.")]
    public List<GameObject> obstaclePrefabs;
    [Range(0f, 1f)]
    public float obstacleSpawnChance = 0.3f;
    public float obstacleHeight = 0.5f;
    public int maximumObstaclesPerRow = 4;
    public LayerMask obstacleLayer;
    private int pearlPatternLane;
    private int pearlPatternRemaining;
    private int pearlPatternDirection = 1;
    private int pearlPatternCooldown;
    private int tutorialObstacleIndex;

    void Start()
    {
        ConfigureDifficulty();
        GenerateStartingMap();
    }

    void ConfigureDifficulty()
    {
        int stage = Mathf.Clamp(GameSession.SelectedStage, 0, 2);
        switch (GameSession.Mode)
        {
            case FishGameMode.Tutorial:
                obstacleSpawnChance = 0.18f;
                maximumObstaclesPerRow = 1;
                collectibleSpawnChance = 0.22f;
                break;
            case FishGameMode.TimeAttack:
                obstacleSpawnChance = 0.34f + stage * 0.04f;
                maximumObstaclesPerRow = 2;
                collectibleSpawnChance = 0.30f;
                break;
            default:
                obstacleSpawnChance = 0.27f + stage * 0.04f;
                maximumObstaclesPerRow = 2;
                collectibleSpawnChance = 0.25f;
                break;
        }
    }

    void Update()
    {
        RecycleRows();
    }

    void GenerateStartingMap()
    {
        int halfWidth = width / 2;

        // 地图不再从 Z = 0 开始
        // 例如 rowsBehindPlayer = 5，就从 Z = -5 开始
        int startingZ = -rowsBehindPlayer;

        for (int i = 0; i < length; i++)
        {
            float rowZ = startingZ + i;

            GameObject rowObject =
                new GameObject("Row_" + rowZ);

            rowObject.transform.position =
                new Vector3(0f, 0f, rowZ);

            rowObject.transform.SetParent(transform);

            for (int x = 0; x < width; x++)
            {
                float xPosition = x - halfWidth;

                GameObject prefabToSpawn;

                if (xPosition >= -4 && xPosition <= 4)
                {
                    prefabToSpawn = tilePrefab;      //Walkable tile
                }
                else
                {
                    prefabToSpawn = borderTilePrefab;   //Border tile
                }

                GameObject tile = Instantiate(
                    prefabToSpawn,
                    rowObject.transform
                );

                tile.transform.localPosition =
                    new Vector3(xPosition, 0f, 0f);
            }

            //Obstacles
            GameObject obstacleContainer = new GameObject("Obstacles");
            obstacleContainer.transform.SetParent(rowObject.transform);
            obstacleContainer.transform.localPosition = Vector3.zero;

            //Collectibles
            GameObject collectibleContainer = new GameObject("Collectibles");
            collectibleContainer.transform.SetParent(rowObject.transform);
            collectibleContainer.transform.localPosition = Vector3.zero;

            if (GameSession.Mode != FishGameMode.Tutorial && i >= 10)
            {
                SpawnObstacles(rowObject.transform);
                SpawnCollectibles(rowObject.transform);
            }

            rows.Add(rowObject.transform);

        }

        // 下一排接在目前地图最前面
        nextRowZ = startingZ + length;

    }

    public void ClearTutorialContent()
    {
        foreach (Transform row in rows)
        {
            if (row == null) continue;
            ClearContainer(row.Find("Obstacles"));
            ClearContainer(row.Find("Collectibles"));
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void SpawnTutorialPearlTrail(float startZ, int lane, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform row = FindRow(startZ + i);
            Transform container = row != null ? row.Find("Collectibles") : null;
            if (container != null) SpawnPearl(container, Mathf.Clamp(lane, -4, 4));
        }
    }

    public void SpawnTutorialPearlPattern(float startZ, int centerLane, int count)
    {
        int pearlCount = Mathf.Max(1, count);
        for (int i = 0; i < pearlCount; i++)
        {
            int rowIndex = i / 2;
            int firstLane = Mathf.Clamp(centerLane + (rowIndex % 2 == 0 ? -1 : 0), -4, 4);
            int secondLane = Mathf.Clamp(centerLane + (rowIndex % 2 == 0 ? 1 : 2), -4, 4);
            if (secondLane == firstLane)
                secondLane = firstLane == 4 ? firstLane - 1 : firstLane + 1;
            int lane = i % 2 == 0 ? firstLane : secondLane;
            Transform row = FindRow(startZ + rowIndex);
            Transform container = row != null ? row.Find("Collectibles") : null;
            if (container != null) SpawnPearl(container, lane);
        }
    }

    public void SpawnTutorialPowerUp(CollectibleKind kind, float z, int lane)
    {
        Transform row = FindRow(z);
        Transform container = row != null ? row.Find("Collectibles") : null;
        if (container == null) return;
        CreateBonusCollectible(container, new List<int> { Mathf.Clamp(lane, -4, 4) }, kind);
    }

    public void SpawnTutorialObstacle(SeaObstacleType type, float z, int lane)
    {
        Transform row = FindRow(z);
        Transform container = row != null ? row.Find("Obstacles") : null;
        if (container == null) return;
        GameObject obstacle = SpawnObstaclePrefab(type, container);
        if (obstacle == null) return;
        obstacle.transform.localPosition = new Vector3(Mathf.Clamp(lane, -4, 4), obstacleHeight, 0f);
    }

    public void SpawnTutorialObstacleLine(SeaObstacleType type, float z)
    {
        for (int lane = -4; lane <= 4; lane++) SpawnTutorialObstacle(type, z, lane);
    }

    private Transform FindRow(float z)
    {
        foreach (Transform row in rows)
            if (row != null && Mathf.Abs(row.position.z - Mathf.Round(z)) < 0.1f) return row;
        return null;
    }

    public void ResetForSelectedRun()
    {
        foreach (Transform row in rows)
        {
            if (row == null) continue;
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();
        pearlPatternRemaining = 0;
        pearlPatternCooldown = 0;
        tutorialObstacleIndex = 0;
        ConfigureDifficulty();
        GenerateStartingMap();
    }

    void SpawnTutorialRow(Transform row, int index)
    {
        // The first safe rows teach movement. The shield then introduces buffs,
        // followed by one clearly separated example obstacle per row.
        if (index == 10)
        {
            List<int> lane = new List<int> { 0 };
            CreateBonusCollectible(row.Find("Collectibles"), lane, CollectibleKind.BubbleShield);
            return;
        }
        if (index == 13 || index == 15 || index == 17 || index == 19)
        {
            SpawnTutorialObstacle(row, index == 13 ? -2 : index == 15 ? 0 : index == 17 ? 2 : -1);
            return;
        }
        if (index >= 7) SpawnCollectibles(row);
    }

    void SpawnTutorialObstacle(Transform row, int lane)
    {
        Transform container = row.Find("Obstacles");
        SeaObstacleType[] lessons = { SeaObstacleType.Coral, SeaObstacleType.Squid, SeaObstacleType.Jellyfish, SeaObstacleType.Crab };
        SeaObstacleType type = lessons[tutorialObstacleIndex % lessons.Length];
        GameObject obstacle = SpawnObstaclePrefab(type, container);
        if (obstacle == null) return;
        obstacle.transform.localPosition = new Vector3(lane, obstacleHeight, 0f);
        tutorialObstacleIndex++;
    }

    void RecycleRows()
    {
        if (cameraTransform == null || rows.Count == 0)
        {
            return;
        }

        Transform oldestRow = rows[0];

        if (oldestRow.position.z <
            cameraTransform.position.z - recycleDistance)
        {
            oldestRow.position =
                new Vector3(0f, 0f, nextRowZ);

            RefreshObstacles(oldestRow);
            RefreshCollectibles(oldestRow);

            nextRowZ += 1f;

            rows.RemoveAt(0);
            rows.Add(oldestRow);
        }
    }

    // 取得目前地图最底下那一排
    public float GetBackRowZ()
    {
        if (rows.Count == 0)
        {
            return float.MinValue;
        }

        return rows[0].position.z;
    }

    void SpawnObstacles(Transform row)
    {
        Transform obstacleContainer =
            row.Find("Obstacles");

        if (obstacleContainer == null)
        {
            return;
        }

        if (Random.value > obstacleSpawnChance) return;

        int obstacleCount = Random.Range(1, maximumObstaclesPerRow + 1);

        List<int> availableXPositions =
            new List<int>();

        for (int x = -4; x <= 4; x++)
        {
            availableXPositions.Add(x);
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            int positionIndex =
                Random.Range(0, availableXPositions.Count);

            int xPosition =
                availableXPositions[positionIndex];

            availableXPositions.RemoveAt(positionIndex);

            SeaObstacleType type = RandomObstacleType();
            GameObject obstacle = SpawnObstaclePrefab(type, obstacleContainer);
            if (obstacle == null) continue;

            obstacle.transform.localPosition =
                new Vector3(xPosition, obstacleHeight, 0f);
        }
    }

    SeaObstacleType RandomObstacleType()
    {
        float roll = Random.value;
        if (GameSession.Mode == FishGameMode.Tutorial)
        {
            if (roll < 0.28f) return SeaObstacleType.Coral;
            if (roll < 0.48f) return SeaObstacleType.Squid;
            if (roll < 0.68f) return SeaObstacleType.Jellyfish;
            if (roll < 0.84f) return SeaObstacleType.Crab;
            return SeaObstacleType.Pufferfish;
        }

        float sharkChance = GameSession.SelectedStage == 2 ? 0.20f : 0.10f;
        if (roll < sharkChance) return SeaObstacleType.Shark;
        if (roll < sharkChance + 0.25f) return SeaObstacleType.Coral;
        if (roll < 0.45f) return SeaObstacleType.Squid;
        if (roll < 0.62f) return SeaObstacleType.Crab;
        if (roll < 0.80f) return SeaObstacleType.Jellyfish;
        return SeaObstacleType.Pufferfish;
    }

    void RefreshObstacles(Transform row)
    {
        Transform obstacleContainer =
            row.Find("Obstacles");

        if (obstacleContainer != null)
        {
            obstacleContainer.name = "Obstacles_Old";
            obstacleContainer.SetParent(null);
            obstacleContainer.gameObject.SetActive(false);
            Destroy(obstacleContainer.gameObject);
        }

        GameObject replacement = new GameObject("Obstacles");
        replacement.transform.SetParent(row);
        replacement.transform.localPosition = Vector3.zero;

        if (GameSession.Mode != FishGameMode.Tutorial) SpawnObstacles(row);
    }

    void SpawnCollectibles(Transform row)
    {
        Transform collectibleContainer =
            row.Find("Collectibles");

        if (collectibleContainer == null)
        {
            return;
        }

        HashSet<int> occupied = new HashSet<int>();
        Transform obstacles = row.Find("Obstacles");
        if (obstacles != null)
            foreach (Transform obstacle in obstacles)
                occupied.Add(Mathf.RoundToInt(obstacle.localPosition.x));

        List<int> freeLanes = new List<int>();
        for (int x = -4; x <= 4; x++)
        {
            if (occupied.Contains(x)) continue;
            freeLanes.Add(x);
        }

        if (pearlPatternRemaining <= 0)
        {
            if (pearlPatternCooldown > 0) pearlPatternCooldown--;
            else if (Random.value < collectibleSpawnChance)
            {
                pearlPatternLane = Random.Range(-2, 3);
                pearlPatternDirection = Random.value < 0.5f ? -1 : 1;
                pearlPatternRemaining = Random.Range(3, 6);
            }
        }

        if (pearlPatternRemaining > 0 && freeLanes.Count > 0)
        {
            int lane = ClosestFreeLane(freeLanes, pearlPatternLane);
            SpawnPearl(collectibleContainer, lane);
            // Occasional paired pearl, never more than two on a row.
            int neighbour = lane + pearlPatternDirection;
            if (Random.value < 0.24f && freeLanes.Contains(neighbour)) SpawnPearl(collectibleContainer, neighbour);
            pearlPatternLane = Mathf.Clamp(pearlPatternLane + pearlPatternDirection, -3, 3);
            if (Mathf.Abs(pearlPatternLane) >= 3) pearlPatternDirection *= -1;
            pearlPatternRemaining--;
            if (pearlPatternRemaining == 0) pearlPatternCooldown = Random.Range(2, 5);
        }

        if (freeLanes.Count > 0)
        {
            float bonusRoll = Random.value;
            if (bonusRoll < 0.025f) CreateBonusCollectible(collectibleContainer, freeLanes, CollectibleKind.TreasureChest);
            else if (bonusRoll < 0.09f) CreateBonusCollectible(collectibleContainer, freeLanes, CollectibleKind.Starfish);
            else if (bonusRoll < 0.115f) CreateBonusCollectible(collectibleContainer, freeLanes, (CollectibleKind)Random.Range(3, 7));
        }
    }

    int ClosestFreeLane(List<int> lanes, int desired)
    {
        int best = lanes[0];
        foreach (int lane in lanes)
            if (Mathf.Abs(lane - desired) < Mathf.Abs(best - desired)) best = lane;
        return best;
    }

    void SpawnPearl(Transform container, int lane)
    {
        SpawnCollectiblePrefab(CollectibleKind.Pearl, container, lane, collectibleHeight);
    }

    void CreateBonusCollectible(Transform container, List<int> freeLanes, CollectibleKind kind)
    {
        int lane = freeLanes[Random.Range(0, freeLanes.Count)];
        SpawnCollectiblePrefab(kind, container, lane, collectibleHeight + 0.2f);
    }

    private GameObject SpawnObstaclePrefab(SeaObstacleType type, Transform container)
    {
        GameObject prefab = GetObstaclePrefab(type);
        if (prefab == null) return null;
        GameObject obstacle = Instantiate(prefab, container);
        obstacle.name = type.ToString();
        SeaObstacle behaviour = obstacle.GetComponent<SeaObstacle>();
        if (behaviour == null) behaviour = obstacle.AddComponent<SeaObstacle>();
        // Final named art keeps its authored materials. The old generic fallback
        // block is tinted so the game remains readable until all slots are filled.
        behaviour.Initialize(type, GetNamedObstaclePrefab(type) == null);
        bool hasSolidCollider = false;
        foreach (Collider collider in obstacle.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger) hasSolidCollider = true;
        if (!hasSolidCollider)
            obstacle.AddComponent<BoxCollider>();
        return obstacle;
    }

    private GameObject GetObstaclePrefab(SeaObstacleType type)
    {
        GameObject named = GetNamedObstaclePrefab(type);
        if (named != null) return named;
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0) return null;
        return obstaclePrefabs[(int)type % obstaclePrefabs.Count];
    }

    private GameObject GetNamedObstaclePrefab(SeaObstacleType type)
    {
        return type switch
        {
            SeaObstacleType.Coral => coralPrefab,
            SeaObstacleType.Squid => squidPrefab,
            SeaObstacleType.Crab => crabPrefab,
            SeaObstacleType.Jellyfish => jellyfishPrefab,
            SeaObstacleType.Pufferfish => pufferfishPrefab,
            SeaObstacleType.Shark => sharkPrefab,
            _ => null
        };
    }

    private GameObject SpawnCollectiblePrefab(CollectibleKind kind, Transform container, int lane, float height)
    {
        GameObject prefab = GetCollectiblePrefab(kind);
        if (prefab == null) return null;
        GameObject collectible = Instantiate(prefab, container);
        collectible.name = kind.ToString();
        collectible.transform.localPosition = new Vector3(lane, height, 0f);
        CollectibleItem item = collectible.GetComponent<CollectibleItem>();
        if (item == null) item = collectible.AddComponent<CollectibleItem>();
        item.kind = kind;
        bool hasTrigger = false;
        foreach (Collider collider in collectible.GetComponentsInChildren<Collider>(true))
            if (collider.isTrigger) hasTrigger = true;
        if (!hasTrigger)
        {
            SphereCollider trigger = collectible.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.48f;
        }
        return collectible;
    }

    private GameObject GetCollectiblePrefab(CollectibleKind kind)
    {
        GameObject named = kind switch
        {
            CollectibleKind.Pearl => pearlPrefab,
            CollectibleKind.Starfish => starfishPrefab,
            CollectibleKind.TreasureChest => treasureChestPrefab,
            CollectibleKind.BubbleShield => bubbleShieldPrefab,
            CollectibleKind.SpeedDash => speedDashPrefab,
            CollectibleKind.PearlMagnet => pearlMagnetPrefab,
            CollectibleKind.InvincibilityBubble => invincibilityBubblePrefab,
            _ => null
        };
        if (named != null) return named;
        if (collectiblePrefabs == null || collectiblePrefabs.Count == 0) return null;
        return collectiblePrefabs[Random.Range(0, collectiblePrefabs.Count)];
    }

    void RefreshCollectibles(Transform row)
    {
        Transform collectibleContainer =
            row.Find("Collectibles");

        if (collectibleContainer != null)
        {
            collectibleContainer.name = "Collectibles_Old";
            collectibleContainer.SetParent(null);
            collectibleContainer.gameObject.SetActive(false);
            Destroy(collectibleContainer.gameObject);
        }

        GameObject replacement = new GameObject("Collectibles");
        replacement.transform.SetParent(row);
        replacement.transform.localPosition = Vector3.zero;

        if (GameSession.Mode != FishGameMode.Tutorial) SpawnCollectibles(row);
    }

}
