using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target; // We will assign the Player here
    public float SmoothSpeed = 5.0f;

    void LateUpdate()
    {
        if (Target != null)
        {
            // Calculate where the camera should be (keep its original Z position!)
            Vector3 desiredPosition = new Vector3(Target.position.x, Target.position.y, transform.position.z);

            // Smoothly move the camera towards the target
            transform.position = Vector3.Lerp(transform.position, desiredPosition, SmoothSpeed * Time.deltaTime);
        }
    }
}