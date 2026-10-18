"""Encode an actual, successful Unity capture and provide chapters plus playback QA."""
from pathlib import Path
import json, subprocess, hashlib, html, math, os
import av
import numpy as np
import imageio_ffmpeg

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/os.environ.get('SHANHAI_DEMO_DIR','Builds/Demo-0.6.0')
FFMPEG=str(Path('F:/Shotcut/ffmpeg.exe')) if Path('F:/Shotcut/ffmpeg.exe').exists() else imageio_ffmpeg.get_ffmpeg_exe()
FFPROBE=Path('F:/Shotcut/ffprobe.exe')
VERSION=os.environ.get('SHANHAI_DEMO_VERSION','0.6.0')
DEST=ROOT.parent/f'山海神隐录_完整游玩Demo_{VERSION}.mp4'
ORDER=['xuanyao','lingxi-main','lingxi-tail','balanced']
NAMES={'xuanyao':'玄曜主线','lingxi-main':'灵曦主线','lingxi-tail':'灵曦主线','balanced':'调和结局回放'}
NAMES['lingxi']='灵曦主线'

def digest(file):
    with file.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def run(args,log):
    with (WORK/log).open('w',encoding='utf-8') as f:
        subprocess.run([FFMPEG,'-hide_banner','-y',*map(str,args)],stdout=f,stderr=f,check=True)

def timestamp(seconds):
    n=int(seconds);return f'{n//3600:02d}:{n//60%60:02d}:{n%60:02d}'

def probe(file):
    return json.loads(subprocess.check_output([str(FFPROBE),'-v','error','-show_format','-show_streams','-show_chapters','-of','json',str(file)],encoding='utf-8'))

def packet_stats(file):
    stats={};container=av.open(str(file))
    for stream in container.streams:
        stats[stream.type]={'packets':0,'start':None,'end':0,'maxGap':0}
    previous={}
    for packet in container.demux():
        if packet.pts is None:continue
        kind=packet.stream.type;s=stats[kind];t=float(packet.pts*packet.time_base)
        s['packets']+=1;s['start']=t if s['start'] is None else min(s['start'],t)
        s['end']=max(s['end'],t+float((packet.duration or 0)*packet.time_base))
        if kind in previous:s['maxGap']=max(s['maxGap'],t-previous[kind])
        previous[kind]=t
    container.close();return stats

def audio_stats(file):
    container=av.open(str(file));squares=0.;samples=0;peak=0.
    for frame in container.decode(audio=0):
        raw=frame.to_ndarray();data=raw.astype(np.float64)
        if not np.issubdtype(raw.dtype,np.floating):
            data/=max(abs(np.iinfo(raw.dtype).min),np.iinfo(raw.dtype).max)
        squares+=float(np.sum(data*data));samples+=data.size;peak=max(peak,float(np.max(np.abs(data))))
    container.close();rms=math.sqrt(squares/max(samples,1))
    assert rms>0.0001,'Game audio must not be silent: '+str(file)
    return {'samples':samples,'rmsDb':20*math.log10(rms),'peakDb':20*math.log10(max(peak,1e-12))}

