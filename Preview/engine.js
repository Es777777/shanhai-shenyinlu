export class StoryEngine {
  constructor(data,profile){this.data=data;this.profile=profile;this.nodes=new Map(data.nodes.map(n=>[n.id,n]));this.newGame()}
  newGame(){this.state={version:1,nodeId:'',route:'',savedAt:'',choiceReply:'',line:0,miniStep:0,mechanicDone:false,playerX:.14,playerY:.65,values:this.data.variables.map(v=>({key:v.key,value:v.initial})),completed:[],visited:[],inspected:[],choices:[],puzzle:[],rubbed:[],matched:[],history:[]};this.enter(this.data.entry)}
  get node(){return this.nodes.get(this.state.nodeId)}
  get dialogueFinished(){return this.state.line>=this.node.lines.length}
  get needsMechanic(){return !!this.node.mechanic&&!this.state.mechanicDone}
  get isEnding(){return ['N911','N912','N913'].includes(this.node.id)}
  get readKey(){return `${this.node.id}:${this.state.line}`}
  get currentLineRead(){return this.profile.readLines.includes(this.readKey)}
  get(key){return this.state.values.find(v=>v.key===key)?.value??0}
  set(key,value){const d=this.data.variables.find(v=>v.key===key);this.state.values.find(v=>v.key===key).value=Math.max(d.min,Math.min(d.max,value))}
  restore(s){if(!s||s.version!==1||!this.nodes.has(s.nodeId)||!Number.isInteger(s.line)||s.line<0||s.line>this.nodes.get(s.nodeId).lines.length)throw Error('存档内容无效');for(const k of ['values','completed','visited','inspected','choices','history','puzzle','rubbed','matched'])if(!Array.isArray(s[k]))throw Error('存档内容不完整');for(const d of this.data.variables)if(s.values.filter(v=>v.key===d.key&&Number.isFinite(v.value)).length!==1)throw Error('存档数值无效');if(s.puzzle.length&&(s.puzzle.length!==6||new Set(s.puzzle).size!==6||s.puzzle.some(v=>v<0||v>5)))throw Error('拼图存档无效');this.state=structuredClone(s);for(const d of this.data.variables)this.set(d.key,this.get(d.key))}
  readLine(){if(this.dialogueFinished)return;const l=this.node.lines[this.state.line];if(!this.currentLineRead)this.profile.readLines.push(this.readKey);this.state.history.push({node:this.node.id,...l});if(this.state.history.length>500)this.state.history.shift();this.state.line++}
  outgoing(){return this.data.edges.filter(e=>e.from===this.node.id).sort((a,b)=>a.priority-b.priority||a.id.localeCompare(b.id))}
  satisfies(e){if(!e.conditions?.length)return true;const check=c=>{const v=this.get(c.key);switch(c.op){case'>=':return v>=c.value;case'<=':return v<=c.value;case'>':return v>c.value;case'<':return v<c.value;case'!=':return v!==c.value;default:return v===c.value}};return e.any?e.conditions.some(check):e.conditions.every(check)}
  choices(){if(!this.dialogueFinished||this.needsMechanic||this.isEnding)return[];return this.node.choices?.length?this.node.choices:this.outgoing().filter(e=>e.kind==='玩家选择'&&this.satisfies(e)).map(e=>({id:e.id,text:e.text,to:e.to,delta:0,toward:false}))}
  finishMechanic(){if(!this.dialogueFinished)throw Error('对白尚未结束');this.state.mechanicDone=true}
  complete(){if(!this.dialogueFinished||this.needsMechanic)throw Error('本幕尚未完成');if(this.state.completed.includes(this.node.id))return;for(const c of this.node.changes)this.set(c.key,c.op==='set'?c.value:this.get(c.key)+c.value);this.state.completed.push(this.node.id);for(const u of this.node.unlocks||[])if(!this.profile.unlocked.includes(u))this.profile.unlocked.push(u);if(this.isEnding&&!this.profile.endings.includes(this.get('ending')))this.profile.endings.push(this.get('ending'))}
  select(id){const c=this.choices().find(c=>c.id===id);if(!c)throw Error('当前选项不可用');this.complete();if(!this.state.choices.some(x=>x.startsWith(this.node.id+':'))){const v=this.get('mcv');this.set('mcv',c.toward?v+Math.sign(50-v)*Math.min(Math.abs(50-v),Math.abs(c.delta)):v+(c.delta||0));this.state.choices.push(this.node.id+':'+c.id);this.state.history.push({node:this.node.id,speaker:'抉择',text:c.text})}if(this.node.id==='N002')this.state.route=c.to==='N101'?'玄曜':'灵曦';if(c.reply)this.state.history.push({node:this.node.id,speaker:'旁白',text:c.reply});this.enter(c.to);this.state.choiceReply=c.reply||''}
  advance(){if(this.choices().length)throw Error('请先选择');this.complete();if(this.isEnding)return null;const edges=this.outgoing().filter(e=>e.kind!=='玩家选择');const edge=edges.find(e=>e.kind!=='兜底'&&this.satisfies(e))||edges.find(e=>e.kind==='兜底');if(!edge)throw Error('没有满足条件的后续剧情');this.enter(edge.to);return edge.to}
  enter(id){Object.assign(this.state,{nodeId:id,line:0,miniStep:0,mechanicDone:false,playerX:.14,playerY:.65,inspected:[],puzzle:[],rubbed:[],matched:[],choiceReply:''});if(!this.state.visited.includes(id))this.state.visited.push(id)}
}
export function defaultProfile(){return{version:1,unlocked:[],readLines:[],endings:[],checkpoints:[],musicVolume:.45,soundVolume:.65,textSpeed:30,autoDelay:2.2,skipUnread:false,reducedMotion:false}}
export function judgeStroke(drawn,guide,tolerance){
 const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
 const length=p=>p.slice(1).reduce((sum,b,i)=>sum+distance(p[i],b),0);
 const segment=(p,a,b)=>{const dx=b.x-a.x,dy=b.y-a.y,d=dx*dx+dy*dy,t=d?Math.max(0,Math.min(1,((p.x-a.x)*dx+(p.y-a.y)*dy)/d)):0;return distance(p,{x:a.x+t*dx,y:a.y+t*dy})};
 if(drawn.length<2||guide.length<2||distance(drawn[0],guide[0])>tolerance||distance(drawn.at(-1),guide.at(-1))>tolerance)return false;
 if(length(drawn)<length(guide)*.7||length(drawn)>length(guide)*1.65)return false;
 for(const[from,to]of[[drawn,guide],[guide,drawn]])for(let i=1;i<from.length;i++){const count=Math.max(1,Math.floor(distance(from[i-1],from[i])/(tolerance*.4)));for(let j=0;j<=count;j++){const t=j/count,p={x:from[i-1].x+(from[i].x-from[i-1].x)*t,y:from[i-1].y+(from[i].y-from[i-1].y)*t};if(Math.min(...to.slice(1).map((b,k)=>segment(p,to[k],b)))>tolerance)return false}}
 return true;
}
