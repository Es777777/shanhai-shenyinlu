import {observeArt,waitArtwork} from './unity-art-loading.mjs';
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {readSavedData,waitSaved} from './unity-storage.mjs';
import {solveInvestigation} from './unity-investigations.mjs';
import {gamePoint} from './unity-danxue-input.mjs';
import {installCapture,startCapture,stopCapture} from '../Tools/demo_capture.mjs';

const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const require=createRequire(process.env.PLAYWRIGHT_LOADER||'C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browserOptions={headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader','--disable-background-timer-throttling','--disable-renderer-backgrounding']};
let browser=await chromium.launch(browserOptions);
const touch=process.env.SHANHAI_TOUCH==='1';
const tail=process.env.SHANHAI_DEMO_TAIL==='1';
const recordingRun=process.env.SHANHAI_DEMO==='1'||tail;
let demo=recordingRun&&!tail;
const out=path.join(root,recordingRun?'Builds/Demo':'Builds/QA/Unity/'+(touch?'TouchPlaythrough':'Playthrough'));await fs.mkdir(out,{recursive:true});
const resume=recordingRun&&(process.env.SHANHAI_DEMO_RESUME==='1'||tail);
const report=resume?JSON.parse(await fs.readFile(path.join(out,'completed-before-restart.json'),'utf8')):{routes:[],errors:[]};
report.note='Two actual WebGL routes using '+(touch?'touch at 844x390':'mouse at 1600x900')+'. Saves observed read-only; no state injection.';
if(resume){
  assert(['xuanyao','balanced'].every(key=>report.segments.some(s=>s.key===key&&s.bytes>0)),'Resume requires both completed first-route recordings');
  report.routes=report.routes.filter(r=>r.route==='玄曜');report.errors=[];
  report.segments=report.segments.filter(s=>s.key!=='lingxi');report.bookmarks=report.bookmarks.filter(m=>m.segment!=='lingxi');
  report.resumedWithFreshBrowser=true;delete report.status;delete report.failure;
}else{report.segments=[];report.bookmarks=[];}
let page,cdp,segment='';
async function mark(title,node=''){
  if(!demo)return;const seconds=await page.evaluate(()=>window.__captureElapsed());
  report.bookmarks.push({segment,title,node,seconds});
  await fs.writeFile(path.join(out,'progress.json'),JSON.stringify(report,null,2));
}
async function beginSegment(key,title){segment=key;await startCapture(page,path.join(out,key+'.webm'));await mark(title);}
async function endSegment(){if(!demo)return;report.segments.push({key:segment,...await stopCapture(page)});}
async function click(x,y){await waitArtwork(page);const p=gamePoint(page,x,y);if(demo)await page.mouse.move(...p,{steps:8});if(touch)await page.touchscreen.tap(...p);else await page.mouse.click(...p,{delay:demo?80:50});await page.waitForTimeout(demo?650:180);await waitArtwork(page);}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png')});if(demo)await page.waitForTimeout(name.includes('notebook')?5500:name.includes('before')?2500:name.includes('scene-changed')?2200:600);}
async function drag(points){
  async function move(type,x,y){
    const [px,py]=gamePoint(page,x,y);
    if(touch)await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:px,y:py,id:1}]});
    else if(type==='touchStart'){await page.mouse.move(px,py);await page.mouse.down();}
    else if(type==='touchEnd')await page.mouse.up();else await page.mouse.move(px,py);
  }
  await move('touchStart',...points[0]);await page.waitForTimeout(35);
  for(let i=1;i<points.length;i++){
    const a=points[i-1],b=points[i],steps=Math.max(4,Math.ceil(Math.hypot(b[0]-a[0],b[1]-a[1])/16));
    for(let j=1;j<=steps;j++){await move('touchMove',a[0]+(b[0]-a[0])*j/steps,a[1]+(b[1]-a[1])*j/steps);await page.waitForTimeout(22);}
  }
  await move('touchEnd',...points.at(-1));await page.waitForTimeout(demo?600:160);
}
const brush=[
  [[.50,.13],[.34,.30],[.16,.39]],[[.50,.13],[.66,.29],[.85,.38]],[[.32,.37],[.68,.37]],[[.23,.52],[.77,.52]],
  [[.50,.38],[.50,.84]],[[.28,.63],[.36,.74]],[[.73,.62],[.63,.75]],[[.17,.85],[.83,.85]],
  [[.48,.13],[.37,.28]],[[.35,.29],[.69,.29],[.68,.53],[.59,.57]],[[.31,.29],[.29,.66],[.77,.66],[.73,.85],[.61,.87]],[[.15,.77],[.61,.77]]
];
async function mechanic(node){
  await shot(node.id+'-'+node.mechanic+'-before');
  if(node.mechanic==='explore'||node.mechanic==='climb'){
    await solveInvestigation(page,node.id,click,shot,{reload:!demo&&node.id==='N101',touch,demo});
  }else if(node.mechanic==='trace'){
    for(let i=0;i<brush.length;i++){
      await drag(brush[i].map(([x,y])=>[(i<8?350:825)+x*380,320+y*380]));
      await waitSaved(page,s=>s.miniStep===i+1);
    }
  }else if(node.mechanic==='mount'){
    // Wrong orientation and seal must remain recoverable without awarding progress.
    await drag([[500,490],[1080,490]]);assert.equal((await readSavedData(page)).miniStep,0);
    await click(390,660);await click(630,660);
    if(!demo){
      const before=await readSavedData(page);
      await page.reload();await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(4000);
      await click(310,page.viewportSize().height<650?465:614);
      const after=await waitSaved(page,s=>s.nodeId==='N104'&&s.mountingRotation===1);
      assert.deepEqual(after.deductions,before.deductions);
      await shot('N104-mount-resumed');
    }
    await click(630,660);
    await drag([[500,490],[1080,490]]);await waitSaved(page,s=>s.miniStep===1);
    await click(1260,660);await click(1180,745);assert.equal((await readSavedData(page)).mechanicDone,false);
    await click(930,660);await click(1180,745);
  }else if(node.mechanic==='rubbing'){
    const points=[];
    for(let row=0;row<8;row++){
      const xs=Array.from({length:16},(_,i)=>365+i*56);if(row%2)xs.reverse();
      for(const x of xs)points.push([x,332+row*48]);
    }
    await drag(points.slice(0,64));await shot(node.id+'-rubbing-partial');
    await drag(points.slice(64));await waitSaved(page,s=>s.rubbed.length>=144);
    await click(1330,505);
  }else if(node.mechanic==='puzzle'){
    let s=await waitSaved(page,s=>s.puzzle.length===6);
    for(let target=0;target<6;target++){
      const from=s.puzzle.indexOf(target);if(from===target)continue;
      await click(460+(from%3)*280,407+Math.floor(from/3)*195);
      await click(460+(target%3)*280,407+Math.floor(target/3)*195);
      s=await waitSaved(page,s=>s.puzzle[target]===target);
    }
    await shot(node.id+'-deduction');
    await click(800,690);assert.equal((await readSavedData(page)).mechanicDone,false);
    await click(1080,345);await click(1080,427);await click(1080,590);
    await click(800,690);assert.equal((await readSavedData(page)).mechanicDone,false);
    await click(1080,590);await click(800,690);
  }else if(node.mechanic==='mirror'){
    for(const [left,right] of [2,0,3,1].entries()){
      await click(500,383+left*87);await click(1100,383+right*87);
      await waitSaved(page,s=>s.matched.includes(left));
    }
    await shot(node.id+'-deduction');
    await click(400,690);assert.equal((await readSavedData(page)).mechanicDone,false);
    await click(800,690);
  }else throw new Error('Unknown mechanic '+node.mechanic);
  const completed=await waitSaved(page,s=>s.mechanicDone);
  assert(completed.completed.includes(node.id),'Mechanic reward must apply immediately');
  await shot(node.id+'-'+node.mechanic+'-complete');
}

