# QuickTimerPlugin（快速计时器）BETA
*有ai生成的内容

ClassIsland插件：在桌面显示一个**置顶浮动计时图标**

## 功能

- **拖动多级选时**：按住图标左键拖动，拖动距离非线性映射到时长档位：
  - 短计时：1–10 分钟，步长 1 分钟
  - 中计时：15–60 分钟，步长 5 分钟
  - 长计时：75–120 分钟，步长 15 分钟
  - 三档范围/步长均可在设置页自定义；拖动时弹出预览层（大号时长 + 短/中/长徽标 + 刻度轨道），拖回原点松手 = 取消，松手即开始计时。
- **计时中**：图标显示进度环 + 剩余时间徽标。
- **操作方式**：
  - 左键单击：暂停 / 继续
  - 左键拖动：多级选时
  - 右键拖动：移动图标（位置记忆）
  - 右键单击：快捷菜单（暂停/继续、±1 分钟、取消、重置位置、隐藏）
- **到点提醒**：通过 ClassIsland 官方提醒（V2 Notification API）发送通知。
- **设置页**：ClassIsland 设置 → 快速计时器，可调整三档范围/步长、拖动灵敏度、徽标与提醒开关。

## 构建

要求 .NET 10 SDK。

```bash
cd QuickTimerPlugin
dotnet build -c Release
```

构建成功后会在 `cipx/` 目录生成 `QuickTimerPlugin.cipx` 安装包，可在 ClassIsland 中安装（设置 → 插件 → 安装插件包）。

注意：若本机没有 PowerShell 7（pwsh），请使用 `dotnet build -c Release -p:GenerateHashSummary=false` 跳过 cipx 的 MD5 摘要生成步骤。

## 调试

使用官方调试模式：先构建 ClassIsland 本体（或在 `QuickTimerPlugin.csproj` 中设置 `ClassIsland_DebugBinaryFile` 等属性指向 ClassIsland 可执行文件），然后在 IDE 中选择 "ClassIsland 插件" 启动配置（`-epp` 参数加载插件输出目录）。

## 结构

```
QuickTimerPlugin/
├── QuickTimerPlugin.csproj        # net10.0 + ClassIsland.PluginSdk 2.1.1.1 + CreateCipx
├── manifest.yml                   # id: community.quicktimer
├── Plugin.cs                      # 插件入口（AppStarted 后创建浮动窗口）
├── icon.png
├── Properties/launchSettings.json # 官方调试启动配置
├── Providers/
│   └── TimerNotificationProvider.cs  # 到点提醒（提醒 V2 API）
├── Services/
│   ├── CountdownService.cs        # 倒计时状态机（DateTimeOffset 防漂移）
│   ├── DragSelection.cs           # 多级非线性拖动映射
│   └── QuickTimerSettings.cs      # 设置持久化（插件配置目录 settings.json）
└── Views/
    ├── FloatingIconWindow.*       # 置顶浮动图标
    ├── TimerOverlayWindow.*       # 拖动选时预览层
    └── SettingsPages/
        └── QuickTimerSettingsPage.*  # 设置页
```
