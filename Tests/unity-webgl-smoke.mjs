import {observeArt,waitArtwork} from './unity-art-loading.mjs';
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {readSavedData,waitSaved} from './unity-storage.mjs';

// Run against a real exported build, never against Preview/.
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const url=process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/';
const require=createRequire(process.env.PLAYWRIGHT_LOADER||'C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const out=path.join(root,'Builds/QA/Unity');
await fs.mkdir(out,{recursive:true});
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const errors=[],report={url,checks:[],screenshots:[],audioStates:[],errors};

async function instrument(context){
  await context.addInitScript(()=>{
    window.__qaAudioContexts=[];
    for(const key of ['AudioContext','webkitAudioContext']){
      const Native=window[key];
      if(Native)window[key]=class extends Native{
        constructor(...args){super(...args);window.__qaAudioContexts.push(this);}
      };
    }
  });
  const page=await context.newPage();observeArt(page);
  page.on('pageerror',error=>errors.push(error.message));
  page.on('console',message=>{
    if(message.type()==='error'||/Uncaught|Exception:|Aborted\(|Failed to initialize player|No GlobalGameManagers/.test(message.text()))errors.push(message.text());
  });
  page.on('response',response=>{if(response.status()>=400)errors.push(`${response.status()} ${response.url()}`);});
  return page;
}
async function loaded(page){
  await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});
  await page.waitForTimeout(4000);
  const canvas=await page.locator('#unity-canvas').evaluate(c=>({width:c.width,height:c.height}));
  assert(canvas.width>0&&canvas.height>0);
}
async function clickGame(page,x,y,touch=false){
  await waitArtwork(page);
  const v=page.viewportSize(),s=Math.min(v.width/1600,v.height/900);
  const px=(v.width-1600*s)/2+x*s,py=(v.height-900*s)/2+y*s;
  if(touch)await page.touchscreen.tap(px,py);else await page.mouse.click(px,py,{delay:60});
  await page.waitForTimeout(250);await waitArtwork(page);
}
async function capture(page,name){
  await page.screenshot({path:path.join(out,name+'.png')});
  report.screenshots.push(name+'.png');
}
try{
  const context=await browser.newContext({viewport:{width:1600,height:900},deviceScaleFactor:1});
  const page=await instrument(context);
  const start=Date.now();await page.goto(url);await loaded(page);
  report.initialLoadSeconds=+( (Date.now()-start)/1000 ).toFixed(1);
  report.checks.push('Real Unity loader completed with a nonempty canvas');
  await capture(page,'01-title');
  await clickGame(page,310,535);
  await clickGame(page,790,745);await clickGame(page,790,745);await clickGame(page,790,745);
  await capture(page,'02-dialogue');
  report.audioStates=await page.evaluate(()=>window.__qaAudioContexts.map(c=>c.state));
  assert(report.audioStates.includes('running'),'A real browser gesture must unlock the audio context');
  report.checks.push('Audio context running after a real mouse click');
  await clickGame(page,1480,875);await capture(page,'03-settings');
  await clickGame(page,930,254);await clickGame(page,1360,135);
  await clickGame(page,1230,875);await clickGame(page,1305,302);
  await capture(page,'04-save');await clickGame(page,1360,135);
  await page.waitForTimeout(1500);
  report.storageDatabases=await page.evaluate(async()=>typeof indexedDB.databases==='function'?(await indexedDB.databases()).map(d=>d.name):[]);
  report.storageFiles=await page.evaluate(async()=>{
    const databases=await indexedDB.databases(),files=[];
    for(const info of databases.filter(d=>d.name?.includes('idbfs'))){
      const db=await new Promise((resolve,reject)=>{const r=indexedDB.open(info.name);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);});
      if(db.objectStoreNames.contains('FILE_DATA'))await new Promise((resolve,reject)=>{
        const r=db.transaction('FILE_DATA','readonly').objectStore('FILE_DATA').openCursor();
        r.onerror=()=>reject(r.error);r.onsuccess=()=>{
          const cursor=r.result;if(!cursor){resolve();return;}
          const data=cursor.value.contents;
          if(data?.byteLength)files.push({path:cursor.key,bytes:data.byteLength,preview:new TextDecoder().decode(data).slice(0,600)});
          cursor.continue();
        };
      });
      db.close();
    }
    return files;
  });
  assert(report.storageFiles.length>0,'PlayerPrefs must persist a file to IndexedDB');
  const saved=await waitSaved(page,s=>s.nodeId==='N001'&&s.line===1,'slot1');
  report.manualSave={nodeId:saved.nodeId,line:saved.line};
  await clickGame(page,1350,875);await clickGame(page,1305,302);await clickGame(page,980,535);
  await waitSaved(page,s=>s.nodeId===saved.nodeId&&s.line===saved.line);
  await capture(page,'04b-manual-load');
  report.checks.push('Manual load confirmed through the real modal dialog');
  await page.reload();await loaded(page);await clickGame(page,310,614);
  await clickGame(page,790,745);await capture(page,'05-continue-after-reload');
  const resumed=await readSavedData(page);
  assert.equal(resumed.nodeId,saved.nodeId);assert.equal(resumed.line,saved.line);
  report.checks.push('IndexedDB confirms manual slot 1 and automatic resume at N001, dialogue line 1');
  report.checks.push('Mouse flow captured: start, dialogue, settings, manual save and continue after reload');
  const button=await page.locator('#fullscreen').boundingBox();
  assert(button&&button.x>=595&&button.x+button.width<=800,'Fullscreen must fit the toolbar gap');
  await page.locator('#fullscreen').click();
  await page.waitForFunction(()=>document.fullscreenElement!==null,null,{timeout:10000});
  await page.evaluate(()=>document.exitFullscreen());
  report.checks.push('Fullscreen works after a real click');
  await context.close();

  const mobile=await browser.newContext({viewport:{width:844,height:390},deviceScaleFactor:1,isMobile:true,hasTouch:true});
  const phone=await instrument(mobile);await phone.goto(url);await loaded(phone);
  await clickGame(phone,310,345,true);await clickGame(phone,790,745,true);
  await capture(phone,'06-mobile-landscape');
  await phone.setViewportSize({width:390,height:844});await phone.waitForTimeout(1000);
  await capture(phone,'07-mobile-portrait');
  assert.equal(await phone.locator('#fullscreen').isVisible(),false);
  report.checks.push('Touch flow and portrait prompt captured at mobile viewport sizes');
  await mobile.close();
  assert.equal(errors.length,0,`Browser errors: ${[...new Set(errors)].slice(0,3).join('\n')}`);
  report.status='passed';
  report.note='Screenshots require visual review. This browser smoke test does not replace real phone testing or certify every story route in Unity.';
  console.log('PASS: real Unity WebGL browser smoke checks');
}catch(error){report.status='failed';report.failure=error.stack;throw error;}
finally{
  await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));
  await browser.close();
}
