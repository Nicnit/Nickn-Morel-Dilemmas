using UnityEngine;

public class AlexParallaxLayer : MonoBehaviour
{
    public float parallaxMultiplier = 0.5f;

    Transform cam;
    Transform[] tiles;
    float tileWidth;

    void Start()
    {
        cam = Camera.main.transform;

        tiles = new Transform[transform.childCount];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = transform.GetChild(i);

        tileWidth = tiles[0].GetComponent<Renderer>().bounds.size.x;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            Camera main = Camera.main;
            if (main == null) return;
            cam = main.transform;
        }

        float parallaxAmount = cam.position.x * parallaxMultiplier;

        float camX = cam.position.x-parallaxAmount;

        float totalWidth = tileWidth * tiles.Length/2;

        int dist = Mathf.RoundToInt(camX / totalWidth);

        float half = (tiles.Length - 1) * 0.5f;

        for (int i = 0; i < tiles.Length; i++)
        {
            float count = i - half;

            tiles[i].position =
                Vector3.right * totalWidth * (dist + count) +
                new Vector3(0f, tiles[i].position.y, tiles[i].position.z)+
                parallaxAmount*Vector3.right;
        }
    }


}
