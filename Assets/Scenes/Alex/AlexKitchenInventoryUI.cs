using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class AlexKitchenInventoryUI : MonoBehaviour
{
    public GameObject itemUI;
    public Vector2 originalPosition, originalSize;
    public static AlexKitchenInventoryUI Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        originalPosition = this.GetComponent<Transform>().localPosition;
        originalSize = this.GetComponent<Transform>().localScale;
    }

    void OnEnable()
    {
        UpdateItems();
    }

    public void UpdateItems()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Loading Items");
        Dictionary<Item, int> items = GameManager.Instance.inventoryManager.GetItems();
        foreach (Item item in items.Keys)
        {
            Instantiate(itemUI, this.gameObject.transform);
        }
    }

    public void Grow()
    {
        transform.DOScale(originalSize * 3f, 0.2f);
    }

    public void Shrink()
    {
        transform.DOScale(originalSize, 0.2f);
    }

    public IEnumerator setTransform(Vector2 position, Vector2 size)
    {
        float time = 1;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            float normalizedTime = t / time;
            this.GetComponent<Transform>().localPosition = Vector2.Lerp(this.GetComponent<Transform>().localPosition, position, normalizedTime);
            this.GetComponent<Transform>().localScale = Vector2.Lerp(this.GetComponent<Transform>().localScale, size, normalizedTime);
            yield return null;
        }
    }
}
