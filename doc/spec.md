# Monopoly 工程说明

整理日期�?026-04-12

## 项目定位

这是一�?Unity 3D 嘉兴文化主题大富翁项目，产品名为 `monopoly`。当前版本的核心玩法已经收敛为：

- 掷骰�?24 格地图移�?
- 购买地产与支付固定过路费
- 停留答题格触发嘉兴文化问�?
- 停留道具格抽道具卡，进入手牌
- 停留运气格抽运气卡，立即生效
- 事件格触发固定效�?
- 4 名角色带被动技�?
- 1 名真人玩�?+ 3 �?AI 对手
- 破产淘汰，剩最�?1 名玩家获�?

当前正式玩法不启用地产升级、车站联动、抵押、拍卖�?

## 工程环境

- Unity 版本：`2022.3.62f3c1`
- 主工程文件：`monopoly.sln`
- C# 工程：`Assembly-CSharp.csproj`
- 构建入口场景：`Assets/Scenes/MenuScene.unity`
- 正式游戏场景：`Assets/Scenes/GameScene.unity`

## 目录结构

- `Assets/GameManager.cs`：主回合循环、答题、抽卡、技能、AI、破产和胜负结算
- `Assets/MenuScene.cs`：主菜单、角色选择、规则说明和进入游戏流程
- `Assets/PlayerManager.cs`：生成玩家、逐格移动、同格角色排布、正�?反向移动
- `Assets/MapManager.cs`：根�?`map.json` �?`MapRoot` 下生成格�?
- `Assets/UIManager.cs`：顶部信息、按钮、日志、运行时答题面板、手牌面板、提示弹�?
- `Assets/TileController.cs`：格子模型加载、格子标题和类型展示
- `Assets/Scripts/Data/MapData.cs`：地图、题目、卡牌的数据结构定义
- `Assets/Resources/map.json`�?4 格地图数�?
- `Assets/Resources/question.json`：统一题库，当�?10 �?
- `Assets/Resources/card.json`：统一卡池，当�?6 张道具卡 + 6 张运气卡
- `Assets/Resources/RolePortraits`：菜单运行时使用的角色立绘资源，来源�?`Assets/Art/Sprites/Role`
- `doc/design.md`：地图与玩法设计文档
- `doc/require.md`：模型、图片和 UI 素材需�?

`doc/script.md` 当前不存在；`doc/rule.md` 保留为原始规则记录，未随代码同步修改�?

## 场景入口

当前启动流程为：

1. 进入 `MenuScene.unity`
2. 点击开始按钮后进入选角色界�?
3. 在菜单中�?2x2 角色卡片查看 4 位角色的形象、背景和技�?
4. 进入规则说明界面
5. 确认后再加载 `GameScene.unity`

`MenuScene` 目前使用 `Assets/Art/Sprites/Role` 下的角色图片作为素材来源，并在运行时�?`Assets/Resources/RolePortraits` 加载展示�?

## 场景与地�?

`GameScene.unity` 中的 `MapRoot` 下当前有 24 个带 `GridConfig` 的定位点，构成经典闭环大富翁布局。运行时地图�?`Assets/Resources/map.json` 驱动，分类分布如下：

- 事件类：4 格，包含起点
- 答题类：6 �?
- 抽卡类：6 格，其中道具�?3 格、运气卡 3 �?
- 地产类：8 �?

当前地图顺序�?`design.md` 中的 24 格版本为准，包括�?

- 嘉禾码头
- 南湖红船
- 五芳斋粽子坊
- 杂货�?
- 蓝印花布�?
- 嘉兴文化问答�?
- 嘉兴南站
- 运气�?
- 三塔
- 马家浜文化问�?
- 烟雨朦胧
- 月河历史街区
- 粽香问答
- 超市
- 子城遗址
- 蓝印花布问答
- 南湖天地
- 运气�?
- 嘉兴�?
- 海宁观潮
- 乌镇水乡问答
- 大运河长虹桥
- 小卖�?
- 运气�?

## 角色与技�?

当前默认 4 位主角固定为�?

- 南湖小鸭：掷骰结果为 1 时自动改�?2
- 蓝印小兔：每次买地返�?200 嘉禾�?
- 粽香熊猫：经过起点时额外获得 300 嘉禾�?
- 月河小狗：收到地产过路费时额外获�?100 嘉禾�?

