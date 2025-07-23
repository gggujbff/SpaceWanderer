#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    SerializedProperty spawnArea, maxObjects, autoSpawn, showSpawnArea, areaColor;
    SerializedProperty spawnableItems;

    private void OnEnable()
    {
        spawnArea = serializedObject.FindProperty("spawnArea");
        maxObjects = serializedObject.FindProperty("maxObjects");
        autoSpawn = serializedObject.FindProperty("autoSpawn");
        showSpawnArea = serializedObject.FindProperty("showSpawnArea");
        areaColor = serializedObject.FindProperty("areaColor");
        spawnableItems = serializedObject.FindProperty("spawnableItems");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(spawnArea);
        EditorGUILayout.PropertyField(maxObjects);
        EditorGUILayout.PropertyField(autoSpawn);
        EditorGUILayout.PropertyField(showSpawnArea);
        EditorGUILayout.PropertyField(areaColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("生成项配置", EditorStyles.boldLabel);

        for (int i = 0; i < spawnableItems.arraySize; i++)
        {
            SerializedProperty item = spawnableItems.GetArrayElementAtIndex(i);
            SerializedProperty prefab = item.FindPropertyRelative("prefab");
            SerializedProperty spawnFrequency = item.FindPropertyRelative("spawnFrequency");
            SerializedProperty isMoving = item.FindPropertyRelative("isMoving");
            SerializedProperty minSpeed = item.FindPropertyRelative("minSpeed");
            SerializedProperty maxSpeed = item.FindPropertyRelative("maxSpeed");
            SerializedProperty useRandomMass = item.FindPropertyRelative("useRandomMass");
            SerializedProperty minMass = item.FindPropertyRelative("minMass");
            SerializedProperty maxMass = item.FindPropertyRelative("maxMass");

            EditorGUILayout.BeginVertical("box");
            string itemName = prefab.objectReferenceValue != null ? prefab.objectReferenceValue.name : $"Item {i + 1}";
            EditorGUILayout.LabelField(itemName, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(prefab);
            EditorGUILayout.PropertyField(spawnFrequency);
            EditorGUILayout.PropertyField(isMoving);

            if (isMoving.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minSpeed);
                EditorGUILayout.PropertyField(maxSpeed);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useRandomMass);
            if (useRandomMass.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minMass);
                EditorGUILayout.PropertyField(maxMass);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("添加生成项"))
            spawnableItems.arraySize++;

        if (GUILayout.Button("删除最后一项") && spawnableItems.arraySize > 0)
            spawnableItems.arraySize--;

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
