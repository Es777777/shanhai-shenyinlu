import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {observeArt,waitArtwork} from './unity-art-loading.mjs';
import {waitSaved} from './unity-storage.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const page=await browser.newPage({viewport:{width:1600,height:900}});observeArt(page);
const out=path.join(root,'Builds/QA/Unity/ArtRetry');await fs.mkdir(out,{recursive:true});
const report={status:'running',errors:[],expectedHttp503:0,attempts:0};
page.on('pageerror',e=>report.errors.push(e.message));
page.on('console',m=>{if(m.type()==='error'){if(/503/.test(m.text()))report.expectedHttp503++;else report.errors.push(m.text());}});
await page.route('**/StreamingAssets/Art/*tianque*',async route=>{
  report.attempts++;if(report.attempts===1)await route.fulfill({status:503,body:'Temporary test failure'});else await route.continue();
});
try{
  await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');
  for(let i=0;i<450&&report.attempts===0;i++)await page.waitForTimeout(200);
  assert.equal(report.attempts,1);await waitArtwork(page);await page.waitForTimeout(1000);
  await page.screenshot({path:path.join(out,'load-failure.png')});
  await page.mouse.click(800,545);await page.waitForTimeout(600);await waitArtwork(page);
  assert.equal(report.attempts,2);await page.screenshot({path:path.join(out,'recovered.png')});
  await page.mouse.click(310,535);await waitSaved(page,s=>s.nodeId==='N001');
  assert.equal(report.errors.length,0);assert.equal(report.expectedHttp503,1);
  report.status='passed';console.log('PASS: failed scene art can be retried and play continues');
}catch(e){report.status='failed';report.failure=e.stack;throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
