using UnityEngine;

public enum SeaObstacleType
{
    Coral,
    Squid,
    Crab,
    Jellyfish,
    Pufferfish,
    Shark
}

public class SeaObstacle : MonoBehaviour
{
    public SeaObstacleType type;

    public void Initialize(SeaObstacleType newType)
    {
        Initialize(newType, true);
    }

    public void Initialize(SeaObstacleType newType, bool applyFallbackTint)
    {
        type = newType;
        name = newType.ToString();
        if (!applyFallbackTint) return;
        Color color = type switch
        {
            SeaObstacleType.Coral => new Color32(246, 121, 105, 255),
            SeaObstacleType.Squid => new Color32(116, 74, 159, 255),
            SeaObstacleType.Crab => new Color32(240, 91, 70, 255),
            SeaObstacleType.Jellyfish => new Color32(115, 211, 255, 255),
            SeaObstacleType.Pufferfish => new Color32(255, 205, 92, 255),
            _ => new Color32(43, 68, 84, 255)
        };
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.material.color = color;
    }

    public bool Interact(PlayerController player)
    {
        if (GameManager.Instance == null) return true;
        GameManager.Instance.NotifyObstacleEncountered(type);
        if (GameManager.Instance.IsInvincible || GameManager.Instance.TryUseShield())
        {
            Destroy(gameObject);
            return false;
        }

        switch (type)
        {
            case SeaObstacleType.Coral:
                GameManager.Instance.ShowStatus("CORAL BLOCK! CHOOSE ANOTHER LANE", 1f);
                return true;
            case SeaObstacleType.Squid:
                GameManager.Instance.ShowInkCloud();
                return true;
            case SeaObstacleType.Crab:
                GameManager.Instance.BeginCrabEscape(player, this);
                return true;
            case SeaObstacleType.Jellyfish:
                player.Stun(1.25f);
                GameManager.Instance.ShowStatus("ZAP! STUNNED", 1.25f);
                return true;
            case SeaObstacleType.Pufferfish:
                GameManager.Instance.ShowStatus("PUFFER BLOCK! FIND ANOTHER WAY", 1.1f);
                return true;
            case SeaObstacleType.Shark:
                if (GameManager.Instance.RetryTutorialSharkLesson()) return true;
                GameManager.Instance.GameOver("Caught by a shark");
                return true;
            default:
                return true;
        }
    }
}
