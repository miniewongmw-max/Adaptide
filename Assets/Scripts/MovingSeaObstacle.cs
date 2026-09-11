using UnityEngine;

public class MovingSeaObstacle : MonoBehaviour
{
    private int direction;
    private float speed;
    private float despawnDistance;

    public void Configure(int travelDirection, float movementSpeed, float limit)
    {
        direction = travelDirection >= 0 ? 1 : -1;
        speed = Mathf.Max(0.5f, movementSpeed);
        despawnDistance = Mathf.Max(9f, limit);
    }

    private void Update()
    {
        transform.localPosition += Vector3.right * (direction * speed * Time.deltaTime);
        if ((direction > 0 && transform.localPosition.x > despawnDistance) ||
            (direction < 0 && transform.localPosition.x < -despawnDistance))
            Destroy(gameObject);
    }
}
