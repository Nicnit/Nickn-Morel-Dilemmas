using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private FoodItem pickupItem;
    [SerializeField] private InventoryItem foodItemUI;
    public void OnInteract()
    {
        //DialogueSystem.TriggerDialogue();
        Debug.Log("Interacted with! " + this.gameObject.name);
    }


    //later make to on click
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //add to inventory
            GameManager.Instance.inventoryManager.AddFoodItem(pickupItem);
            Destroy(this.gameObject);
        }
    }

    void OnEnable()
    {
        GetComponent<SpriteRenderer>().sprite = pickupItem.defaultSprite;
    }
}