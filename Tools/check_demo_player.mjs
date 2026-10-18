// Validate the delivered local HTML player and actual MP4 in Chrome.
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const work=path.join(root,process.env.SHANHAI_DEMO_DIR||'Builds/Demo-0.6.0');
const report=JSON.parse(await fs.readFile(path.join(work,'video-verification.json'),'utf8'));
const require=createRequire(process.env.PLAYWRIGHT_LOADER||'C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe'});
const result={status:'running',errors:[],checks:[]};
try{
  const page=await browser.newPage({viewport:{width:1600,height:1100}});
  page.on('pageerror',e=>result.errors.push(e.message));
  const player=process.env.SHANHAI_DEMO_PLAYER||path.join(root,'../山海神隐录_Demo播放_'+report.version+'.html');
  await page.goto(pathToFileURL(player).href);
  await page.waitForFunction(()=>document.querySelector('video').readyState>=1);
  const media=await page.locator('video').evaluate(v=>({duration:v.duration,width:v.videoWidth,height:v.videoHeight,error:v.error?.message}));
  assert(!media.error);
  assert(Math.abs(media.duration-report.durationSeconds)<1);
  assert.equal(media.width,1600);assert.equal(media.height,900);
  assert.equal(await page.locator('nav button').count(),report.chapters.length);
  await page.locator('video').evaluate(v=>{v.muted=true;});
  for(const index of [0,Math.floor(report.chapters.length/2),report.chapters.length-1]){
    await page.locator('nav button').nth(index).click();
    const expected=report.chapters[index].seconds;
    await page.waitForFunction(t=>{const v=document.querySelector('video');return !v.paused&&!v.seeking&&v.currentTime>t+0.3;},expected,{timeout:30000});
    const state=await page.locator('video').evaluate(v=>({time:v.currentTime,error:v.error?.message}));
    assert(!state.error);assert(Math.abs(state.time-expected)<5);
    result.checks.push({chapter:index,expected,actual:state.time});
    await page.locator('video').evaluate(v=>v.pause());
  }
  result.metadata=media;
  assert.equal(result.errors.length,0);
  result.status='passed';
  console.log('PASS: local MP4 metadata, playback, first/middle/final chapter seeking; '+report.chapters.length+' chapter buttons.');
}catch(error){result.status='failed';result.failure=error.stack;throw error;}
finally{await fs.writeFile(path.join(work,process.env.SHANHAI_DEMO_PLAYER_REPORT||'player-verification.json'),JSON.stringify(result,null,2));await browser.close();}
