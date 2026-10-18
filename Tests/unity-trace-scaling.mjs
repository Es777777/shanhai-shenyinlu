import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {readSavedData,waitSaved} from './unity-storage.mjs';

const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const baseline=process.argv.includes('--baseline');
const out=path.join(root,'Builds/QA/Unity',baseline?'TraceBaseline':'TraceScaling');await fs.mkdir(out,{recursive:true});
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const context=await browser.newContext({viewport:{width:1894,height:1140},deviceScaleFactor:1,hasTouch:true});
const page=await context.newPage(),report={errors:[],checks:[]};
const touchClient=await context.newCDPSession(page);
page.on('pageerror',e=>report.errors.push(e.message));
function pos(x,y){const v=page.viewportSize(),s=Math.min(v.width/1600,v.height/900);return [(v.width-1600*s)/2+x*s,(v.height-900*s)/2+y*s];}
async function click(x,y){await page.mouse.click(...pos(x,y),{delay:60});await page.waitForTimeout(160);}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png')});}
const brush=[[[.5,.13],[.34,.3],[.16,.39]],[[.5,.13],[.66,.29],[.85,.38]],[[.32,.37],[.68,.37]],[[.23,.52],[.77,.52]],[[.5,.38],[.5,.84]],[[.28,.63],[.36,.74]],[[.73,.62],[.63,.75]],[[.17,.85],[.83,.85]],[[.48,.13],[.37,.28]],[[.35,.29],[.69,.29],[.68,.53],[.59,.57]],[[.31,.29],[.29,.66],[.77,.66],[.73,.85],[.61,.87]],[[.15,.77],[.61,.77]]];
async function stroke(i){
 const points=brush[i].map(([x,y])=>[(i<8?350:825)+x*380,320+y*380]);
 const touch=!baseline&&i>=9;
 async function finger(type,point){const [x,y]=pos(...point);await touchClient.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x,y,id:0,radiusX:4,radiusY:4,force:1}]});}
 if(touch)await finger('touchStart',points[0]);else{await page.mouse.move(...pos(...points[0]));await page.mouse.down();}
 for(let j=1;j<points.length;j++){
  const a=points[j-1],b=points[j],steps=24;
  for(let k=1;k<=steps;k++){const p=[a[0]+(b[0]-a[0])*k/steps,a[1]+(b[1]-a[1])*k/steps];if(touch)await finger('touchMove',p);else await page.mouse.move(...pos(...p));await page.waitForTimeout(20);}
 }
 await shot('stroke-'+i+'-held');if(touch)await finger('touchEnd',points.at(-1));else await page.mouse.up();await page.waitForTimeout(250);
}
try{
 await page.goto('http://127.0.0.1:8766/');
 await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});
 await page.waitForTimeout(3000);await click(310,535);
 let state=await waitSaved(page,s=>s.nodeId==='N001');
 while(state.nodeId!=='N102'||state.line<story.nodes.find(n=>n.id==='N102').lines.length){
  const n=story.nodes.find(n=>n.id===state.nodeId);
  if(state.line<n.lines.length){await click(790,745);await click(790,745);state=await waitSaved(page,s=>s.line>state.line||s.nodeId!==n.id);}
  else if(n.mechanic==='explore'&&!state.mechanicDone){
   if(!baseline){const {solveInvestigation}=await import('./unity-investigations.mjs');await solveInvestigation(page,n.id,click);}
   else {for(const t of n.targets){await click(t.x*1600,t.y*900);await waitSaved(page,s=>s.inspected.includes(t.id));}await click(1330,782);}
   state=await waitSaved(page,s=>s.mechanicDone);
  }else{
   const count=n.choices.length||story.edges.filter(e=>e.from===n.id&&e.kind==='玩家选择').length;
   await click(count?800:1350,count?420-(100+count*90)*.3+117:776);
   state=await waitSaved(page,s=>s.nodeId!==n.id);
  }
 }
 await shot('1894x1140-before');
 if(baseline){await stroke(0);await shot('1894x1140-after');report.state=await readSavedData(page);report.status='captured';}
 else {
  // Leaving the writing box must cancel the stroke without drawing on the UI.
  await page.mouse.move(...pos(540,369.4));await page.mouse.down();await page.mouse.move(...pos(100,130),{steps:12});await shot('outside-cancelled');await page.mouse.up();
  assert.equal((await readSavedData(page)).miniStep,0);
  const sizes=[[1894,1140],[1280,720],[1920,900],[844,390]];
  for(let i=0;i<12;i++){
   const [width,height]=sizes[Math.floor(i/3)];await page.setViewportSize({width,height});await page.waitForTimeout(350);
   await shot('stroke-'+i+'-before');
   await stroke(i);await waitSaved(page,s=>s.miniStep===i+1);report.checks.push({stroke:i+1,width,height,input:i>=9?'touch':'mouse'});
  }
  await waitSaved(page,s=>s.mechanicDone);await shot('completed');assert.equal(report.errors.length,0);report.status='passed';
 }
 console.log(JSON.stringify({status:report.status,checks:report.checks.length,state:report.state?{miniStep:report.state.miniStep}:undefined}));
}catch(e){report.status='failed';report.failure=e.stack;await shot('failure');throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
