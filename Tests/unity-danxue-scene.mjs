import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {waitSaved,readSavedData} from './unity-storage.mjs';
import {solveDanxue,gamePoint} from './unity-danxue-input.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const out=path.join(root,'Builds/QA/Unity/Danxue');await fs.mkdir(out,{recursive:true});
const report={status:'running',checks:[],errors:[],note:'Real Unity WebGL, no state injection. Desktop mouse and CDP touch gestures. IndexedDB is read only.'};
let page;
try{
 const context=await browser.newContext({viewport:{width:1894,height:1140},hasTouch:true,deviceScaleFactor:1});
 page=await context.newPage();
 page.on('pageerror',e=>report.errors.push(e.message));
 page.on('console',m=>{if(m.type()==='error'||/Uncaught|Exception:|Aborted\(/.test(m.text()))report.errors.push(m.text());});
 const click=async(x,y)=>{await page.mouse.click(...gamePoint(page,x,y),{delay:60});await page.waitForTimeout(200);};
 const shot=async(name)=>{await page.mouse.move(...gamePoint(page,10,710));await page.waitForTimeout(250);await page.screenshot({path:path.join(out,name+'.png')});};
 await page.goto('http://127.0.0.1:8766/');
 await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(3000);
 await click(310,535);let state=await waitSaved(page,s=>s.nodeId==='N001');
 while(state.nodeId!=='N401'||state.line<story.nodes.find(n=>n.id==='N401').lines.length){
  const node=story.nodes.find(n=>n.id===state.nodeId);
  if(state.line<node.lines.length){
   const old=state.line;await click(790,745);await click(790,745);state=await waitSaved(page,s=>s.line>old);continue;
  }
  if(node.id==='N002')await click(800,543);else await click(1350,776);
  state=await waitSaved(page,s=>s.nodeId!==node.id);
 }
 await page.waitForTimeout(900);await shot('01-integrated-scene-desktop');
 await solveDanxue(page,click,async name=>{
  await shot(name);
  if(name==='N401-one-mouth-clear'){
   // Same saved scene continues at a phone-sized canvas; reload keeps the cleaned mouth.
   await page.setViewportSize({width:844,height:390});await page.waitForTimeout(1000);
  }
  if(name==='N401-phoenix-awake'){
   await page.setViewportSize({width:1894,height:1140});await page.waitForTimeout(800);await shot('02-phoenix-desktop');
  }
 },{reload:true,touch:true});
 report.checks.push('Scene and physical objects captured at 1894x1140','Exit stays gated before puzzle solved','Mural evidence and fan pickup persist','Short sweep and sweep outside mouth cannot clear ash','First mouth clears with desktop mouse','Refresh preserves partial mask and owned fan','Remaining mouths clear with touch at 844x390','All three cleanings required before shutters can move','Wrong linked state recoverable','Phoenix appears in original scene after 101 vent solution','Actual right staircase finishes N401');
 const stateDone=await readSavedData(page);assert.equal(stateDone.mechanicDone,true);
 assert.equal(report.errors.length,0);report.status='passed';console.log('PASS: integrated Danxue scene, saved clearing progress, scaled mouse/touch and phoenix reveal');
}catch(error){report.status='failed';report.failure=error.stack;if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});throw error;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
