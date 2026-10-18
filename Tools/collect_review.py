"""Collect unmodified screenshots from the current, manually reviewed H5 export."""
from pathlib import Path
import json,shutil,html,argparse
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--build-dir',default='Builds/H5');args=parser.parse_args()
review=root/'Documentation/review';review.mkdir(exist_ok=True)
base=root/'Builds/QA/Unity'
shots=[
 ('Playthrough/N101-scene-changed.png','扶桑：转镜后金乌显现'),
 ('Playthrough/N201-scene-changed.png','钟山：冰碑与石眼开门'),
 ('Playthrough/N301-scene-changed.png','幽都：四盏名灯与城门'),
 ('Playthrough/N401-phoenix-awake.png','丹穴：风闸与凤凰'),
 ('Playthrough/N403-scene-changed.png','凶犁：河碑、河纹石渠与通路'),
 ('Playthrough/N501-scene-changed.png','忘川：实物舟船与配重'),
 ('Playthrough/N601-scene-changed.png','幽都：桑叶机关开启'),
 ('Playthrough/N801-scene-changed.png','不周：双卷修复断桥'),
 ('TraceScaling/stroke-11-held.png','手机尺寸：描字笔画在纸内'),
 ('Playthrough/N402-rubbing-partial.png','拓印：纸纹与柔和显影'),
 ('Playthrough/N104-mount-before.png','装裱：金乌拓片与实物框'),
 ('Playthrough/N603-puzzle-before.png','拼图：两人的记忆画面'),
 ('Playthrough/N702-mirror-before.png','配对：双卷纸面操作'),
 ('Playthrough/玄曜-N911-final-scene.png','结局：永明卷'),
 ('Playthrough/灵曦-N912-final-scene.png','结局：共生卷'),
 ('Playthrough/玄曜-N913-final-scene.png','结局：调和卷'),
 ('Playthrough/N603-deduction.png','记忆：证据选择与推理'),
 ('Playthrough/N702-deduction.png','双卷：结合人物遭遇推演'),
 ('TouchPlaythrough/N104-mount-resumed.png','手机：装裱观察与旋转进度恢复'),
 ('Quality/844-title.png','手机：大按钮标题页'),
 ('Quality/844-large-text.png','手机：字号设置后的对白'),
 ('Quality/844-settings.png','手机：设置面板'),
 ('ArtRetry/load-failure.png','断网：卷页加载失败可重试'),
 ('codex-review.png','17 条图鉴：实际游戏图像核对'),
]
newest=max(p.stat().st_mtime for p in (root/args.build_dir/'Build').iterdir() if p.is_file())
files=[];cards=[]
for source,title in shots:
    src=base/source;assert src.stat().st_mtime>=newest,'Stale screenshot: '+source
    dest=review/src.name;shutil.copy2(src,dest)
    files.append(str(dest.relative_to(root)).replace('\\','/'))
    cards.append('<figure><figcaption>'+html.escape(title)+'</figcaption><a href="'+html.escape(dest.name)+'"><img loading="lazy" src="'+html.escape(dest.name)+'"></a></figure>')
(review/'index.html').write_text('<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>山海神隐录 0.5.0 实际画面</title><style>body{margin:0;background:#101e22;color:#e7d8ba;font:18px/1.6 system-ui}main{max-width:1440px;margin:auto;padding:28px}h1{font-weight:500}p{color:#acb9b2}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(330px,1fr));gap:24px}figure{margin:0;background:#182b2c;padding:14px}img{width:100%;height:auto;display:block;margin-top:10px}a{color:inherit}</style><main><h1>山海神隐录 · 0.5.0</h1><p>正式 H5 的实际运行截图。点击可查看原图。包括结局画面，含剧情剧透。</p><section>'+''.join(cards)+'</section></main></html>',encoding='utf-8')
data={'version':'0.5.0','status':'passed','method':'Visual inspection of actual H5 screenshots, including art registration, glyphs, layer clipping, physical object identity, progress feedback and scaled input. Codex contact sheet uses cropped actual screenshots, not mockups.','screenshots':files,'source':'Builds/QA/Unity','coverage':{'investigations':8,'workshops':5,'codexEntries':17,'endingScenes':3},'findingsResolved':['Ending CGs no longer receive duplicate foreground actors; collection and replay remain accessible','Compact title, toolbar and persistent text-size setting','Fractional-scale portrait seams fixed by drawing the body once and feathering only pixel-aligned outer edges','Bridge perspective and leaf-gate ambient shading adjusted','Mount orientation and seal, randomized memory evidence, and mirror reasoning verified','Lantern pickup no longer overlaps the ice inscription hit area','Paper stays inside the mounting frame','Workshop header contrast increased','Rubbing mask preserves generated paper grain and blends reveal edges','World clues and mechanism panels replaced throughout the eight investigations']}
(root/'Documentation/visual-review.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Collected',len(files),'fresh review screenshots.')
