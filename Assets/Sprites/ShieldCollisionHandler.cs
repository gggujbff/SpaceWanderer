using System.Collections.Generic;
using UnityEngine;

public class ShieldCollisionHandler : MonoBehaviour
{
    [Tooltip("碰撞后销毁的物体标签名单")]
    public List<string> destructibleTags = new List<string> { "Obstacle" };

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destructibleTags.Contains(other.tag))
        {
            Destroy(other.gameObject);
        }
    }
}