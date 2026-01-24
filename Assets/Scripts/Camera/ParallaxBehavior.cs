using UnityEngine;

public class ParallaxBehavior : MonoBehaviour
{
    Transform cam;
    Vector3 camStartPos;

    Transform[] backgrounds;
    float[] backSpeed;
    float farthestBack;

    public float parallaxSpeed = 0.5f;

    void Start()
    {
        cam = Camera.main.transform;
        camStartPos = cam.position;

        int count = transform.childCount;
        backgrounds = new Transform[count];
        backSpeed = new float[count];

        for (int i = 0; i < count; i++)
            backgrounds[i] = transform.GetChild(i);

        CalculateBackSpeed(count);
    }

    void CalculateBackSpeed(int count)
    {
        farthestBack = 0f;

        for (int i = 0; i < count; i++)
        {
            float depth = Mathf.Abs(backgrounds[i].position.z - cam.position.z);
            if (depth > farthestBack)
                farthestBack = depth;
        }

        for (int i = 0; i < count; i++)
        {
            float depth = Mathf.Abs(backgrounds[i].position.z - cam.position.z);
            backSpeed[i] = 1f - (depth / farthestBack);
        }
    }

    void LateUpdate()
    {
        if(cam==null) cam = Camera.main.transform;
        float deltaX = cam.position.x - camStartPos.x;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * parallaxSpeed;
            Vector3 pos = backgrounds[i].position;
            backgrounds[i].position = new Vector3(
                pos.x + deltaX * speed,
                pos.y,
                pos.z
            );
        }

    }
}
