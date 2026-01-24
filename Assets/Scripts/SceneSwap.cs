using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneSwap : MonoBehaviour
{
    [SerializeField] private string nextScene;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collision is with the player object
        if (other.CompareTag("Player"))
        {
            SwapScene();
        }
    }

    public void SwapScene()
    {
        SceneManager.LoadScene(nextScene);
    }   
}