`MenuScene` 中可以选择当前由真人操控的角色。进�?`GameScene` 后：

- 选中的角色会被放到玩�?1 的位置，由真人操�?
- 其余 3 位角色按剩余顺序补位，由 AI 操控
- 角色技能跟随角色本身，而不再依赖固定玩家序�?

## 核心流程

1. `MapLoader` 读取 `Resources/map.json`
2. `MapManager` 根据 `MapRoot` 下已有的 24 �?`GridConfig` 定位点生成格�?
3. `PlayerManager` 生成 4 位角色，并放到起点格
4. `GameManager` 等待地图生成完成后初始化地产、玩家运行时状态、题库和卡池
5. 真人玩家�?`PlayerMove` 阶段点击移动按钮掷骰，AI 自动掷骰
6. `PlayerManager` 逐格移动角色，正向移动时触发经过效果，最终触发停留效�?
7. 若停留到答题格，则弹出问答面板；若停留到道具�?运气格，则抽卡并结算；若停留到地产，则自动判断过路费
8. 真人玩家�?`PlayerAction` 阶段可买地、使用道具卡或结束回合；AI 会做简单买�?用卡决策
9. 资金不足以支付费用时玩家破产并淘汰，地产回收为无�?
10. 当只�?1 名玩家存活时游戏结束

## 已实现的玩法

### 地产

- `tileCost > 0` 的格子视为可购买地产
- 无主地产可购�?
- 落到他人地产时支付固定过路费
- 当前版本租金直接使用 `tileIncome`
- 不启用升级和联动加成

### 答题

- 统一�?`Assets/Resources/question.json` 取题
- 真人玩家通过运行时问答面板答�?
- AI 按概率答�?
- 答对加钱，答错扣钱，并在日志中显示正确答案与解释

### 道具�?

当前通过 `Assets/Resources/card.json` 配置，抽到后进入手牌。首批道具卡为：

- 油纸伞：抵消一次跳过回�?
- 乌篷船票：前�?2 �?
- 粽香补给：获�?500 嘉禾�?
- 蓝印花护符：下一次过路费减免 300
- 导游图：额外掷一次骰子并移动
- 商铺优惠券：下一次买地返�?300

道具卡默认上限为 3 张，超过上限时自动丢弃最早获得的一张�?

### 运气�?

当前通过 `Assets/Resources/card.json` 配置，抽到后立即触发。首批运气卡为：

- 市集行商：获�?600 嘉禾�?
- 讲解费：支付 400 嘉禾�?
- 游客打赏：随机从一名其他玩家处获得 200 嘉禾�?
- 烟雨耽搁：下回合跳过
- 文创热卖：获�?500 嘉禾�?
- 水路绕行：后退 2 �?

### 事件

当前固定事件效果为：

- 起点：经过获�?1000 嘉禾�?
- 烟雨朦胧：下回合跳过
- 海宁观潮：支�?300 嘉禾�?
- 大运河长虹桥：获�?300 嘉禾�?

### AI

当前 AI 已实现：

- 自动掷骰与移�?
- 简单买地策�?
- 简单用卡策�?
- 答题概率判定

AI 目前仍是轻量规则型，不追求复杂博弈�?

### 淘汰与胜�?

- 玩家无法支付费用时立即破�?
- 破产玩家会失去全部地产与手牌
- 角色模型会从地图上隐�?
- 只剩 1 名玩家时弹出结束提示

## UI 系统

`UIManager` 当前负责�?

- 顶部回合名、骰点和状态展�?
- 移动、买地、结束回合按钮的显隐
- 顶部玩家信息条与当前回合高亮
- 运行时答题面�?
- 底部道具手牌面板
- 游戏结束提示弹窗
- 日志输出到场�?UI

## 玩家移动与排�?

`PlayerManager` 当前支持�?

- 正向移动与反向移�?
- 逐格跳跃移动
- 落地晃动效果
- 同格角色自动排布

同格排布规则�?

- 1 人居�?
- 2 人沿对角线分�?
- 3 人三角形分布
- 4 �?2x2 分布

同格人数变化时，非移动中的角色会以短平滑动画重新站位，避免瞬移突变�?

## 资源加载约定

