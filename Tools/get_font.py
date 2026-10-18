from pathlib import Path
import requests
ROOT=Path(__file__).resolve().parents[1]
base='https://raw.githubusercontent.com/notofonts/noto-cjk/main/'
for url, path in [(base+'Serif/OTF/SimplifiedChinese/NotoSerifCJKsc-Regular.otf', ROOT/'Assets/Resources/Fonts/StorySerif.otf'),(base+'Serif/LICENSE',ROOT/'Assets/Resources/Fonts/OFL.txt')]:
    if path.exists(): continue
    r=requests.get(url,timeout=90); r.raise_for_status(); path.write_bytes(r.content)
    print(path.name,len(r.content),flush=True)
