using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 玩家Transform
    
    [Header("位置偏移")]
    public Vector3 offset = new Vector3(0, 0, -10); // 相机与目标的偏移量

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}