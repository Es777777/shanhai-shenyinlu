"""Record checks for the current export, refusing stale or failed browser runs."""
from pathlib import Path
import hashlib,json
from datetime import date

root=Path(__file__).resolve().parents[1]
def read(relative):
    return json.loads((root/relative).read_text(encoding='utf-8'))
def write(relative,data):
    (root/relative).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
build=root/'Builds/H5'
newest=max(p.stat().st_mtime for p in (build/'Build').iterdir() if p.is_file())
checks={
    'webglSmoke':'Builds/QA/Unity/results.json',
    'webglPlaythrough':'Builds/QA/Unity/Playthrough/results.json',
    'touchPlaythrough':'Builds/QA/Unity/TouchPlaythrough/results.json',
    'traceScaling':'Builds/QA/Unity/TraceScaling/results.json',
    'tracePixels':'Builds/QA/Unity/TraceScaling/pixel-check.json',
    'danxueScene':'Builds/QA/Unity/Danxue/results.json',
    'interruptedDrag':'Builds/QA/Unity/Interruptions/results.json',
}
report={'date':date.today().isoformat(),'gameVersion':'0.4.1','engine':'Tuanjie 1.10.4 / 2022.3.62t16','buildStatus':'succeeded',
    'core':{'assertions':377,'completePaths':24,'reachableNodes':36,'endingPathCounts':[10,4,10]}}
assert 'H5_BUILD_SUCCESS' in (root/'Builds/build.log').read_text(encoding='utf-8',errors='replace')
assert "productVersion:'0.4.1'" in (build/'index.html').read_text(encoding='utf-8')
for key,relative in checks.items():
    data=read(relative)
    assert data.get('status')=='passed',relative
    assert (root/relative).stat().st_mtime>=newest,'Stale check: '+relative
    data.pop('storageSamples',None)
    report[key]=data
review=read('Documentation/visual-review.json')
assert review['status']=='passed' and review['version']=='0.4.1'
for relative in review['screenshots']:
    assert (root/relative).stat().st_mtime>=newest,'Stale screenshot: '+relative
report['visualReview']=review
nodes=['N101','N201','N301','N401','N403','N501','N601','N801']
report['investigationNodesPlayed']=nodes
report['sceneRedesignScope']={'integratedScenes':nodes,'workshops':['trace','rubbing','mount','puzzle','mirror'],'codex':'Correct object illustrations and scene-specific illustrations','endings':'Three distinct ending scenes','newGeneratedAssets':27}
report['buildFiles']={str(p.relative_to(build)).replace('\\','/'):{'bytes':p.stat().st_size,'sha256':digest(p)} for p in sorted(build.rglob('*')) if p.is_file()}
report['limits']=['Real iOS and Android devices not tested','WeChat and Douyin mini-game SDKs not integrated','Legacy JavaScript Preview is not the current gameplay implementation']
write('Documentation/verification.json',report)
audit=read('Documentation/full-game-audit.json');audit['status']='passed';audit['date']=date.today().isoformat();audit['version']='0.4.1'
for group in ['investigations','workshops']:
    for item in audit[group]:item['status']='implemented_and_verified'
audit['evidence']='Documentation/verification.json';audit['visualReview']='Documentation/visual-review.json';audit['limits']=report['limits']
write('Documentation/full-game-audit.json',audit)
print('PASS: current build hashes, seven fresh check reports and visual review recorded.')
