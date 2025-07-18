using UnityEngine;

public class Energy : MonoBehaviour
{
    [Header("获取能量值")]
    public float energyAmount = 10f;
    
    [Header("获取分数")]
    public float scoreAmount = 1f;
    
    [Header("附带的物品")]
    public float MissileAmount = 0f; // 附带导弹数量
    
    [Header("其余属性")]
    public float mass = 1f; // 质量    (或者直接写成回收速度更方便？？？)
}