- 地图：`DataLoader.LoadJson<MapWrapper>("map")`
- 题库：`DataLoader.LoadJson<QuestionWrapper>("question")`
- 卡池：`DataLoader.LoadJson<CardWrapper>("card")`
- 玩家模型：`Resources.Load<GameObject>($"Prefabs/PlayerModels/{name}")`
- 格子模型：`Resources.Load<GameObject>($"Prefabs/TileModels/{tileName}")`

## 当前已知限制

- 题目数量目前只有 10 道，后续还需要扩�?
- AI 只是基础策略，还不够“聪明�?
- 没有做抵押、拍卖、升级和联动类深度机�?
- `doc/rule.md` 中蓝印小兔仍写着“升级减费”，与当前已实现代码不一致；正式实现已按 `design.md` 的“买地返 200”执�?

## 2026-04-13 菜单补充

- 选角界面调整为“顶部横�?+ 横排 4 张竖�?+ 底部大按钮”的布局，角色卡直接展示角色主题与技能�?
- 菜单字体优先�?`Assets/Resources/Fonts/MenuFont.ttf` 运行时加载；该文件来源于 `Assets/Fonts/SOURCE HAN SERIF SC HEAVY (TRUETYPE).TTF`�?
- 选角页点击“开始游戏”后会先进入规则说明页，再进入正式对局�?
- 主菜单保留场景中原有标题文字，只对现�?`开始游戏` 按钮做彩色描边风格重绘，并新增同尺寸�?`退出游戏` 按钮�?
- 角色卡进一步强化为“上方角色名 + 更大的角色图 + 下方技能名与技能描述”的结构�?
- `MenuScene` 现已不再依赖场景里预放置的默认开始按钮，主菜单入口按钮改为运行时�?`MenuScene.cs` 创建�?

## 2026-04-13 局�?UI 补充

- 局�?`UIManager` 现在也优先加�?`Assets/Resources/Fonts/MenuFont.ttf`，菜单和对局内字体风格统一�?
- 顶部信息、操作按钮、玩家信息条、对局播报、道具手牌、问答弹窗和提示弹窗都改为统一的卡片式布局与配色�?
- 玩家信息条改为卡片式显示，突出当前行动角色，并在资金文本中显示“行动中 / 已出局”等状态�?
- 局内面板配色已进一步向菜单的角色卡风格靠拢，采用更鲜明的红 / �?/ �?/ �?/ 橙彩色系区分不同模块�?
## 2026-04-13 局�?UI 重构补记

- `Assets/UIManager.cs` 已重建为运行时接管式 UI：顶部信息、操作按钮、玩家状态、日志区、手牌区、问答弹窗、提示弹窗都由脚本统一创建和排版�?
- 运行时会�?`GameScene` 里的�?`TurnText`、`DiceText`、`StatusText`、按钮组、玩家状态区和日志文本重新挂入彩色卡片面板，避免旧排版残留�?
- `PlayerInfoArea` 上原有的布局组件会在运行时禁用，玩家状态卡的位置改为代码统一排布，减少重叠、错位和拉伸问题�?
- 对局内继续统一使用 `Assets/Resources/Fonts/MenuFont.ttf`，并将玩家状态文案修正为“嘉禾币 / 行动�?/ 已出局”等清晰文本�?
- 操作按钮区域现已固定在右下角，并在运行时�?`UIManager` 重新绑定�?`RequestRollDice / RequestBuyOrUpgrade / RequestEndTurn`，不再依赖场景里旧按钮的空点击事件�?
- 玩家状态卡中的角色名与金钱文本改为自适应宽度和自动缩放，尽量避免中文角色名、金额状态被裁切�?
- `GameScene` 的主 HUD 现已切换为“场景内预摆 + 脚本填充数据”的模式：顶部信息面板、操作区、玩家状态区、日志区、手牌区的彩色底板都直接存在于场景中�?

## 2026-04-13 ??? UI ??

- `GameScene` ?????????????????????????????????????????????? UI?
- `Assets/UIManager.cs` ?????????????????????????????????????????????????? UI ???
- `PlayerInfoArea` ?????? `PlayerHudContent` ? 4 ? `PlayerInfoItem` ????`HandPanel` ?????? 3 ???????
- `TurnText / DiceText / StatusText` ??? `TopPanel` ????????????????????? Canvas ?????????

## 2026-04-13 Tile Base Style Update

