"""Synthesize distinct, original material feedback; no external sound samples."""
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).parent/'.deps'))
import numpy as np
import soundfile as sf

rate=24000
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio'
rng=np.random.default_rng(20261018)
for name,duration in [('stone',.75),('sand',.65),('wood',.7),('paper',.42),('reveal',2.2),('error',.32)]:
    t=np.arange(int(duration*rate))/rate
    noise=rng.normal(0,1,len(t))
    smooth=np.convolve(noise,np.ones(15)/15,mode='same')
    if name=='stone':
        y=.3*smooth*np.exp(-2*t)+sum(np.sin(2*np.pi*f*t)*np.exp(-t*d)*v for f,d,v in [(94,8,.5),(173,12,.22),(281,18,.1)])
    elif name=='sand':y=(noise-smooth)*.13*np.sin(np.pi*t/duration)**2+smooth*.3
    elif name=='wood':y=.33*np.sin(2*np.pi*(115*t+22*t*t))*np.exp(-t*5)+.16*np.sin(2*np.pi*307*t)*np.exp(-t*10)+smooth*.14
    elif name=='paper':y=(noise-smooth)*.17*np.sin(np.pi*t/duration)**3*(.55+.45*np.sin(t*77)**2)
    elif name=='reveal':y=sum(np.sin(2*np.pi*f*t)*v*np.exp(-t*1.8) for f,v in [(392,.35),(587.33,.2),(783.99,.15)])
    else:y=.2*np.sin(2*np.pi*(310*t-90*t*t))*np.exp(-t*12)
    y*=np.minimum(1,t/.015)*np.minimum(1,(duration-t)/.06)
    sf.write(out/(name+'.ogg'),np.clip(y,-.8,.8),rate,format='OGG',subtype='VORBIS')
    print(name,len(y)/rate)
