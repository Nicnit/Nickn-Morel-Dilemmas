using System.Threading;
using UnityEngine;

public class CameraTransition : MonoBehaviour
{
    public Transform transitionPoint;
    public Transform player;
    public Transform room1;
    public Transform room2;
    private Transform selectedRoom;
    private Vector3 recordedCameraPos;
    public float timer;
    public AnimationCurve curve;
    public float offsetMult = .5f;

    void Awake()
    {
        FixedUpdate();
    }
    void FixedUpdate()
    {
        Transform previousSelected = selectedRoom;
        if(transitionPoint.position.y < player.position.y)
        {
            selectedRoom = room1;
        }
        else
        {
            selectedRoom = room2;
        }

        if(previousSelected!=selectedRoom) {
            timer = 0;
            recordedCameraPos = transform.position;
        }
    }

    void Update()
    {
        Vector3 offset = (Vector3)((Vector2)player.transform.position-(Vector2)selectedRoom.position)*offsetMult+new Vector3(0, 0, -10);
        if(timer<1) {
            timer+= Time.deltaTime;
            transform.position = Vector3.Lerp(recordedCameraPos, selectedRoom.transform.position+offset, curve.Evaluate(timer));
        }
        else
        {
            transform.position = selectedRoom.transform.position+offset;
        }
    }
}
