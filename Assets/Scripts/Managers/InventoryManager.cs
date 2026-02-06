using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SliceOperation
{
    public Vector3 centerPoint;
    public Vector3 planeNormal;
    public SliceOperation upperHullSlice;
    public SliceOperation lowerHullSlice;
    public bool upperDestroyed = false;
    public bool lowerDestroyed = false;
}

public class FoodItemObject
{
    public FoodItem foodItem;
    public float starQuality;
    public tags tags;
    public SliceOperation sliceOperation;
}
public class InventoryManager : MonoBehaviour
{
    public Dictionary<Item, int> items = new Dictionary<Item, int>();
    public static List<FoodItemObject> foodItems = new List<FoodItemObject>();


    public void AddFoodItem(FoodItem item, float quality = 5)
    {
        FoodItemObject foodobject = new FoodItemObject();
        foodobject.foodItem = item;
        foodobject.starQuality = quality;

        foodItems.Add(foodobject);
        if (AlexKitchenInventoryUI.Instance != null)
        {
            AlexKitchenInventoryUI.Instance.UpdateItems();
        }
    }

    public void AddFoodObject(FoodItemObject item)
    {
        foodItems.Add(item);
        AlexKitchenInventoryUI.Instance.UpdateItems();
    }


    public void AddItem(Item item) // remove later
    {
        if (items.ContainsKey(item))
        {
            items[item] += 1;
        }
        else
        {
            items.Add(item, 1);
        }
    }

    public void AddItem(Item item, int quantity) // remove later
    {
        if (items.ContainsKey(item))
        {
            items[item] += quantity;
        }
        else
        {
            items.Add(item, quantity);
        }
    }

    public Dictionary<Item, int> GetItems() // remove later
    {
        return items;
    }

    public List<FoodItemObject> GetFoodItems()
    {
        return foodItems;
    }
}
