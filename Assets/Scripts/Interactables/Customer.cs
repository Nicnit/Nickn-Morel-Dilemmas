using UnityEngine;

public class Customer : MonoBehaviour, IInteractable
{
    private int tableNum;

    //TODO: change recipe based on day, remove serialized field
    private FoodItem orderedDish;
    [SerializeField] private CustomerType customerType;

    public void Start()
    {
        orderedDish = GameManager.Instance.orderManager.SelectRandomDish();
    }

    public void OnInteract()
    {
        Debug.Log("Interacted with! " + tableNum);
        if (GameManager.Instance.customerManager.GetTakenOrder(tableNum))
        {
            if (GameManager.Instance.orderManager.OrderDelivery(tableNum))
            {
                Destroy(this.gameObject);
            }
            else
            {
                Debug.Log("This isn't the correct order or you have no currently held dishes");
            }
        }
        else
        {
            GameManager.Instance.orderManager.AddOrder(tableNum, orderedDish);  
            Debug.Log("Ordered: " + orderedDish.name + " at table " + tableNum);    
            SetTakenOrder(true);
        }
    }

    public CustomerType GetCustomerType()
    {
        return customerType;
    }

    public void SetTakenOrder(bool value)
    {
        GameManager.Instance.customerManager.SetTakenOrder(tableNum, value);
    }

    public void SetTableNum(int tableNum)
    {
        this.tableNum = tableNum;
    }
}