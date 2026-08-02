using UnityEngine;
using UnityEngine.UI;

public class CurrencyManager : MonoBehaviour
{
    public int coins = 100;
    public Text coinText;

    void Start()
    {
         PlayerPrefs.DeleteKey("Coins");
        coins = PlayerPrefs.GetInt("Coins", 100);
        UpdateUI();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveCoins();
    }

    public void SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            SaveCoins();
        }
    }

    void SaveCoins()
    {
        PlayerPrefs.SetInt("Coins", coins);
        UpdateUI();
    }

    void UpdateUI()
    {
        coinText.text = "Coins : " + coins;
    }
}