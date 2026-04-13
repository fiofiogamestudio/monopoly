# 嘉兴文化大富翁素材需求清单

整理日期：2026-04-06

## 使用范围

这份文档只描述素材需求，包括 3D 模型、2D 图片、UI 图标、命名规则和生成建议。地图机制和格子规则见 `doc/design.md`。

当前工程的关键加载规则：

- 地图数据来自 `Assets/Resources/map.json`。
- 格子模型通过 `Resources.Load<GameObject>($"Prefabs/TileModels/{tileData.tileName}")` 加载。
- 因此运行时格子 prefab 名称必须尽量和 `map.json` 中的 `tileName` 完全一致。
- 当前代码已给少量旧模型加了别名兜底，例如 `嘉禾码头 -> 起点`、`嘉兴南站 -> 嘉兴站`、`子城遗址 -> 子城`、`杂货铺/超市/小卖部 -> 名创优品/奶茶店`。
- 别名只用于首版过渡，正式展示仍建议补齐同名 prefab。

## 总体美术方向

推荐风格：

- Q 版低多边形棋盘模型。
- 俯视或 45 度等距视角友好。
- 每个格子像一个小型纪念品/微缩景观，便于在棋盘上识别。
- 色彩明快但不要过度饱和。
- 单格模型应控制在一个棋盘格范围内，避免挡住玩家棋子和 UI 文本。
- 中文文字尽量后期在 Unity 中添加，不依赖文生 3D 工具直接生成。

统一 3D 生成 prompt 前缀：

```text
Stylized low-poly board game asset, Jiaxing cultural theme, isometric view, cute miniature diorama, clean topology, simple PBR materials, bright but not noisy colors, Unity game ready, no background, centered object, scale suitable for a 1x1 board tile.
```

中文文字处理建议：

- 模型里保留空白招牌、牌匾、站牌。
- 中文名用 Unity TextMeshPro、贴图或单独 UI 标签添加。
- 不建议让文生 3D 工具直接生成中文，因为很容易变成乱码或伪字。

## 当前已有素材

### 已有运行时格子 prefab

这些已在 `Assets/Resources/Prefabs/TileModels` 下，可被当前框架直接加载：

| prefab | 对应格子 | 状态 |
| --- | --- | --- |
| `起点.prefab` | 起点 | 可用 |
| `南湖红船.prefab` | 南湖红船 | 可用 |
| `蓝印花布馆.prefab` | 蓝印花布馆 | 可用 |
| `三塔.prefab` | 三塔 | 可用 |
| `天主教堂.prefab` | 天主教堂 | 可用 |
| `南湖天地.prefab` | 南湖天地 | 可用 |
| `嘉兴站.prefab` | 嘉兴站 | 可用 |
| `名创优品.prefab` | 南湖天地装饰候选 | 可用但不是地图格子名 |
| `奶茶店.prefab` | 南湖天地装饰候选 | 可用但不是地图格子名 |
| `子城.prefab` | 子城遗址候选 | 名称不匹配，需要复制或改别名 |

### 已有玩家 prefab

这些已在 `Assets/Resources/Prefabs/PlayerModels` 下：

| prefab | 状态 |
| --- | --- |
| `Duck.prefab` | 当前代码会生成，显示为“南湖小鸭” |
| `Rabbit.prefab` | 当前代码会生成，显示为“蓝印小兔” |
| `Panda.prefab` | 当前代码会生成，显示为“粽香熊猫” |
| `Dog.prefab` | 当前代码会生成，显示为“月河小狗” |

### 已有美术源目录

`Assets/Art/Models/毕设模型` 下已有可整理的模型源目录：

- `三塔`
- `乌镇（拼接建筑）`
- `五芳斋`
- `南湖天地`
- `名创优品`
- `嘉兴站`
- `地区区块以及参考`
- `天主教堂`
- `奶茶店`
- `子城标志`
- `子城遗址等级1`
- `子城遗址等级2`
- `月河`
- `服装店`
- `红船`
- `红船革命纪念馆`
- `蓝印花店铺1`
- `蓝印花店铺2`
- `蓝印花店铺3`
- `蓝印花店铺4`
- `起点`

优先从这些已有源目录整理 prefab，只有缺口再用文生 3D 或图生 3D 补。

## P0 必须补齐的运行时格子模型

P0 的目标：24 个地图格子都能显示明确模型，不再出现空格子。新版地图不做地产升级，因此优先补“格子本体”和通用答题/抽卡/事件模型。

