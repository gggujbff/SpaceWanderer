using System.Collections;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public PathData pathData;
    private int currentIndex = 0;

    private void Start()
    {
        if (pathData == null || pathData.nodes == null || pathData.nodes.Count < 2)
        {
            Debug.LogError("路径数据无效：需要至少 2 个节点");
            return;
        }

        transform.position = pathData.nodes[0].position;
        StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath()
    {
        int index = 0;

        while (true)
        {
            // ⚠ 确保路径数据仍然有效
            if (pathData == null || pathData.nodes == null || pathData.nodes.Count < 2)
            {
                Debug.LogWarning("路径数据无效或已被动态清空！");
                yield break;
            }

            // 当前节点
            if (index >= pathData.nodes.Count)
                index = 0;  // 防止 loop 模式下 index 超过总数

            var current = pathData.nodes[index];
            int nextIndex = index + 1;

            // 非循环路径终点判断
            if (!pathData.loop && nextIndex >= pathData.nodes.Count)
                yield break;

            // ⚠ 循环路径下也要确保 nextIndex 不超界
            int safeNextIndex = pathData.loop
                ? nextIndex % pathData.nodes.Count
                : nextIndex;

            if (safeNextIndex >= pathData.nodes.Count)
            {
                Debug.LogWarning($"即将访问的 nextIndex 超出范围：{safeNextIndex}, 当前节点数：{pathData.nodes.Count}");
                yield break;
            }

            var next = pathData.nodes[safeNextIndex];

            // 移动到下一个节点
            while (Vector2.Distance(transform.position, next.position) > 0.05f)
            {
                Vector2 direction = (next.position - (Vector2)transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                transform.position = Vector2.MoveTowards(transform.position, next.position,
                    current.moveSpeed * Time.deltaTime);
                yield return null;

                // 🛡 再次检查路径合法性，防止运行时路径被外部清空
                if (pathData == null || pathData.nodes.Count < 2)
                {
                    Debug.LogWarning("路径在运行中被修改为无效状态，终止移动");
                    yield break;
                }
            }

            transform.position = next.position;

            if (next.waitTime > 0)
                yield return new WaitForSeconds(next.waitTime);

            index = nextIndex;
        }
    }

}