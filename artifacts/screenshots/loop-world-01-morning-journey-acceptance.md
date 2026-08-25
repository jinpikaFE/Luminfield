# LOOP-01 / WORLD-01 晨报行程验收

- Worktree：`/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- 分支：`codex/base-01-playtest-bindings`
- 引擎：Godot 4.7.1 .NET
- 图形：OpenGL 4.1 Metal Compatibility，Apple M3
- 窗口：1280×720，内部 640×360

## 运行时证据

- `loop-01-morning-navigation-actions.png`：晨报可行动摘要使用标准焦点按钮，
  按钮文本、摘要编号、七卡滚动区和关闭按钮均未裁切。
- `world-01-multi-segment-journey-hud.png`：家园到低语林地生成两段行程；HUD
  同时显示最终目的地、第 1/2 段和偏航回线方向，小地图仍显示当前段导航线。

## 确定性入口

- 晨报：`--playtest-farm --open-morning-briefing`
- 两段行程：`--playtest-farm --select-route-destination=WhisperingWoods`
- 两个入口都配合 `--capture-playtest=res://artifacts/screenshots/<文件名>` 自动截图。

## 验证

- 晨报、路线、行程、HUD 与主流程聚焦测试：67/67。
- Phase G 快检：51/51。
- 完整 C#：1111/1111。
- 双语键：2181/2181，键集合一致。
- Godot 导入与主场景 180 帧启动通过。

## 未覆盖

- 自动测试证明精确终点换段和偏航终点不误判；本轮没有用真人连续步行方式走完
  两段路线。
- 物理手柄、真人目标选择耗时、实际迷路率、Windows/Linux 对应系统实机仍待验收。
