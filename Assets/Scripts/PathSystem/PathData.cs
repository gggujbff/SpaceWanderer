using System.Collections.Generic;
using UnityEngine;


public class PathData : MonoBehaviour
{
    public List<PathNode> nodes = new List<PathNode>();

    [Header("路径参数")]
    [Tooltip("是否循环路径（最后一个点后返回第一个点）")]
    public bool loop = true;

    [Header("新增节点默认值")]
    [Tooltip("新添加节点的默认等待时间")]
    public float defaultWaitTime = 0f;

    [Tooltip("新添加节点的默认移动速度")]
    public float defaultMoveSpeed = 2f;

    private void OnDrawGizmos()
    {
        if (nodes == null || nodes.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Gizmos.DrawLine(nodes[i].position, nodes[i + 1].position);
            Gizmos.DrawSphere(nodes[i].position, 0.1f);
        }

        Gizmos.DrawSphere(nodes[nodes.Count - 1].position, 0.1f);

        if (loop)
        {
            Gizmos.DrawLine(nodes[^1].position, nodes[0].position);
        }
    }
}