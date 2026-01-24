using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AlexSceneSwap : MonoBehaviour
{
    public Transform movePlayer;
    [SerializeField] private string nextScene;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player"))
        {
            var movement = other.GetComponent<AlexTopDownMovement>();
            if (movement != null)
            {
                movement.SetCurrDirection(Vector3.zero);
                movement.SetDirection(Vector3.zero);
            }

            isTransitioning = true;
            StartCoroutine(SwapSceneCoroutine(other.gameObject));
        }
    }

    IEnumerator SwapSceneCoroutine(GameObject player)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        Scene targetScene = SceneManager.GetSceneByName(nextScene);
        if (!targetScene.isLoaded)
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
            yield return loadOp;
        }

        targetScene = SceneManager.GetSceneByName(nextScene);

        ActivateSceneRoots(targetScene);

        SceneManager.SetActiveScene(targetScene);

        if (movePlayer != null)
        {
            player.transform.position = movePlayer.position;
        }

        DeactivateScene(currentScene);

        isTransitioning = false;
    }


    void DeactivateScene(Scene targetScene)
    {
        if (!targetScene.IsValid()) return;

        foreach (GameObject rootObject in targetScene.GetRootGameObjects())
        {
            rootObject.SetActive(false);
        }
    }

    public void ActivateScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid()) return;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            rootObject.SetActive(true);
        }

        SceneManager.SetActiveScene(scene);
    }

    void ActivateSceneRoots(Scene scene)
    {
        if (!scene.IsValid()) return;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            rootObject.SetActive(true);
        }
    }

}
