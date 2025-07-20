// 文件名：MapObjectEnums.cs
// 放置在Unity项目的Scripts文件夹中（或子文件夹）
public enum MapObjectType 
{ 
    Collectible,       // 可采集物体
    MovingObstacle,    // 运动障碍物
    FixedObstacle,     // 固定障碍物
    Interference,      // 干扰物体
    EnvironmentalEvent // 环境事件
}

public enum CollectibleState 
{ 
    FreeFloating,      // 自由悬浮
    AttachedToObstacle,// 吸附在障碍物上
    Grabbed,           // 被抓取
    Colliding,         // 碰撞中
    Destroyed,         // 被破坏
    Harvested          // 被收获
}

public enum MovingObstacleState 
{ 
    FreeFloating,      // 自由悬浮
    Colliding,         // 碰撞中
    Damaged,           // 被伤害
    Destroyed          // 被破坏
}

public enum CollectibleSubType 
{ 
    Garbage,           // 垃圾物体
    Resource,          // 资源物体
    Prop,              // 道具物体
    CollectibleObstacle// 可采集障碍物（存疑子类）
}