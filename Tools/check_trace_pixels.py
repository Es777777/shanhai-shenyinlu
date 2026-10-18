"""Check the actual WebGL screenshots: vermilion ink stays inside writing paper."""
from pathlib import Path
import sys, json
from PIL import Image

directory=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'Builds/QA/Unity/TraceScaling'
reports=[]
for path in sorted(directory.glob('stroke-*-held.png')):
    im=Image.open(path).convert('RGB')
    before=Image.open(path.with_name(path.name.replace('-held','-before'))).convert('RGB')
    assert im.size==before.size
    w,h=im.size;s=min(w/1600,h/900);ox=(w-1600*s)/2;oy=(h-900*s)/2
    count=outside=static_red=0
    for py in range(max(0,int(oy+132*s)),min(h,int(oy+812*s))):
        for px in range(max(0,int(ox+180*s)),min(w,int(ox+1420*s))):
            r,g,b=im.getpixel((px,py))
            if abs(r-194)>6 or abs(g-79)>6 or abs(b-61)>6:continue
            count+=1;x=(px-ox)/s;y=(py-oy)/s
            if not (318<=y<=702 and (348<=x<=732 or 823<=x<=1207)):
                # The painted worktable contains a red seal. Exclude only unchanged
                # source pixels, never newly drawn ink outside the two paper grids.
                if all(abs(a-b)<=2 for a,b in zip((r,g,b),before.getpixel((px,py)))):static_red+=1
                else:outside+=1
    reports.append({'file':path.name,'inkPixels':count-static_red,'outsidePaper':outside,'unchangedArtPixels':static_red})
    assert count>10,(path,'No visible ink, screenshot cannot validate rendering')
    assert outside<5,(path,'Ink escaped paper',outside)
assert len(reports)==12,('All 12 held strokes must be inspected',len(reports))
(directory/'pixel-check.json').write_text(json.dumps({'status':'passed','checks':reports},indent=2),encoding='utf-8')
print('PASS: all 12 rendered strokes stay within the paper across four viewport sizes.')
