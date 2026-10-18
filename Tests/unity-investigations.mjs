import assert from 'node:assert/strict';
import {readSavedData,waitSaved} from './unity-storage.mjs';
import {solveDanxue,gamePoint} from './unity-danxue-input.mjs';

export const spots={
 N101:[[370,330],[390,532],[684,497],[1490,470]],N201:[[350,330],[380,512],[1038,244],[1040,520]],
 N301:[[300,320],null,[704,512],[1010,280]],N403:[[300,330],[415,582],[827,218],[1440,460]],
 N501:[[500,390],[440,620],[1110,570],[1110,600]],N601:[[300,300],null,[1046,366],[1090,620]],
 N801:[[230,300],[390,510],[470,475],[1450,230]]
};
const toolNames={N101:'mirror',N201:'ember',N301:'everflame',N403:'brush',N501:'ballast',N601:'leaf',N801:'rope'};
async function brushRiver(page,touch,short=false){
 const points=short?[[220,300],[240,300]]:[[220,300],[420,300],[220,400]];
 const cdp=touch?await page.context().newCDPSession(page):null;
 async function send(type,x,y){
  const [px,py]=gamePoint(page,x,y);
  if(touch)await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:px,y:py,id:1}]});
  else if(type==='touchStart'){await page.mouse.move(px,py);await page.mouse.down();}
  else if(type==='touchEnd')await page.mouse.up();else await page.mouse.move(px,py);
  await page.waitForTimeout(35);
 }
 await send('touchStart',...points[0]);
 for(let i=1;i<points.length;i++)for(let j=1;j<=24;j++)await send('touchMove',points[i-1][0]+(points[i][0]-points[i-1][0])*j/24,points[i-1][1]+(points[i][1]-points[i-1][1])*j/24);
 await send('touchEnd',...points.at(-1));if(cdp)await cdp.detach();
}
export async function solveInvestigation(page,id,click,shot=async()=>{},options={}){
 if(id==='N401')return solveDanxue(page,click,shot,options);
 const s=spots[id];assert(s,'No investigation solver for '+id);
 await shot(id+'-scene-before');
 if(!options.demo){await click(...s[3]);assert.equal((await readSavedData(page)).mechanicDone,false);}
 await click(...s[0]);
 if(options.demo)await page.waitForTimeout(3500);
 const before=await readSavedData(page),item=toolNames[id];
 if(!before.items?.includes(item))await click(...s[1]);
 await waitSaved(page,x=>x.items.includes(item));
 if(id==='N403'){
  if(!options.demo){await brushRiver(page,options.touch,true);assert.equal((await readSavedData(page)).investigation.prepared,false);}
  await brushRiver(page,options.touch);
 }else await click(...s[id==='N201'?0:2]);
 await waitSaved(page,x=>x.investigation.prepared);
 await shot(id+'-mechanism');
 await click(page.viewportSize().height<650?1180:1340,page.viewportSize().height<650?150:100);await shot(id+'-notebook');await click(1345,198);
 if(id==='N101'){
  await click(684,497);await waitSaved(page,x=>x.investigation.dials[0]===0);
  if(options.reload){
   const saved=await readSavedData(page);await page.reload();await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(2500);
   await click(310,page.viewportSize().height<650?465:614);await waitSaved(page,x=>x.nodeId===id&&x.investigation.dials[0]===0);
   assert.deepEqual((await readSavedData(page)).investigation,saved.investigation);
  }
  await click(684,222);await click(1100,222);
 }else if(id==='N201'){
  for(let i=0;i<2;i++)await click(837,244);for(let i=0;i<3;i++)await click(1239,244);
 }else if(id==='N301'){
  if(!options.demo){await click(704,512);await waitSaved(page,x=>x.investigation.progress===0);}
  for(const x of [1153,704,1378,929])await click(x,512);
 }else if(id==='N403'){
  for(const i of [0,3,6,7,8])for(let turn=0;turn<3;turn++)await click(827+i%3*147,218+Math.floor(i/3)*146);
 }else if(id==='N501'){
  await click(921,530);await click(1171,530);
 }else if(id==='N601'){
  await click(1180,366);await click(1130,366);for(let i=0;i<3;i++)await click(1090,366);
 }else if(id==='N801'){
  if(!options.demo){await click(1150,300);await waitSaved(page,x=>x.investigation.progress===0);}
  for(const [step,cell] of [8,9,5,6,2,3].entries()){
   await click(step%2?240:105,810);await click(610+cell%4*180,300+Math.floor(cell/4)*150);
   await waitSaved(page,x=>x.investigation.progress===step+1);
  }
 }
 await waitSaved(page,x=>x.investigation.solved);await shot(id+'-solved');
 await page.waitForTimeout(2500);await shot(id+'-scene-changed');
 await click(...s[3]);await waitSaved(page,x=>x.mechanicDone&&x.completed.includes(id));
}
