using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;
    private GameObject grabbedEnergy;

    private void OnTriggerEnter2D(Collider2D other)  // 当钩爪与能量碰撞时
    {
        if (other.CompareTag("Energy"))
        {
            Debug.Log("钩中能量物体！");
            grabbedEnergy = other.gameObject;
            
            // 禁用能量物体的物理组件，防止干扰
            Rigidbody2D rb = grabbedEnergy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
            }
            
            Collider2D col = grabbedEnergy.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // 将能量物体设为钩爪的子对象，使其跟随移动
            grabbedEnergy.transform.SetParent(transform);
            grabbedEnergy.transform.localPosition = Vector3.zero;
            
            // 立即开始回收钩爪
            hookSystem.RetrieveHook();
        }
    }

    public GameObject GetGrabbedEnergy() => grabbedEnergy;  // 获取被钩中的能量

    public void ReleaseGrabbedEnergy()   // 释放被钩中的能量
    {
        if (grabbedEnergy != null)
        {
            grabbedEnergy.transform.SetParent(null);
            grabbedEnergy = null;
        }
    }
}