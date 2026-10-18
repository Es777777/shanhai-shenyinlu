import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {observeArt,waitArtwork} from './unity-art-loading.mjs';
import {waitSaved,readSavedData} from './unity-storage.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url))),out=path.join(root,'Builds/QA/Unity/Usability');await fs.mkdir(out,{recursive:true});
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs'),{chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const report={status:'running',cases:[],errors:[],note:'Desktop Chrome emulation and network throttling only, not physical device certification.'};let page;
async function click(x,y){await waitArtwork(page);const b=await page.locator('canvas').boundingBox(),s=Math.min(b.width/1600,b.height/900);await page.mouse.click(b.x+(b.width-1600*s)/2+x*s,b.y+(b.height-900*s)/2+y*s);await page.waitForTimeout(300);await waitArtwork(page);}
try{
 for(const spec of [{name:'desktop-dpi2',viewport:{width:1280,height:720},dpr:2},{name:'mobile-dpi3-safearea',viewport:{width:915,height:412},dpr:3,mobile:true}]){
  const context=await browser.newContext({viewport:spec.viewport,deviceScaleFactor:spec.dpr,hasTouch:!!spec.mobile,isMobile:!!spec.mobile,...(spec.mobile?{userAgent:'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36'}:{})});
  if(spec.mobile)await context.addInitScript(()=>document.addEventListener('DOMContentLoaded',()=>Object.assign(document.getElementById('game-frame').style,{left:'20px',right:'20px',bottom:'18px'})));
  page=await context.newPage();observeArt(page);page.on('pageerror',e=>report.errors.push(e.message));
  const cdp=await context.newCDPSession(page);
  if(spec.mobile)await cdp.send('Network.emulateNetworkConditions',{offline:false,latency:120,downloadThroughput:1024*1024,uploadThroughput:512*1024});
  const start=Date.now();await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await waitArtwork(page);await page.waitForTimeout(600);
  const metrics=await page.evaluate(()=>({viewport:window.shanhaiViewport(),canvas:[document.querySelector('canvas').width,document.querySelector('canvas').height],transfer:performance.getEntriesByType('resource').reduce((n,r)=>n+r.encodedBodySize,0)}));
  metrics.titleSeconds=(Date.now()-start)/1000;assert.equal(metrics.viewport.compact,!!spec.mobile);
  await click(310,spec.mobile?345:535);await waitSaved(page,s=>s.nodeId==='N001');
  await click(1470,47);await click(500,spec.mobile?720:650);
  // Hit near the edge of the enlarged slider, not only its four-pixel track.
  await click(940,spec.mobile?210:230);await click(1200,spec.mobile?740:710);await click(1360,135);
  await waitSaved(page,p=>p.musicVolume>.57&&p.musicVolume<.63&&p.textScale>1.29,'profile');
  await page.screenshot({path:path.join(out,spec.name+'-dialogue.png')});
  const button=await page.locator('#fullscreen').boundingBox(),frame=await page.locator('canvas').boundingBox();assert(button&&button.y+button.height<=frame.y+frame.height+1);
  // Run N001 naturally to unlock the first codex entry.
  const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8')),node=story.nodes.find(n=>n.id==='N001');
  while((await readSavedData(page)).line<node.lines.length){await click(790,735);await click(790,735);}
  await click(1350,776);await waitSaved(page,s=>s.nodeId==='N002');await click(1270,47);await page.screenshot({path:path.join(out,spec.name+'-codex.png')});
  assert.equal(await page.locator('#fullscreen').isVisible(),false,'DOM fullscreen must not cover reading panels');
  const checks=['Shared CSS viewport and fullscreen bounds','Enlarged slider touch area','Persistent text size','Readable codex at large font','Fullscreen hidden behind panels'];
  if(!spec.mobile){
    await click(1360,135);
    await page.evaluate(()=>{const ext=document.querySelector('canvas').getContext('webgl2').getExtension('WEBGL_lose_context');if(!ext)throw new Error('Context-loss test extension unavailable');ext.loseContext();});
    await page.locator('#reload').waitFor({state:'visible'});await page.screenshot({path:path.join(out,'context-loss.png')});
    await Promise.all([page.waitForEvent('load'),page.locator('#reload').click()]);
    await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await waitArtwork(page);await click(310,614);await waitSaved(page,s=>s.nodeId==='N002');
    checks.push('Actual graphics-context loss displays reload and resumes autosave');
  }
  report.cases.push({name:spec.name,...metrics,checks});await context.close();
 }
 assert.equal(report.errors.length,0);report.status='passed';console.log('PASS: DPR 2 desktop, DPR 3 mobile, safe-area simulation, large text and 8 Mbit/s loading');
}catch(e){report.status='failed';report.failure=e.stack;if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
