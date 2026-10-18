import {createRequire} from 'node:module';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
const page=await browser.newPage({viewport:{width:1500,height:1400}});
await page.goto('http://127.0.0.1:8765/Preview/');
for(const folder of ['Backgrounds','Characters']){
 const names=(await fs.readdir(path.join(root,'Assets/Art',folder))).filter(n=>n.endsWith('.png'));
 await page.setContent(`<body style="margin:0;padding:20px;background:#d0cbbd;font:22px Georgia;color:#203330"><h2>${folder}</h2><div style="display:grid;grid-template-columns:repeat(3,1fr);gap:18px">${names.map(n=>`<div><div>${n}</div><img style="width:470px;height:${folder==='Backgrounds'?265:340}px;object-fit:contain;background:#a7b5b0" src="http://127.0.0.1:8765/Assets/Art/${folder}/${n}"></div>`).join('')}</div></body>`);
 await page.evaluate(()=>Promise.all([...document.images].map(img=>img.decode())));
 await page.screenshot({path:path.join(root,'Builds/QA/assets-'+folder+'.png'),fullPage:true});
}
await browser.close();
