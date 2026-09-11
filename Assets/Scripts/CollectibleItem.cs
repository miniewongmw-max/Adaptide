using UnityEngine;

public enum CollectibleKind
{
    Pearl,
    Starfish,
    TreasureChest,
    BubbleShield,
    SpeedDash,
    PearlMagnet,
    InvincibilityBubble
}

public class CollectibleItem : MonoBehaviour
{
    public CollectibleKind kind;
    private bool collected;

    private void Start()
    {
        SeaLifeMotion motion = GetComponent<SeaLifeMotion>();
        if (motion == null) motion = gameObject.AddComponent<SeaLifeMotion>();
        motion.Configure(false, true);
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        if (GameManager.Instance != null)
        {
            switch (kind)
            {
                case CollectibleKind.Pearl: GameManager.Instance.CollectPearls(1); break;
                case CollectibleKind.Starfish: GameManager.Instance.AddScore(2); break;
                case CollectibleKind.TreasureChest: GameManager.Instance.CollectPearls(10); break;
                case CollectibleKind.BubbleShield: GameManager.Instance.GrantPowerUp(0); break;
                case CollectibleKind.SpeedDash: GameManager.Instance.GrantPowerUp(1); break;
                case CollectibleKind.PearlMagnet: GameManager.Instance.GrantPowerUp(2); break;
                case CollectibleKind.InvincibilityBubble: GameManager.Instance.GrantPowerUp(3); break;
            }
        }
        GameAudioManager.Play(kind == CollectibleKind.Pearl ? GameSfx.Pearl : GameSfx.PowerUp);
        Destroy(gameObject);
    }
}
