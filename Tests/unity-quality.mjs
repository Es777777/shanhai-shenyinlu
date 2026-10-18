import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {observeArt,waitArtwork,artRequests} from './unity-art-loading.mjs';
import {gamePoint} from './unity-danxue-input.mjs';
import {waitSaved} from './unity-storage.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const out=path.join(root,'Builds/QA/Unity/Quality');await fs.mkdir(out,{recursive:true});
let activePage;
const report={status:'running',checks:[],errors:[],viewports:[]};
try{
 for(const viewport of [{width:1600,height:900},{width:844,height:390}]){
  const context=await browser.newContext({viewport,deviceScaleFactor:1,hasTouch:viewport.width<1000});
  const page=await context.newPage();activePage=page;observeArt(page);page.on('pageerror',e=>report.errors.push(e.message));
  page.on('console',m=>{if(m.type()==='error'||/Exception:|Aborted\(/.test(m.text()))report.errors.push(m.text());});
  const start=Date.now();await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');
  await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});for(let i=0;i<150&&!artRequests(page).length;i++)await page.waitForTimeout(200);await waitArtwork(page);
  const titleAssets=artRequests(page);assert(titleAssets.some(u=>u.includes('tianque')));assert(!titleAssets.some(u=>/xiongli|ending-|buzhou-room/.test(u)));
  const stats=await page.evaluate(()=>performance.getEntriesByType('resource').reduce((sum,r)=>sum+r.encodedBodySize,0));
  report.viewports.push({viewport,titleLoadSeconds:(Date.now()-start)/1000,titleTransferBytes:stats,titleArtRequests:titleAssets.length});
  async function click(x,y){await waitArtwork(page);const point=gamePoint(page,x,y);if(viewport.width<1000)await page.touchscreen.tap(...point);else await page.mouse.click(...point);await page.waitForTimeout(250);await waitArtwork(page);}
  await page.screenshot({path:path.join(out,viewport.width+'-title.png')});
  await click(310,page.viewportSize().height<650?345:535);await waitSaved(page,s=>s.nodeId==='N001');await page.waitForTimeout(1700);
  await page.screenshot({path:path.join(out,viewport.width+'-dialogue.png')});
  await click(1470,47);await click(500,viewport.width<1000?720:650);
  await page.screenshot({path:path.join(out,viewport.width+'-settings.png')});
  await click(1200,710);await click(1360,135);await page.waitForTimeout(700);
  await waitSaved(page,p=>Math.abs(p.textScale-1.3)<0.001,'profile');await page.screenshot({path:path.join(out,viewport.width+'-large-text.png')});
  if(viewport.width<1000){const b=await page.locator('#fullscreen').boundingBox();assert(b.height>=40&&b.width>=100);}
  await page.reload();await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(1500);await waitArtwork(page);
  await click(310,page.viewportSize().height<650?465:614);await waitSaved(page,p=>Math.abs(p.textScale-1.3)<0.001,'profile');
  report.checks.push(viewport.width+'px: title loads only needed art; text size persists; dialogue and settings captured');
  await context.close();
 }
 assert.equal(report.errors.length,0);report.status='passed';console.log('PASS: streamed art and readable mobile UI');
}catch(e){report.status='failed';report.failure=e.stack;if(activePage){await activePage.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});report.lastArtRequests=artRequests(activePage);}throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
