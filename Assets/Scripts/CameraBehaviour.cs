using UnityEngine;

/// Will adjust the camera to follow and face a target

public class CameraBehaviour : MonoBehaviour
{
    [Tooltip("What object should the camera be looking at")]
    public Transform target;

    [Tooltip("How offset will the camera be to the target")]
    public Vector3 offset = new Vector3(0, 3, -6);

    void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;

            transform.LookAt(target);
        }
    }
}