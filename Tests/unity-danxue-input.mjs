import assert from 'node:assert/strict';
import {readSavedData,waitSaved} from './unity-storage.mjs';

export function gamePoint(page,x,y){
 const v=page.viewportSize(),s=Math.min(v.width/1600,v.height/900);
 return [(v.width-1600*s)/2+x*s,(v.height-900*s)/2+y*s];
}
export async function sweepVent(page,index,{touch=false,short=false,outside=false}={}){
 const x=[808,1045,1286][index],points=outside?[[x,580],[x,740]]:short?[[x-30,580],[x+30,580]]:[[x-55,580],[x+55,580],[x-55,580],[x+55,580]];
 const cdp=touch?await page.context().newCDPSession(page):null;
 async function send(type,x,y){
  const [px,py]=gamePoint(page,x,y);
  if(touch)await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:px,y:py,radiusX:2,radiusY:2,id:1}]});
  else if(type==='touchStart'){await page.mouse.move(px,py);await page.mouse.down();}
  else if(type==='touchEnd')await page.mouse.up();
  else await page.mouse.move(px,py);
  await page.waitForTimeout(30);
 }
 await send('touchStart',...points[0]);
 for(let p=1;p<points.length;p++)for(let j=1;j<=14;j++){
  const a=points[p-1],b=points[p];await send('touchMove',a[0]+(b[0]-a[0])*j/14,a[1]+(b[1]-a[1])*j/14);
 }
 await send('touchEnd',...points.at(-1));if(cdp)await cdp.detach();
 await page.waitForTimeout(350);
}
export async function solveDanxue(page,click,shot=async()=>{},options={}){
 if(!options.demo){await click(1480,410);assert.equal((await readSavedData(page)).mechanicDone,false);
 await click(808,580);assert.equal((await readSavedData(page)).investigation.prepared,false);}
 await click(370,330);await waitSaved(page,s=>s.evidence.includes('N401'));
 if(options.demo)await page.waitForTimeout(4500);
 await click(395,575);await waitSaved(page,s=>s.items.includes('fan'));
 if(!options.demo){await sweepVent(page,0,{short:true,touch:options.touch});assert.equal((await readSavedData(page)).investigation.progress,0);
 await sweepVent(page,0,{outside:true,touch:options.touch});assert.equal((await readSavedData(page)).investigation.progress,0);}
 await sweepVent(page,0,{touch:options.touch});await waitSaved(page,s=>s.investigation.progress===1);await shot('N401-one-mouth-clear');
 if(options.reload){
  const old=await readSavedData(page);await page.reload();
  await page.waitForFunction(()=>document.getElementById('loading')?.style.display==='none',null,{timeout:180000});await page.waitForTimeout(2500);
  await click(310,614);await waitSaved(page,s=>s.nodeId==='N401'&&s.investigation.progress===1);
  assert.deepEqual((await readSavedData(page)).investigation,old.investigation);
 }
 await sweepVent(page,1,{touch:options.touch});await waitSaved(page,s=>s.investigation.progress===3);
 await sweepVent(page,2,{touch:options.touch});await waitSaved(page,s=>s.investigation.prepared&&s.investigation.progress===7&&!s.items.includes('fan'));
 await page.waitForTimeout(900);await shot('N401-cleared-furnace');
 // Middle lever disturbs all three; the wrong state is recoverable without resetting the scene.
 if(!options.demo){await click(1045,580);await waitSaved(page,s=>s.investigation.dials.slice(0,3).join('')==='111'&&!s.investigation.solved);
 await shot('N401-wrong-wind');await click(1045,580);}
 await click(808,580);await waitSaved(page,s=>s.investigation.dials.slice(0,3).join('')==='110');
 await click(1286,580);await waitSaved(page,s=>s.investigation.solved);
 await page.waitForTimeout(3500);await shot('N401-phoenix-awake');
 await click(1480,410);await waitSaved(page,s=>s.mechanicDone&&s.completed.includes('N401'));
}