- `Assets/Prefabs/Tile.prefab` 的基础地块底座已改成几何积木风，当前由 `BaseFrame`、`TopPlate`、`ShowcasePad`、四条边框和 `FrontPlaque` 组成�?- `Assets/TileController.cs` 现在会在初始化时按地块类型统一给底座上色，不再依赖 prefab 里旧的材质引用�?- 当前配色统一为柔和彩色系：地标暖红、风物青绿、事件水蓝、机会金黄、道具天蓝、答题莓粉�?- `TileController` 会在重新初始化时重置 `modelBase` 位置并清理旧模型实例，避免偏移累加和重复模型�?
## 2026-04-13 Scene UI Authority

- `Assets/UIManager.cs` 已改为只负责数据刷新、按钮绑定、弹窗显隐和日志显示，不再在运行时改 `RectTransform`、颜色、字体、描边或按钮样式�?- `Assets/PlayerInfoItem.cs` 不再在运行时重写状态卡的尺寸、字体、文本位置和描边，场景中预摆出来的样式就是运行时样式基准�?- 现在 `Assets/Scenes/GameScene.unity` 里的局�?UI 布局与美术风格由场景本身决定，后续应直接在场景里调整按钮位置、面板大小、字体和配色�?
## 2026-04-13 Action Buttons Update

- `GameScene` 右下角操作区现在以场景中�?`ActionPanel` 为准，三颗按钮统一收回到面板内部排版，并改成更一致的彩色圆角按钮风格�?- 三个按钮的场景默认文案已与运行时一致：`掷骰前进`、`购买地产`、`结束回合`�?- `Assets/UIManager.cs` 现在会在按钮不可用时直接隐藏按钮，而不是只禁用；若当前阶段没有任何可操作按钮，整个 `ActionPanel` 也会隐藏�?
## 2026-04-13 Hand Card Interaction Update

- `GameScene` �?3 个手牌槽现在直接使用场景内卡片结构，每张卡都包含 `CardText`、`SelectionFrame`、`UseButton` 3 个子节点�?- `Assets/UIManager.cs` 已改成“两段式”交互：先点击卡片选中，再点击卡片上的 `使用` 按钮触发道具�?- 被选中的卡片会显示描边；未选中卡片不会显示 `使用` 按钮�?- `UIManager` 的手牌兜底收集逻辑现在只会读取 `HandBody` 下的 3 个顶层卡槽，避免把卡片内部的小按钮误识别成主手牌按钮�?
## 2026-04-13 Question And Money Feedback Update

- `GameScene` 的问答弹窗卡片已重新划分为“标题区 / 题干�?/ 选项区”，避免题干文本和选项按钮互相遮挡�?- 真人玩家答题后，`GameManager` 会先弹出结果提示，明确显示“回答正�?/ 回答错误”、奖励或扣费金额、正确答案与题目解释，再继续结算金钱变化�?- `Assets/UIManager.cs` 新增了金钱变化反馈链路：金额变化会先从地图上的角色位置飞向顶部对应角色的钱栏，再在钱栏附近弹�?`+xxx / -xxx` 飘字，最后才刷新 HUD 上显示的金额数字�?- 金钱 HUD 现在支持临时显示值覆盖，因此 `GameManager` 中的加钱、扣钱、转账、买地花费等操作都能先播反馈动画，再更新可见金额�?
## 2026-04-13 Debug Log Toggle Update

- `GameScene` 左侧 `LogPanel` 现在默认隐藏，不会开局直接显示�?- `Assets/UIManager.cs` 已接�?`F1` 快捷键控制调试日志栏显示与隐藏，日志内容仍会在后台持续累积，重新打开时可继续查看�?
## 2026-04-13 Turn Marker Update

- `Assets/PlayerManager.cs` 现在会为每个角色自动创建 3D 头顶标记，但同一时刻只显示当前行动角色的标记，避免地图上信息过多�?- 标记由“下指箭�?+ 身份牌”组成，会跟随当前行动角色在 3D 世界中悬浮，并在移动时做轻微上下浮动和脉冲放大�?- 主角�?AI 的区分通过头顶身份牌完成：真人玩家显示“玩家”暖色标记，AI 显示“AI”冷色标记�?- `Assets/LookAtCamera.cs` 已修正为真正朝向相机�?billboard 逻辑，供头顶身份牌等世界空间提示复用�?
## 2026-04-13 Facing And Top HUD Fix

