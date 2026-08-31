using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
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

    [Header("Collectibles")]
    public List<GameObject> collectiblePrefabs;
    [Range(0f, 1f)]
    public float collectibleSpawnChance = 0.15f;
    public float collectibleHeight = 0.5f;

    [Header("Obstacles")]
    public List<GameObject> obstaclePrefabs;
    [Range(0f, 1f)]
    public float obstacleSpawnChance = 0.3f;
    public float obstacleHeight = 0.5f;
    public int maximumObstaclesPerRow = 4;
    public LayerMask obstacleLayer;

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
                obstacleSpawnChance = 0.28f + stage * 0.04f;
                maximumObstaclesPerRow = 2;
                collectibleSpawnChance = 0.24f;
                break;
            case FishGameMode.TimeAttack:
                obstacleSpawnChance = 0.48f + stage * 0.06f;
                maximumObstaclesPerRow = 4 + stage;
                collectibleSpawnChance = 0.20f;
                break;
            default:
                obstacleSpawnChance = 0.38f + stage * 0.07f;
                maximumObstaclesPerRow = 3 + stage;
                collectibleSpawnChance = 0.18f;
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

            if (i >= 15) //Start spawning after the 15th row
            {
                SpawnObstacles(rowObject.transform);
                SpawnCollectibles(rowObject.transform);
            }

            rows.Add(rowObject.transform);

        }

        // 下一排接在目前地图最前面
        nextRowZ = startingZ + length;

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
        if (obstaclePrefabs == null ||
            obstaclePrefabs.Count == 0)
        {
            return;
        }

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

            int prefabIndex =
                Random.Range(0, obstaclePrefabs.Count);

            GameObject obstacle = Instantiate(
                obstaclePrefabs[prefabIndex],
                obstacleContainer
            );

            obstacle.transform.localPosition =
                new Vector3(xPosition, obstacleHeight, 0f);

            SeaObstacle behaviour = obstacle.GetComponent<SeaObstacle>();
            if (behaviour == null) behaviour = obstacle.AddComponent<SeaObstacle>();
            behaviour.Initialize(RandomObstacleType());
        }
    }

    SeaObstacleType RandomObstacleType()
    {
        float roll = Random.value;
        if (GameSession.Mode == FishGameMode.Tutorial)
        {
            if (roll < 0.30f) return SeaObstacleType.Squid;
            if (roll < 0.55f) return SeaObstacleType.Jellyfish;
            if (roll < 0.78f) return SeaObstacleType.Crab;
            return SeaObstacleType.Pufferfish;
        }

        float sharkChance = GameSession.SelectedStage == 2 ? 0.20f : 0.10f;
        if (roll < sharkChance) return SeaObstacleType.Shark;
        if (roll < 0.30f) return SeaObstacleType.Squid;
        if (roll < 0.52f) return SeaObstacleType.Crab;
        if (roll < 0.75f) return SeaObstacleType.Jellyfish;
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

        SpawnObstacles(row);
    }

    void SpawnCollectibles(Transform row)
    {
        if (collectiblePrefabs == null ||
            collectiblePrefabs.Count == 0)
        {
            return;
        }

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
            if (Random.value > collectibleSpawnChance)
            {
                continue;
            }

            Vector3 localPosition =
                new Vector3(x, collectibleHeight, 0f);

            int randomIndex =
                Random.Range(0, collectiblePrefabs.Count);

            GameObject collectible = Instantiate(
                collectiblePrefabs[randomIndex],
                collectibleContainer
            );

            collectible.transform.localPosition = localPosition;
            CollectibleItem item = collectible.GetComponent<CollectibleItem>();
            if (item == null) item = collectible.AddComponent<CollectibleItem>();
            item.kind = CollectibleKind.Pearl;
        }

        if (freeLanes.Count > 0)
        {
            float bonusRoll = Random.value;
            if (bonusRoll < 0.025f) CreateBonusCollectible(collectibleContainer, freeLanes, CollectibleKind.TreasureChest);
            else if (bonusRoll < 0.09f) CreateBonusCollectible(collectibleContainer, freeLanes, CollectibleKind.Starfish);
            else if (bonusRoll < 0.115f) CreateBonusCollectible(collectibleContainer, freeLanes, (CollectibleKind)Random.Range(3, 7));
        }
    }

    void CreateBonusCollectible(Transform container, List<int> freeLanes, CollectibleKind kind)
    {
        int lane = freeLanes[Random.Range(0, freeLanes.Count)];
        GameObject root = new GameObject(kind.ToString());
        root.transform.SetParent(container);
        root.transform.localPosition = new Vector3(lane, collectibleHeight + 0.2f, 0f);
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.48f;
        CollectibleItem item = root.AddComponent<CollectibleItem>();
        item.kind = kind;

        PrimitiveType shape = kind == CollectibleKind.TreasureChest ? PrimitiveType.Cube : PrimitiveType.Sphere;
        GameObject visual = GameObject.CreatePrimitive(shape);
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = kind == CollectibleKind.Starfish ? new Vector3(0.8f, 0.15f, 0.8f) :
            kind == CollectibleKind.TreasureChest ? new Vector3(0.8f, 0.55f, 0.55f) : Vector3.one * 0.65f;
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) Destroy(visualCollider);
        Color color = kind == CollectibleKind.Starfish ? new Color32(255, 143, 83, 255) :
            kind == CollectibleKind.TreasureChest ? new Color32(255, 206, 92, 255) : new Color32(91, 235, 218, 255);
        visual.GetComponent<Renderer>().material.color = color;
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

        SpawnCollectibles(row);
    }

}
