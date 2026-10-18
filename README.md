# 山海神隐录

横屏分支剧情解谜游戏，包含两条互斥路线、六章剧情和三个结局。正式运行版本为团结引擎导出的 H5；团结引擎基于 Unity 2022 LTS。

本仓库包含可编辑工程、完整游戏美术、剧情数据、测试工具和验证文档。首次克隆后，用团结引擎 **1.10.4 / 2022.3.62t16** 打开仓库根目录，按下文“团结引擎工程”步骤运行或构建。H5 游玩包通过 [GitHub Releases](https://github.com/Es777777/shanhai-shenyinlu/releases) 下载；Demo 视频另行保存。仓库中的 `Builds/` 会在构建和测试时生成。

## 正式 H5 游戏

下载 Release 附件 **ShanHai-v0.4.1-H5.zip**，完整解压后双击 **开始H5游戏.cmd**。需要 Python 3 和支持 WebGL 2 的浏览器；游玩不需要安装 Unity 或团结编辑器。GitHub 自动生成的 Source code 压缩包是工程源码，游玩请使用上述 H5 附件。

正式导出目录为 `Builds/H5`，独立交付包为项目旁的 **山海神隐录_H5游戏.zip**。完整解压该包后，双击 **开始H5游戏.cmd**，打开 <http://127.0.0.1:8766/>。启动器需要 Python 3（本机已安装），游玩时保留服务窗口。请通过 HTTP 访问，不要直接双击 HTML。

源码压缩包不包含构建缓存和导出文件；可使用单独的 H5 游戏包，或按下文重新构建。实际验证结果见 `Documentation/DELIVERY.md`。

启动器会核对当前地址的页面及完整 H5 构建文件。同一版本可复用已有服务；如果旧游戏仍在运行，会提示先关闭旧服务窗口，再打开新版包。沿用 `http://127.0.0.1:8766/` 可继续访问该地址下的浏览器存档。

## 全场景完善版（0.4.1）

0.4.1 修复了拼图拖动被横竖屏切换、菜单或失焦打断后仍可能交换碎片的问题。构建脚本也会拦截资源归档读取失败的异常导出，正式包仍须通过真实浏览器检查。

**开始试玩.cmd** 与 **开始H5游戏.cmd** 都启动正式 H5。八处调查现在都在完整场景中操作：扶桑转镜折光、钟山提灯融冰与石眼、幽都名灯、丹穴清灰与联动风闸、凶犁河碑清沙与石渠、忘川舟船配重、桑叶三重门、不周双卷修桥。壁画、河碑、门与出口都属于对应环境；拾取器物后直接点击或擦拭场景实物，解开后出现开门、流沙散去、凤凰显现、舟身平稳或残阶修复等变化。

描字、拓印、装裱、记忆拼图与双卷配对使用生成的工作台、纸张、实物框与卷轴。图鉴中的烛、羽、桑叶和古卷各自使用物品图，三种结局各有独立场景。27 张新增场景、状态图和图集的完整生成提示词与文件位置记录在 `ArtSource/full-game-prompts.json`。

右上角“线索手记”可重读已发现的线索，“一点提示”分三层提供方向、方法和具体解法；需要时会短暂提示当前可操作位置。解开机关后点击实际出口继续。原先确认的剧情分支、明昧与碎片奖励规则保留。

旧存档可继续使用；已经完成的调查不会强制重做。体验新版可从第一章章节快照回放，或开始新旅程。更新后关闭旧页面再打开正式 H5，必要时 Ctrl+F5 刷新；不要清理站点数据，以免删除存档。

`Preview/` 保留早期 JavaScript 流程原型，未同步本轮机关玩法，不能用于验收当前游戏。仅调试旧原型时使用：

```powershell
python Tools/serve.py --directory . --port 8765
```

打开 <http://127.0.0.1:8765/Preview/>。预览服务器仅监听本机。浏览器需要支持 JavaScript 模块与 Canvas；通过 HTTP 访问，不要直接双击 HTML。

双击入口需要本机 Python 3（当前电脑已安装）。启动后保留服务窗口；再次双击会复用已运行的预览服务。

点击对白继续；空格可以推进对白。书写按顺序描笔画，拓印用按住擦拭，拼图可拖动或点击两块交换，双卷映照需完成四组对应。竖屏时会提示旋转设备。

本地保存一个自动存档与六个手动存档，保存当前对白位置、探索进度、已完成笔画、拓印覆盖、拼图与双卷进度。新游戏不清空手动存档、图鉴、结局和章节快照。浏览器清理站点数据会删除这些记录。浏览器预览和 Unity WebGL 的存储空间彼此独立。

## 团结引擎工程

双击 **打开团结工程.cmd**，或用团结 Hub 添加此目录。当前本机编辑器与项目版本为团结引擎 **1.10.4 / 2022.3.62t16**，安装在 `F:/Unity/Tuanjie/1.10.4`。工程已由真实编辑器完成导入。实际构建及验证状态见 `Documentation/DELIVERY.md`。

迁移前的标准 Unity 工程已在本机归档到 `F:/Unity/Backups/山海神隐录_Unity迁移前_20261016.zip`。当前目录包含团结引擎生成的资源元数据，请使用团结引擎打开；如需回到标准 Unity，应从该备份恢复独立副本。

首次打开后，在菜单选择 **山海神隐录 → 准备场景与项目设置**，打开 `Assets/Scenes/Story.unity`，点击 Play。启动场景已挂载 `StoryGame`，UI 由 C# 创建，不依赖手工拖拽组件。`ProjectBuilder.Setup` 会准备场景与 WebGL 设置。

也可以在 Tuanjie Cowork 的 **Open Game Project** 中选择此 `ShanHai` 目录。Cowork 负责项目管理与 AI 辅助，运行和构建使用上述已安装并授权的团结编辑器。

正式 H5 构建使用已激活的团结编辑器和对应版本的 WebGL Build Support，本机已完成安装。可使用菜单 **山海神隐录 → 构建 H5 WebGL**，或：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/build.ps1 -UnityPath "F:/Unity/Tuanjie/1.10.4/Editor/Tuanjie.exe"
python Tools/serve.py --directory Builds/H5 --port 8766
```

生成目录为 `Builds/H5`。WebGL 模板包含加载进度、错误反馈、全屏与移动设备画布适配；gzip 使用 Unity 解压回退，便于普通静态服务器部署。微信与抖音小游戏 SDK 尚未接入，本版以 H5 为首发目标。

命令行构建默认限制为两个原生编译任务，可通过 `-BuildJobs 1` 进一步降低内存压力，缓存损坏时加 `-CleanBuild`。项目内的辅助程序为 Bee 添加 `-j` 参数，不修改已安装的编辑器文件；在团结 1.10.4 上验证。引擎升级后需重新验证此辅助配置，编辑器菜单构建不启用该限流。

## 修改剧情

原始 Excel 和 ZIP 未修改。`Tools/import_story.py` 只读取工作簿，并把快照写入 `Source/workbook_snapshot.json`。

没有原始工作簿的独立源码包会使用随包快照重新生成剧情；指定工作簿路径时则读取该文件。

```powershell
python Tools/import_story.py "../山海神隐录_Unity分支剧情填写模板.xlsx"
```

`Source/content.json` 保存对白、互动内容与已确认的额外选项。转换器读取幕节点、剧情链、变量定义，并合并这份演出数据。新增幕时需要在此补齐对白和互动配置；转换器会检查缺失幕、断链与不可达节点，不会静默跳过。

2026-10-16 已确认的规则：

- N102 → N103 → N104；玄曜第一章三个节点各增加明昧 8。
- N303 不额外增加明昧；相柳选择已在 N304/N305 结算 ±8。
- N402 只增加一个记忆碎片，不减少明昧。
- 双卷拼图链的门槛为碎片 ≥2。
- 终局三选项分别为明昧 +20、−20、向 50 靠近最多 20；所有数值限制在 0–100。
- 最终值 ≥60 为永明，≤40 为共生，其余为调和。
- N203/N503 的叙事选项保留不同回应，按原表均增加一次结契纹，不额外改变明昧。
- 灵曦线烛火初始为 0，N502 的 −1 按原表下限截断为 0。

状态在幕完成时结算，每个节点每份旅程只结算一次。读档不会重复加分。结局在玩家读完结局并收卷时登记。章节选择恢复首次到达该章节的真实快照，而不是凭空重置数值。

## 测试

```powershell
powershell -ExecutionPolicy Bypass -File Tools/test_core.ps1
node Tests/preview-tests.mjs --engine-only
```

C# 核心测试不需要 Unity。浏览器自动操作测试使用环境中已有的 Playwright 与 Chrome：`node Tests/preview-tests.mjs`。本机依赖路径在测试脚本顶部，可以按其他机器的安装位置调整。

引擎正式导出的 WebGL 包通过另一个脚本验证：先在 8766 端口提供 `Builds/H5`，再运行 `node Tests/unity-webgl-smoke.mjs`。该脚本检查真实加载、声音解锁和全屏，并记录桌面与手机尺寸下的操作截图；运行结果位于 `Builds/QA/Unity`，不能用浏览器预览的测试结果代替。

`node Tests/unity-playthrough.mjs` 通过真实鼠标操作走两条主线，并从章节快照回跳验证第三结局；覆盖八处调查与五类工作台互动，并逐页检查已解锁图鉴和结局册。测试只读取 IndexedDB 核对进度，不注入或修改游戏状态。记录位于 `Builds/QA/Unity/Playthrough`。设置 `$env:SHANHAI_TOUCH='1'` 后运行同一脚本，以 844×390 模拟触摸走完相同流程；记录位于 `Builds/QA/Unity/TouchPlaythrough`。

`node Tests/unity-trace-scaling.mjs` 在 1894×1140、1280×720、1920×900、844×390 四种窗口下实际绘制十二笔，另查越界取消。随后运行 `python Tools/check_trace_pixels.py` 检查截图中的朱墨是否逸出纸面。打包必须通过这两项新增检查。

`node Tests/unity-danxue-scene.mjs` 从实际剧情进入丹穴，检查鼠标与手机尺寸模拟触摸清灰、无效扫动、刷新续玩、风闸错误状态恢复、凤凰出现及石阶出口。记录与实际游戏截图在 `Builds/QA/Unity/Danxue`。新增三张生成美术的完整提示词在 `ArtSource/danxue-prompts.json`。

## 完整游玩 Demo

项目旁的 `山海神隐录_完整游玩Demo_0.4.1.mp4` 为正式 H5 的实际操作录制，包含玄曜主线、灵曦主线与章节回放的第三结局。`山海神隐录_Demo章节_0.4.1.txt` 提供时间表，`山海神隐录_Demo播放_0.4.1.html` 可本地播放和点击章节跳转。视频保留游戏配乐与音效，含结局剧透。

录制工具通过实际鼠标操作游戏，仅只读观察存档，不注入剧情或进度。录制入口为 `$env:SHANHAI_DEMO='1'; node Tests/unity-playthrough.mjs`；完成后运行 `python Tools/export_demo.py` 编码 MP4 并生成章节。录制需要 Playwright、Chrome；导出使用本机现有 FFmpeg、ffprobe 与 PyAV。依赖路径可按其他电脑环境调整，普通玩家直接打开 MP4 即可。

当前录像由四段实际操作素材合并，没有加速：玄曜主线、灵曦主线、共生结局收卷与图鉴补录、第三结局回放。灵曦结局对白之后录制浏览器异常，补录由重新实际游玩获得；原始失败日志仍保留。两条主线覆盖 34 个幕节点，另两个可选分支由核心路径检查覆盖，视频不包含每一种选择排列。

本机硬件编码器在长视频导出时不稳定，本次使用软件编码。可设置 `$env:SHANHAI_CPU_ENCODE='1'` 后运行导出。重新录制普通三段素材时，导出器也支持原始玄曜、灵曦、调和三段，不依赖本次补录文件。

`node Tests/unity-interruption.mjs` 检查拼图拖动中切换竖屏、返回横屏及打开菜单的中断行为，并确认之后新拖动可正常操作。

`python Tests/test_start_preview.py` 使用实际本地 HTTP 服务检查启动器：相同包复用原地址、同标题旧版本被阻止、同版本但美术数据不同被阻止、不完整的本地包不会误打开另一个副本。

## 文件位置

- `Assets/Scripts/Core`：引擎无关的剧情状态机、序列化模型和互动判定。
- `Assets/Scripts/Runtime`：Unity 运行界面、互动、音频与本地存档。
- `Assets/Editor`：场景生成、构建、资源导入设置与资源检查。
- `Assets/Resources`：运行时剧情、新生成的美术、原创音频与字体。
- `Preview`：独立浏览器预览运行器，与 Unity 引擎分别实现和验证。
- `Source`：原表快照与补全的对白演出。
- `ArtSource/generation-prompts.json`：内置 image_gen 美术生成提示词。
- `ArtSource/investigation-prompts.json`：早期道具图集与独立神兽拓片的生成提示词。
- `ArtSource/full-game-prompts.json`：0.4.0 全场景、机关图集、工作台、图鉴物品与结局画面的生成提示词。
- `Tools/make_audio.py`：原创合成音频的可复现源程序。

美术没有使用提供的“开发素材.zip”。字体为 Noto Serif CJK SC，遵循随附的 SIL Open Font License；其他场景与角色由内置 image_gen 生成，音乐和音效由程序合成。本版未制作人物语音配音。
