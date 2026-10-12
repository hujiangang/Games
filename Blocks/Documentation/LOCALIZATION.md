# 界面文案与多语言

运行时文案集中存放在 `Assets/Resources/Localization/`：

- `zh-CN.json`：默认中文。
- `en.json`：英文示例，包含全部界面文字和 50 个关卡名称。
- `locale` 为语言代码；`fontResource` 为 Resources 下字体路径；`entries` 为 key/value 列表。

`GameText.Get(key, args)` 读取文案，`GameText.Bind(Text, key, args)` 绑定控件。控件对象名只作为代码和测试定位标识，不参与翻译。`level.counter` 等动态文案使用 `{0}`、`{1}` 占位符；译文必须保留编号和格式。

```csharp
GameText.Bind(title, "level.Level_" + level + ".name");
GameText.Bind(counter, "level.counter", level, total);
GameText.SetLocale("en"); // 立即刷新已绑定控件，并保存选择
GameText.SetLocale("zh-CN");
```

新增语言时复制一份表，将 `locale` 和文件名改为对应语言代码，翻译 value。缺少某个 key 时回退到中文；语言表不存在时保留当前语言，`SetLocale` 返回 false。当前默认中文，没有额外增加语言选择按钮。

`LevelData.displayName` 保留给关卡设计工具和图册使用，游戏中的关卡标题读取 `level.Level_N.name`。重新生成关卡并修改名字时，同步更新语言表。

字体为 OFL 授权的 Noto Sans SC 子集。增加文字后运行 `python tools/build_ui_font.py`，脚本会扫描语言表并重建字体。新增该字体不覆盖的文字系统时，设置对应语言的 `fontResource`，并确认字符覆盖及授权。

验证包括中英文 key 集合、占位符一致、所有关卡标题、即时切换、已打开弹窗刷新及英文弹窗溢出检查。增加语言后也应补上长文本和目标屏幕的截图检查。