try{
  for(const route of [0,1]){
    if(resume&&route===0)continue;
    const context=await browser.newContext({viewport:touch?{width:844,height:390}:{width:1600,height:900},hasTouch:touch,deviceScaleFactor:1});
    if(recordingRun)await installCapture(context);
    page=await context.newPage();observeArt(page);
    page.on('crash',()=>{report.errors.push('Browser renderer crashed');void context.browser().close();});
    cdp=await context.newCDPSession(page);
    page.on('pageerror',e=>report.errors.push(e.message));
    page.on('console',m=>{if(m.type()==='error'||/Uncaught|Exception:|Aborted\(|Failed to initialize player|No GlobalGameManagers/.test(m.text()))report.errors.push(m.text());});
    await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');
    await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});
    await page.waitForTimeout(4000);
    if(demo){await page.mouse.click(780,400);await beginSegment(route===0?'xuanyao':'lingxi',route===0?'玄曜主线 · 主菜单':'灵曦主线 · 主菜单');await page.waitForTimeout(3000);}
    await click(310,page.viewportSize().height<650?345:535);
    const run={route:route===0?'玄曜':'灵曦',nodes:[],mechanics:[],endings:[]};report.routes.push(run);
    let replayBalanced=false;
    let state=await waitSaved(page,s=>s.nodeId==='N001');
    const deadline=Date.now()+(demo?40:12)*60*1000;
    while(Date.now()<deadline){
      const node=story.nodes.find(n=>n.id===state.nodeId);assert(node);
      if(run.nodes.at(-1)!==node.id){run.nodes.push(node.id);console.log(run.route,node.id,node.mechanic||'dialogue');await mark(node.title,node.id);}
      if(state.line<node.lines.length){
        const previous=state.line;
        if(demo){await page.waitForTimeout(Math.max(3200,node.lines[state.line].text.length*160+900));await click(790,745);}
        else{await click(790,745);await click(790,745);}
        state=await waitSaved(page,s=>s.line>previous||s.nodeId!==node.id);continue;
      }
      if(node.mechanic&&!state.mechanicDone){
        await mechanic(node);run.mechanics.push(node.mechanic);
        state=await readSavedData(page);continue;
      }
      if(/^N91[123]$/.test(node.id)){
        if(tail&&!demo){demo=true;await beginSegment('lingxi-tail','共生结局 · 收卷与图鉴');await page.waitForTimeout(1000);}
        await shot(run.route+'-'+node.id+'-final-scene');
        await click(1350,776);await shot(run.route+'-'+node.id+'-ending');
        const ending=Number(node.id.at(-1));
        const profile=await waitSaved(page,p=>p.endings.includes(ending),'profile');
        run.endings.push(node.id);run.codexEntries=profile.unlocked.length;run.valuesBeforeEndingReward=state.values;
        assert.equal(node.id,replayBalanced?'N913':route===0?'N911':'N912');
        if(route===0&&!replayBalanced){
          if(demo){await page.waitForTimeout(3500);await endSegment();await beginSegment('balanced','调和结局 · 章节回放入口');await page.waitForTimeout(1500);}
          const chapter=profile.checkpoints.findIndex(c=>c.nodeId.endsWith(':N801'));
          assert(chapter>=0&&chapter<8);
          await click(1360,135);await click(1470,47);await click(1105,463);
          await click(190+(chapter%2)*590+282,280+Math.floor(chapter/2)*105+40);
          await click(980,535);
          state=await waitSaved(page,s=>s.nodeId==='N801');
          replayBalanced=true;run.chapterReplay='N801';continue;
        }
        break;
      }
      const count=node.choices.length||story.edges.filter(e=>e.from===node.id&&e.kind==='玩家选择').length;
      if(count){
        if(demo)await page.waitForTimeout(2400);
        const choice=node.id==='N803'&&replayBalanced?2:node.id==='N302'?1:node.id==='N002'||node.id==='N803'?route:0;
        await click(800,420-(100+count*90)*.3+83+choice*90+34);
      }else await click(1350,776);
      state=await waitSaved(page,s=>s.nodeId!==node.id);
    }
    assert(run.endings.length,'Playthrough must reach an ending');
    await click(1360,135);await click(1300,47);
    await mark('山海图鉴');
    const collected=await readSavedData(page,'profile');
    for(let index=0;index<story.codex.length;index++){
      if(index>0&&index%8===0)await click(670,724);
      const entry=story.codex[index];if(!collected.unlocked.includes(entry.id))continue;
      await click(190+(index%8)%2*292+136,278+Math.floor((index%8)/2)*97+38);
      await shot('codex-'+entry.id);
    }
    await click(1360,135);await click(1470,47);await click(1105,557);await shot(run.route+'-ending-collection');
    if(demo){await mark('结局收藏');await page.waitForTimeout(3500);await endSegment();}
    await context.close();
    if(demo&&route===0){await browser.close();browser=await chromium.launch(browserOptions);}
  }
  const covered=new Set(report.routes.flatMap(r=>r.mechanics));
  for(const name of ['explore','trace','mount','rubbing','puzzle','mirror','climb'])assert(covered.has(name),name+' must be played');
  assert.deepEqual([...new Set(report.routes.flatMap(r=>r.endings))].sort(),['N911','N912','N913']);
  assert.equal(report.errors.length,0,`Browser errors: ${[...new Set(report.errors)].slice(0,3).join('\n')}`);report.status='passed';console.log('PASS: both real WebGL routes and all seven mechanic types');
}catch(error){
  report.status='failed';report.failure=error.stack;
  if(page&&!page.isClosed()){
    await shot('failure').catch(()=>{});
    const prefs=await page.evaluate(()=>window.__qaPrefsBytes||[]).catch(()=>[]);
    await fs.writeFile(path.join(out,'playerprefs-diagnostic.bin'),Buffer.from(prefs));
  }
  throw error;
}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
