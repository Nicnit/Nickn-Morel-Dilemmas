using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    // public List<GameObject> possibleCustomers = new List<GameObject>();
    public GameObject[] tables = new GameObject[5]; // also hardcoded as 5 atm 
    public GameObject[] spawnedCustomers = new GameObject[5]; //hardcoded as 5 atm
    public float timeInBetween;  // in second
    public float time;

    public void Start()
    {
        LoadData();
    }
    
    private void Update()
    {
        time += Time.deltaTime;
        if (time > timeInBetween)
        {
            SpawnCustomer();
            time = 0;
        }
    }

    //randomized, updates gameobject and customerManager
    public void SpawnCustomer()
    {
        List<int> openTables = GameManager.Instance.customerManager.GetFreeTables();
        if (openTables.Count == 0)
        {
            return;
        }
        int tableIndex = Random.Range(0, openTables.Count);
        List<GameObject> possibleCustomers = GameManager.Instance.customerManager.GetPossibleCustomers();
        int customerIndex = Random.Range(0, possibleCustomers.Count);

        SpawnCustomer(possibleCustomers[customerIndex].GetComponent<Customer>().GetCustomerType(), openTables[tableIndex], false);
    }

    //used for loadData
    public void SpawnCustomer(CustomerType customerType, int tableNum, bool takenOrder)
    {
        GameObject toSpawnCustomer = GameManager.Instance.customerManager.GetGameObjectFromCustomerType(customerType);
        GameObject spawnedCustomer = Instantiate(toSpawnCustomer, tables[tableNum].transform.position, Quaternion.identity);
        spawnedCustomer.GetComponent<Customer>().SetTableNum(tableNum);
        spawnedCustomers[tableNum] = spawnedCustomer;
        GameManager.Instance.customerManager.AddCustomer(customerType, tableNum, takenOrder);
    }

    public void DespawnCustomer(int tableNum)
    {
        Destroy(spawnedCustomers[tableNum]);
        spawnedCustomers[tableNum] = null;
    }

    public void LoadData()
    {
        CustomerInfo[] spawnedCustomers = GameManager.Instance.customerManager.GetSpawnedCustomers();
        foreach (CustomerInfo customerInfo in spawnedCustomers)
        {
            if (customerInfo != null)
            {
                SpawnCustomer(customerInfo.customerType, customerInfo.tableNum, customerInfo.takenOrder);
            }
        }
    }
}