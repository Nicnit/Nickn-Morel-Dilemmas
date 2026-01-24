using UnityEngine;
using System.Collections.Generic;

public class KitchenInventoryUI : MonoBehaviour
{
    public GameObject itemUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateItems();
    }

    void OnEnable()
    {
        UpdateItems();
    }

    public void UpdateItems()
    {
        Debug.Log("Loading Items");
        Dictionary<Item, int> items = GameManager.Instance.inventoryManager.GetItems();
        foreach (Item item in items.Keys)
        {
            Instantiate(itemUI, this.gameObject.transform);
        }
    }
}
