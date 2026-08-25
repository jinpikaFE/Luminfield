# LOC-POST 星光邮驿配送验收

- Worktree：`/Users/edy/Desktop/personal/Luminfield-wt-base-01`
- 分支：`codex/base-01-playtest-bindings`
- 引擎：Godot 4.7.1 .NET
- 图形：OpenGL 4.1 Metal Compatibility，Apple M3
- 窗口：1280×720，内部 640×360

## 运行时证据

- `loc-post-delivery-board.png`：邮路面板同时显示当天两条稳定路线、目标 NPC、
  金币/关系奖励、接取状态、焦点按钮和返回按钮；中文在 640×360 内部画布中无
  裁切、遮挡或溢出。

## 确定性入口

```text
--playtest-starlight-post-delivery
--capture-playtest=res://artifacts/screenshots/loc-post-delivery-board.png
```

## 验证

- 邮路领域与试玩注册聚焦测试：9/9。
- 本地化聚焦测试：6/6，中英键集合一致。
- Phase G 快速门禁：51/51。
- Godot 4.7.1 编辑器导入与主场景 180 帧无界面启动通过。
- Apple M3 / Metal 自动截图进程成功完成。

## 未覆盖

- 自动测试覆盖接取、正确/错误收件人、错误工具、奖励原子性、同日恢复、跨日失效
  和邮件隔离；本轮截图仅用于面板布局验收，没有代替真人连续走到收件人并手动按键
  完成两条路线。
- 物理手柄、真人节奏、Windows 与 Linux 对应系统实机仍待后续稳定候选验收。
