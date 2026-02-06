using UnityEngine;
using EzySlice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
public class EzyMeshSlicer : MonoBehaviour
{
    Vector3 p1World, p2World;
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

    void Start()
    {
        GetComponent<Collider2D>().enabled = false;
        GetComponent<Collider2D>().enabled = true;
    }

    public void InitializeFromItem(InventoryItem item) // when moving an already sliced item onto the board
    {
        if (item == null || item.foodItem == null || item.foodItem.sliceOperation == null)
        {
            rootSliceOperation = null;
            return;
        }

        rootSliceOperation = item.foodItem.sliceOperation;
    }

    Vector3 MouseWorldPos => Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));

    public void OnMouseDown()
    {
        if (sliceTimer > 0) return;
        sliceLineRenderer.enabled = true;
        p1World = MouseWorldPos;
        sliceLineRenderer.SetPosition(0, p1World);
    }

    public void OnMouseUp()
    {
        if (sliceTimer > 0) return;
        p2World = MouseWorldPos;
        sliceLineRenderer.SetPosition(1, p2World);
        sliceTimer = 1;
    }


    void Update()
    {
        //Mouse outline logic
        
        if (Input.GetMouseButton(1))
        {
            Vector3 currentPoint = MouseWorldPos;
            
            if (outlinePoints.Count == 0)
            {
                outlinePoints.Add(currentPoint);
            }
            else
            {
                outlineDistance += Vector2.Distance(currentPoint, outlinePoints[outlinePoints.Count - 1]);
                if (outlineDistance >= 0.025f)
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

                //animate each unselected mesh to go away from the player and be destroyed
                if(selectedMeshes.Count > 0) foreach(Transform child in selectedMeshes[0].transform.parent)
                {
                    if(!child.gameObject.activeSelf || selectedMeshes.Contains(child)) continue;
                    if(child.GetComponent<Collider2D>()) child.GetComponent<Collider2D>().enabled = false;
                    
                    // mark branch as destroyed in the slice tree
                    SliceInfo info = child.GetComponent<SliceInfo>();
                    if (info?.sliceOperation != null)
                    {
                        if (info.isUpperHull) info.sliceOperation.upperDestroyed = true;
                        else info.sliceOperation.lowerDestroyed = true;
                    }

                    child.DOMove(child.position + (child.position-child.parent.position)*3, .25f).OnComplete(()=>
                    {
                        child.DOMove(transform.position + transform.up, 0.5f).OnComplete(()=>
                        {
                            Destroy(child.gameObject);
                        });
                    });
                }
                
                //animate each selected mesh to go toward the player and add to inventory
                foreach(Transform mesh in selectedMeshes)
                {
                    if(mesh.GetComponent<Collider2D>()) mesh.GetComponent<Collider2D>().enabled = false;

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
                

                outlinePoints.Clear();
                outlineLineRenderer.positionCount = 0;
            }
        }
        




        /// Slicing logic

        if (sliceTimer > 0f)
        {
            allowCut = true;

            sliceTimer -= Time.deltaTime / sliceTime;
            sliceTimer = Mathf.Max(sliceTimer, 0f);

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
            sliceLineRenderer.SetPosition(1, MouseWorldPos);
            if (allowCut)
            {
                allowCut = false;
                SliceAllChildren();
                DOTween.To(() => effectLineRenderer.widthMultiplier, x => effectLineRenderer.widthMultiplier = x, 0, 0.5f).OnComplete(() =>
                {
                    effectLineRenderer.enabled = false;
                });

                sliceLineRenderer.enabled = false;
            }
        }
    }


    void SliceAllChildren()
    {
        //detect all meshes the line intersects
        var meshesToSlice = GetComponentsInChildren<MeshFilter>().Where(mf => mf.gameObject.activeInHierarchy && CutHitsMesh(mf.transform, p1World, p2World)).ToArray();

        Vector3 planeNormal = Vector3.Cross((p2World - p1World).normalized, Camera.main.transform.forward).normalized;

        foreach (var meshFilter in meshesToSlice)
        {
            Transform child = meshFilter.transform;
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (!meshRenderer) continue;

            SlicedHull hull = child.gameObject.Slice(p1World, planeNormal);
            if (hull == null) continue;

            // create a local slice operation
            SliceOperation currentSlice = new SliceOperation
            {
                centerPoint = child.InverseTransformPoint((p1World + p2World) * 0.5f),
                planeNormal = child.InverseTransformDirection(planeNormal)
            };

            // create upper and lower pieces
            GameObject upper = SetupHull(hull.CreateUpperHull(child.gameObject, meshRenderer.sharedMaterial), child, currentSlice, true);
            GameObject lower = SetupHull(hull.CreateLowerHull(child.gameObject, meshRenderer.sharedMaterial), child, currentSlice, false);

            // add the pieces to the tree
            SliceInfo childSliceInfo = child.GetComponent<SliceInfo>();
            if (childSliceInfo?.sliceOperation != null)
            {
                if (childSliceInfo.isUpperHull) childSliceInfo.sliceOperation.upperHullSlice = currentSlice;

                else childSliceInfo.sliceOperation.lowerHullSlice = currentSlice;
            }
            else if (rootSliceOperation == null) rootSliceOperation = currentSlice;

            StartCoroutine(MoveSlice(upper.transform, planeNormal, sliceMoveTime, sliceMoveDistance));
            StartCoroutine(MoveSlice(lower.transform, -planeNormal, sliceMoveTime, sliceMoveDistance));
            Destroy(child.gameObject);
        }
    }

    GameObject SetupHull(GameObject hull, Transform parent, SliceOperation sliceOp, bool isUpper)
    {
        hull.transform.SetParent(parent.parent, false);
        DragFoodInto.AddPolygonCollider2DFromMesh(hull);
        DragFoodInto.CloneRigidbody2D(hull, parent.gameObject);
        
        SliceInfo info = hull.AddComponent<SliceInfo>();
        info.sliceOperation = sliceOp;
        info.isUpperHull = isUpper;
        return hull;
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

    List<Transform> GetTransformsInsideOutline(List<Transform> meshTransforms, List<Vector3> outlinePoints) // get all transforms inside the outline
    {
        if (outlinePoints.Count < 3) return new List<Transform>();
        
        // Convert outline points to screen space
        List<Vector2> screenPolygon = outlinePoints.Select(p => (Vector2)Camera.main.WorldToScreenPoint(p)).ToList();
        
        // Use mesh bounds center instead of transform position (pivot may be offset after slicing)
        return meshTransforms.Where(t => {
            var renderer = t.GetComponent<MeshRenderer>();
            Vector3 center = renderer ? renderer.bounds.center : t.position;
            return IsPointInPolygon(Camera.main.WorldToScreenPoint(center), screenPolygon);
        }).ToList();
    }

    bool IsPointInPolygon(Vector2 point, List<Vector2> polygon) // detect if point is in the outline, which is a list of points
    {
        int intersections = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            if ((a.y > point.y) != (b.y > point.y))
            {
                float xIntersect = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < xIntersect) intersections++;
            }
        }
        return (intersections % 2) == 1;
    }
}
