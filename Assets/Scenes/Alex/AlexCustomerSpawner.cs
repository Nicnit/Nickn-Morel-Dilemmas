using UnityEngine;
using System.Collections.Generic;
using System;

public class AlexCustomerSpawner : MonoBehaviour
{
    public Transform tableHolder;
    private List<GameObject> possibleCustomers = new List<GameObject>();
    public GameObject[] tables = new GameObject[5]; // also hardcoded as 5 atm 
    public GameObject[] spawnedCustomers = new GameObject[5]; //hardcoded as 5 atm
    public float timeInBetween;  // in second
    public float time;

    public void Start()
    {
        possibleCustomers = GameManager.Instance.customerManager.possibleCustomers;

        ResizeArrays();

        LoadData();
    }

    public void ResizeArrays()
    {
        int count = tableHolder.childCount;
        Array.Resize(ref tables, count);
        Array.Resize(ref spawnedCustomers, count);
        Array.Resize(ref GameManager.Instance.customerManager.activeCustomers, count);

        for(int i = 0; i < count; i++)
        {
            tables[i] = tableHolder.GetChild(i).gameObject;
        }
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
        int tableIndex = UnityEngine.Random.Range(0, openTables.Count);
        int customerIndex = GetRandomCustomer();

        SpawnCustomer(possibleCustomers[customerIndex].GetComponent<Customer>().GetCustomerType(), openTables[tableIndex], false);
    }

    int GetRandomCustomer()
    {
        //based on weights, time
        int customerIndex = UnityEngine.Random.Range(0, possibleCustomers.Count);
        return customerIndex;
    }

    //used for loadData
    public void SpawnCustomer(CustomerType customerType, int tableNum, bool takenOrder)
    {
        GameObject toSpawnCustomer = GameManager.Instance.customerManager.GetGameObjectFromCustomerType(customerType);
        GameObject spawnedCustomer = Instantiate(toSpawnCustomer, tables[tableNum].transform.position, Quaternion.identity);
        spawnedCustomer.GetComponent<Customer>().SetTableNum(tableNum);
        print(tableNum);
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