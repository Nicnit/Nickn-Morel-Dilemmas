using UnityEngine;

public class ParallaxLayer : MonoBehaviour
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
        if(cam==null) cam = Camera.main.transform;
        float camX = cam.position.x;

        transform.position = new Vector3(camX * parallaxMultiplier, transform.position.y, transform.position.z);

        foreach (Transform tile in tiles)
        {
            float diff = tile.position.x - camX;

            if (diff < -tileWidth)
                tile.position += Vector3.right * (tileWidth * tiles.Length - 0.01f);
            else if (diff > tileWidth)
                tile.position -= Vector3.right * (tileWidth * tiles.Length - 0.01f);
        }
    }
}