| 目标 prefab 名称 | 类型 | 当前状态 | 建议来源 | 生成/整理要求 |
| --- | --- | --- | --- | --- |
| `嘉禾码头.prefab` | 事件/起点 | 缺失 | 可由 `起点.prefab` 改造 | 码头、嘉禾币标识、小旗帜，作为起点更醒目 |
| `五芳斋粽子坊.prefab` | 地产 | 运行 prefab 缺失 | `Assets/Art/Models/毕设模型/五芳斋` 或重新生成 | 小型粽子店，竹叶、粽子、暖色店面，留空招牌 |
| `杂货铺.prefab` | 道具 | 缺失 | 新生成通用模型 | 小店铺、卡牌箱、油纸伞/船票元素 |
| `超市.prefab` | 道具 | 缺失 | 复制杂货铺或新生成 | 小型便利店/货架，风格比杂货铺更现代 |
| `小卖部.prefab` | 道具 | 缺失 | 复制杂货铺或新生成 | 小摊位、小货架、怀旧招牌 |
| `嘉兴文化问答馆.prefab` | 答题 | 缺失 | 新生成通用模型 | 开本书、问号牌、小讲台，可偏红色 |
| `嘉兴南站.prefab` | 车站地产 | 运行 prefab 缺失 | 由 `嘉兴站.prefab` 改造或新生成 | 现代高铁站，玻璃幕墙，留空站牌 |
| `运气卡.prefab` | 运气 | 缺失 | 新生成通用模型 | 浮动卡牌、骰子、云纹、柔光；3 个运气格共用同名模型 |
| `马家浜文化问答.prefab` | 答题 | 缺失 | 复制问答馆或新生成 | 问答牌搭配史前陶器/稻作图形 |
| `烟雨朦胧.prefab` | 事件 | 缺失 | 新生成 | 小船搁浅、水面、雨雾、荷叶，表达“船只搁浅” |
| `月河历史街区.prefab` | 地标/事件 | 运行 prefab 缺失 | `Assets/Art/Models/毕设模型/月河` 或新生成 | 江南水街、白墙黑瓦、小桥、灯笼 |
| `粽香问答.prefab` | 答题 | 缺失 | 复制问答馆或新生成 | 问号牌搭配粽叶/蒸笼元素 |
| `子城遗址.prefab` | 风物地产 | 名称不匹配 | 复制/整理 `子城.prefab` 或源目录 | 城墙遗址、石门、青砖，名称必须匹配 `子城遗址` |
| `蓝印花布问答.prefab` | 答题 | 缺失 | 复制问答馆或新生成 | 问号牌搭配蓝白布纹/染缸元素 |
| `海宁观潮.prefab` | 事件 | 缺失 | 新生成 | 潮水浪花、观潮栏杆、警示牌 |
| `乌镇水乡问答.prefab` | 答题 | 缺失 | 复制问答馆或新生成 | 问号牌搭配水巷/乌篷船元素 |
| `大运河长虹桥.prefab` | 事件 | 缺失 | 新生成 | 古桥、运河水面、小船 |

## P1 建议补充的反馈/装饰模型

这些不影响地图完整显示，但能提升可玩性反馈。新版地图首版不做地产升级，所以不再把升级模型列为必须项。

| 素材名 | 用途 | 要求 |
| --- | --- | --- |
| `地产归属旗帜.prefab` | 显示地产所属玩家 | 小旗帜或颜色标记，支持换材质/颜色 |
| `过路费提示牌.prefab` | 显示地产收费感 | 小木牌或金币标识，可放在地产边缘 |
| `答题灯牌.prefab` | 强化答题格识别 | 问号、书本、讲解牌组合 |
| `抽卡牌堆.prefab` | 强化抽卡格识别 | 道具卡/运气卡牌堆，可换色复用 |

## P1 四位主角模型

当前代码已经默认生成 4 位嘉兴文化主题动物主角：`Duck`、`Rabbit`、`Panda`、`Dog`。首版配置为 1 名真人玩家 + 3 名 AI。

推荐首版角色：

