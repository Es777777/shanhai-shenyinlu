"""Contact sheets made from decoded movie frames for human visual review."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/'Builds/Demo'
report=json.loads((WORK/'video-verification.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
for start in range(0,len(report['chapters']),12):
    batch=report['chapters'][start:start+12]
    sheet=Image.new('RGB',(1920,392*((len(batch)+2)//3)),(10,25,29))
    draw=ImageDraw.Draw(sheet)
    for j,chapter in enumerate(batch):
        index=start+j;x=j%3*640;y=j//3*392
        with Image.open(WORK/'review'/f'{index:02d}.png') as source:
            sheet.paste(source.convert('RGB').resize((640,360),Image.Resampling.LANCZOS),(x,y+32))
        seconds=int(chapter['seconds']);caption=f'{index:02d} | {seconds//60:02d}:{seconds%60:02d} | '+chapter['title']
        draw.text((x+8,y+3),caption,font=font,fill=(232,221,198))
    dest=WORK/'review'/f'contact-{start//12+1}.jpg'
    sheet.save(dest,quality=92)
    print(dest)
