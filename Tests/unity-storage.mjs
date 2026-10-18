// Read-only observation of the game's real IndexedDB saves; never inject state.
export async function readSavedData(page,key='slot0'){
  return page.evaluate(async key=>{
    const names=await indexedDB.databases();
    if(!names.some(d=>d.name==='/idbfs'))return null;
    const db=await new Promise((resolve,reject)=>{const r=indexedDB.open('/idbfs');r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);});
    try{
      if(!db.objectStoreNames.contains('FILE_DATA'))return null;
      const text=await new Promise((resolve,reject)=>{
        const r=db.transaction('FILE_DATA','readonly').objectStore('FILE_DATA').openCursor();
        r.onerror=()=>reject(r.error);r.onsuccess=()=>{
          const c=r.result;if(!c){resolve('');return;}
          if(String(c.key).endsWith('/PlayerPrefs')){window.__qaPrefsBytes=Array.from(c.value.contents);resolve(new TextDecoder().decode(c.value.contents));}
          else c.continue();
        };
      });
      const name='shanhai.v1.'+key;let keyAt=text.indexOf(name);
      while(keyAt>=0&&text[keyAt+name.length]==='.')keyAt=text.indexOf(name,keyAt+name.length);
      if(keyAt<0)return null;
      // Binary length bytes can themselves equal '{'; find the JSON header.
      const start=text.indexOf('{"version":',keyAt);if(start<0)return null;
      let depth=0,quoted=false,escaped=false;
      for(let i=start;i<text.length;i++){
        const c=text[i];
        if(quoted){if(escaped)escaped=false;else if(c==='\\')escaped=true;else if(c==='"')quoted=false;continue;}
        if(c==='"')quoted=true;
        else if(c==='{'||c==='[')depth++;
        else if(c==='}'||c===']'){if(--depth===0)return JSON.parse(text.slice(start,i+1));}
      }
      return null;
    }finally{db.close();}
  },key);
}

export async function waitSaved(page,predicate,key='slot0',timeout=12000){
  const end=Date.now()+timeout;let state;
  while(Date.now()<end){
    state=await readSavedData(page,key);
    if(state&&predicate(state))return state;
    await page.waitForTimeout(120);
  }
  const summary=state?{nodeId:state.nodeId,line:state.line,miniStep:state.miniStep,mechanicDone:state.mechanicDone}:null;
  throw new Error(`Saved-state condition timed out (${key}): ${JSON.stringify(summary)}`);
}
