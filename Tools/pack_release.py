"""Package source and playable preview, excluding installers and local caches."""
from pathlib import Path
import argparse
import json
import zipfile
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--h5',action='store_true',help='Also package a successfully built and tested H5 export')
args=parser.parse_args()
dest=ROOT.parent/'山海神隐录_工程与试玩.zip'
excluded={'Library','Temp','Obj','Logs','UserSettings','.git','.downloads','.deps','__pycache__','Builds'}
with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in ROOT.rglob('*'):
        if not p.is_file() or any(part in excluded for part in p.relative_to(ROOT).parts):continue
        if p.suffix in ['.csproj','.sln','.user']:continue
        z.write(p,Path('ShanHai')/p.relative_to(ROOT))
print(dest,round(dest.stat().st_size/1048576,1),'MiB')
if args.h5:
    build=ROOT/'Builds/H5'
    log=ROOT/'Builds/build.log'
    qa=ROOT/'Builds/QA/Unity/results.json'
    if not log.exists() or 'H5_BUILD_SUCCESS' not in log.read_text(encoding='utf-8',errors='replace'):
        raise SystemExit('No successful engine build recorded; refusing to package an incomplete export.')
    if not qa.exists() or json.loads(qa.read_text(encoding='utf-8')).get('status')!='passed':
        raise SystemExit('Real WebGL browser checks must pass before packaging.')
    playthrough=ROOT/'Builds/QA/Unity/Playthrough/results.json'
    if not playthrough.exists() or json.loads(playthrough.read_text(encoding='utf-8')).get('status')!='passed':
        raise SystemExit('Real WebGL playthrough checks must pass before packaging.')
    scaling=ROOT/'Builds/QA/Unity/TraceScaling/results.json'
    pixels=ROOT/'Builds/QA/Unity/TraceScaling/pixel-check.json'
    danxue=ROOT/'Builds/QA/Unity/Danxue/results.json'
    touch=ROOT/'Builds/QA/Unity/TouchPlaythrough/results.json'
    interruption=ROOT/'Builds/QA/Unity/Interruptions/results.json'
    for check in [scaling,pixels,danxue,touch,interruption]:
        if not check.exists() or json.loads(check.read_text(encoding='utf-8')).get('status')!='passed':
            raise SystemExit('Scaled calligraphy, pixels, Danxue and full touch routes must pass before packaging.')
    newest_build=max(p.stat().st_mtime for p in (build/'Build').rglob('*') if p.is_file())
    if min(qa.stat().st_mtime,playthrough.stat().st_mtime,scaling.stat().st_mtime,pixels.stat().st_mtime,danxue.stat().st_mtime,touch.stat().st_mtime,interruption.stat().st_mtime)<newest_build:
        raise SystemExit('The export changed after verification. Run browser checks again.')
    dest=ROOT.parent/'山海神隐录_H5游戏.zip'
    with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for p in build.rglob('*'):
            if p.is_file():z.write(p,p.relative_to(build))
        for name in ['serve.py','start_preview.py']:
            z.write(ROOT/'Tools'/name,Path('Tools')/name)
        z.write(ROOT/'开始H5游戏.cmd','开始H5游戏.cmd')
        z.write(ROOT/'Assets/Resources/Fonts/OFL.txt','字体许可_OFL.txt')
        z.writestr('启动与部署说明.txt',
            '山海神隐录 — 团结引擎正式 WebGL 导出\n\n'
            '本机试玩：完整解压，双击“开始H5游戏.cmd”（需要 Python 3），保留服务窗口。\n'
            '地址：http://127.0.0.1:8766/ 。请勿直接双击 index.html。\n'
            '若提示同一地址正在运行其他版本，请先关闭旧游戏的服务窗口，再启动本包；沿用原地址可继续访问原浏览器存档。\n'
            '部署：把 index.html、Build、字体许可_OFL.txt、StreamingAssets（如有）按原结构上传静态网站空间。\n'
            '.unityweb 文件使用引擎解压回退，不要额外设置 Content-Encoding: gzip。\n'
            '存档保存在当前浏览器、当前站点本地；清理网站数据会删除存档。\n'
            '横屏游玩。H5 版未接入微信/抖音小游戏 SDK，不含账号、广告、支付或云存档。\n')
    print(dest,round(dest.stat().st_size/1048576,1),'MiB')
