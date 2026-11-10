"""Created 2026-11-10: losslessly pack completed gzip traces into verified raw tar.xz.

This changes compression only. Raw stream bytes/hashes are verified before removing
any as-run gzip container. Only call pack() after the producing process exits 0.
"""
import argparse, contextlib, gzip, hashlib, io, json, os, struct, tarfile
from pathlib import Path
ARCHIVE='traces.tar.xz'
INDEX='trace-index.json'
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
class HashingReader:
 def __init__(self,stream):self.stream=stream;self.digest=hashlib.sha256();self.count=0
 def read(self,size=-1):
  data=self.stream.read(size);self.digest.update(data);self.count+=len(data);return data
 def finish(self):
  while self.read(1024*1024):pass
  return {'bytes':self.count,'sha256':self.digest.hexdigest()}
# [FIXED] Byte widths of the frozen runner's per-frame/per-AI records.
RECORD_WIDTHS={'trajectory.bin':244,'geometry.bin':188,'team-possession.bin':8}
class RecordCodecReader:
 def __init__(self,stream,width,decode=False,records=1024):
  self.stream=stream;self.width=width;self.decode=decode;self.records=records;self.previous=bytes(width);self.buffer=b'';self.offset=0;self.ended=False
 def block(self,data):
  width=self.width;size=len(data);rows=size//width
  if size%width:raise ValueError('Partial fixed-width trace record')
  mask=(1<<(size*8))-1;step=width*8;previous=int.from_bytes(self.previous,'little')
  if self.decode:
   raw=bytearray(size)
   for col in range(width):raw[col::width]=data[col*rows:(col+1)*rows]
   value=(int.from_bytes(raw,'little')+previous)&mask
   shift=step
   while shift<size*8:value=(value+(value<<shift))&mask;shift*=2
   result=value.to_bytes(size,'little');self.previous=result[-width:];return result
  value=int.from_bytes(data,'little');delta=((value-(value<<step)-previous)&mask).to_bytes(size,'little')
  self.previous=data[-width:]
  return b''.join(delta[col::width] for col in range(width))
 def read(self,size=-1):
  if size<0:
   parts=[]
   while block:=self.read(1024*1024):parts.append(block)
   return b''.join(parts)
  result=[];remaining=size
  while remaining:
   if self.offset==len(self.buffer):
    if self.ended:break
    block=self.stream.read(self.width*self.records)
    if not block:self.ended=True;break
    self.buffer=self.block(block);self.offset=0
   count=min(remaining,len(self.buffer)-self.offset);result.append(self.buffer[self.offset:self.offset+count]);self.offset+=count;remaining-=count
  return b''.join(result)
try:
 import numpy as _numpy
except ImportError:
 _numpy=None
