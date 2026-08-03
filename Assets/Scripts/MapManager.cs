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
        GenerateStartingMap();
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

        int obstacleCount =
        Random.Range(0, maximumObstaclesPerRow + 1);

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
        }
    }

    void RefreshObstacles(Transform row)
    {
        Transform obstacleContainer =
            row.Find("Obstacles");

        if (obstacleContainer == null)
        {
            return;
        }

        // Remove obstacles from the row's previous use
        foreach (Transform obstacle in obstacleContainer)
        {
            Destroy(obstacle.gameObject);
        }

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

        for (int x = -4; x <= 4; x++)
        {
            if (Random.value > collectibleSpawnChance)
            {
                continue;
            }

            Vector3 localPosition =
                new Vector3(x, collectibleHeight, 0f);

            Vector3 worldPosition =
                row.TransformPoint(localPosition);

            // Do not spawn on an obstacle
            bool obstacleFound = Physics.CheckBox(
                worldPosition + Vector3.up * 0.5f,
                new Vector3(0.4f, 0.5f, 0.4f),
                Quaternion.identity,
                obstacleLayer,
                QueryTriggerInteraction.Collide
            );

            if (obstacleFound)
            {
                continue;
            }

            int randomIndex =
                Random.Range(0, collectiblePrefabs.Count);

            GameObject collectible = Instantiate(
                collectiblePrefabs[randomIndex],
                collectibleContainer
            );

            collectible.transform.localPosition = localPosition;
        }
    }

    void RefreshCollectibles(Transform row)
    {
        Transform collectibleContainer =
            row.Find("Collectibles");

        if (collectibleContainer == null)
        {
            return;
        }

        foreach (Transform collectible in collectibleContainer)
        {
            Destroy(collectible.gameObject);
        }

        SpawnCollectibles(row);
    }

}