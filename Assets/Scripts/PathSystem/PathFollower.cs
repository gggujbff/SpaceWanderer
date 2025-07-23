using System.Collections;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public PathData pathData;
    private int currentIndex = 0;

    private void Start()
    {
        if (pathData != null && pathData.nodes.Count >= 2)
        {
            transform.position = pathData.nodes[0].position;
            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator FollowPath()
    {
        while (true)
        {
            PathNode current = pathData.nodes[currentIndex];
            int nextIndex = (currentIndex + 1) % pathData.nodes.Count;
            PathNode next = pathData.nodes[nextIndex];

            // 等待当前点停留时间
            if (current.waitTime > 0)
                yield return new WaitForSeconds(current.waitTime);

            // 平滑移动
            while (Vector2.Distance(transform.position, next.position) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, next.position, current.moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = next.position; // 最后一点矫正位置
            currentIndex = nextIndex;
        }
    }
}