import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {installCapture,startCapture,stopCapture} from './demo_capture.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const dir=path.join(root,'Builds/Demo');await fs.mkdir(dir,{recursive:true});
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader','--disable-background-timer-throttling','--disable-renderer-backgrounding',...(process.env.SHANHAI_ANGLE?['--use-angle='+process.env.SHANHAI_ANGLE]:[])]});
try{
 const context=await browser.newContext({viewport:{width:1600,height:900}});await installCapture(context);
 const page=await context.newPage();page.on('pageerror',e=>console.error(e.message));page.on('console',m=>console.log(m.type(),m.text()));await page.goto('http://127.0.0.1:8766/');
 await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(2500);
 console.log(await page.evaluate(()=>{const c=document.getElementById('unity-canvas'),g=c.getContext('webgl2')||c.getContext('webgl'),d=g.getExtension('WEBGL_debug_renderer_info');return {renderer:g.getParameter(d.UNMASKED_RENDERER_WEBGL),audio:window.__captureContexts.length};}));
 await page.mouse.click(780,400);await page.waitForTimeout(500);await page.screenshot({path:path.join(dir,'probe-before.png')});
 console.log(await startCapture(page,path.join(dir,'capture-probe.webm')));
 await page.waitForTimeout(3000);await page.mouse.click(310,535);await page.waitForTimeout(6000);await page.mouse.click(790,745);await page.waitForTimeout(4000);
 console.log(await stopCapture(page));
}finally{await browser.close();}
