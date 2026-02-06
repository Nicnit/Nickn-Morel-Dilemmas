using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StarDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Slider> stars = new List<Slider>();
    
    private void Start()
    {
        GameManager.Instance.starManager.OnStarValueChanged += LoadStars;
        LoadStars();
    }
    void LoadStars()
    {
        LoadStars(GameManager.Instance.starManager.GetStarValue());
    }

    //TODO: Probably unncessary later on when stars are day based but temporary testing
    private void LoadStars(float starValue)
    {
        Debug.Log("LOADING STARRS: " + starValue);
        int index = 0;
        while (starValue > 0)
        {
            if (starValue >= 1)
            {
                starValue -= 1;
                stars[index].value = 1;
                Debug.Log("add star");
            }
            else
            {
                Debug.Log("add decimal");
                stars[index].value = starValue;
                starValue = 0;
            }

            index += 1;
        }
    }
}
