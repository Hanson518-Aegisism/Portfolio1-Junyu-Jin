# 可开关的科幻滑门 Prefab

将 `Assets/sci-fi-door/Prefabs/SciFiDoor.prefab` 拖入场景即可使用。

## 开关门

运行时玩家靠近门口（默认 2.75 米内）按 **E** 开关门。玩家对象需要带 `Player` 标签。相邻的门只响应最近的一扇。可在 `SciFi Sliding Door` 组件中调整交互距离、按键、动画时长和默认打开状态，也可以关闭玩家按键交互，改由其他脚本调用 `Open()`、`Close()`、`Toggle()` 或 `SetOpen(bool)`。

动画默认 1.2 秒，两扇门板各滑动 1.2 米，门框不动，中央锁扣随各自的门板移动。已生成实际 AnimationClip 和 Animator Controller，支持中途平滑反向。门板碰撞体随动画移动，打开后门洞可通行；防夹区域有人时拒绝关门，关门途中有人进入则重新打开。防夹识别 CharacterController、NavMeshAgent 和非运动学刚体。

可选的 `Open Sound` / `Close Sound` 音效槽已接好，留空时无声。

- 已配置项目使用的 URP Lit 材质，包含基础颜色、法线、金属度、粗糙度、环境遮蔽和自发光。
- `SciFiDoor_MetallicSmoothness.png` 为生成的打包贴图：R 通道为金属度，A 通道为 `1 - Roughness`，按线性数据导入。
- 保留原尺寸，整体约宽 4.08 米、高 3.57 米、深 1.91 米；根节点缩放为 (1,1,1)，原点在整体底部中央。
- 门框使用精确的非凸 MeshCollider，门板使用各自的 BoxCollider 与运动学 Rigidbody。根节点保持非 Static；不要给整个门再添加覆盖门洞的碰撞盒。

源文件名为 `Sci-Fi_Door_anim.fbx`，但原始 FBX 没有动画。当前开关门动画是在项目中新增的：从合并网格的独立部件中拆出左右门板和附着件，保留原始三角面、UV 和材质。派生网格位于 `Meshes`，动画及控制器位于 `Animations`，原始 FBX 保留。

门板打开时会伸到门框两侧，安装时需要两侧留出滑动空间，通常藏入墙体。门洞净宽约 2.2 米；墙上也需要留出相应洞口，避免墙体或原有碰撞体继续挡住通路。若已有 NavMesh，请按场景的导航方案处理这扇动态门的通行。

`SciFiDoor_Static.prefab` 保留了原来的静态版本。

开关门预览为 `SciFiDoor_Closed.png` 和 `SciFiDoor_Open.png`。重新生成使用 Unity 菜单 `Tools > Sci-Fi Door > Build Animated Sliding Door`，会更新动画 Prefab、派生网格和动画资源；手动修改前可先创建 Prefab Variant。
