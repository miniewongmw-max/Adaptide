using UnityEngine;

public class AnimalTrafficLane : MonoBehaviour
{
    private GameObject animalPrefab;
    private SeaObstacleType animalType;
    private int direction;
    private float speed;
    private float spawnInterval;
    private float offscreenDistance;
    private float spawnHeight;
    private float nextSpawnTime;
    private bool applyFallbackTint;
    private int animalsPerWave;
    private int spawnedInWave;
    private float pauseDuration;

    [Header("Animal Facing")]
    [SerializeField] private Vector3 rightFacingRotation = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 leftFacingRotation = new Vector3(0f, 90f, 0f);

    public SeaObstacleType AnimalType => animalType;
    public int Direction => direction;
    public float Speed => speed;
    public int AnimalsPerWave => animalsPerWave;
    public float PauseDuration => pauseDuration;

    public void Configure(GameObject prefab, SeaObstacleType type, int travelDirection,
        float laneSpeed, float interval, float distance, float height, bool tintFallback,
        int waveSize, float wavePause)
    {
        animalPrefab = prefab;
        animalType = type;
        direction = travelDirection >= 0 ? 1 : -1;
        speed = Mathf.Max(0.5f, laneSpeed);
        spawnInterval = Mathf.Max(0.65f, interval);
        offscreenDistance = Mathf.Max(8f, distance);
        spawnHeight = height;
        applyFallbackTint = tintFallback;
        animalsPerWave = Mathf.Max(2, waveSize);
        pauseDuration = Mathf.Max(0.75f, wavePause);

        // Prewarm the lane as though animals had already entered from off-screen,
        // then continue spawning every fixed interval from the hidden side.
        float spacing = speed * spawnInterval;
        for (int i = 0; i < 3; i++)
        {
            float travelled = i * spacing;
            if (travelled > offscreenDistance * 1.7f) break;
            SpawnAnimal(StartX() + direction * travelled);
        }
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (animalPrefab == null || Time.time < nextSpawnTime) return;
        SpawnAnimal(StartX());
        spawnedInWave++;
        if (spawnedInWave >= animalsPerWave)
        {
            spawnedInWave = 0;
            nextSpawnTime = Time.time + pauseDuration;
        }
        else nextSpawnTime = Time.time + spawnInterval;
    }

    private float StartX() => direction > 0 ? -offscreenDistance : offscreenDistance;

    private void SpawnAnimal(float x)
    {
        GameObject animal = Instantiate(animalPrefab, transform);
        animal.name = animalType + " Traffic";
        animal.transform.localPosition = new Vector3(x, spawnHeight, 0f);
        animal.transform.localRotation = Quaternion.Euler(
            direction > 0 ? rightFacingRotation : leftFacingRotation);

        SeaObstacle obstacle = animal.GetComponent<SeaObstacle>();
        if (obstacle == null) obstacle = animal.AddComponent<SeaObstacle>();
        obstacle.Initialize(animalType, applyFallbackTint);

        Collider[] colliders = animal.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            BoxCollider trigger = animal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
        }
        else
        {
            foreach (Collider collider in colliders) collider.isTrigger = true;
        }

        Rigidbody body = animal.GetComponent<Rigidbody>();
        if (body == null) body = animal.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        MovingSeaObstacle movement = animal.GetComponent<MovingSeaObstacle>();
        if (movement == null) movement = animal.AddComponent<MovingSeaObstacle>();
        movement.Configure(direction, speed, offscreenDistance + 2f);
        animal.GetComponent<SeaLifeMotion>()?.Configure(true);
    }
}
