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

   
    public int rowsBehindPlayer = 5;

    public float recycleDistance = 5f;

    [Header("Starting Safe Zone")]
    [Min(1)] public int safeRowsAheadOfStart = 6;
    [Min(0)] public int pearlRowsAheadOfStart = 2;

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
    public float obstacleSpawnChance = 0.1f;
    public float obstacleHeight = 0.5f;
    public int maximumObstaclesPerRow = 4;
    public LayerMask obstacleLayer;
    [Header("Crossing Traffic Lanes")]
    [Min(8f)] public float animalOffscreenDistance = 12f;

    [Header("Traffic Tuning - Editable In Inspector")]
    [Tooltip("Minimum and maximum movement speed of Squid, Jellyfish, Pufferfish and Shark. X = Min, Y = Max.")]
    public Vector2 animalSpeedRange = new Vector2(0.7f, 1.5f);

    [Tooltip("Seconds between animals in the same wave. X = Min, Y = Max. Larger values spawn animals less frequently.")]
    public Vector2 animalSpawnIntervalRange = new Vector2(3.5f, 5.5f);

    [Tooltip("How many animals appear before the lane takes a longer break. X = Min, Y = Max.")]
    public Vector2Int animalsPerWaveRange = new Vector2Int(1, 2);

    [Tooltip("Pause between traffic waves. X = Min, Y = Max. Larger values give the player a longer crossing window.")]
    public Vector2 animalWavePauseRange = new Vector2(4.0f, 6.0f);

    [Tooltip("Extra speed multiplier used only in Time Attack mode. 1 = same speed as normal mode.")]
    [Range(1f, 2f)]
    public float timeAttackAnimalSpeedMultiplier = 1.15f;
    [Range(3, 5)] public int minimumMovingRowsBeforeCoral = 1;
    [Range(3, 5)] public int maximumMovingRowsBeforeCoral = 5;
    private int pearlPatternLane;
    private int pearlPatternRemaining;
    private int pearlPatternDirection = 1;
    private int pearlPatternCooldown;
    private int tutorialObstacleIndex;
    private int movingRowsUntilCoral;
    private SeaObstacleType lastTrafficType = SeaObstacleType.Coral;
    private float startingPlayerZ;

    public float StartingSafeMaxZ => startingPlayerZ + safeRowsAheadOfStart;

    void Start()
    {
        ConfigureDifficulty();
        GenerateStartingMap();
    }

    void ConfigureDifficulty()
    {
        movingRowsUntilCoral = Random.Range(minimumMovingRowsBeforeCoral, maximumMovingRowsBeforeCoral + 1);
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
        PlayerController startingPlayer = FindAnyObjectByType<PlayerController>();
        startingPlayerZ = startingPlayer != null ? Mathf.Round(startingPlayer.transform.position.z) : 0f;

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

            if (GameSession.Mode != FishGameMode.Tutorial)
            {
                // Use world position rather than the loop index. The scene can
                // override rowsBehindPlayer, and index-based spawning previously
                // put the first traffic lane directly on the player's Z row.
                if (rowZ > StartingSafeMaxZ) SpawnObstacles(rowObject.transform);
                if (rowZ >= startingPlayerZ + pearlRowsAheadOfStart) SpawnCollectibles(rowObject.transform);
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

        if (movingRowsUntilCoral <= 0)
        {
            SpawnCoralRestRow(obstacleContainer);
            movingRowsUntilCoral = Random.Range(minimumMovingRowsBeforeCoral, maximumMovingRowsBeforeCoral + 1);
            return;
        }

        SpawnAnimalTrafficRow(obstacleContainer);
        movingRowsUntilCoral--;
    }

    private void SpawnAnimalTrafficRow(Transform obstacleContainer)
    {
        SeaObstacleType type = RandomMovingAnimalType();
        GameObject prefab = GetObstaclePrefab(type);
        if (prefab == null) return;
        int direction = Random.value < 0.5f ? -1 : 1;
        float modeSpeed = GameSession.Mode == FishGameMode.TimeAttack
            ? timeAttackAnimalSpeedMultiplier
            : 1f;

        float minSpeed = Mathf.Max(0.05f, Mathf.Min(animalSpeedRange.x, animalSpeedRange.y));
        float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(animalSpeedRange.x, animalSpeedRange.y));
        float speed = Random.Range(minSpeed, maxSpeed) * modeSpeed;
        float interval = Random.Range(animalSpawnIntervalRange.x, animalSpawnIntervalRange.y);
        int waveSize = Random.Range(animalsPerWaveRange.x, animalsPerWaveRange.y + 1);
        float wavePause = Random.Range(animalWavePauseRange.x, animalWavePauseRange.y);
        AnimalTrafficLane lane = obstacleContainer.gameObject.AddComponent<AnimalTrafficLane>();
        lane.Configure(prefab, type, direction, speed, interval, animalOffscreenDistance,
            obstacleHeight, GetNamedObstaclePrefab(type) == null, waveSize, wavePause);
    }

    private void SpawnCoralRestRow(Transform obstacleContainer)
    {
        // Rest rows use only stationary obstacles. Crab behaves like coral here:
        // it occupies a fixed tile and never receives MovingSeaObstacle/traffic-lane movement.
        SeaObstacleType staticType = Random.value < 0.70f
            ? SeaObstacleType.Coral
            : SeaObstacleType.Crab;

        int obstacleCount = staticType == SeaObstacleType.Crab
            ? 1
            : Random.Range(1, 3);

        List<int> lanes = new List<int>();
        for (int x = -4; x <= 4; x++) lanes.Add(x);

        for (int i = 0; i < obstacleCount && lanes.Count > 0; i++)
        {
            int choice = Random.Range(0, lanes.Count);
            GameObject obstacle = SpawnObstaclePrefab(staticType, obstacleContainer);
            if (obstacle != null)
                obstacle.transform.localPosition = new Vector3(lanes[choice], obstacleHeight, 0f);
            lanes.RemoveAt(choice);
        }
    }

    private SeaObstacleType RandomMovingAnimalType()
    {
        SeaObstacleType selected = WeightedMovingAnimalType();
        for (int attempt = 0; selected == lastTrafficType && attempt < 4; attempt++)
            selected = WeightedMovingAnimalType();
        lastTrafficType = selected;
        return selected;
    }

    private static SeaObstacleType WeightedMovingAnimalType()
    {
        // Crab is intentionally excluded: it is now a stationary tile obstacle,
        // spawned on the same kind of rest rows as coral.
        float roll = Random.value;
        if (roll < 0.30f) return SeaObstacleType.Squid;
        if (roll < 0.60f) return SeaObstacleType.Jellyfish;
        if (roll < 0.85f) return SeaObstacleType.Pufferfish;
        return SeaObstacleType.Shark;
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
