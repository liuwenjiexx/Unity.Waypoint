# Unity Waypoint

Unity 路径编辑器



## 使用

1. 创建 Empty GameObject
2. 添加 `WaypointPath` 组件
3. 点击 `Unlock` 按钮，解锁进入编辑状态
4. 在Scene 窗口中按住 `shift + 鼠标左键` 添加点
5. 编辑完成点击 `Lock` 按钮，锁定路径，防止误编辑



##  创建分支

1. 选中主路径的点
2. 选择 `Branchs/Add` 菜单 添加分支
3. 按 `shit+鼠标左键` 添加点
4. 选择主分支
5. Branch/To/Point 面板， 点击 `Pick` 按钮选择要连接的目标分支的起点



## 路径状态

使用`WaypointTracker`组件获取路径中状态，包含（进度，起点距离）