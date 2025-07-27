using System.Collections;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public PathData pathData;
    private int currentIndex = 0;

    private void Start()
    {
        // 无路径或无节点：不执行任何行为，静默跳过
        if (pathData == null || pathData.nodes == null || pathData.nodes.Count == 0)
        {
            // 可选输出：Debug.Log("路径为空，PathFollower 不执行");
            return;
        }

        // 只有一个点：只设置初始位置，不执行路径移动
        if (pathData.nodes.Count == 1)
        {
            transform.position = pathData.nodes[0].position;
            return;
        }

        // 节点 ≥2：开始路径跟随
        transform.position = pathData.nodes[0].position;
        StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath()
    {
        int index = 0;

        while (true)
        {
            if (pathData == null || pathData.nodes == null || pathData.nodes.Count < 2)
                yield break;

            if (index >= pathData.nodes.Count)
                index = 0;

            var current = pathData.nodes[index];
            int nextIndex = index + 1;

            if (!pathData.loop && nextIndex >= pathData.nodes.Count)
                yield break;

            int safeNextIndex = pathData.loop ? nextIndex % pathData.nodes.Count : nextIndex;

            if (safeNextIndex >= pathData.nodes.Count)
                yield break;

            var next = pathData.nodes[safeNextIndex];

            // **只平滑移动位置，不旋转父物体**
            while (Vector2.Distance(transform.position, next.position) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, next.position,
                    current.moveSpeed * Time.deltaTime);

                yield return null;

                if (pathData == null || pathData.nodes == null || pathData.nodes.Count < 2)
                    yield break;
            }

            // **不再设置 transform.position = next.position;**

            if (next.waitTime > 0)
                yield return new WaitForSeconds(next.waitTime);

            index = nextIndex;
        }
    }

}
