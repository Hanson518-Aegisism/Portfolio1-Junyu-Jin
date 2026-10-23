# Tomb Soul 可用预制体

## 当前场景已配置

`Assets/Scenes/Test.unity` 中的 `Enemy_Test` 已替换为 Tomb Soul 模型，保留原追踪脚本与参数，并将根节点缩放归一、脚底对齐现有 NavMesh。

选中 `Enemy_Test`，在 `Enemy Chase Player` 组件的 `Chase Scream / 追击尖叫` 区域，将音频拖入 `Chase Scream`。可以调整 `Chase Scream Volume` 和 `Chase Scream Cooldown`（默认 8 秒）。声音在进入追击时播放一次，使用敌人位置的 3D 音源；冷却至少持续至音频播放完，避免重复叠加。留空不播放。

停止移动时播放 Idle，搜索和追击移动时循环 Crawl；播放速率随实际移动速度调整（默认 2.2 米/秒对应 1 倍速，上限 1.8 倍）。`TombSoul_Visual` 的 `Tomb Soul Animation Driver` 组件可调整参考速度与播放速率范围。动画不驱动位移，由 NavMeshAgent 控制移动。

在 `Assets/Tomb-Soul/Prefabs` 中：

- `TombSoul_Enemy.prefab`：完整敌人。拖到场景已经烘焙的 NavMesh 上使用，包含 EnemyChasePlayer、NavMeshAgent、胶囊碰撞器和夜视威胁标记；自动寻找带 Player 标签的玩家。
- `TombSoul_Visual.prefab`：只替换模型。作为现有敌人的子对象使用，保留原敌人 AI、导航和碰撞组件，关闭旧模型的 Renderer。它会读取父对象的 NavMeshAgent 速度，静止播放 Idle，移动播放 Crawl。

模型预设高度为 1.8 米，原点在地面，面向本地 +Z，根运动关闭，使用项目的 URP 材质和法线贴图。现有 Enemy_Test 的根节点缩放为 2.317，且中心不在地面；直接挂模型会继承放大。需要直接使用时推荐放置完整的 TombSoul_Enemy，保持它的根节点缩放为 (1,1,1)，并以脚底位置对齐地面。

若原敌人已有 NightVisionMark 且手动指定了旧 Renderer，请将其 Renderers 更新为新模型的 SkinnedMeshRenderer。

原始资源只有 Idle 和 Crawl 两个动作，没有攻击或死亡动画。完整敌人使用项目现有的接近玩家后触发 PlayerDeath 的逻辑。

可以通过 Unity 菜单 `Tools > Tomb Soul > Build Ready To Use Prefabs` 重新生成。生成会更新这些预制体、材质与控制器；手动调整前可先另存为 Prefab Variant。