- `Assets/LookAtCamera.cs` �?billboard 朝向已反转修正，头顶身份牌不再从主相机视角倒字显示�?- `Assets/PlayerManager.cs` 新增�?`playerFacingYawOffset` 和统一朝向逻辑，角色出生时会先朝向第一段路线方向，移动时会直接面向实际前进方向，不再沿用之前那套侧向朝向计算�?- `Assets/UIManager.cs` 左上角信息面板的文本改成了更直接的“当前角�?/ 上次骰点 / 当前阶段”三行说明，状态文案也收敛成更易读的玩家提示�?- `Assets/PlayerInfoItem.cs` 顶部四张角色状态卡的副文本已缩短为“玩家或 AI + 金额 + 当前”格式，避免长文案把状态栏挤坏�?- `Assets/Scenes/GameScene.unity` �?`TopPanel`、`TurnText`、`DiceText`、`StatusText`、`PlayerInfoArea`、`PlayerHudContent` 的锚点和尺寸已重新整理，局内左上角与顶部状态栏现在以场景布局为准，运行时不会再把它们改乱�?
## 2026-04-13 Tile Label Readability

- Tile labels were enlarged and given stronger contrast in `Assets/Prefabs/Tile.prefab`.
- `TileController` now forces white label text and applies a colored badge background by tile type.

## 2026-04-13 Menu UI Prefab

- Added `Assets/Resources/Prefabs/UI/MenuShell.prefab` for the main menu shell layout.
- `MenuScene.cs` reads UI nodes from the prefab so layout adjustments can be made in the prefab.

## 2026-04-13 Tile Label Scale Fix

- Reduced world-space label canvas scale in `Assets/Prefabs/Tile.prefab` to prevent oversized label panels covering the board.

## 2026-04-13 Tile Materials And Character Scale

- `TileController` now replaces missing-material renderers with a safe runtime material to avoid purple error shaders.
- Player model scale is adjustable via `playerModelScale` in `Assets/PlayerManager.cs` (default 0.5).

## 2026-04-13 Tile Label Surface Layout

- The tile labels in `Assets/Prefabs/Tile.prefab` no longer use `LookAtCamera`; the old billboard behavior has been removed from the tile prefab.
- Both world-space label canvases are now fixed to the tile surface instead of turning toward the camera, so the board reads like a Monopoly board rather than floating signboards.
- The player turn marker still keeps its billboard behavior, but tile name/type labels now follow the tile's own rotation only.

## 2026-04-13 Simple Board Cell Layout

- `Assets/TileController.cs` 现已将地格视觉收敛为简化的大富翁棋盘格风格：`BaseFrame` 作为黑色外边框，`TopPlate` 作为按类型着色的内面�?- `TrimLeft / TrimRight / TrimFront / TrimBack / FrontPlaque / ShowcasePad` 这几层装饰默认会被隐藏，不再作为主要视觉元素参与地图显示�?- 地格中央显示格子名，上方小签显示格子类型，文字直接贴在格子顶面阅读，不再依赖中央模型展示�?- `TileController` 新增 `useTileModels` 开关，当前默认值为 `false`，因此地格默认不再加载地标模型，优先保证棋盘式地图的整体观感与可读性�?
## 2026-04-13 Tile And Marker Facing Fix

- `Assets/Prefabs/Tile.prefab` 中两层贴地文字的平铺方向已修正为正向朝上，避免从主相机视角看到反字�?- `Assets/LookAtCamera.cs` 现改为直接同步到相机旋转，而不是仅仅朝向相机位置，因此头顶悬浮指示会与镜头平面严格对齐�?- `Assets/Resources/Prefabs/UI/PlayerTurnMarker.prefab` 中的世界空间 Canvas 已恢复为正向摆放，并关闭�?`invertForward`，使箭头和“玩�?/ AI”身份牌都直接面向主相机�?
## 2026-04-13 Question Result And Movement Timing Fix

