using UnityEngine;
using EzySlice;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using DG.Tweening;
public class EzyMeshSlicer : MonoBehaviour
{
    Vector3 p1World, p2World;
    public GameObject sliceParticle;
    public float sliceTime = 1;
    public AnimationCurve sliceCurve;
    public float sliceMoveDistance = 1;
    public float sliceMoveTime = 1;
    public AnimationCurve sliceMoveCurve;
    public LineRenderer sliceLineRenderer;
    public LineRenderer effectLineRenderer;
    public LineRenderer outlineLineRenderer;
    private float sliceTimer;
    private bool allowCut;
    private List<Vector3> outlinePoints = new List<Vector3>();
    private float outlineDistance;
    private SliceOperation rootSliceOperation; // Root of the slice tree
    private Dictionary<GameObject, (SliceOperation operation, bool isUpper)> meshToSliceOperation = new Dictionary<GameObject, (SliceOperation, bool)>(); // Track which mesh belongs to which slice and if it's upper/lower

    void Start()
    {
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Collider2D>().enabled = true;
    }

    public void OnMouseDown()
    {

        if(sliceTimer > 0) return;
        sliceLineRenderer.enabled = true;
        p1World = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));
        sliceLineRenderer.SetPosition(0, p1World);
    }

    public void OnMouseUp()
    {

        if(sliceTimer > 0) return;
        p2World = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));
        sliceLineRenderer.SetPosition(1, p2World);
        sliceTimer = 1;
    }


    void Update()
    {
        
        if (Input.GetMouseButton(1))
        {
            Vector3 currentPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));
            
            if (outlinePoints.Count == 0)
            {
                outlinePoints.Add(currentPoint);
            }
            else
            {
                outlineDistance += Vector2.Distance(currentPoint, outlinePoints[outlinePoints.Count - 1]);
                if (outlineDistance >= 0.1f)
                {
                    outlinePoints.Add(currentPoint);
                    outlineDistance = 0f;
                }
            }
            
            outlineLineRenderer.positionCount = outlinePoints.Count;
            for (int i = 0; i < outlinePoints.Count; i++)
            {
                outlineLineRenderer.SetPosition(i, outlinePoints[i]);
            }
        }
        else
        {
            if(outlineLineRenderer.positionCount > 0)
            {
                var selectedMeshes = GetTransformsInsideOutline(
                    new List<Transform>(GetComponentsInChildren<MeshFilter>().Select(mf => mf.transform)),
                    outlinePoints
                );
                
                // Get inventory item once before animations
                InventoryItem parentInventoryItem = null;
                if(selectedMeshes.Count > 0)
                {
                    parentInventoryItem = selectedMeshes[0].transform.parent.GetComponent<InventoryItem>();
                }
                
                foreach(Transform mesh in selectedMeshes){
                    mesh.DOMove(mesh.position + (mesh.position-mesh.parent.position)*3, .25f).OnComplete(()=>
                    {
                        mesh.DOMove(transform.position -transform.up, 0.5f).OnComplete(()=>
                        {
                            if (parentInventoryItem != null && parentInventoryItem.foodItem != null && selectedMeshes.IndexOf(mesh) == 0)
                            {
                                FoodItemObject modifiedFoodItem = new FoodItemObject();
                                modifiedFoodItem.foodItem = parentInventoryItem.foodItem.foodItem;
                                modifiedFoodItem.starQuality = parentInventoryItem.foodItem.starQuality;
                                modifiedFoodItem.sliceOperation = rootSliceOperation;
                                modifiedFoodItem.tags = tags.chopped;
                                
                                GameManager.Instance.inventoryManager.AddFoodObject(modifiedFoodItem);
                                
                            }
                            Destroy(parentInventoryItem.gameObject);
                        });
                    });
                }
                if(selectedMeshes.Count > 0) foreach(Transform child in selectedMeshes[0].transform.parent){
                    if(!child.gameObject.activeSelf || selectedMeshes.Contains(child)) continue;
                    child.DOMove(child.position + (child.position-child.parent.position)*3, .25f).OnComplete(()=>
                    {
                        child.DOMove(transform.position + transform.up, 0.5f).OnComplete(()=>
                        {
                            Destroy(child.gameObject);
                        });
                    });
                }
                

                outlinePoints.Clear();
                outlineLineRenderer.positionCount = 0;
            }
        }

        if (sliceTimer > 0f)
        {
            allowCut = true;

            sliceTimer -= Time.deltaTime / sliceTime;
            sliceTimer = Mathf.Max(sliceTimer, 0f); // IMPORTANT

            effectLineRenderer.enabled = true;
            effectLineRenderer.widthMultiplier = 1f;

            int num = 10;
            if (effectLineRenderer.positionCount != num)
                effectLineRenderer.positionCount = num;

            effectLineRenderer.SetPosition(0, p1World);

            for (int i = 1; i < num; i++)
            {
                float percent = i / (num - 1f);
                float curveT = sliceCurve.Evaluate(1f - sliceTimer);
                effectLineRenderer.SetPosition(
                    i,
                    Vector3.Lerp(p1World, p2World, percent * curveT)
                );
            }
        }
        else
        {
            sliceLineRenderer.SetPosition(1, Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5)));
            if (allowCut)
            {
                allowCut = false;
                SliceAllChildren();
                StartCoroutine(ShrinkSliceEffect(0.5f));

                sliceLineRenderer.enabled = false;
            }
        }
    }


    IEnumerator ShrinkSliceEffect(float duration)
    {
        for(float t=0; t<duration; t+=Time.fixedDeltaTime)
        {
            effectLineRenderer.widthMultiplier = Mathf.Lerp(1, 0, t/duration);
            yield return new WaitForFixedUpdate();
        }
        effectLineRenderer.enabled = false;
    }

    void SliceAllChildren()
    {
        List<Transform> children = new List<Transform>();
        foreach (MeshFilter child in transform.GetComponentsInChildren<MeshFilter>()) 
        {
            if(child.gameObject.activeInHierarchy == false) continue;
            if(CutHitsMesh(child.transform, p1World, p2World)) children.Add(child.transform);
        }

        Transform[] childrenArray = children.ToArray();
        
        // Record slice operation in local space
        Vector3 centerPoint = (p1World + p2World) * 0.5f;
        Vector3 swipeDir = (p2World - p1World).normalized;
        Vector3 planeNormalOp = Vector3.Cross(swipeDir, Camera.main.transform.forward).normalized;
        
        // Convert to local space relative to this slicer's transform
        Vector3 localCenterPoint = transform.parent != null ? transform.parent.InverseTransformPoint(centerPoint) : centerPoint;
        
        SliceOperation currentSlice = new SliceOperation
        {
            centerPoint = localCenterPoint,
            planeNormal = planeNormalOp
        };
        
        // If this is the first slice, make it root; otherwise it will be added to the tree by the parent
        if (rootSliceOperation == null)
        {
            rootSliceOperation = currentSlice;
        }
        
        //go throgh every child and slice
        foreach (Transform child in childrenArray)
        {
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (!meshFilter || !meshRenderer) continue;

            //get direction and normal to make the slicing plane
            Vector3 swipeDir2 = (p2World - p1World).normalized;
            Vector3 planeNormal = Vector3.Cross(swipeDir2, Camera.main.transform.forward).normalized;

            Vector3 planePoint = p1World;

            //create the hull, which is just the sliced mesh data from the plane, used from the ezyslice library
            SlicedHull hull = child.gameObject.Slice(
                planePoint,
                planeNormal
            );

            if (hull == null) continue;

            //make the two sliced mesh portions
            GameObject upper = hull.CreateUpperHull(child.gameObject, meshRenderer.sharedMaterial);
            GameObject lower = hull.CreateLowerHull(child.gameObject, meshRenderer.sharedMaterial);

            upper.transform.SetParent(child.transform.parent, false);
            lower.transform.SetParent(child.transform.parent, false);

            // Track which slice operation this mesh belongs to and whether it's upper or lower
            meshToSliceOperation[upper] = (currentSlice, true);
            meshToSliceOperation[lower] = (currentSlice, false);
            
            // If this mesh came from a previous slice, add current slice to the appropriate branch
            if (meshToSliceOperation.ContainsKey(child.gameObject))
            {
                var (parentSlice, wasUpper) = meshToSliceOperation[child.gameObject];
                if (wasUpper)
                {
                    parentSlice.upperHullSlice = currentSlice;
                }
                else
                {
                    parentSlice.lowerHullSlice = currentSlice;
                }
                meshToSliceOperation.Remove(child.gameObject);
            }

            //do the moving animation
            StartCoroutine(MoveSlice(upper.transform, planeNormal, sliceMoveTime, sliceMoveDistance));
            StartCoroutine(MoveSlice(lower.transform, -planeNormal, sliceMoveTime, sliceMoveDistance));

            Destroy(child.gameObject);
        }
    }

    bool CutHitsMesh(Transform meshTf, Vector3 mousePoint1, Vector3 mousePoint2)
    {
        MeshFilter mf = meshTf.GetComponent<MeshFilter>();
        if (!mf) return false;

        //get mesh triangle data
        Mesh mesh = mf.mesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        Vector2 m1 = mousePoint1;
        Vector2 m2 = mousePoint2;

        for (int i = 0; i < tris.Length; i += 3)
        {
            //get triangle points in world space
            Vector2 a = meshTf.TransformPoint(verts[tris[i]]);
            Vector2 b = meshTf.TransformPoint(verts[tris[i + 1]]);
            Vector2 c = meshTf.TransformPoint(verts[tris[i + 2]]);

            //first detect if line intersects triangle of mesh
            if (LineIntersectsTriangle(m1, m2, a, b, c))
            {
                //then check if both points are outside of triangle to make the player have to actually slice through
                if (IsPointInTriangle(m1, a, b, c) ||
                    IsPointInTriangle(m2, a, b, c))
                    return false;

                return true;
            }
        }
        return false;
    }


    bool LineIntersectsTriangle(Vector2 p1, Vector2 p2, Vector2 a, Vector2 b, Vector2 c) //returns true if the line from p1 to p2 intersects with any line on the triangle
    {
        bool Seg(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2)
        {
            Vector2 r = p2 - p, s = q2 - q;
            float d = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(d) < 1e-6f) return false;
            float u = ((q.x - p.x) * r.y - (q.y - p.y) * r.x) / d;
            float t = ((q.x - p.x) * s.y - (q.y - p.y) * s.x) / d;
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        return Seg(p1, p2, a, b) ||
            Seg(p1, p2, b, c) ||
            Seg(p1, p2, c, a);
    }


    public bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) // returns true if our point p is inside the triangle
    {
        float s1 = Mathf.Sign((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x));
        float s2 = Mathf.Sign((c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x));
        float s3 = Mathf.Sign((a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x));

        return (s1 == s2 && s2 == s3);
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

    public List<Transform> GetTransformsInsideOutline(List<Transform> meshTransforms, List<Vector3> outlinePoints) // get all transforms in outline
    {
        List<Transform> insideTransforms = new List<Transform>();
        
        if (outlinePoints.Count < 3) return insideTransforms; // atleast 3
        
        foreach (Transform t in meshTransforms)
        {
            Vector2 point2D = new Vector2(t.position.x, t.position.y);
            if (IsPointInPolygon(point2D, outlinePoints))
            {
                insideTransforms.Add(t);
            }
        }
        
        return insideTransforms;
    }

    private bool IsPointInPolygon(Vector2 point, List<Vector3> polygon) // if point in polygon return true
    {
        int intersections = 0;
        int count = polygon.Count;
        
        for (int i = 0; i < count; i++)
        {
            Vector2 a = new Vector2(polygon[i].x, polygon[i].y);
            Vector2 b = new Vector2(polygon[(i + 1) % count].x, polygon[(i + 1) % count].y);
            
            // check if ray intersects edge
            if ((a.y > point.y) != (b.y > point.y))
            {
                float xIntersect = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < xIntersect)
                {
                    intersections++;
                }
            }
        }
        
        // odd = inside, even = outside
        return (intersections % 2) == 1;
    }
}
