"""Created 2026-11-10: bit preservation and failure-before-deletion for capture storage."""
import gzip, importlib.util, io, json, struct, tempfile, unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('trace_store',Path(__file__).resolve().parents[1]/'dotnet-ci/retained_trace_store.py')
store=importlib.util.module_from_spec(spec);spec.loader.exec_module(store)
class TraceStoreTests(unittest.TestCase):
 def test_bit_patterns_and_block_boundaries_match_with_both_decoders(self):
  numpy=store._numpy
  try:
   for fast in [False,True]:
    store._numpy=numpy if fast else None
    for count in [1,1023,1024,1025]:
     raw=struct.pack('<6I',0,0x80000000,0x7f800000,0xff800000,0x7fc00001,0xffffffff)*count
     encoded=store.WordRecordCodecReader(io.BytesIO(raw),24).read()
     self.assertEqual(raw,store.WordRecordCodecReader(io.BytesIO(encoded),24,decode=True).read())
  finally:store._numpy=numpy
 def test_roundtrip_retains_raw_bytes_and_empty_unwritten_stream(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d);raw=bytes(range(244))*1025
   with gzip.open(p/'trajectory.bin.gz','wb') as f:f.write(raw)
   (p/'aligned.bin.gz').write_bytes(b'')
   with gzip.open(p/'ledger.bin.gz','wb') as f:f.write(b'ordered-events'*123)
   store.pack(p,delete_originals=True)
   self.assertFalse((p/'trajectory.bin.gz').exists())
   with store.open_trace(p,'trajectory.bin.gz') as f:self.assertEqual(raw,f.read())
   with store.open_trace(p,'aligned.bin.gz') as f:self.assertEqual(b'',f.read())
   with store.open_trace(p,'ledger.bin.gz') as f:self.assertEqual(b'ordered-events'*123,f.read())
   store.verify_store(p)
 def test_corruption_is_rejected_before_originals_are_deleted(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)
   with gzip.open(p/'ledger.bin.gz','wb') as f:f.write(b'authoritative-event-record')
   store.pack(p)
   archive=p/store.ARCHIVE;raw=bytearray(archive.read_bytes());raw[-1]^=1;archive.write_bytes(raw)
   with self.assertRaises(ValueError):store.pack(p,delete_originals=True)
   self.assertTrue((p/'ledger.bin.gz').exists())
if __name__=='__main__':unittest.main()
