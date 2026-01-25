using UnityEngine;
using EzySlice;
using System.Collections;

public class EzyMeshSlicer : MonoBehaviour
{
    Vector3 p1World, p2World;
    public GameObject sliceParticle;
    public float sliceTime = 1;
    public AnimationCurve sliceCurve;
    public float sliceMoveDistance = 1;
    public float sliceMoveTime = 1;
    public AnimationCurve sliceMoveCurve;
    private float sliceTimer;
    private GameObject sliceParticleInstance;  

    public void SetPoint1()
    {
        if(sliceTimer > 0) return;

        p1World = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));
    }

    public void SetPoint2()
    {
        if(sliceTimer > 0) return;

        p2World = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));

        sliceTimer = 1;
        sliceParticleInstance = Instantiate(sliceParticle, p1World, Quaternion.identity);
        var main = sliceParticleInstance.GetComponent<ParticleSystem>().main;
        main.duration = sliceTime;
        sliceParticleInstance.GetComponent<ParticleSystem>().Play();
    }

    void FixedUpdate()
    {
        if(sliceTimer > 0)
        {
            sliceTimer -= Time.fixedDeltaTime/sliceTime;
            sliceParticleInstance.transform.position = Vector3.Lerp(p1World, p2World, sliceCurve.Evaluate(1 - sliceTimer));
        }
        else
        {
            if(sliceParticleInstance != null)
            {
                SliceAllChildren();
            }
        }
    }

    void SliceAllChildren()
    {
        var children = new Transform[transform.childCount];
        for (int i = 0; i < children.Length; i++) children[i] = transform.GetChild(i);
        
        foreach (Transform child in children)
        {
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (!meshFilter || !meshRenderer) continue;

            Vector3 swipeDir = (p2World - p1World).normalized;
            Vector3 planeNormal = Vector3.Cross(swipeDir, Camera.main.transform.forward).normalized;

            Vector3 planePoint = p1World;

            SlicedHull hull = child.gameObject.Slice(
                planePoint,
                planeNormal
            );

            if (hull == null) continue;

            GameObject upper = hull.CreateUpperHull(child.gameObject, meshRenderer.sharedMaterial);
            GameObject lower = hull.CreateLowerHull(child.gameObject, meshRenderer.sharedMaterial);

            upper.transform.SetParent(transform, false);
            lower.transform.SetParent(transform, false);

            StartCoroutine(MoveSlice(upper.transform, planeNormal, sliceMoveTime, sliceMoveDistance));
            StartCoroutine(MoveSlice(lower.transform, -planeNormal, sliceMoveTime, sliceMoveDistance));

            Destroy(child.gameObject);
        }
    }

    IEnumerator MoveSlice(Transform sliceTransform, Vector3 point, float duration, float distance)
    {
        for(float t=0; t<duration; t+=Time.fixedDeltaTime)
        {
            Vector3 originalposition = sliceTransform.position;
            Vector3 targetPosition = originalposition + point.normalized * distance;
            sliceTransform.position = Vector3.Lerp(originalposition, targetPosition, sliceMoveCurve.Evaluate(t/duration));
            yield return new WaitForFixedUpdate();
        }
    }
}