class WordRecordCodecReader(RecordCodecReader):
 def block(self,data):
  width=self.width;size=len(data);rows=size//width
  if size%width or width%4:raise ValueError('Partial fixed-width word record')
  if self.decode:
   delta=bytearray(size)
   for col in range(width):delta[col::width]=data[col*rows:(col+1)*rows]
   data=bytes(delta)
  if _numpy is not None:
   words=_numpy.frombuffer(data,dtype='<u4').reshape(rows,width//4)
   previous=_numpy.frombuffer(self.previous,dtype='<u4')
   if self.decode:
    result=_numpy.cumsum(words,axis=0,dtype=_numpy.uint32);result+=previous
    raw=result.astype('<u4',copy=False).tobytes();self.previous=raw[-width:];return raw
   result=words.copy();result[1:]=words[1:]-words[:-1];result[0]=words[0]-previous
   delta=result.astype('<u4',copy=False).tobytes()
  else:
   words=struct.unpack('<'+str(size//4)+'I',data);previous=list(struct.unpack('<'+str(width//4)+'I',self.previous));result=[]
   for i,value in enumerate(words):
    col=i%(width//4)
    updated=(value+previous[col])&0xffffffff if self.decode else (value-previous[col])&0xffffffff
    result.append(updated);previous[col]=updated if self.decode else value
   delta=struct.pack('<'+str(len(result))+'I',*result)
   if self.decode:self.previous=delta[-width:];return delta
  self.previous=data[-width:]
  return b''.join(delta[col::width] for col in range(width))
def decoded(source,entry):
 codec=entry.get('codec',{'kind':'raw'})
 if codec['kind']=='raw':return source
 if codec['kind']=='delta-shuffle-le-v1':return RecordCodecReader(source,codec['recordBytes'],decode=True,records=codec['blockRecords'])
 if codec['kind']=='word-delta-shuffle-le-v1':return WordRecordCodecReader(source,codec['recordBytes'],decode=True,records=codec['blockRecords'])
 raise ValueError('Unknown trace codec')
def verify_archive(archive,index):
 if sha(archive)!=index['archiveSha256']:raise ValueError('Retained archive SHA-256 mismatch')
 with tarfile.open(archive,'r:xz') as tar:
  members=tar.getmembers()
  if len(members)!=len(index['streams']) or {m.name for m in members}!=set(index['streams']):raise ValueError('Unexpected retained members')
  for m in members:
   if not m.isfile():raise ValueError('Retained trace must be a regular file')
   expected=index['streams'][m.name]
   with tar.extractfile(m) as f:actual=HashingReader(decoded(f,expected)).finish()
   if any(actual[k]!=expected[k] for k in ['bytes','sha256']):raise ValueError('Retained raw stream differs: '+m.name)
 return index
def verify_store(folder):
 folder=Path(folder);index=json.loads((folder/INDEX).read_text());return verify_archive(folder/ARCHIVE,index)
@contextlib.contextmanager
def open_trace(folder,name):
 folder=Path(folder);direct=folder/name
 if direct.exists():
  with gzip.open(direct,'rb') as f:yield f
 else:
  index=json.loads((folder/INDEX).read_text());raw=name.removesuffix('.gz');expected=index['streams'][raw]
  with tarfile.open(folder/ARCHIVE,'r:xz') as tar:
   with tar.extractfile(raw) as source:
    reader=HashingReader(decoded(source,expected));yield reader;actual=reader.finish()
    if any(actual[k]!=expected[k] for k in ['bytes','sha256']):raise ValueError('Retained raw stream differs: '+raw)
def pack(folder,delete_originals=False,preset=1):
 folder=Path(folder);archive=folder/ARCHIVE;index_path=folder/INDEX
 previous=verify_store(folder) if archive.exists() and index_path.exists() else None
 if previous and previous['format']=='record-tar-xz-v2':index=previous
 else:
  paths=sorted(folder.glob('*.bin.gz'))
  names=sorted(previous['streams']) if previous else [p.name.removesuffix('.gz') for p in paths]
  if not names:raise ValueError('No completed traces in '+str(folder))
  temporary=folder/(ARCHIVE+'.part');entries={}
  with tarfile.open(temporary,'w:xz',preset=preset) as tar:
   for name in names:
    path=folder/(name+'.gz')
    if previous:
     entry=dict(previous['streams'][name]);size=entry['bytes'];context=open_trace(folder,name+'.gz')
    else:
     if path.stat().st_size==0:size=0
     else:
      with path.open('rb') as f:f.seek(-4,2);size=struct.unpack('<I',f.read(4))[0]
     entry={'bytes':size,'asRunGzipSha256':sha(path),'asRunGzipBytes':path.stat().st_size};context=gzip.open(path,'rb')
    width=RECORD_WIDTHS.get(name)
    codec={'kind':'word-delta-shuffle-le-v1','recordBytes':width,'blockRecords':1024} if width and size%width==0 else {'kind':'raw'}
    entry['codec']=codec
    info=tarfile.TarInfo(name);info.size=size;info.mtime=0;info.mode=0o644
    with context as f:
     raw=HashingReader(f);encoded=WordRecordCodecReader(raw,width) if codec['kind']!='raw' else raw
     tar.addfile(info,encoded)
     if encoded.read(1):raise ValueError('Trace exceeds its size footer')
     actual=raw.finish()
     if actual['bytes']!=size:raise ValueError('Trace size mismatch')
     if previous and actual['sha256']!=entry['sha256']:raise ValueError('Repack changed raw bytes')
     entry['sha256']=actual['sha256'];entries[name]=entry
  index={'format':'record-tar-xz-v2','archiveSha256':sha(temporary),'streams':entries,'asRunGzipContainersRetained':False}
  verify_archive(temporary,index)
  os.replace(temporary,archive)
  temp_index=folder/(INDEX+'.part');temp_index.write_text(json.dumps(index,indent=2)+'\n');os.replace(temp_index,index_path)
 if delete_originals:
  for name,entry in index['streams'].items():
   path=folder/(name+'.gz')
   if path.exists():
    if sha(path)!=entry['asRunGzipSha256']:raise ValueError('Gzip source changed after packing: '+str(path))
    path.unlink()
 return index
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('folder',type=Path);p.add_argument('--delete-originals',action='store_true');p.add_argument('--preset',type=int,default=1);a=p.parse_args()
 index=pack(a.folder,a.delete_originals,a.preset)
 print(json.dumps({'folder':str(a.folder),'rawBytes':sum(x['bytes'] for x in index['streams'].values()),'asRunGzipBytes':sum(x['asRunGzipBytes'] for x in index['streams'].values()),'retainedBytes':(a.folder/ARCHIVE).stat().st_size,'verified':True}),flush=True)
