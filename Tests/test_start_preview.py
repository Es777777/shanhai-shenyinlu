"""Real HTTP regressions for launching the downloaded package beside an old server."""
from pathlib import Path
import contextlib,functools,http.server,io,sys,tempfile,threading,unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Tools'))
import start_preview

class QuietHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self,*args):pass

class LaunchTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        base=Path(self.temp.name);self.local=base/'new';self.served=base/'running'
        for directory in [self.local,self.served]:
            (directory/'Build').mkdir(parents=True)
            (directory/'index.html').write_text('<title>山海神隐录</title>version=0.4.1',encoding='utf-8')
            (directory/'Build/H5.wasm.unityweb').write_bytes(b'wasm-fixture')
            (directory/'Build/H5.data.unityweb').write_bytes(b'new-art-and-scenes')
        self.server=http.server.ThreadingHTTPServer(('127.0.0.1',0),functools.partial(QuietHandler,directory=str(self.served)))
        self.thread=threading.Thread(target=self.server.serve_forever,daemon=True);self.thread.start()
        self.port=self.server.server_address[1]

    def tearDown(self):
        self.server.shutdown();self.server.server_close();self.thread.join();self.temp.cleanup()

    def launch(self):
        with patch.object(start_preview,'ROOT',self.local),patch.object(sys,'argv',['start_preview.py','--h5','--port',str(self.port),'--no-browser']),contextlib.redirect_stdout(io.StringIO()):
            start_preview.main()

    def test_identical_package_reuses_same_save_origin(self):
        self.launch()

    def test_same_title_older_version_is_not_opened(self):
        (self.served/'index.html').write_text('<title>山海神隐录</title>version=0.4.0',encoding='utf-8')
        with self.assertRaisesRegex(SystemExit,'Another game build'):self.launch()

    def test_same_version_different_assets_are_not_opened(self):
        (self.served/'Build/H5.data.unityweb').write_bytes(b'old-placeholder-art')
        with self.assertRaisesRegex(SystemExit,'Another game build'):self.launch()

    def test_incomplete_local_package_does_not_reuse_another_copy(self):
        (self.local/'Build/H5.wasm.unityweb').unlink()
        with self.assertRaisesRegex(SystemExit,'Game files are missing'):self.launch()

if __name__=='__main__':unittest.main()
