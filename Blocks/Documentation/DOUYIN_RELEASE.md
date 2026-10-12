# 抖音接入与发布

## 当前实际状态

- 已有：TTSDK 6.2.1+ API 的条件编译适配代码、初始化、宿主 AppID 校验、激励视频、插屏、前后台生命周期及侧边栏入口。
- 已有：完整观看奖励、一次性回调结算、失败/提前关闭/超时恢复、广告期间暂停声音与输入。
- 已有：中文观看确认界面；玩家可取消观看并继续游戏；本地模拟默认关闭。
- 已验证：无 SDK 的 Unity 编译、50 关运行、受控广告回调逻辑及 Windows 试玩构建。
- **未完成：官方 TTSDK 的安装与真实 SDK 分支编译、开发者账号配置、真机广告播放、抖音导出包和审核发布。**

本次环境中项目没有 TTSDK。官方安装流程需要在 BGDT 登录后安装 SDK；用户要求将 AppID 与广告位 ID 留到后续配置，因此不能将当前结果标记为已接通真实抖音广告。

## 接入顺序

1. 在抖音小游戏开发者平台创建/选择正确项目，开通流量主，建立激励视频和插屏广告位。
2. 按官方 [安装 TTSDK](https://partner.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/unity-game-access/install-connect/install-sdk) 导入 BGDT，登录，安装与 Unity 2022.3.62f2c1 及导出方案兼容的稳定版本。接口最低版本为 **6.2.1**。
3. 如果 TTSDK 使用 asmdef，在 `Assets/Scripts/Game/Blocks.Game.asmdef` 的 Assembly Definition References 添加 SDK 对应运行时程序集。以实际导入包的程序集名称为准。
4. 在目标平台 Scripting Define Symbols 添加 `BLOCKS_DOUYIN`，等待 Unity 完成真实 SDK 分支编译。若实际 SDK 类型/签名有变动，以 SDK 源码及官方当前接口为准更新适配器。
5. 在 BGDT/TTSDK 的游戏配置中填写 AppID；同时填写 `Assets/Resources/Config/Douyin.json`：

```json
{
  "enabled": true,
  "appId": "你的小游戏AppID",
  "rewardedVideoAdId": "你的激励视频广告位ID",
  "interstitialAdId": "你的插屏广告位ID",
  "editorSimulation": false,
  "firstInterstitialLevel": 6,
  "interstitialEveryClears": 3,
  "interstitialCooldownSeconds": 120,
  "initialDelaySeconds": 90,
  "loadTimeoutSeconds": 20
}
```

6. 执行 `Blocks > Check Douyin release configuration` 和 `Blocks > Validate content and gameplay rules`。
7. 选择官方支持的发布方案。当前本机 Unity 安装含 Android 模块，**没有 WebGL 模块**。若需要覆盖 iOS，请按官方 WebGL 方案安装匹配的工具链并导出；Windows EXE 仅用于本地试玩。
8. 在抖音开发者工具和真机完成下述验收，再从平台上传与提交审核。

客户端不放 AppSecret。当前玩法使用本地进度；未实现账号登录、云存档和服务端奖励结算。若后续加入付费道具或跨设备资产，应单独接入服务端鉴权及持久化。

## 广告策略

### 激励视频

- 有提示次数时直接使用，预览至少 4 秒。
- 次数为 0 时，眼睛图标显示播放标记及“+1”角标；点击打开完整观看可获得次数的说明弹窗。
- 玩家在确认弹窗主动选择观看后发起广告。
- 仅 `OnClose` 报告完整观看且完成次数至少为 1 时发放一次奖励。
- 奖励写入本地存档并更新次数，不自动打开预览、不立即消耗。玩家随后点击“看一眼”才扣 1 次并显示图案。
- 重复回调和旧请求迟到回调不重复发奖。
- 提前关闭、无填充、加载错误、超时不发奖，也不扣已有提示。
- SDK 未初始化、未配置或非宿主环境时提示暂不可用，不模拟播放成功。

### 插屏

- 插屏为平台提供的可关闭广告；关闭按钮和实际图文/视频外观由抖音控制。关闭后继续，不增加提示次数。
- 第 6 关及之后，累计至少 3 次首次通关。
- 本次启动至少 90 秒；距离上次激励/插屏结束或失败至少 120 秒。
- 只在通关后点击“下一关”时尝试，不打断正在拼图。
- 失败、无填充或超时继续下一关；SDK 不可用时直接进入下一关。
- 重玩已经完成的关卡不累计首次通关次数。

平台要求广告模块启动后及广告间隔达到最低时长；本项目配置更保守，但仍须通过真实宿主确认。参见 [插屏广告](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/ads/interstitial-ad)。

## 真机验收

| 情况 | 预期 |
| --- | --- |
| 首次启动、拖放、两指触摸、点 UI | 正常进入关卡；只有一个触点控制拖拽；点击 UI 不拖动后面的拼块 |
| 第一关、斜边图案、第 50 关 | 能完成且只结算一次；最后一关显示全部完成 |
| 关闭/重开、切后台、锁屏 | 进度和静音保留，声音和拖动恢复正常 |
| 视频完整观看、提前关闭、断网、无填充 | 仅完整观看增加并保存 1 次；不会自动用掉，重进游戏仍可用；其他情况能继续游戏 |
| 关间插屏、连续重玩、连续快速过关 | 不在关中弹出，不因错误卡住下一关，频率限制生效 |
| 刘海屏、窄屏、低端机、宿主右上角胶囊 | HUD 不被裁剪和宿主遮挡，初始色块可见，工具栏收起和展开均正常，拖动流畅，音乐能恢复 |
| 棋盘拼到一半拿下来、在上下左右自由放置、再拿回棋盘 | 保持自由位置和拼图尺寸，不限定回收区域；再次拼入正常吸附；整关完成前均可继续调整 |
| 支持/不支持侧边栏的宿主 | 能力检测成功才显示入口；跳转失败给出反馈 |

正式发行还需要在平台填写该账号自己的主体、隐私说明和游戏资料。这些内容未用占位账号发布。

## 相关官方接口

- [初始化与宿主环境](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/init/init)
- [激励视频广告](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/ads/rewardedvideoad)
- [插屏广告](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/ads/interstitial-ad)
- [前后台生命周期](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/system/game-lifecycle)
- [侧边栏](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/open-ability/sidebar)
