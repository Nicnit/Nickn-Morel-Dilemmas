using NUnit.Framework.Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using EzySlice;
using System.Collections.Generic;

public class DragFoodInto : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;
    public void AddItem(InventoryItem item)
    {
        Vector3 itemScale = item.transform.lossyScale;
        Vector3 itemPosition = item.transform.position;
        item.transform.SetParent(transform, true);
        item.transform.position = itemPosition;
        item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y, -1);
        item.transform.localScale = new Vector3(item.transform.localScale.x, item.transform.localScale.y, item.transform.localScale.y);

        // If this is a slicer, initialize it with the existing slice tree
        EzyMeshSlicer slicer = GetComponent<EzyMeshSlicer>();
        if (slicer != null)
        {
            slicer.InitializeFromItem(item);
        }

        // Recreate sliced version from slice operations
        if (item.foodItem != null && item.foodItem.sliceOperation != null)
        {
            // Save references before anything gets destroyed
            MeshRenderer meshRenderer = item.meshRenderer;
            if (meshRenderer != null)
            {
                meshRenderer.gameObject.SetActive(true);
                RecreateSlicedMesh(item, item.foodItem.sliceOperation, meshRenderer);
            }
        }
        else if (item.meshRenderer != null)
        {
            item.meshRenderer.gameObject.SetActive(true);
        }
    }

    public void Remove(InventoryItem item)
    {
        item.meshRenderer.gameObject.SetActive(false);
    }

    private void RecreateSlicedMesh(InventoryItem item, SliceOperation sliceOp, MeshRenderer meshRenderer)
    {
        // get original mesh
        MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
        
        if (meshFilter == null) return;
        
        Material material = meshRenderer.sharedMaterial;
        Transform parentTransform = item.transform;
        
        // collect lower meshes during slicing
        List<GameObject> leafMeshes = new List<GameObject>();
        
        // recursive slicing
        SliceRecursive(meshRenderer.gameObject, sliceOp, material, parentTransform, leafMeshes);
        
        // randomize positions
        if (leafMeshes.Count > 0)
        {
            RandomizeHullPositions(leafMeshes);
        }
    }
    
    private void RandomizeHullPositions(List<GameObject> meshes)
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            meshes[i].transform.localPosition += new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                0
            );
            meshes[i].transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }
    }

    private void SliceRecursive(GameObject meshObject, SliceOperation sliceOp, Material material, Transform parentTransform, List<GameObject> leafMeshes)
    {
        if (sliceOp == null) return;

        // local to world
        Vector3 worldCenterPoint = meshObject.transform.TransformPoint(sliceOp.centerPoint);
        Vector3 worldPlaneNormal = meshObject.transform.TransformDirection(sliceOp.planeNormal);
        SlicedHull hull = meshObject.Slice(worldCenterPoint, worldPlaneNormal);
        
        if (hull == null) return;

        GameObject upper = hull.CreateUpperHull(meshObject, material);
        GameObject lower = hull.CreateLowerHull(meshObject, material);

        // do upper hull stuff
        if (sliceOp.upperDestroyed)
        {
            Object.Destroy(upper);
        }
        else
        {
            AddPolygonCollider2DFromMesh(upper);
            CloneRigidbody2D(upper, meshObject);
            SliceInfo upperInfo = upper.AddComponent<SliceInfo>();
            upperInfo.sliceOperation = sliceOp;
            upperInfo.isUpperHull = true;
            upper.transform.SetParent(parentTransform, false);
            
            if (sliceOp.upperHullSlice != null)
                SliceRecursive(upper, sliceOp.upperHullSlice, material, parentTransform, leafMeshes);
            else
                leafMeshes.Add(upper);
        }

        // do lower hull stuff
        if (sliceOp.lowerDestroyed)
        {
            Object.Destroy(lower);
        }
        else
        {
            AddPolygonCollider2DFromMesh(lower);
            CloneRigidbody2D(lower, meshObject);
            SliceInfo lowerInfo = lower.AddComponent<SliceInfo>();
            lowerInfo.sliceOperation = sliceOp;
            lowerInfo.isUpperHull = false;
            lower.transform.SetParent(parentTransform, false);
            
            if (sliceOp.lowerHullSlice != null)
                SliceRecursive(lower, sliceOp.lowerHullSlice, material, parentTransform, leafMeshes);
            else
                leafMeshes.Add(lower);
        }

        Object.Destroy(meshObject);
    }

    public static void AddPolygonCollider2DFromMesh(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Vector3[] vertices = mf.sharedMesh.vertices;
        if (vertices.Length < 3) return;

        // project 3d to 2d
        List<Vector2> points2D = new List<Vector2>();
        foreach (Vector3 v in vertices)
        {
            Vector2 p = new Vector2(v.x, v.y);

            bool isDuplicate = false;
            foreach (Vector2 existing in points2D)
            {
                if (Vector2.Distance(existing, p) < 0.001f)
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate) points2D.Add(p);
        }

        if (points2D.Count < 3) return;


        List<Vector2> hull = ComputeConvexHull(points2D);
        if (hull.Count < 3) return;

        PolygonCollider2D polyCollider = obj.AddComponent<PolygonCollider2D>();
        polyCollider.SetPath(0, hull.ToArray());
    }


    public static List<Vector2> ComputeConvexHull(List<Vector2> points)
    {
        if (points.Count < 3) return points;

        // Find point with lowest y
        int startIdx = 0;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].y < points[startIdx].y ||
                (points[i].y == points[startIdx].y && points[i].x < points[startIdx].x))
            {
                startIdx = i;
            }
        }

        Vector2 start = points[startIdx];
        points.RemoveAt(startIdx);

        // sort by polar angle relative to start point
        points.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.y - start.y, a.x - start.x);
            float angleB = Mathf.Atan2(b.y - start.y, b.x - start.x);
            if (Mathf.Abs(angleA - angleB) < 1e-6f)
            {
                // if same angle, closer point first
                return Vector2.Distance(start, a).CompareTo(Vector2.Distance(start, b));
            }
            return angleA.CompareTo(angleB);
        });

        List<Vector2> hull = new List<Vector2> { start };

        foreach (Vector2 p in points)
        {
            // remove points that make a clockwise turn
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(p);
        }

        return hull;
    }

    public static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
    public static void CloneRigidbody2D(GameObject obj, GameObject objToCopy)
    {
        Rigidbody2D rbCopy = objToCopy.GetComponent<Rigidbody2D>();
        if (rbCopy == null) return;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = rbCopy.bodyType;
        rb.mass = rbCopy.mass;
        rb.linearDamping = rbCopy.linearDamping;
        rb.angularDamping = rbCopy.angularDamping;
        rb.gravityScale = rbCopy.gravityScale;
        rb.collisionDetectionMode = rbCopy.collisionDetectionMode;
        rb.sleepMode = rbCopy.sleepMode;
        rb.interpolation = rbCopy.interpolation;
        rb.constraints = rbCopy.constraints;
        rb.sharedMaterial = rbCopy.sharedMaterial;
    }
}