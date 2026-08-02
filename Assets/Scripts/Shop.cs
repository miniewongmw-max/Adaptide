using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Shop : MonoBehaviour
{
    public CurrencyManager money;
    public GameObject confirmPanel;
    public GameObject buyButton;
    public Text messageText;

    private int currentPrice;

    void Start()
    {
        messageText.text = "";
        buyButton.SetActive(true);
    }

    // Buy Button
    public void OpenConfirm(int price)
    {
        currentPrice = price;
        confirmPanel.SetActive(true);
        buyButton.SetActive(false);
        ShowMessage("Confirm Purchase?");
    }

    // Yes Button
    public void ConfirmPurchase()
    {
        confirmPanel.SetActive(false);

        if (money.coins >= currentPrice)
        {
            money.SpendCoins(currentPrice);
            buyButton.SetActive(true);
            ShowMessage("Purchase Successful!");
        }
        else
        {
            buyButton.SetActive(true);
            ShowMessage("Insufficient Coins!");
        }
    }

    // No Button
    public void CancelPurchase()
    {
        confirmPanel.SetActive(false);
        buyButton.SetActive(true);
        ShowMessage("Purchase Cancelled.");
    }

    void ShowMessage(string msg)
    {
        StopAllCoroutines();
        StartCoroutine(Message(msg));
    }

    IEnumerator Message(string msg)
    {
        messageText.text = msg;

        yield return new WaitForSeconds(2f);

        messageText.text = "";
    }
}