def main():
    report=json.loads((WORK/'results.json').read_text(encoding='utf-8'))
    assert report['status']=='passed' and len(report['segments'])==3,'Complete real gameplay capture required'
    recovery={s['key'] for s in report['segments']}=={'xuanyao','lingxi-tail','balanced'}
    order=ORDER if recovery else ['xuanyao','lingxi','balanced']
    if recovery:
        recovered=json.loads((WORK/'lingxi-main-capture.json').read_text(encoding='utf-8'))
        assert recovered['status']=='failed' and not recovered['errors']
        assert recovered['routes'][-1]['nodes'][-1]=='N912'
        # The recorder stopped after the last N912 dialogue. A fresh real-input replay
        # captured closing the scroll and the collection screen; preserve the failed log.
        assert report['routes'][-1]['endings']==['N912']
    else:
        assert {s['key'] for s in report['segments']}==set(order)
    chapter_list=[];sources=[];offset=0.0
    for key in order:
        source=WORK/(key+'.webm');stats=packet_stats(source)
        assert stats['audio']['packets']>100 and stats['video']['packets']>100,key
        duration=stats['audio']['end']
        # MediaRecorder can write audio first in one segment and video first in
        # another. Concat matches stream indices, so normalize without re-encoding.
        normalized=WORK/(key+'-ordered.mkv')
        run(['-i',source,'-map','0:v:0','-map','0:a:0','-c','copy','-avoid_negative_ts','disabled',normalized],key+'-remux.log')
        ordered=probe(normalized)['streams']
        assert [s['codec_type'] for s in ordered]==['video','audio']
        sources.append({'key':key,'file':source.name,'concatFile':normalized.name,'durationSeconds':duration,'streams':stats,'audioLevel':audio_stats(source),'sha256':digest(source)})
        marks=[m for m in (recovered if key=='lingxi-main' else report)['bookmarks'] if m['segment']==('lingxi' if key=='lingxi-main' else key)]
        for mark in marks:
            if mark['seconds']<duration:
                chapter_list.append({'seconds':offset+mark['seconds'],'title':NAMES[key]+' · '+mark['title'],'node':mark['node']})
        offset+=duration
    chapter_list[0]['seconds']=0
    concat=WORK/'source-concat.txt'
    concat.write_text(''.join(f"file '{s['concatFile']}'\nduration {s['durationSeconds']:.6f}\n" for s in sources),encoding='utf-8')
    meta=[';FFMETADATA1',f'title=山海神隐录 · 完整游玩 Demo {VERSION}','artist=ShanHai Studio','comment=正式团结引擎 H5 实录；两条主线、八处调查、五类互动与三个结局；原游戏配乐与音效。']
    for i,chapter in enumerate(chapter_list):
        end=chapter_list[i+1]['seconds'] if i+1<len(chapter_list) else offset
        title=chapter['title'].replace('\\','\\\\').replace('=','\\=').replace(';','\\;').replace('#','\\#')
        meta.extend(['[CHAPTER]','TIMEBASE=1/1000',f"START={round(chapter['seconds']*1000)}",f'END={round(end*1000)}','title='+title])
    metadata=WORK/'chapters.ffmeta';metadata.write_text('\n'.join(meta)+'\n',encoding='utf-8')
    print(f'Encoding {offset/60:.1f} minutes; {len(chapter_list)} chapters',flush=True)
    hardware=os.environ.get('SHANHAI_CPU_ENCODE')!='1' and subprocess.run([FFMPEG,'-hide_banner','-loglevel','error','-f','lavfi','-i','color=c=black:s=1600x900:r=30:d=1','-c:v','h264_nvenc','-preset','p4','-cq','19','-f','null','NUL'],capture_output=True).returncode==0
    cpu_threads=os.environ.get('SHANHAI_ENCODE_THREADS','4')
    encoder=['-c:v','h264_nvenc','-preset','p4','-rc','vbr','-cq','19','-b:v','0'] if hardware else ['-c:v','libx264','-preset','veryfast','-crf','19','-threads',cpu_threads]
    print('Encoder: '+('h264_nvenc' if hardware else 'libx264'),flush=True)
    def encode(options):
        run(['-threads','2','-f','concat','-safe','0','-i',concat,'-i',metadata,'-map','0:v:0','-map','0:a:0','-map_metadata','1','-map_chapters','1','-vf','fps=30',*options,'-pix_fmt','yuv420p','-af','aresample=async=1:first_pts=0','-c:a','aac','-b:a','192k','-t',f'{offset:.6f}','-movflags','+faststart',DEST],'encode.log')
        encoded_info=probe(DEST)
        assert abs(float(encoded_info['format']['duration'])-offset)<1,'Incomplete encoder output'
        for stream in encoded_info['streams']:
            if stream['codec_type'] in ['video','audio']:assert abs(float(stream['duration'])-offset)<1,'Incomplete media stream'
    try:
        encode(encoder)
    except (subprocess.CalledProcessError,AssertionError):
        if not hardware:raise
        (WORK/'hardware-encode-failure.log').write_bytes((WORK/'encode.log').read_bytes())
        print('Hardware encoder failed; retrying with libx264.',flush=True)
        encode(['-c:v','libx264','-preset','veryfast','-crf','19','-threads',cpu_threads])
    info=probe(DEST);video=next(s for s in info['streams'] if s['codec_type']=='video');audio=next(s for s in info['streams'] if s['codec_type']=='audio')
    assert video['codec_name']=='h264' and audio['codec_name']=='aac'
    assert video['width']==1600 and video['height']==900 and video['avg_frame_rate']=='30/1'
    assert abs(float(info['format']['duration'])-offset)<1
    assert len(info['chapters'])==len(chapter_list)
    print('Checking full decode and original audio level...',flush=True)
    run(['-v','error','-i',DEST,'-map','0:v:0','-map','0:a:0','-f','null','NUL'],'decode.log')
    assert not (WORK/'decode.log').read_text(encoding='utf-8').strip(),'Decoder reported errors'
    run(['-i',DEST,'-vn','-af','volumedetect','-f','null','NUL'],'audio-level.log')
    # Extract real movie frames for inspection; these are not substitutes for the recording.
    frames=WORK/'review';frames.mkdir(exist_ok=True)
    for index,chapter in enumerate(chapter_list):
        run(['-ss',str(min(chapter['seconds']+2,offset-1)),'-i',DEST,'-frames:v','1','-update','1',frames/f'{index:02d}.png'],'extract.log')
    lines=[f'山海神隐录 · 完整游玩 Demo {VERSION}',f'时长：{timestamp(offset)}；1600×900；H.264 + AAC；游戏原声。','录制自本机正式 H5，以实际鼠标操作完成；未注入存档或剧情状态。','顺序：玄曜主线 → 灵曦主线 → 章节回放调和结局。两条互斥路线分别从新旅程开始。','包含结局剧透。第三结局使用游戏内章节回放，无需重复相同的前序剧情。','灵曦路线结局对白后录制浏览器异常，收卷与图鉴通过重新实际游玩补录并拼接；无加速。','对白按阅读节奏停留。视频没有人物语音，声音为游戏配乐与音效。','']
    lines.extend(timestamp(c['seconds'])+'  '+c['title'] for c in chapter_list)
    if not recovery:lines=[line for line in lines if not line.startswith('灵曦路线结局对白后')]
    (ROOT.parent/f'山海神隐录_Demo章节_{VERSION}.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8-sig')
    buttons=''.join(f'<button data-time="{c["seconds"]:.3f}"><time>{timestamp(c["seconds"])}</time>{html.escape(c["title"])}</button>' for c in chapter_list)
    (ROOT.parent/f'山海神隐录_Demo播放_{VERSION}.html').write_text('<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>山海神隐录 · 完整游玩 Demo</title><style>body{margin:0;background:#0a191d;color:#e8ddc6;font:16px/1.6 system-ui}main{max-width:1440px;margin:auto;padding:24px}h1{font-size:26px;font-weight:500}p{color:#b9c1b7}video{width:100%;background:#000}nav{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:8px;margin-top:24px}button{text-align:left;background:#142b30;color:#ecdfc1;border:1px solid #47534b;padding:12px;cursor:pointer}button:hover{background:#294247}time{color:#c5a76c;margin-right:14px}</style><main><h1>山海神隐录 · 完整游玩 Demo '+VERSION+'</h1><p>两条主线 · 八处调查 · 五类互动 · 三个结局。正式 H5 实录，保留游戏配乐与音效。含结局剧透。</p><video controls preload="metadata" src="'+html.escape(DEST.name)+'"></video><p>点击章节跳转；视频自带章节标记。总时长 '+timestamp(offset)+'。</p><nav>'+buttons+'</nav></main><script>const v=document.querySelector("video");document.querySelectorAll("button").forEach(b=>b.onclick=()=>{v.currentTime=Number(b.dataset.time);v.play();});</script></html>',encoding='utf-8')
    result={'status':'awaiting_visual_review','version':VERSION,'video':DEST.name,'durationSeconds':float(info['format']['duration']),'width':1600,'height':900,'fps':30,'videoCodec':'h264','audioCodec':'aac','bytes':DEST.stat().st_size,'sha256':digest(DEST),'fullDecode':'passed','sources':sources,'chapters':chapter_list,'capture':'Actual Unity canvas and Web Audio output; real mouse input; saves observed read-only; no state injection','editing':'Four captured segments joined in story order without speed-up. Lingxi main recording ends after the final N912 dialogue; closing the scroll and collection screens were recaptured by a fresh real-input replay after a recording-browser failure. Original failure report retained as lingxi-main-capture.json. CFR conversion repeats frames when source render is unchanged.'}
    if not recovery:result['editing']='Three continuous recorded segments joined in story order without speed-up; CFR conversion repeats frames when source render is unchanged.'
    result['buildManifestSha256']=report.get('buildManifestSha256','')
    (WORK/'video-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(DEST,flush=True)

if __name__=='__main__':main()
