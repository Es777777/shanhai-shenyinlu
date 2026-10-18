"""Original deterministic procedural music and sound design. No external samples."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).parent / '.deps'))
import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Audio'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 24000
rng = np.random.default_rng(20261016)

def save(name, audio):
    peak = max(float(np.max(np.abs(audio))), .001)
    audio = np.tanh(audio / peak * .8) * .72
    with sf.SoundFile(OUT / (name+'.ogg'), 'w', samplerate=RATE, channels=1 if audio.ndim==1 else audio.shape[1], format='OGG', subtype='VORBIS') as stream:
        for start in range(0,len(audio),4096):
            stream.write(audio[start:start+4096])
    print(name, round(len(audio)/RATE,2), flush=True)

def note(freq, seconds, kind='pluck'):
    t = np.arange(int(seconds*RATE))/RATE
    if kind=='pluck':
        y = sum(np.sin(2*np.pi*freq*h*t+.07*h)*np.exp(-t*(1.2+h*.3))/h**1.5 for h in range(1,7))
        y *= np.minimum(1,t/.008)
    else:
        y = (np.sin(2*np.pi*freq*t + .018*np.sin(2*np.pi*4*t)) + .13*np.sin(4*np.pi*freq*t)) * np.sin(np.pi*np.minimum(1,t/seconds))**1.4
    y *= np.minimum(1,(seconds-t)/.05)
    return y

def music(name, root, duration, mood, ending=False):
    n = int(duration*RATE)
    audio = np.zeros((n,2),np.float32)
    scale = [0,2,4,7,9] if mood!='dark' else [0,3,5,7,10]
    melody = [0,2,3,1,4,3,2,1,0,1,3,2,4,2,1,0]
    for index, start in enumerate(np.arange(0,duration,1.5)):
        octave = 12 if index%8 in [4,5] else 0
        freq = root*2**((scale[melody[index%16]]+octave)/12)
        y = note(freq,4.0,'flute' if mood=='mist' and index%3==0 else 'pluck') * (.28 if index%4 else .42)
        at=int(start*RATE)
        pan=.25+.5*((index*7)%11)/10
        for delay, volume in [(0,1),(.28,.23),(.57,.11)]:
            indices=(np.arange(len(y))+at+int(delay*RATE))%n
            if ending:
                valid=np.arange(len(y))+at+int(delay*RATE)<n
                indices=indices[valid]; yd=y[valid]
            else: yd=y
            audio[indices,0] += yd*volume*(1-pan)
            audio[indices,1] += yd*volume*pan
    t=np.arange(n)/RATE
    # A faint continuous fifth gives the loop a calm tonal centre.
    for ch in [0,1]: audio[:,ch]+=.025*np.sin(2*np.pi*round(root*.5*duration)/duration*t)+.014*np.sin(2*np.pi*round(root*.75*duration)/duration*t)
    if ending: audio *= np.minimum(1,(duration-t)/6)[:,None]
    save(name,audio)

for args in [('prologue',146.832,60,'dark'),('ming',164.814,75,'warm'),('mei',146.832,75,'mist'),('fusang',196,90,'warm'),('youdu',130.813,90,'dark'),('buzhou',146.832,80,'dark'),('ending1',196,120,'warm'),('ending2',164.814,120,'mist'),('ending3',174.614,120,'warm')]:
    music(*args,ending=args[0].startswith('ending'))

for name, seconds, frequency in [('confirm',.3,880),('puzzle',.5,660),('lamp',1,523.25),('pact',2,392),('tuopian',.4,220),('brush',.25,190)]:
    t=np.arange(int(seconds*RATE))/RATE
    y=np.sin(2*np.pi*frequency*t)*np.exp(-t*8) + .2*np.sin(2*np.pi*frequency*1.5*t)*np.exp(-t*12)
    if name in ['brush','tuopian']: y=.35*rng.normal(size=len(t))*np.exp(-t*24)+y*.1
    y *= np.minimum(1,t/.005)*np.minimum(1,(seconds-t)/.05)
    save(name,y)
t=np.arange(30*RATE)/RATE
save('tide', .2*np.sin(2*np.pi*55*t)+.13*np.sin(2*np.pi*82.5*t)+.02*rng.normal(size=len(t)))
