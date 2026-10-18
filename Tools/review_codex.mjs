import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const require=createRequire(process.env.PLAYWRIGHT_LOADER||'C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const {chromium}=require('playwright');
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe'});
try{
 const page=await browser.newPage({viewport:{width:1300,height:1800}});
 let html='<body style="background:#102025;color:#ead8ac;font:22px sans-serif;padding:20px"><h2>0.4.1 — 图鉴实际截图核对</h2><main style="display:grid;grid-template-columns:repeat(3,400px);gap:20px">';
 for(const entry of story.codex){
  const file=path.join(root,'Builds/QA/Unity/Playthrough/codex-'+entry.id+'.png');
  const base64=(await fs.readFile(file)).toString('base64');
  html+='<section><div>'+entry.title+' / '+entry.id+'</div><div style="position:relative;width:400px;height:251px;overflow:hidden;margin-top:8px"><img style="position:absolute;width:1163.64px;height:654.55px;left:-592.73px;top:-163.64px" src="data:image/png;base64,'+base64+'"></div></section>';
 }
 await page.setContent(html+'</main></body>');
 await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));
 await page.screenshot({path:path.join(root,'Builds/QA/Unity/codex-review.png'),fullPage:true});
}finally{await browser.close();}