| 目标 prefab | 显示名 | 文化主题 | 模型要求 | 可复用来源 |
| --- | --- | --- | --- | --- |
| `Duck.prefab` | 南湖小鸭 | 南湖红船、水乡河道 | 小鸭向导造型，红船色彩，小桨、船票或红色领巾 | 新增小鸭模型 |
| `Rabbit.prefab` | 蓝印小兔 | 蓝印花布、传统染织 | 蓝白布纹耳饰或围巾，布卷、染缸或画笔，小工匠造型 | 复用并改造现有 Rabbit |
| `Panda.prefab` | 粽香熊猫 | 五芳斋粽子、嘉兴风物 | 围裙、蒸笼、粽子篮、竹叶元素，温暖绿色/米色 | 复用并改造现有 Panda |
| `Dog.prefab` | 月河小狗 | 月河历史街区、市集摊位 | 小掌柜/摊主造型，灯笼、钱袋、小摊牌，江南水街气质 | 新增小狗模型 |

如果想完全贴合“嘉兴文化”，建议优先用“文化服饰 + 道具”来表达，而不是强调动物本身。比如熊猫不是嘉兴符号，但穿上粽香小厨服装、拿粽子篮后，可以作为可爱棋子使用。

角色模型通用要求：

- 体量要比格子建筑小。
- 轮廓清晰，棋盘视角能一眼区分。
- 根节点朝向和 Rabbit/Panda 尽量一致，避免移动时朝向奇怪。
- 每个角色需要一张 2D 头像，用于 HUD 和角色选择界面。
- 角色主色建议区分明显：红、绿、蓝、橙/棕。

角色技能对应关系见 `doc/design.md`。

## 2D 图片与 UI 素材需求

首版如果不追求完整 UI 美术，可以先用 Unity 默认 UI 和文字。但为了展示效果，建议补这些 2D 素材：

| 素材 | 优先级 | 用途 | 要求 |
| --- | --- | --- | --- |
| 嘉禾币图标 | P0 | 金钱 HUD、奖励/扣款日志 | 圆形钱币或米粒纹样，清晰小图标 |
| 道具卡背面 | P0 | 抽道具卡时展示 | 嘉兴水乡纹样、油纸伞/船票元素 |
| 运气卡背面 | P0 | 命运/机会抽卡时展示 | 云纹、骰子、卡牌元素 |
| 答题弹窗背景 | P1 | 答题 UI | 书卷或讲解牌风格 |
| 道具卡图标 6 张 | P1 | 手牌显示 | 油纸伞、乌篷船票、粽香补给、蓝印花护符、导游旗、商铺优惠券 |
| 运气卡图标 5 张 | P1 | 抽卡结果展示 | 市集旺铺、讲解费、游客打赏、烟雨耽搁、文创热卖 |
| 主菜单背景 | P2 | MenuScene | 嘉兴水乡/南湖主题背景 |
| 游戏标题 Logo | P2 | 菜单与说明界面 | “嘉兴文化大富翁”或最终项目名 |

2D 图片建议尺寸：

- 卡牌图标：`512x512`。
- UI 背景：`1920x1080` 或 `2048x1024`。
- 小图标：`256x256` 起步即可。

## 文生 3D / 图生 3D prompt 草案

### 五芳斋粽子坊

```text
Stylized low-poly miniature Chinese rice dumpling shop inspired by Jiaxing Wufangzhai, green bamboo leaves, zongzi decorations, warm storefront, blank signboard for later Chinese text, board game tile asset, Unity ready, no background.
```

### 嘉兴南站

```text
Stylized low-poly modern high-speed railway station building, compact miniature for board game, glass facade, simple platform roof, blank station signboard, Jiaxing South Railway Station inspired, Unity ready, no background.
```

### 月河历史街区

```text
Stylized low-poly Jiangnan canal historic street, white walls and black roof tiles, small stone bridge, red lanterns, narrow canal, miniature board game diorama, Jiaxing Yuehe inspired, Unity ready, no background.
```

### 答题区 A/B

```text
Stylized low-poly quiz tile, open book, small podium, question mark sign, cultural trivia theme, cute board game marker, Unity ready, no background.
```

### 道具区 A/B

```text
Stylized low-poly prop card stall, small market table, mystery card box, umbrella and ticket props, board game item tile, Unity ready, no background.
```

### 烟雨朦胧

```text
Stylized low-poly misty canal event tile, small wooden boat stuck in shallow water, light rain, fog cloud shapes, Jiangnan rainy atmosphere, cute miniature board game asset, Unity ready, no background.
```

### 运气卡格

