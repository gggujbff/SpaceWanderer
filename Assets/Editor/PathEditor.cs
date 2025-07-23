using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathData))]
public class PathEditor : Editor
{
    private PathData pathData;
    private int selectedNodeIndex = -1;

    private void OnSceneGUI()
    {
        pathData = (PathData)target;

        // 鼠标点击添加路径点（不需要Ctrl）
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && !e.control)
        {
            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            Undo.RecordObject(pathData, "Add Path Node");
            pathData.nodes.Add(new PathNode
            {
                position = worldPos,
                waitTime = pathData.defaultWaitTime,
                moveSpeed = pathData.defaultMoveSpeed
            });

            selectedNodeIndex = pathData.nodes.Count - 1;
            e.Use();
        }

        // 画结点 + Handles
        for (int i = 0; i < pathData.nodes.Count; i++)
        {
            PathNode node = pathData.nodes[i];
            Handles.color = (i == selectedNodeIndex) ? Color.yellow : Color.cyan;

            EditorGUI.BeginChangeCheck();
            Vector2 newPos = Handles.PositionHandle(node.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pathData, "Move Node");
                node.position = newPos;
            }

            // 点击选择结点
            float handleSize = HandleUtility.GetHandleSize(node.position) * 0.1f;
            if (Handles.Button(node.position + Vector2.up * 0.3f, Quaternion.identity, handleSize, handleSize, Handles.CircleHandleCap))
            {
                selectedNodeIndex = i;
            }

            // 编号标签
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            Handles.Label(node.position + Vector2.right * 0.2f, $"#{i}", labelStyle);
        }

        // 画路径线
        Handles.color = Color.green;
        for (int i = 0; i < pathData.nodes.Count - 1; i++)
        {
            Handles.DrawLine(pathData.nodes[i].position, pathData.nodes[i + 1].position);
        }

        if (pathData.nodes.Count > 1)
        {
            // 可选闭环
            Handles.DrawLine(pathData.nodes[^1].position, pathData.nodes[0].position);
        }

        SceneView.RepaintAll(); // 实时刷新
    }

    public override void OnInspectorGUI()
    {
        pathData = (PathData)target;

        DrawDefaultInspector();

        // 显示当前选中结点的参数
        if (selectedNodeIndex >= 0 && selectedNodeIndex < pathData.nodes.Count)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"选中结点 #{selectedNodeIndex}", EditorStyles.boldLabel);

            var node = pathData.nodes[selectedNodeIndex];
            node.waitTime = EditorGUILayout.FloatField("等待时间", node.waitTime);
            node.moveSpeed = EditorGUILayout.FloatField("移动速度", node.moveSpeed);

            if (GUILayout.Button("删除该节点"))
            {
                Undo.RecordObject(pathData, "Delete Node");
                pathData.nodes.RemoveAt(selectedNodeIndex);
                selectedNodeIndex = -1;
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("清除所有路径点"))
        {
            Undo.RecordObject(pathData, "Clear Path");
            pathData.nodes.Clear();
            selectedNodeIndex = -1;
        }
    }
}
