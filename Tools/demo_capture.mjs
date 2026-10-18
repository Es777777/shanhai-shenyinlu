// Actual Unity canvas + its Web Audio output, without microphone or desktop capture.
// APIs: https://developer.mozilla.org/en-US/docs/Web/API/HTMLCanvasElement/captureStream
// https://developer.mozilla.org/en-US/docs/Web/API/AudioContext/createMediaStreamDestination
import fs from 'node:fs/promises';
const targets=new WeakMap();
const lastWrites=new WeakMap(),watchers=new WeakMap();
export async function installCapture(context){
 await context.addInitScript(()=>{
  window.__captureContexts=[];
  const connect=AudioNode.prototype.connect,disconnect=AudioNode.prototype.disconnect;
  AudioNode.prototype.connect=function(destination,...rest){
   const result=connect.call(this,destination,...rest);
   if(destination===this.context.destination){
    if(!this.context.__captureBus)this.context.__captureBus=this.context.createMediaStreamDestination();
    connect.call(this,this.context.__captureBus,...rest);
   }
   return result;
  };
  AudioNode.prototype.disconnect=function(...args){
   if(args[0]===this.context.destination&&this.context.__captureBus){try{disconnect.call(this,this.context.__captureBus,...args.slice(1));}catch{}}
   return disconnect.apply(this,args);
  };
  for(const key of ['AudioContext','webkitAudioContext']){
   const Native=window[key];if(Native)window[key]=class extends Native{constructor(...args){super(...args);window.__captureContexts.push(this);}};
  }
 });
}
export async function startCapture(page,file){
 await page.waitForFunction(()=>window.__captureContexts?.some(c=>c.__captureBus),null,{timeout:30000});
 await fs.writeFile(file,Buffer.alloc(0));
 if(!targets.has(page))await page.exposeFunction('__writeCaptureChunk',async base64=>{await fs.appendFile(targets.get(page),Buffer.from(base64,'base64'));lastWrites.set(page,Date.now());});
 targets.set(page,file);
 const setup=await page.evaluate(async()=>{
  const canvas=document.getElementById('unity-canvas');
  const contexts=window.__captureContexts.filter(c=>c.__captureBus);
  if(contexts.length!==1)throw new Error('Expected one game audio context, got '+contexts.length);
  const context=contexts[0];await context.resume();
  const stream=canvas.captureStream(30);for(const t of context.__captureBus.stream.getAudioTracks())stream.addTrack(t);
  const mimeType=['video/webm;codecs=vp8,opus','video/webm;codecs=vp9,opus'].find(t=>MediaRecorder.isTypeSupported(t));
  if(!mimeType)throw new Error('No supported audio/video recorder codec');
  const recorder=new MediaRecorder(stream,{mimeType,videoBitsPerSecond:12000000,audioBitsPerSecond:192000});
  let queue=Promise.resolve(),bytes=0,error='';
  recorder.ondataavailable=e=>{if(e.data.size)queue=queue.then(async()=>{
   const base64=await new Promise((resolve,reject)=>{const f=new FileReader();f.onload=()=>resolve(f.result.slice(f.result.indexOf(';base64,')+8));f.onerror=reject;f.readAsDataURL(e.data);});
   await window.__writeCaptureChunk(base64);bytes+=e.data.size;
  });};
  recorder.onerror=e=>{error=e.error?.message||'MediaRecorder error';};
  recorder.start(1000);const started=performance.now();
  window.__captureElapsed=()=> (performance.now()-started)/1000;
  window.__stopCapture=()=>new Promise((resolve,reject)=>{
   recorder.onstop=async()=>{try{await queue;if(error)throw new Error(error);stream.getVideoTracks().forEach(t=>t.stop());resolve({durationSeconds:(performance.now()-started)/1000,bytes,mimeType,width:canvas.width,height:canvas.height,audioTracks:stream.getAudioTracks().length});}catch(e){reject(e);}};
   recorder.stop();
  });
  return {mimeType,width:canvas.width,height:canvas.height,audioTracks:stream.getAudioTracks().length};
 });
 lastWrites.set(page,Date.now());
 const watcher=setInterval(()=>{
  if(page.isClosed()){clearInterval(watcher);return;}
  if(Date.now()-lastWrites.get(page)>45000){
   clearInterval(watcher);console.error('Capture received no audio/video chunks for 45 seconds; closing the unresponsive recorder.');
   void page.context().browser().close().catch(()=>{});
  }
 },5000);
 watchers.set(page,watcher);return setup;
}
export async function stopCapture(page){const result=await page.evaluate(()=>window.__stopCapture());clearInterval(watchers.get(page));const stat=await fs.stat(targets.get(page));if(stat.size!==result.bytes)throw new Error('Recorded byte count mismatch');return result;}