- `Assets/Scenes/GameScene.unity` 中的 `NoticeCard` 已重新调整为稳定的“标题在上、正文在中、按钮在下”结构，正文区域高度增大，确认按钮下移，避免答题结果解释被按钮遮挡�?- `NoticeBody` 文本现改为左上对齐并允许纵向溢出，适合显示“答题结�?+ 正确答案 + 解释”这类多行内容�?- `NoticeConfirm` 的文字样式已切回项目主字体，按钮文字与结果弹窗风格保持一致�?- `Assets/PlayerManager.cs` 新增 `playerStepDuration` �?`playerLandingSettleDuration`，并将移动单步和落地晃动的默认时长缩短，整体走格速度更快�?- `Assets/GameManager.cs` 现在会在主角色步进结束后继续等待所有平滑站位动画结束，再进入下一段结算或下一位玩家回合，避免一个角色尚未完全停稳时另一个角色就开始移动�?- `Assets/Scenes/GameScene.unity` 中的 `PlayerManager` 已同步配置更快的移动参数：`playerStepDuration = 0.22`、`playerLandingSettleDuration = 0.1`、`playerPackingTransitionDuration = 0.12`�?

## 2026-04-13 Property HUD Polish

- 顶部 4 张玩家状态卡现在会显示房产摘要，例如“房产：2处·五芳斋/子城”，并且当前行动角色高亮时改为深色文字，避免亮黄底上的白字看不清。
- 右下角 `ActionPanel` 现在常驻一块场景内的房产信息区，不再靠运行时代码生成 UI；当前角色停在可购买房产或他人房产上时，会显示售价、过路费和所有者。
- 棋盘格颜色重新增强了类别区分：地标、风物、事件、运气、道具、答题 6 类颜色比之前更鲜明，便于一眼分辨格子功能。

## 2026-04-14 Text Encoding Repair

- `Assets/Resources/question.json`、`Assets/Resources/card.json`、`Assets/Resources/map.json` 已整体恢复为正常中文内容，修复了题目、卡牌名、地块名和地块说明的乱码问题。
- `Assets/UIManager.cs` 中与玩家直接交互的标题和面板文本已统一修复，包括答题标题、提示弹窗标题、右侧房产信息、下方手牌标题、空槽文案和金额飞字中的“嘉禾币”。
- `Assets/PlayerInfoItem.cs` 已清理乱码文案，顶部状态卡现在稳定显示“角色 / 玩家或 AI / 金额 / 房产摘要 / 已出局”等文本。
- `Assets/GameManager.cs` 中答题结果弹窗的正文换行已改为真实换行符，不会再把 `\n` 原样显示到文本框里。

## 2026-04-14 Menu Card And Turn Marker Polish

- `Assets/MenuScene.cs` ����ͳһ����ѡ�ǿ���˵��ҳ�Ľ�ɫ����ʽ��ѡ�п�Ƭ���е������������ɫ�����������Ƹ����Ĵ��۳̶ȣ�������֡�ѡ�к��ַ��������塱�����⡣
- ˵��ҳ���ٵ����߾�ͷ��˵����ı��֣�����ֱ�Ӹ��� `Assets/Resources/Prefabs/UI/RoleCard.prefab` �Ľ�ɫ����ʽ���� `RulePortraitFrame` ����ʾ��ǰѡ�н�ɫ��������Ƭ������ϼ������İ�չʾ��
- `Assets/Resources/Prefabs/UI/RoleCard.prefab` �� `Assets/Resources/Prefabs/UI/MenuShell.prefab` ��ռλ�����Ѿ�ͬ����Ϊ���ģ�����ֱ���� prefab ��ͼ����Ű�ʱ�������ӽ�����ʱ��ʵ��Ч����
- `Assets/Resources/Prefabs/UI/PlayerTurnMarker.prefab` �����ս��ͷ�롰��� / AI�������֮��ļ�࣬��������ռ� Canvas �� `SortingOrder` ��ߵ� 320����֤ͷ�������Ⱦ���ȼ����ڵظ��ϵ����ֲ㡣

## 2026-04-14 Selection Polish And Victory Target

- `Assets/MenuScene.cs` ��ѡ�н�ɫ���ѸĻ�ԭ��ɫϵ������ʹ�÷�ɫ���֣���ǰ��Ϊ��ɫ��� + ��΢����Ư����ͻ��ѡ�񵫲��ƻ��ɶ��ԡ�
- `Assets/Resources/Prefabs/UI/PlayerTurnMarker.prefab` ����������ͷ����ͷ����ͼ�ͷ������Ƶļ�࣬ͬʱ���ֱȵظ����ָ��ߵ�����㼶��
- `Assets/UIManager.cs` �ķ�����Ϣ����Ϊ���̵� 3 �нṹ��`�ۼ� / ·��`��`����`��`��ǰ״̬`�������Ķ�������
- `Assets/GameManager.cs` ����Ŀ����ʤ���ߣ���ǰĬ��ֵΪ `30000` �κ̱ң����ڼ�֧�֡��Ʋ���̭��������߻�ʤ����Ҳ֧�֡��ȴﵽĿ��������ʤ������

