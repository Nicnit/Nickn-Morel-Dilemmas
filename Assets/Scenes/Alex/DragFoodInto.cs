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
        Vector3 itemPosition = item.transform.position;
        item.transform.SetParent(transform, true);
        item.transform.position = itemPosition;
        item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y, -1);
        item.transform.localScale = new Vector3(item.transform.localScale.x, item.transform.localScale.y, item.transform.localScale.y);

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
        // Get the original mesh and material from the mesh renderer
        MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
        
        if (meshFilter == null) return;
        
        Material material = meshRenderer.sharedMaterial;
        Transform parentTransform = item.transform;
        
        // Collect leaf meshes during slicing
        List<GameObject> leafMeshes = new List<GameObject>();
        
        // Start recursive slicing on the mesh renderer's game object
        SliceRecursive(meshRenderer.gameObject, sliceOp, material, parentTransform, leafMeshes);
        
        // Randomize positions of the leaf meshes
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

        Vector3 worldCenterPoint = parentTransform.TransformPoint(sliceOp.centerPoint);
        SlicedHull hull = meshObject.Slice(worldCenterPoint, sliceOp.planeNormal);
        
        if (hull == null) return;

        GameObject upper = hull.CreateUpperHull(meshObject, material);
        GameObject lower = hull.CreateLowerHull(meshObject, material);

        upper.transform.SetParent(parentTransform, false);
        lower.transform.SetParent(parentTransform, false);

        if (sliceOp.upperHullSlice != null)
            SliceRecursive(upper, sliceOp.upperHullSlice, material, parentTransform, leafMeshes);
        else
            leafMeshes.Add(upper);
        
        if (sliceOp.lowerHullSlice != null)
            SliceRecursive(lower, sliceOp.lowerHullSlice, material, parentTransform, leafMeshes);
        else
            leafMeshes.Add(lower);

        Object.Destroy(meshObject);
    }
}
