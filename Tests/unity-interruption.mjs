import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {readSavedData,waitSaved} from './unity-storage.mjs';
import {solveInvestigation} from './unity-investigations.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const out=path.join(root,'Builds/QA/Unity/Interruptions');await fs.mkdir(out,{recursive:true});
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const context=await browser.newContext({viewport:{width:1600,height:900}}),page=await context.newPage();
const report={status:'running',checks:[],errors:[]};page.on('pageerror',e=>report.errors.push(e.message));
page.on('console',m=>{if(m.type()==='error'||/Uncaught|Exception:|Aborted\(|Failed to initialize player|No GlobalGameManagers/.test(m.text()))report.errors.push(m.text());});
async function click(x,y){await page.mouse.click(x,y,{delay:50});await page.waitForTimeout(170);}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png')});}
try{
 await page.goto('http://127.0.0.1:8766/');await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(2500);await click(310,535);
 let s=await waitSaved(page,x=>x.nodeId==='N001');
 while(true){
  const n=story.nodes.find(n=>n.id===s.nodeId);
  if(s.line<n.lines.length){const old=s.line;await click(790,745);await click(790,745);s=await waitSaved(page,x=>x.line>old||x.nodeId!==n.id);continue;}
  if(n.id==='N603')break;
  if(n.mechanic&&!s.mechanicDone){
   console.log(n.id,n.mechanic);
   if(n.mechanic==='explore')await solveInvestigation(page,n.id,click);
   else if(n.mechanic==='rubbing'){
    for(let row=0;row<8;row++){
     const y=332+row*48,a=row%2?1205:365,b=row%2?365:1205;
     await page.mouse.move(a,y);await page.mouse.down();await page.mouse.move(b,y,{steps:64});await page.mouse.up();
    }
    await waitSaved(page,x=>x.rubbed.length>=144);await click(1330,505);
   }else throw new Error('Unexpected mechanic '+n.mechanic);
   s=await readSavedData(page);continue;
  }
  const count=n.choices.length||story.edges.filter(e=>e.from===n.id&&e.kind==='玩家选择').length;
  await click(count?800:1350,count?420-(100+count*90)*.3+117+(n.id==='N002'?90:0):776);
  s=await waitSaved(page,x=>x.nodeId!==n.id);
 }
 const before=(await waitSaved(page,x=>x.puzzle.length===6)).puzzle;
 await page.mouse.move(460,407);await page.mouse.down();await page.waitForTimeout(120);
 await page.setViewportSize({width:390,height:844});await page.waitForTimeout(700);await shot('portrait-during-drag');
 await page.setViewportSize({width:1600,height:900});await page.waitForTimeout(500);
 await page.mouse.move(740,407,{steps:10});await page.mouse.up();await page.waitForTimeout(800);
 await shot('after-interruption');
 assert.deepEqual((await readSavedData(page)).puzzle,before,'A drag interrupted by portrait must not swap a piece when released after returning');
 report.checks.push('Portrait cancels unfinished puzzle drag; releasing after landscape does not swap.');
 await page.mouse.move(460,407);await page.mouse.down();await page.mouse.move(740,407,{steps:20});await page.mouse.up();
 await waitSaved(page,x=>x.puzzle[1]===before[0]);report.checks.push('A fresh drag still swaps normally after interruption.');
 const after=(await readSavedData(page)).puzzle;
 await page.mouse.move(460,407);await page.mouse.down();await page.waitForTimeout(120);
 await page.keyboard.press('Escape');await page.waitForTimeout(300);
 await page.mouse.move(740,407,{steps:10});await page.mouse.up();await click(1360,135);
 assert.deepEqual((await readSavedData(page)).puzzle,after,'Opening a menu must cancel the unfinished puzzle drag');
 report.checks.push('Opening the menu while dragging also cancels the gesture without a swap.');
 await shot('recovered');
 assert.equal(report.errors.length,0);report.status='passed';console.log('PASS: interruption and recovery');
}catch(e){report.status='failed';report.failure=e.stack;await shot('failure');throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
