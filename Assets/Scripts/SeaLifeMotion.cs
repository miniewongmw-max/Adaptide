using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Lightweight procedural motion for imported reef art. Replacement prefabs
/// receive depth and motion without requiring an Animator Controller.
/// </summary>
public class SeaLifeMotion : MonoBehaviour
{
    private static int phaseCounter;
    [Header("Idle Motion")]
    [SerializeField] private float bobHeight = 0.11f;
    [SerializeField] private float bobSpeed = 1.9f;
    [SerializeField] private float swayAngle = 8f;
    [SerializeField] private float yawAngle = 7f;
    [SerializeField] private float swaySpeed = 1.45f;

    [Header("Hit Reaction")]
    [SerializeField] private float facePlayerDuration = 0.65f;
    [SerializeField] private float turnSpeed = 10f;

    private Quaternion restingLocalRotation;
    private float restingHeight;
    private float phase;
    private float spinAngle;
    private float reactUntil;
    private float shakeUntil;
    private float shakeStrength = 1f;
    private float spinReactionStarted;
    private float spinReactionUntil;
    private float spinReactionTurns;
    private Quaternion spinReactionStartRotation;
    private Transform reactionTarget;
    private bool moving;
    private bool spins;
    private bool configured;

    private void Awake()
    {
        phase = (++phaseCounter * 1.618f) % (Mathf.PI * 2f);
        CaptureRestPose();
    }

    public void Configure(bool isMoving, bool continuousSpin = false)
    {
        moving = isMoving;
        spins = continuousSpin;
        CaptureRestPose();
        ApplyShadows();
    }

    public void CaptureRestPose()
    {
        restingLocalRotation = transform.localRotation;
        restingHeight = transform.localPosition.y;
        configured = true;
    }

    public void ReactTo(Transform target)
    {
        if (target == null) return;
        reactionTarget = target;
        reactUntil = Time.time + facePlayerDuration;
    }

    public void Shake(float duration = 0.65f, float strength = 1f)
    {
        reactionTarget = null;
        reactUntil = 0f;
        spinReactionUntil = 0f;
        shakeStrength = Mathf.Max(0.5f, strength);
        shakeUntil = Time.time + duration;
    }

    public void SpinOnce(float duration = 0.72f, float turns = 1f)
    {
        reactionTarget = null;
        reactUntil = 0f;
        shakeUntil = 0f;
        spinReactionStarted = Time.time;
        spinReactionUntil = Time.time + Mathf.Max(0.2f, duration);
        spinReactionTurns = Mathf.Max(1f, turns);
        spinReactionStartRotation = transform.localRotation;
    }

    public void ApplyShadows()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private void Update()
    {
        if (!configured) CaptureRestPose();

        float motionScale = moving ? 1.35f : 1f;
        float wave = Mathf.Sin(Time.time * bobSpeed + phase);
        Vector3 position = transform.localPosition;
        // Upward-only bobbing keeps low-pivot imported FBX models from clipping
        // into the tile or appearing to dive underground.
        position.y = restingHeight + (wave + 1f) * 0.5f * bobHeight * motionScale;
        transform.localPosition = position;

        spinAngle = spins ? Mathf.Repeat(spinAngle + 65f * Time.deltaTime, 360f) : 0f;
        Quaternion idleRotation = restingLocalRotation * Quaternion.Euler(
            wave * swayAngle * 0.7f * motionScale,
            spinAngle + Mathf.Sin(Time.time * swaySpeed * 0.82f + phase * 1.3f) * yawAngle * motionScale,
            Mathf.Sin(Time.time * swaySpeed + phase) * swayAngle * motionScale);

        if (Time.time < spinReactionUntil)
        {
            float duration = Mathf.Max(0.01f, spinReactionUntil - spinReactionStarted);
            float t = Mathf.Clamp01((Time.time - spinReactionStarted) / duration);
            float eased = t * t * (3f - 2f * t);
            transform.localRotation = spinReactionStartRotation
                * Quaternion.Euler(0f, 360f * spinReactionTurns * eased, 0f);
            return;
        }

        if (Time.time < shakeUntil)
        {
            float shake = Mathf.Sin(Time.time * 42f + phase);
            Quaternion shakeRotation = idleRotation * Quaternion.Euler(
                shake * 5f * shakeStrength,
                shake * 12f * shakeStrength,
                -shake * 9f * shakeStrength);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, shakeRotation, turnSpeed * Time.deltaTime);
            return;
        }

        if (reactionTarget != null && Time.time < reactUntil)
        {
            Vector3 direction = reactionTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing, turnSpeed * Time.deltaTime);
            }
            return;
        }

        reactionTarget = null;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, idleRotation, turnSpeed * 0.55f * Time.deltaTime);
    }
}
