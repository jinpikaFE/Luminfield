# Luminfield

简体中文 | [English](README.md)

Luminfield 是一款使用 Godot 4.7.1 .NET 与 C# 制作的原创双语农场生活
RPG 垂直切片。项目没有使用《星露谷物语》或其他商业游戏的素材、角色、
地图、文本、音乐及代码。

## 可玩内容

当前版本包含以下完整流程：

1. 与米菈交谈，领取星芽莓种子。
2. 开垦至少三块农田。
3. 种下并浇灌三株星芽莓。
4. 在小屋睡眠两次，让已浇水的作物成长。
5. 收获三株成熟的星芽莓。
6. 返回米菈处交付作物并完成试玩。

完成教程后可以继续经营：

- 在右侧的暮光商亭购买星芽莓和月辉根种子。
- 出售星芽莓、月辉根以及价值更高的加工品，赚取微光币。
- 在月井蒸馏台投入 2 份作物，睡眠一夜后领取星芽莓蜜饯或月辉根药剂。
- 用收入购买更多种子，形成“种植 → 加工 → 出售 → 扩种”的持续循环。

原有 48×32 农场现已通过南侧发光石阶连接到 192×128 格的大世界。玩家可以
沿道路自由探索家园农场、低语林地、星落草甸、晶辉谷、月水湿地与星落遗迹
六个区域。地图按 32×32 格区块加载，只保留玩家周围 3×3 个区块，因此镜头
可以连续跨区移动，而不必一次创建整张地图。

右上角迷你地图会随移动揭开已探索区块，隐藏未知区域，并标记已发现地标。
探索迷雾与区块记录会和常规存档一起保存。

## 工具与背包

8 格快捷栏和背包现在是两个层次：

- 第 1 格固定为“手”，用于收获成熟作物和查看地标。
- 星尘铲负责开垦农田，也能挖掘大世界里的晶体。
- 月纹柴刀负责砍伐可采集树木。
- 凝露壶最多携带 12 份水。
- 面向池塘、溪流或湿地水面使用汲水桶，可以补满凝露壶。
- 完整背包共有 24 格，按 `B` 或 `Tab` 打开；最上方 8 格就是快捷栏。

目标区会识别前方物件：青绿色轮廓表示可以按 `E` 执行，暖金色表示需要切换
工具，玫红色表示体力、水量、种子或背包容量不足。树、晶体、水面、作物、NPC、
门、商亭、蒸馏台和床都会高亮真实对象范围，而不是只显示一个无含义的地面框。

树木产出微光木，晶簇产出晶辉碎片。采集后的节点会从地图消失、变为可通行，
并随存档保持已采集状态。旧存档中的 `hoe` 会自动迁移为新的固定工具顺序，
不会丢失已有种子和收成。

游戏内一天约等于现实四分钟，也可以在小屋的床上提前结束当天。

## 操作方式

| 操作 | 键盘 | 手柄 |
| --- | --- | --- |
| 移动 | WASD / 方向键 | 左摇杆 / 十字键 |
| 使用 / 交互 | E / 空格 | A |
| 切换快捷栏 | 数字键 1–8 | 肩键 |
| 打开背包 | B / Tab | Y |
| 暂停 | Esc | Start |

## 本地工具链

项目固定使用以下版本：

- Godot 4.7.1 .NET
- .NET SDK 10.0.302
- 项目目标框架 `net8.0`

当前工作区的工具安装在仓库外部：
`/Users/edy/.codex/tools/luminfield/`。

```bash
export LUMINFIELD_TOOLS=/Users/edy/.codex/tools/luminfield
export DOTNET_ROOT="$LUMINFIELD_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

"$DOTNET_ROOT/dotnet" build
"$LUMINFIELD_TOOLS/godot/Godot_mono.app/Contents/MacOS/Godot" \
  --path /Users/edy/Desktop/personal/Luminfield --editor
```

## 验证与导出

```bash
dotnet test tests/Luminfield.Tests/Luminfield.Tests.csproj
godot --headless --path . --editor --quit
godot --headless --path . --quit-after 180
./scripts/export_all.sh
```

存档会通过原子写入保存到 `user://saves/slot_1.json`。损坏的存档不会被
静默覆盖，而是保留为带 `.broken-<时间戳>` 后缀的文件。
微光币、加工队列、加工品、凝露壶水量、已采集世界节点和 24 格背包均会随
现有 `schemaVersion: 1` 存档保存；旧存档会获得安全默认值并迁移旧工具 ID。
探索进度使用稳定区块 ID 保存；旧存档会默认揭开家园所在区块。

发布产物位于：

- `builds/macos/Luminfield.zip`：ARM64 与 x86_64 通用应用，使用本地
  ad-hoc 签名供测试。
- `builds/windows/Luminfield.exe`：Windows x86_64 程序及相邻的
  .NET 数据目录。
- `builds/linux/Luminfield.x86_64`：Linux x86_64 程序及相邻的
  .NET 数据目录。

导出脚本会在 macOS ad-hoc 签名时重新生成权限 DER，以适配当前 macOS
版本的本地启动要求。Developer ID 签名与公证不属于本次垂直切片范围。

关键视觉验收截图保存在 `artifacts/screenshots/`。
