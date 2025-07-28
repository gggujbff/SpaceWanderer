using UnityEngine;

public class BackGroundController : MonoBehaviour
{
    public Transform target; 
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("跟随速度")]
    public float kSpeed = 1;

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = (target.position + offset) * kSpeed;
        }
    }
}