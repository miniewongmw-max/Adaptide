using UnityEngine;

/// <summary>Places imported art from its visible bottom, so different prefab pivots cannot sink into a row.</summary>
public static class PrefabGrounding
{
    public static void AlignVisibleBottom(GameObject instance, Transform localSpace, float localSurfaceHeight, float clearance = .015f)
    {
        if (instance == null || localSpace == null) return;
        if (!TryGetVisibleBounds(instance, out Bounds bounds)) return;

        float surfaceY = localSpace.TransformPoint(new Vector3(0f, localSurfaceHeight, 0f)).y;
        Vector3 position = instance.transform.position;
        position.y += surfaceY + Mathf.Max(0f, clearance) - bounds.min.y;
        instance.transform.position = position;
        instance.GetComponent<SeaLifeMotion>()?.CaptureRestPose();
    }

    private static bool TryGetVisibleBounds(GameObject instance, out Bounds bounds)
    {
        bool found = false;
        bounds = default;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (found) return true;

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            if (!found) { bounds = collider.bounds; found = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        return found;
    }
}
