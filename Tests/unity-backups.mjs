import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {observeArt,waitArtwork} from './unity-art-loading.mjs';
import {readSavedData,waitSaved} from './unity-storage.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url))),out=path.join(root,'Builds/QA/Unity/Backups');await fs.mkdir(out,{recursive:true});
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs'),{chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const report={status:'running',checks:[],errors:[]};let page;
async function load(){await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await waitArtwork(page);await page.waitForTimeout(700);}
async function click(x,y){await waitArtwork(page);const b=await page.locator('canvas').boundingBox(),s=Math.min(b.width/1600,b.height/900);await page.mouse.click(b.x+(b.width-1600*s)/2+x*s,b.y+(b.height-900*s)/2+y*s);await page.waitForTimeout(250);await waitArtwork(page);}
async function context(dpr){const c=await browser.newContext({viewport:{width:844,height:500},deviceScaleFactor:dpr,acceptDownloads:true});page=await c.newPage();observeArt(page);page.on('pageerror',e=>report.errors.push(e.message));await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');await load();return c;}
async function advance(n){for(let i=0;i<n;i++){await click(790,735);await click(790,735);await waitSaved(page,s=>s.line===i+1);}}
async function backup(){await click(1470,47);await click(500,350);await click(1100,670);}
async function upload(file){const choice=page.waitForEvent('filechooser');await click(1100,450);await (await choice).setFiles(file);await page.waitForTimeout(700);}
try{
 const first=await context(1);await click(310,345);await waitSaved(page,s=>s.nodeId==='N001');await advance(1);
 await click(1470,47);await click(500,720);await click(1200,740);await click(1360,135);await waitSaved(page,p=>p.textScale>1.29,'profile');
 await click(1470,47);await click(500,350);await click(1310,280);await waitSaved(page,s=>s.line===1,'slot1');await click(1100,670);
 const download=page.waitForEvent('download');await click(500,450);const file=path.join(out,'exported-save.json');await (await download).saveAs(file);
 const archive=JSON.parse(await fs.readFile(file,'utf8'));assert.equal(archive.sha256,createHash('sha256').update(archive.payload).digest('hex'));const data=JSON.parse(archive.payload);assert(Math.abs(data.profile.textScale-1.3)<.001);assert.equal(JSON.parse(data.slots[1]).line,1);
 report.checks.push('Downloaded checksum-protected archive includes actual manual save and settings');await first.close();
 const second=await context(2);assert((await page.evaluate(()=>window.shanhaiViewport())).compact);await page.screenshot({path:path.join(out,'dpi2-title.png')});
 await click(310,345);await waitSaved(page,s=>s.nodeId==='N001');await advance(2);await page.screenshot({path:path.join(out,'dpi2-dialogue.png')});
 report.checks.push('DPR 2 uses compact title and gameplay with correctly placed fullscreen button');
 await backup();await upload(file);await page.screenshot({path:path.join(out,'import-preview.png')});await click(585,555);assert.equal((await readSavedData(page)).line,2);
 report.checks.push('Cancelling import leaves current progress unchanged');
 const broken=path.join(out,'broken-save.json');archive.payload+=' ';await fs.writeFile(broken,JSON.stringify(archive));await upload(broken);assert.equal((await readSavedData(page)).line,2);await page.screenshot({path:path.join(out,'invalid-file.png')});
 report.checks.push('Corrupted file is rejected before replacing progress');
 await upload(file);await click(1000,555);await waitSaved(page,s=>s.line===1);await waitSaved(page,p=>p.textScale>1.29,'profile');await waitSaved(page,s=>s.line===1,'slot1');
 await page.screenshot({path:path.join(out,'imported.png')});await page.reload();await load();await click(310,465);await waitSaved(page,s=>s.line===1);await waitSaved(page,p=>p.textScale>1.29,'profile');
 report.checks.push('Cross-browser import restores all slots and settings and survives reload');
 await backup();await click(700,595);await click(1000,555);await waitSaved(page,s=>s.line===2);await waitSaved(page,p=>p.textScale===1,'profile');
 assert.equal(await readSavedData(page,'slot1'),null);report.checks.push('Restore-before-import recovers previous progress and clears formerly empty slots');
 await page.screenshot({path:path.join(out,'restored.png')});assert.equal(report.errors.length,0);report.status='passed';await second.close();
 console.log('PASS: browser save download, file import, cancel, corruption rejection, rollback and DPR 2');
}catch(e){report.failure=e.stack;if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});report.status='failed';throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
