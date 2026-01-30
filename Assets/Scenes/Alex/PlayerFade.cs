using System.Collections;
using UnityEngine;

public class PlayerFade : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public IEnumerator Fade(float alpha, float duration)
    {
        float currentTime = 0f;
        Color originalColor = spriteRenderer.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / duration;
            spriteRenderer.color = Color.Lerp(originalColor, targetColor, t);
            yield return null;
        }

        spriteRenderer.color = targetColor;
    }
}