## 2026-04-14 Active Turn Readability

- `Assets/PlayerManager.cs` ���ڻ�Ϊ��ǰ�ж���ɫ��ʾһ�� 3D ���¸�����������ǿ����ǰ�ж�����ı�ʶ�ȣ�������ֻ�Ե�ǰ�ж���ɫ��ʾ��������΢�������塣
- `Assets/PlayerManager.cs` �� `Assets/Scenes/GameScene.unity` �е� `turnMarkerMinHeight` / `turnMarkerVerticalPadding` ��ͬ���µ���`PlayerTurnMarker` ���������ɫͷ����

## 2026-04-14 Tile Label Wrap And Highlight Tuning

- `Assets/TileController.cs` ���ڻ�Ը�������ͳһ��ʽ�������� 4 ���ֵ����ƻ��Զ����������ʾ���������ӽ��µĿɶ��ԡ�
- `Assets/PlayerManager.cs` �ĵ�ǰ�ж���ɫ�����������˰뾶�����ޡ����������������ƫ�Ʋ����������� Inspector �м���΢���������Ȧ�Ĵ�С��߶ȡ�
## 2026-04-14 房产展示补记

- 顶部玩家状态栏不再把房产信息直接塞进主状态文本，而是拆成状态卡下方的一张独立小卡片，用于显示“无房产 / 1处 / 2处”等房产摘要。
- 已被购买的地产格现在会在朝外一侧显示归属牌，使用角色主题色和角色名标记当前所有者；无主或已回收的地产不会显示归属牌。

## 2026-04-14 Camera Control Update

- GameScene 的主相机现在挂有 Assets/GameCameraController.cs，保留原有 45 度正交视角，并以当前相机为最远视野。
- 鼠标滚轮可在当前最远视野到 4 倍放大之间缩放；放大后，相机会自动聚焦当前行动角色，并在角色移动时平滑跟随。
- 回合切换到下一名角色时，相机会先平滑移动到该角色附近；同时支持 W/A/S/D 手动平移，并限制在地图范围附近，不会无限拖出棋盘。

## 2026-04-14 Game Config Update

- Assets/Resources/game_config.json 现在负责管理对局基础数值，当前已接入 startMoney、enableTargetMoneyVictory、targetMoneyToWin 3 项。
- 当前默认配置调整为：初始资金 8000 嘉禾币，目标胜利资金 18000 嘉禾币；GameManager 会在 Awake 阶段优先读取该配置，再初始化玩家资金。## 2026-04-14 Camera Focus Fix

- Assets/GameCameraController.cs 的跟随逻辑现已改为基于主相机中心射线与棋盘平面的交点来计算焦点偏移，而不是简单把相机 x/z 直接对齐到角色坐标；因此在缩放后跟随角色移动时，当前行动角色会更稳定地保持在镜头中心附近。## 2026-04-14 Tile Layering Fix

- Assets/MapManager.cs 现在会在生成地格时按格子索引为每个地格施加极小的稳定高度偏移，用于降低重叠区域的 z-fighting 闪烁；当前默认值为 	ileLayerYOffsetStep = 0.002、	ileLayerCycle = 7。
- 同样的参数也已写入 GameScene 里的 MapManager 组件，后续可直接在 Inspector 中微调，不需要改代码。## 2026-04-14 Owner Sign Readability Tuning

- Assets/Prefabs/Tile.prefab 中的地产归属牌已整体放大，包括底牌尺寸、前侧底座和世界空间画布缩放；Assets/TileController.cs 也同步提高了归属文字的最佳适配字号范围，购买后的所有者标识会更容易看清。
## 2026-04-14 Menu Resolution Lock

- `Assets/Scenes/MenuScene.unity` 已挂载 `Assets/ForceResolution.cs`，进入菜单时会在 `Awake()` 阶段强制 `1920x1080` 窗口化显示。
