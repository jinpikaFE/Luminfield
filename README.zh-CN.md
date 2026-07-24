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

游戏内一天约等于现实四分钟，也可以在小屋的床上提前结束当天。

## 操作方式

| 操作 | 键盘 | 手柄 |
| --- | --- | --- |
| 移动 | WASD / 方向键 | 左摇杆 / 十字键 |
| 使用 / 交互 | E / 空格 | A |
| 切换快捷栏 | 数字键 1–8 | 肩键 |
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
