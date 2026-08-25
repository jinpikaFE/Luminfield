# LOC-WATCH 星落巡守台验收

- Worktree：`/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- 分支：`codex/base-01-playtest-bindings`
- 引擎：Godot 4.7.1 .NET
- 图形：OpenGL 4.1 Metal Compatibility，Apple M3
- 窗口：1280×720，内部 640×360

## 运行时证据

- `loc-watch-board.png`：今日巡守面板同时显示两条巡路、一项悬赏和三项遗迹
  准备，包含奖励、进度、状态、焦点按钮与说明。中文在 640×360 内部画布中无
  裁切、遮挡或溢出，三栏信息层级和暖金/青绿焦点状态可区分；底部“返回”按钮
  完整落在安全画布内。

## 确定性入口

```text
--playtest-starfall-watch-board
--capture-playtest=res://artifacts/screenshots/loc-watch-board.png
```

## 验证

- LOC-WATCH 领域、存档、真实匹配/不匹配战斗接线、试玩注册与本地化聚焦测试：
  10/10。
- C# 编译：0 警告、0 错误。
- Phase G 快速门禁：51/51。
- 中英本地化键集合一致：2336/2336。
- Godot 4.7.1 编辑器导入与主场景 180 帧无界面启动通过。
- Apple M3 / Metal 自动截图进程成功完成。

## 未覆盖

- 自动测试覆盖巡路到达、匹配/不匹配击败、战败失败、奖励容量原子性、三项准备、
  同日恢复、非法存档归一化、跨日重置，以及邮件、主线任务和施工状态隔离；本轮
  截图只用于面板布局验收，没有代替真人连续完成两条巡路和一项悬赏。
- 物理手柄、真人节奏、Windows 与 Linux 对应系统实机仍待后续稳定候选验收。
