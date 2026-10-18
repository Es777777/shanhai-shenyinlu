const tracked=new WeakMap();
export function observeArt(page){
  if(tracked.has(page))return;
  const state={pending:new Set(),last:0,urls:new Set()};tracked.set(page,state);
  page.on('framenavigated',frame=>{if(frame===page.mainFrame()){state.pending.clear();state.urls.clear();state.last=Date.now();}});
  page.on('request',r=>{if(r.url().includes('/StreamingAssets/Art/')){state.pending.add(r);state.urls.add(r.url());state.last=Date.now();}});
  for(const name of ['requestfinished','requestfailed'])page.on(name,r=>{if(state.pending.delete(r))state.last=Date.now();});
}
export async function waitArtwork(page){
  observeArt(page);const s=tracked.get(page),deadline=Date.now()+45000;
  while(!s.urls.size||s.pending.size||Date.now()-s.last<250){
    if(Date.now()>deadline)throw new Error('Scene artwork did not finish loading');
    await page.waitForTimeout(70);
  }
}
export function artRequests(page){return [...(tracked.get(page)?.urls||[])];}
