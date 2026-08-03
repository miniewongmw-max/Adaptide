using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform cameraTransform;

    public int width = 9;
    public int length = 20;

    // Player 后面先生成多少排
    public int rowsBehindPlayer = 5;

    public float recycleDistance = 5f;

    private List<Transform> rows = new List<Transform>();

    private float nextRowZ;

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

                GameObject tile = Instantiate(
                    tilePrefab,
                    rowObject.transform
                );

                tile.transform.localPosition =
                    new Vector3(xPosition, 0f, 0f);
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
}