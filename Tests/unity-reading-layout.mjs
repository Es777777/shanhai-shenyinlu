// Focused rendering fixtures use the public import UI. Complete playthrough tests
// remain separate and use no injected saves or skipped story states.
import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';
import {createRequire} from 'node:module';import {fileURLToPath} from 'node:url';import {createHash} from 'node:crypto';
import {observeArt,waitArtwork} from './unity-art-loading.mjs';import {waitSaved} from './unity-storage.mjs';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url))),out=path.join(root,'Builds/QA/Unity/Reading');await fs.mkdir(out,{recursive:true});
const story=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs'),{chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--enable-unsafe-swiftshader']});
const report={status:'running',method:'Synthetic saves imported through player-facing file picker for focused large-font rendering, not evidence of full playthrough.',screenshots:[],errors:[]};let page;
async function click(x,y){await waitArtwork(page);const b=await page.locator('canvas').boundingBox(),s=Math.min(b.width/1600,b.height/900);await page.mouse.click(b.x+(b.width-1600*s)/2+x*s,b.y+(b.height-900*s)/2+y*s);await page.waitForTimeout(300);await waitArtwork(page);}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png')});report.screenshots.push(name+'.png');}
try{
 for(const nodeId of ['N603','N702','N601']){
  const node=story.nodes.find(n=>n.id===nodeId);
  const state={version:1,nodeId,route:'玄曜',savedAt:'阅读布局测试',choiceReply:'',line:node.lines.length,miniStep:0,mechanicDone:false,values:story.variables.map(v=>({key:v.key,value:v.initial})),completed:[],visited:[nodeId],inspected:[],choices:[],puzzle:nodeId==='N603'?[0,1,2,3,4,5]:[],rubbed:[],matched:nodeId==='N702'?[0,1,2,3]:[],deductions:[],reasoningOrder:[0,1,2],reasoningHints:0,mountingRotation:0,mountingSeal:-1,history:[],items:[],evidence:['N101','N201','N301','N401','N403','N501','N601','N801'],investigation:{node:'',prepared:false,solved:false,progress:0,hints:0,dials:[]}};
  const profile={version:1,unlocked:story.codex.map(c=>c.id),readLines:[],endings:[],checkpoints:[],musicVolume:.45,soundVolume:.65,textSpeed:30,autoDelay:2.2,textScale:1.3,skipUnread:false,reducedMotion:false};
  const payload=JSON.stringify({version:1,profile,slots:[JSON.stringify(state),'','','','','','']}),fixture=path.join(out,nodeId+'-fixture.json');
  await fs.writeFile(fixture,JSON.stringify({version:1,format:'shanhai-save',createdAt:'rendering fixture',payload,sha256:createHash('sha256').update(payload).digest('hex')}));
  const context=await browser.newContext({viewport:{width:844,height:390},deviceScaleFactor:2,hasTouch:true});page=await context.newPage();observeArt(page);page.on('pageerror',e=>report.errors.push(e.message));
  await page.goto(process.env.SHANHAI_WEBGL_URL||'http://127.0.0.1:8766/');await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await waitArtwork(page);
  await click(380,720);await click(1100,670);const chooser=page.waitForEvent('filechooser');await click(1100,450);await(await chooser).setFiles(fixture);await page.waitForTimeout(600);await click(1000,555);await click(1360,135);await click(310,465);await waitSaved(page,s=>s.nodeId===nodeId);await page.waitForTimeout(800);await waitArtwork(page);
  if(nodeId==='N601'){await click(1160,150);await shot('large-notebook');await click(1310,200);await click(1270,47);await shot('large-codex');}
  else{await shot(nodeId+'-large-reasoning');await click(1260,190);await click(1260,190);await click(1260,190);await waitSaved(page,s=>s.reasoningHints===3);await shot(nodeId+'-hint');}
  await context.close();
 }
 assert.equal(report.errors.length,0);report.status='passed';console.log('PASS: focused large-font reading fixtures rendered; inspect screenshots');
}catch(e){report.status='failed';report.failure=e.stack;if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});throw e;}
finally{await fs.writeFile(path.join(out,'results.json'),JSON.stringify(report,null,2));await browser.close();}
