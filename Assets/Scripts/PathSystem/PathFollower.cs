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
            int index = 0;

            while (true)
            {
                var current = pathData.nodes[index];
                var nextIndex = index + 1;

                // 如果是非循环模式，且已经到最后一个节点，终止
                if (!pathData.loop && nextIndex >= pathData.nodes.Count)
                    yield break;

                var next = pathData.loop ? pathData.nodes[nextIndex % pathData.nodes.Count] : pathData.nodes[nextIndex];

                // 移动到下一个点
                while (Vector2.Distance(transform.position, next.position) > 0.05f)
                {
                    Vector2 direction = (next.position - (Vector2)transform.position).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);

                    transform.position = Vector2.MoveTowards(transform.position, next.position,
                        current.moveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = next.position;

                if (next.waitTime > 0)
                    yield return new WaitForSeconds(next.waitTime);

                index = nextIndex;
            }

        }
    }
}