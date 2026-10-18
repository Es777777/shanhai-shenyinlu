import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
import {StoryEngine,defaultProfile,judgeStroke} from '../Preview/engine.js';
import {brushGuides} from '../Preview/mechanics.js';
const root=path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data=JSON.parse(await fs.readFile(path.join(root,'Assets/Resources/Story/story.json'),'utf8'));
const counts=[0,0,0,0],seen=new Set();let routes=0;
function walk(state,depth=0){assert(depth<45);const e=new StoryEngine(data,defaultProfile());e.restore(state);seen.add(e.node.id);while(!e.dialogueFinished)e.readLine();if(e.needsMechanic)e.finishMechanic();if(e.isEnding){e.complete();routes++;counts[e.get('ending')]++;assert.equal(e.get('fragments'),e.state.route==='玄曜'?2:4);return}const choices=e.choices();if(choices.length){const before=structuredClone(e.state);for(const c of choices){e.restore(before);e.select(c.id);walk(e.state,depth+1)}}else{e.advance();walk(e.state,depth+1)}}
walk(new StoryEngine(data,defaultProfile()).state);assert.equal(routes,24);assert.equal(seen.size,36);assert.deepEqual(counts,[0,10,4,10]);
assert.equal(judgeStroke([{x:0,y:0},{x:100,y:100}],[{x:0,y:0},{x:100,y:0},{x:100,y:100}],12),false);
console.log('PASS preview engine parity: 24 routes, 36 nodes, endings 10/4/10');
if(process.argv.includes('--engine-only'))process.exit(0);
const require=createRequire('C:/Users/asd/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/.loader.cjs');
const{chromium}=require('playwright');
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',args:['--autoplay-policy=no-user-gesture-required']});
const context=await browser.newContext({viewport:{width:1600,height:900},deviceScaleFactor:1});
const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`)});
const out=path.join(root,'Builds/QA');await fs.mkdir(out,{recursive:true});
async function screenshot(name){await page.evaluate(()=>document.fonts.ready);await page.screenshot({path:path.join(out,name+'.png')})}
async function goNode(id,{finished=true,route='玄曜'}={}){await page.evaluate(({id,finished,route})=>{const g=window.shanhai;g.engine.newGame();const state=structuredClone(g.engine.state);state.nodeId=id;state.route=route;state.line=finished?g.engine.nodes.get(id).lines.length:0;g.loadGame(state)}, {id,finished,route});}
async function canvasPoint(x,y){const b=await page.locator('#mechanic canvas').boundingBox();const dim=await page.locator('#mechanic canvas').evaluate(c=>({w:c.width,h:c.height}));return{x:b.x+x/dim.w*b.width,y:b.y+y/dim.h*b.height}}
async function stroke(points){const p=await canvasPoint(points[0].x,points[0].y);await page.mouse.move(p.x,p.y);await page.mouse.down();for(const q of points.slice(1)){const t=await canvasPoint(q.x,q.y);await page.mouse.move(t.x,t.y,{steps:8})}await page.mouse.up()}
try{
 await page.goto('http://127.0.0.1:8765/Preview/');await page.waitForFunction(()=>window.shanhai);await page.evaluate(()=>document.fonts.ready);await screenshot('01-title');
 await page.locator('[data-action="new"]').click();await page.waitForFunction(()=>window.shanhai.playing);await page.locator('[data-action="next"]').click();await screenshot('02-dialogue');
 // Real save/load UI and storage survive browser reload.
 await page.locator('[data-action="save"]').click();await page.locator('[data-slot="1"]').click();await page.locator('[data-action="close"]').click();
 const saved=await page.evaluate(()=>JSON.parse(localStorage.getItem('shanhai.preview.v1.slot1')));assert.equal(saved.nodeId,'N001');
 await page.reload();await page.waitForFunction(()=>window.shanhai);await page.locator('[data-action="continue"]').click();assert.equal(await page.evaluate(()=>window.shanhai.engine.node.id),'N001');
 // Exploration: the character must travel to both points before progression unlocks.
 await goNode('N101');assert.equal(await page.locator('#explore-finish').isDisabled(),true);
 for(let i=0;i<2;i++){await page.locator(`[data-target="${i}"]`).click();await page.waitForFunction(count=>window.shanhai.engine.state.inspected.length===count,i+1)}
 await screenshot('03-exploration');await page.locator('#explore-finish').click();assert.equal(await page.evaluate(()=>window.shanhai.engine.state.mechanicDone),true);
 // Drawing a tap must fail; all twelve actual pointer strokes must pass.
 await goNode('N102');let p=await canvasPoint(65+.5*370,25+.13*370);await page.mouse.click(p.x,p.y);assert.equal(await page.evaluate(()=>window.shanhai.engine.state.miniStep),0);
 for(let i=0;i<12;i++){await stroke(brushGuides[i].map(([x,y])=>({x:(i<8?65:520)+x*370,y:25+y*370})));if(i===5)await screenshot('04-calligraphy');assert.equal(await page.evaluate(()=>window.shanhai.engine.state.miniStep),i+1)}
 assert.equal(await page.evaluate(()=>window.shanhai.engine.state.mechanicDone),true);
 // Dragging outside mount frame is rejected, then valid placement enables seal.
 await goNode('N104');await stroke([{x:210,y:180},{x:550,y:20}]);assert.equal(await page.evaluate(()=>window.shanhai.engine.state.miniStep),0);
 await stroke([{x:210,y:180},{x:840,y:190}]);await page.getByRole('button',{name:'落印入册',exact:true}).click();assert.equal(await page.evaluate(()=>window.shanhai.engine.state.mechanicDone),true);
 // Rub real areas of the canvas; cannot seal a blank sheet.
 await goNode('N402',{route:'灵曦'});assert.equal(await page.getByRole('button',{name:'落印',exact:true}).isDisabled(),true);
 for(const y of[35,100,165,230,295,365]){await stroke([{x:25,y},{x:900,y}])}
 await screenshot('05-rubbing');assert.equal(await page.getByRole('button',{name:'落印',exact:true}).isDisabled(),false);await page.getByRole('button',{name:'落印',exact:true}).click();
 // Puzzle: six image tiles swapped by pointer, not a finish bypass.
 await goNode('N603',{route:'灵曦'});await screenshot('06-puzzle');for(let i=0;i<6;i++){const tiles=await page.evaluate(()=>window.shanhai.engine.state.puzzle);const from=tiles.indexOf(i);if(from===i)continue;await stroke([{x:from%3*280+140,y:Math.floor(from/3)*195+95},{x:i%3*280+140,y:Math.floor(i/3)*195+95}])}
 assert.equal(await page.evaluate(()=>window.shanhai.engine.state.mechanicDone),true);
 // Match wrong pair then the four correct ones; mismatch does not grant a fragment.
 await goNode('N702');await page.locator('[data-left="0"]').click();await page.locator('[data-right="0"]').click();assert.equal(await page.evaluate(()=>window.shanhai.engine.state.matched.length),0);
 for(const[i,target]of[2,0,3,1].entries()){await page.locator(`[data-left="${i}"]`).click();await page.locator(`[data-right="${target}"]`).click();if(i===1)await screenshot('07-mirror')}
 assert.equal(await page.evaluate(()=>window.shanhai.engine.state.mechanicDone),true);
 await goNode('N801');assert.equal(await page.locator('[data-target="2"]').isDisabled(),true);for(let i=0;i<3;i++){await page.locator(`[data-target="${i}"]`).click();await page.waitForFunction(count=>window.shanhai.engine.state.inspected.length===count,i+1)}await page.locator('#explore-finish').click();
 await goNode('N803');await page.evaluate(()=>{window.shanhai.engine.set('mcv',66);window.shanhai.render()});await screenshot('08-final-choice');await page.locator('[data-action="choice:final_balance"]').click();assert.equal(await page.evaluate(()=>window.shanhai.engine.get('mcv')),50);
 await goNode('N913');await page.locator('[data-action="next"]').click();assert((await page.evaluate(()=>window.shanhai.profile.endings)).includes(3));await screenshot('09-ending');
 // Touch events and responsive canvas at phone landscape resolution.
 const mobile=await browser.newContext({viewport:{width:844,height:390},isMobile:true,hasTouch:true,deviceScaleFactor:1});const phone=await mobile.newPage();await phone.goto('http://127.0.0.1:8765/Preview/');await phone.waitForFunction(()=>window.shanhai);await phone.locator('[data-action="new"]').tap();await phone.locator('[data-action="next"]').tap();await phone.screenshot({path:path.join(out,'10-mobile-landscape.png')});assert.equal(await phone.locator('#rotate').isVisible(),false);await phone.setViewportSize({width:390,height:844});assert.equal(await phone.locator('#rotate').isVisible(),true);await mobile.close();
 assert.deepEqual(errors,[],'Browser errors');console.log('PASS browser interaction QA: save/load, exploration, 12 strokes, mount, rubbing, puzzle, mirror, climb, ending and mobile layout.');
 await fs.writeFile(path.join(out,'results.json'),JSON.stringify({date:'2026-10-16',engineRoutes:routes,nodes:seen.size,endingDistribution:counts.slice(1),browserErrors:errors,status:'passed',note:'Browser preview tested; these results do not certify a Unity WebGL build.'},null,2));
} finally {await browser.close()}
