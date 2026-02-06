using System;
using UnityEngine;

public class StarManager : MonoBehaviour
{
    public float starValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public event Action OnStarValueChanged;

    public float GetStarValue()
    {
        return starValue;
    }

    public void IncreaseStarValue(float value)
    {
        starValue += value;
        Debug.Log("stored star value: " + starValue);
        OnStarValueChanged?.Invoke();
    }

    public void DecreaseStarValue(float value)
    {
        starValue -= value;
        if (starValue < 0)
        {
            starValue = 0;
        }
        OnStarValueChanged?.Invoke();
    }
}
