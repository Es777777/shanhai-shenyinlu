"""Serve a Unity WebGL build or the browser preview on localhost."""
import argparse
import functools
import http.server
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map={**http.server.SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.data':'application/octet-stream','.unityweb':'application/octet-stream','.json':'application/json','.ogg':'audio/ogg'}
    def end_headers(self):
        self.send_header('Cache-Control','no-cache')
        super().end_headers()
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8765);parser.add_argument('--directory',default='Builds/H5');args=parser.parse_args()
    directory=(ROOT/args.directory).resolve()
    if not (directory/'index.html').exists():raise SystemExit('Missing index.html: '+str(directory))
    server=http.server.ThreadingHTTPServer(('127.0.0.1',args.port),functools.partial(Handler,directory=str(directory)))
    print('Open http://127.0.0.1:'+str(args.port),flush=True);server.serve_forever()
