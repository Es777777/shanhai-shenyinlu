import hashlib
import http.client
import json
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import unittest
import urllib.request

ROOT=Path(__file__).resolve().parents[1]
EXE=ROOT/'Builds/Launcher/ShanHai.exe'

def manifest(root):
    data={'version':'fixture','files':[{'path':p.relative_to(root).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(root.rglob('*')) if p.is_file() and p.name!='build-manifest.json']}
    (root/'build-manifest.json').write_text(json.dumps(data),encoding='utf-8')

class LauncherTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)/'game';self.root.mkdir()
        for name,content in {'index.html':b'<title>ShanHai</title>','Build/game.wasm':b'wasm','Build/game.data':b'data','StreamingAssets/Art/scene.jpg':b'painted-scene'}.items():
            path=self.root/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(content)
        manifest(self.root)
        with socket.socket() as s:s.bind(('127.0.0.1',0));self.port=s.getsockname()[1]
        self.server=None
    def tearDown(self):
        if self.server:self.server.terminate();self.server.wait(timeout=5)
        self.temp.cleanup()
    def run_launcher(self,*args):
        return subprocess.run([str(EXE),'--root',str(self.root),'--port',str(self.port),'--no-browser',*args],capture_output=True,encoding='utf-8',errors='replace',timeout=15)
    def serve(self):
        self.server=subprocess.Popen([str(EXE),'--root',str(self.root),'--port',str(self.port),'--no-browser'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        until=time.monotonic()+10
        while time.monotonic()<until:
            try:
                with urllib.request.urlopen(f'http://127.0.0.1:{self.port}/',timeout=1) as r:self.assertEqual(r.status,200)
                return
            except OSError:time.sleep(.1)
        self.fail('Launcher server did not start')
    def test_complete_package_verifies(self):self.assertEqual(self.run_launcher('--verify').returncode,0)
    def test_corrupt_scene_rejected(self):
        (self.root/'StreamingAssets/Art/scene.jpg').write_bytes(b'corrupt')
        self.assertNotEqual(self.run_launcher('--verify').returncode,0)
    def test_missing_file_rejected(self):
        (self.root/'Build/game.wasm').unlink()
        self.assertNotEqual(self.run_launcher('--verify').returncode,0)
    def test_identical_package_reuses_address(self):
        self.serve();result=self.run_launcher();self.assertEqual(result.returncode,0);self.assertIn('already running',result.stdout)
    def test_same_version_different_streamed_art_rejected(self):
        self.serve();other=self.root.parent/'other'
        import shutil
        shutil.copytree(self.root,other)
        (other/'StreamingAssets/Art/scene.jpg').write_bytes(b'other-scene');manifest(other)
        original=self.root;self.root=other
        try:self.assertNotEqual(self.run_launcher().returncode,0)
        finally:self.root=original
    def test_outside_directory_is_not_served(self):
        self.serve();(self.root.parent/'secret.txt').write_text('outside fixture')
        connection=http.client.HTTPConnection('127.0.0.1',self.port)
        connection.request('GET','/../secret.txt');response=connection.getresponse();self.assertEqual(response.status,403);connection.close()

if __name__=='__main__':unittest.main()