```text
Stylized low-poly lucky card tile, floating cards, dice, cloud pattern, soft glow, board game chance tile, Jiaxing cultural color palette, Unity ready, no background.
```

### 子城遗址

```text
Stylized low-poly ancient city wall ruins, stone gate fragment, mossy bricks, small heritage site marker, miniature board game tile, Unity ready, no background.
```

### 南湖天地店铺组合

```text
Stylized low-poly modern shopping block decoration, milk tea shop, clothing shop, small lifestyle store, compact modular storefronts, blank signboards, board game tile decoration asset, Unity ready, no background.
```

### 南湖小鸭

```text
Stylized low-poly cute duck board game character, Jiaxing Nanhu red boat navigator theme, small guide outfit, red scarf or red boat color accents, holding a small wooden oar or boat ticket, friendly mascot proportions, Unity game ready, no background, centered character.
```

### 蓝印小兔

```text
Stylized low-poly cute rabbit board game character, blue calico textile artisan theme, blue and white indigo cloth pattern outfit, holding a fabric roll or dye brush, traditional craft feeling, friendly mascot proportions, Unity game ready, no background, centered character.
```

### 粽香熊猫

```text
Stylized low-poly cute panda board game character, Jiaxing zongzi chef theme, warm apron, bamboo leaves, small zongzi basket or steamer, green and cream color palette, friendly mascot proportions, Unity game ready, no background, centered character.
```

### 月河小狗

```text
Stylized low-poly cute dog board game character, Jiangnan Yuehe historic street merchant theme, small shopkeeper outfit, coin pouch, lantern or market stall sign prop, warm brown and orange color palette, friendly mascot proportions, Unity game ready, no background, centered character.
```

## 交付命名规范

模型交付建议：

- 源模型可放在 `Assets/Art/Models/...`。
- 运行时 prefab 必须放在 `Assets/Resources/Prefabs/TileModels` 或 `Assets/Resources/Prefabs/PlayerModels`。
- 地图格子 prefab 名称必须和 `map.json` 的 `tileName` 一致。
- 通用装饰模型可以不放在 `TileModels`，但建议放在 `Assets/Resources/Prefabs/TileDecorations`。
- 2D UI 图片建议放在 `Assets/Art/UI`，后续需要运行时加载再迁移到 `Resources`。

最容易踩坑的命名：

- `子城.prefab` 当前可通过代码别名临时匹配 `子城遗址`，但正式展示建议新增 `子城遗址.prefab`。
- `五芳斋` 源目录不能自动匹配 `五芳斋粽子坊`，需要生成 `五芳斋粽子坊.prefab`。
- `嘉兴站.prefab` 当前可通过代码别名临时匹配 `嘉兴南站`，但正式展示建议单独做 `嘉兴南站.prefab`。
- 三个道具格现在分别叫 `杂货铺`、`超市`、`小卖部`，当前可通过代码别名临时复用 `名创优品/奶茶店`，但正式展示建议分别生成这 3 个 prefab。
- 三个运气格都叫 `运气卡`，只需要一个 `运气卡.prefab` 即可复用。
- 四位主角建议使用英文 prefab 名称：`Duck.prefab`、`Rabbit.prefab`、`Panda.prefab`、`Dog.prefab`，中文显示名放在角色数据或 UI 文本里。

## 推荐制作顺序

第一批先做 P0 运行时模型：

1. `嘉禾码头.prefab`
2. `五芳斋粽子坊.prefab`
3. `嘉兴南站.prefab`
4. `杂货铺.prefab`
5. `嘉兴文化问答馆.prefab`
6. `运气卡.prefab`
7. `马家浜文化问答.prefab`
8. `烟雨朦胧.prefab`
9. `月河历史街区.prefab`
10. `粽香问答.prefab`
11. `子城遗址.prefab`
12. `蓝印花布问答.prefab`
13. `超市.prefab`
14. `海宁观潮.prefab`
15. `乌镇水乡问答.prefab`
16. `大运河长虹桥.prefab`
17. `小卖部.prefab`

第二批做 2D UI 和卡牌图标：

1. 嘉禾币图标。
2. 道具卡背面。
3. 运气卡背面。
4. 6 张道具卡图标。
5. 5 张运气卡图标。
6. 答题弹窗背景。
7. 四位主角头像。

第三批做角色和表现补强：

1. 地产归属旗帜。
2. 过路费提示牌。
3. 四位主角头像和选择界面立绘。
4. 主菜单背景和标题 Logo。
