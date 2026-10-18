from pathlib import Path
from fontTools import subset
import shutil

ROOT=Path(__file__).resolve().parents[1]
font=ROOT/'Assets/Resources/Fonts/StorySerif.otf'
source=ROOT/'Tools/.downloads/NotoSerifCJKsc-Regular.otf'
if not source.exists():shutil.copyfile(font,source)
texts=''.join(chr(i) for i in range(32,127))
for directory in ['Assets/Scripts','Source','Preview']:
    for path in (ROOT/directory).rglob('*'):
        if path.suffix in ['.cs','.json','.js','.html']:texts+=path.read_text(encoding='utf-8')
options=subset.Options();options.name_IDs=['*'];options.name_legacy=True;options.name_languages=['*']
f=subset.load_font(str(source),options)
subsetter=subset.Subsetter(options=options);subsetter.populate(text=texts);subsetter.subset(f)
subset.save_font(f,str(font),options)
print('Font subset:',len(set(texts)),'characters;',font.stat().st_size,'bytes')
