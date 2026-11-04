"""Created 2026-11-04: reject incomplete event capture and nonterminal divergence."""
import base64,struct,tempfile,unittest
from pathlib import Path
from analyze import verify_published_ledger,first_difference

class EvidenceChecks(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        self.path=self.root/'ledger.bin'
        self.raw=[bytes([ordinal,1,0,0])+struct.pack('<IHH',1,10,draw) for ordinal,draw in [(7,1),(1,0)]]
        self.path.write_bytes(struct.pack('<i',2)+b''.join(bytes([raw[0]])+struct.pack('<i',len(raw))+raw for raw in self.raw))
        self.rows=[dict(frame=1,ordinal=raw[0],raw=base64.b64encode(raw).decode()) for raw in self.raw]
    def tearDown(self):self.temp.cleanup()
    def test_publish_order_may_differ_from_canonical_order(self):
        self.assertEqual(verify_published_ledger(self.rows[::-1],self.path,1),2)
    def test_missing_publish_is_rejected(self):
        with self.assertRaisesRegex(AssertionError,'disagree'):verify_published_ledger(self.rows[:1],self.path,1)
    def test_duplicate_publish_is_rejected(self):
        with self.assertRaisesRegex(AssertionError,'disagree'):verify_published_ledger(self.rows+self.rows[:1],self.path,1)
    def test_unaccounted_ledger_bytes_are_rejected(self):
        self.path.write_bytes(self.path.read_bytes()+b'ignored')
        with self.assertRaisesRegex(AssertionError,'trailing'):verify_published_ledger(self.rows,self.path,1)
    def test_nonterminal_digest_difference_is_detected(self):
        a,b=self.root/'plain',self.root/'observed';a.write_bytes(bytes(96));other=bytearray(96);other[33]=1;b.write_bytes(other)
        self.assertEqual(first_difference(a,b,32),dict(byteOffset=33,frame=2))
        self.assertEqual(a.read_bytes()[-32:],b.read_bytes()[-32:])

if __name__=='__main__':unittest.main()
