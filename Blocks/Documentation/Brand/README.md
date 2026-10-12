# 色块拼图 · 美术素材

生成方式：Codex 内置 image_gen；首批生成日期：2026-09-30，新增背景日期：2026-10-05。游戏名沿用「色块拼图」，英文副名 COLOR BLOCKS。

配色：雾蓝背景、深蓝文字、薄荷绿主按钮；拼块使用蓝、杏黄、珊瑚、薄荷和浅紫。游戏背景弱化装饰，给棋盘和自由拖拽留出清晰空间。

运行时从 `Config/Backgrounds.json` 中的雾蓝、薄荷、暖杏和浅紫四张背景随机选择，切换关卡时避开上一张。Logo 保留在 `Assets/Resources/Brand/logo.png`，当前关卡内不显示，项目没有额外加载页。其余原图保存在 `Assets/Art/Brand/`，供图标和宣传使用。宣传图是概念插画，并非游戏截图。

打开 [`gallery.html`](gallery.html) 可查看全部素材及实际界面；[`assets.json`](assets.json) 记录实际输出尺寸，生成工具的输出尺寸与提示中的目标尺寸可能不同。

| 文件 | 实际尺寸 | 用途 |
| --- | --- | --- |
| `logo.png` | 1835 × 857，RGBA | 中文品牌 Logo，保留透明通道；关卡内不显示 |
| `background.png` | 948 × 1659，RGB | 竖屏背景，运行时淡化显示 |
| `app-icon.png` | 1254 × 1254，RGB | 不透明应用图标，已配置为默认 Player 图标 |
| `mosaic-emblem.png` | 1254 × 1254，RGBA | 透明拼块徽标，宣传与活动素材 |
| `promo-sailboat.png` | 1659 × 948，RGB | 帆船主题宣传插画 |

保留生成原图。发布时可通过 Unity 图标导入流程或平台上传工具输出要求的像素尺寸。

## 生成提示词

新增三张背景的完整提示词见 [`background-prompts.json`](background-prompts.json)。对应文件为 `Assets/Resources/Brand/background-mint.png`、`background-apricot.png`、`background-lavender.png`，原图均为 941 × 1672；Unity 导入上限为 1024。中间留出大面积空白，装饰集中在外围，便于辨认色块。

### logo.png

Use case: logo-brand. Create a finished game logo PNG on a genuinely transparent alpha background for a Chinese casual polygon puzzle mobile game. Exact Chinese title: 色块拼图. Small secondary exact text: COLOR BLOCKS. Landscape composition, around 1536x1024 with generous transparent margin, tightly arranged single horizontal lockup: a distinctive square pinwheel mosaic symbol at left, four large interlocking geometric polygon tiles with one diagonal seam, rounded slightly tactile bevels, at right beautiful thick friendly custom Chinese typography 色块拼图 in deep ink blue, very legible and accurately written. Premium calm playful puzzle branding, not childish. Palette mist blue, mint teal, warm apricot yellow, soft coral, small lavender accent. Soft 2.5D ceramic candy finish with subtle clean shading, crisp edges readable when small. No extra text, no trademark symbols, no watermark, no mockup, no background rectangle. One logo only. Preserve alpha transparency.

### mosaic-emblem.png

Create a finished square mobile game app icon, no text. Full bleed square 1024x1024, pale mist blue background with very subtle gradient. Center one bold large rounded square mosaic emblem made of five broad interlocking geometric ceramic tiles: sky blue at upper left, warm apricot yellow upper right, mint teal lower left, soft coral pink lower right, small lavender diamond near center. Premium casual polygon puzzle game, calm playful, crisp polished 2.5D bevels, soft shadows, coherent elegant geometry, tightly fitted seam lines, generous outer margins 12%, high readability at tiny sizes. No letters, no numbers, no objects other than mosaic, no watermark, no external mockup border.

### background.png

Use case: stylized-concept. Asset type: portrait background image for a calm casual polygon puzzle game, 1024x1792 vertical. Create a beautiful quiet mist-blue paper-like background, with a very subtle pale blue to ivory gradient and a few large rounded ceramic geometric puzzle fragments cropped along the extreme left and right borders and bottom corners. Mint teal, pale sky blue, warm apricot, soft coral, a small lavender accent. Mostly near-white desaturated colors and soft ambient shadows. Reserve the entire central 80 percent as clean, nearly flat pale mist-blue negative space where the interactive game will be drawn; no central subjects or board. Restrained polished mobile game art direction, spacious, gentle daylight. No text, no UI, no frame, no logo, no checkerboard, no transparency. Opaque full bleed rectangular image.

### app-icon.png

Edit this mosaic emblem into a finished app store icon. Preserve the exact five colored interlocking puzzle tiles, material, lighting, proportions, and shape. Place the emblem centered on a fully opaque solid pale mist-blue (#E8F2F7) background that fills the ENTIRE square image, including all four corners. There must be NO transparency anywhere. Clean any stray floating pixels around the emblem. Keep 12 percent background margin. 1024x1024 square. No text or additional objects. Deliver a complete opaque app icon, not a cutout.

### promo-sailboat.png

Use case: ads-marketing. Create polished landscape key art, 1792x1024, for a Chinese casual geometric puzzle game. Exact title text at upper left: 色块拼图. Small line beneath it exactly: 拼一块，放松一下. Friendly highly legible deep ink-blue Chinese type. Right two thirds: a charming arrangement of 7 LARGE simple broad ceramic polygon puzzle tiles assembling into a stylized little sailboat over a pale mist-blue surface, soft sky blue, mint teal, warm apricot, coral, lavender. A couple of loose polygon tiles in foreground implying assembling the picture. Calm premium 2.5D toy-like illustration, matte rounded bevels, gentle daylight, subtle ambient shadows, uplifting and uncluttered. Wide spacious editorial composition. Left third clear to read text. Opaque pale blue background, rounded abstract cloud forms only near outer corners. Do not add app store badges, QR codes, buttons, numbers, logos from other brands, or watermarks. This is promotional key art rather than a screenshot.
