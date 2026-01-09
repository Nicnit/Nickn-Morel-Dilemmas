using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform;
    public Vector3 offset;
    public float smoothSpeed = 5f;
    public float pixelPerUnit = 100f; //TODO: needs to be modified with updated assets

    void LateUpdate()
    {
        Vector3 desiredPosition = playerTransform.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * pixelPerUnit) / pixelPerUnit;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * pixelPerUnit) / pixelPerUnit;
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
