#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    private ObjectSpawner spawner;
    private SerializedProperty spawnZonesProp;
    private bool[] showZoneDetails;

    private void OnEnable()
    {
        spawner = (ObjectSpawner)target;
        spawnZonesProp = serializedObject.FindProperty("spawnZones");
        
        if (showZoneDetails == null || showZoneDetails.Length != spawnZonesProp.arraySize)
        {
            showZoneDetails = new bool[spawnZonesProp.arraySize];
            for (int i = 0; i < showZoneDetails.Length; i++)
            {
                showZoneDetails[i] = i == 0;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSpawn"), 
            new GUIContent("自动生成", "是否在游戏开始时自动启动生成功能"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("showSpawnAreas"), 
            new GUIContent("显示区域", "是否在场景视图中显示生成区域的可视化边界"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("生成区域配置", EditorStyles.boldLabel);
        
        for (int i = 0; i < spawnZonesProp.arraySize; i++)
        {
            SerializedProperty zoneProp = spawnZonesProp.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginHorizontal();
            
            showZoneDetails[i] = EditorGUILayout.Foldout(showZoneDetails[i], 
                $"区域 {i+1}: {zoneProp.FindPropertyRelative("zoneName").stringValue}");
            
            if (GUILayout.Button("↑", GUILayout.Width(25)))
            {
                if (i > 0)
                {
                    spawnZonesProp.MoveArrayElement(i, i - 1);
                    bool temp = showZoneDetails[i];
                    showZoneDetails[i] = showZoneDetails[i - 1];
                    showZoneDetails[i - 1] = temp;
                }
            }
            
            if (GUILayout.Button("↓", GUILayout.Width(25)))
            {
                if (i < spawnZonesProp.arraySize - 1)
                {
                    spawnZonesProp.MoveArrayElement(i, i + 1);
                    bool temp = showZoneDetails[i];
                    showZoneDetails[i] = showZoneDetails[i + 1];
                    showZoneDetails[i + 1] = temp;
                }
            }
            
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                spawnZonesProp.DeleteArrayElementAtIndex(i);
                System.Array.Resize(ref showZoneDetails, spawnZonesProp.arraySize);
                break;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showZoneDetails[i])
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("zoneName"), 
                    new GUIContent("区域名称", "生成区域的名称，用于在编辑器中识别不同区域"));

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("spawnArea"), 
                    new GUIContent("区域范围", "定义区域的矩形范围 (x,y) 是左下角坐标，(width,height) 是尺寸"));

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("areaColor"), 
                    new GUIContent("区域颜色", "在场景视图中显示的区域颜色，帮助可视化区域位置"));

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("maxObjects"), 
                    new GUIContent("最大物体数", "该区域内允许同时存在的最大物体数量"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("生成设置", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("spawnRate"), 
                    new GUIContent("生成速率", "理论上每秒生成的物体数量 (实际生成受随机概率影响)"));

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("randomSpawnChance"), 
                    new GUIContent("生成概率", "每次尝试生成时实际生成的概率 (0-1之间)，值越高生成越频繁"));

                EditorGUILayout.PropertyField(zoneProp.FindPropertyRelative("useWeightedSpawn"), 
                    new GUIContent("权重生成", "是否使用权重系统来决定生成哪种物体，启用后将根据spawnWeight属性随机选择"));
                
                SerializedProperty itemsProp = zoneProp.FindPropertyRelative("spawnableItems");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("生成项列表", EditorStyles.boldLabel);
                
                for (int j = 0; j < itemsProp.arraySize; j++)
                {
                    SerializedProperty itemProp = itemsProp.GetArrayElementAtIndex(j);
                    
                    EditorGUILayout.BeginVertical("helpbox");
                    
                    EditorGUILayout.BeginHorizontal();
                    string itemName = itemProp.FindPropertyRelative("prefab").objectReferenceValue != null 
                        ? itemProp.FindPropertyRelative("prefab").objectReferenceValue.name 
                        : $"未设置预制体 {j+1}";
                    
                    EditorGUILayout.LabelField($"项 {j+1}: {itemName}", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        if (j > 0)
                        {
                            itemsProp.MoveArrayElement(j, j - 1);
                        }
                    }
                    
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        if (j < itemsProp.arraySize - 1)
                        {
                            itemsProp.MoveArrayElement(j, j + 1);
                        }
                    }
                    
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        itemsProp.DeleteArrayElementAtIndex(j);
                        break;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("prefab"), 
                        new GUIContent("预制体", "生成时使用的预制体"));

                    EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("spawnFrequency"), 
                        new GUIContent("生成频率", "该物体的生成频率 (值越高生成越频繁)，实际生成还受区域spawnRate和randomSpawnChance影响"));

                    if (zoneProp.FindPropertyRelative("useWeightedSpawn").boolValue)
                    {
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("spawnWeight"), 
                            new GUIContent("生成权重", "当区域启用权重生成时，该值决定此物体被选中的概率 (权重越高越容易生成)"));
                    }

                    EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("isMoving"), 
                        new GUIContent("是否移动", "生成的物体是否应该移动"));
                    
                    if (itemProp.FindPropertyRelative("isMoving").boolValue)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("minSpeed"), 
                            new GUIContent("最小速度", "当物体移动时的最小速度"));

                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("maxSpeed"), 
                            new GUIContent("最大速度", "当物体移动时的最大速度"));
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("useRandomMass"), 
                        new GUIContent("随机质量", "是否为生成的物体使用随机质量值"));
                    
                    if (itemProp.FindPropertyRelative("useRandomMass").boolValue)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("minMass"), 
                            new GUIContent("最小质量", "当useRandomMass启用时，物体的最小质量值"));

                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("maxMass"), 
                            new GUIContent("最大质量", "当useRandomMass启用时，物体的最大质量值"));
                        
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("fixedMass"), 
                            new GUIContent("固定质量", "当useRandomMass未启用时，物体使用的固定质量值"));
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                if (GUILayout.Button("添加生成项"))
                {
                    itemsProp.arraySize++;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("添加生成区域"))
        {
            int newIndex = spawnZonesProp.arraySize;
            spawnZonesProp.arraySize++;
            
            SerializedProperty newZoneProp = spawnZonesProp.GetArrayElementAtIndex(newIndex);
            newZoneProp.FindPropertyRelative("zoneName").stringValue = $"生成区域 {newIndex+1}";
            newZoneProp.FindPropertyRelative("spawnArea").rectValue = new Rect(-5f, -5f, 10f, 10f);
            newZoneProp.FindPropertyRelative("areaColor").colorValue = 
                new Color(Random.value, Random.value, Random.value, 0.5f);
            newZoneProp.FindPropertyRelative("maxObjects").intValue = 5;
            newZoneProp.FindPropertyRelative("spawnRate").floatValue = 1f;
            newZoneProp.FindPropertyRelative("randomSpawnChance").floatValue = 0.1f;
            
            newZoneProp.FindPropertyRelative("spawnableItems").ClearArray();
            
            System.Array.Resize(ref showZoneDetails, newIndex + 1);
            showZoneDetails[newIndex] = true;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif