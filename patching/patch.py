"""Stage 1: two raw IL byte patches on the official PCL 2.99.0.0 executable.

usage: python patch.py <official.exe> <output.exe>

  1. PCL.PageDownloadInstall::LoaderInit  -- replace the erased loader construction with
     `ldsfld PCL.ModDownload::DlClientListLoader` so the download page shows the source list.
  2. PCL.PageDownloadInstall::LoadInstanceFinished -- `ldc.i4.2` (Collapsed) -> `ldc.i4.0` (Visible)
     for the Minecraft download card.

Requires: pip install pefile
"""
import pefile, struct, sys

if len(sys.argv) < 3:
    sys.exit("usage: python patch.py <official.exe> <output.exe>")
src, dst = sys.argv[1], sys.argv[2]

data = bytearray(open(src, "rb").read())
pe = pefile.PE(data=bytes(data), fast_load=True)


def rva2off(rva):
    for s in pe.sections:
        va = s.VirtualAddress
        size = max(s.Misc_VirtualSize, s.SizeOfRawData)
        if va <= rva < va + size:
            return s.PointerToRawData + (rva - va)
    raise ValueError("rva not mapped 0x%X" % rva)


def il_code(off):
    b = data[off]
    if (b & 3) == 2:
        return off + 1, b >> 2
    flags = struct.unpack_from("<H", data, off)[0]
    assert (flags & 3) == 3, "unexpected method header 0x%X" % flags
    size = struct.unpack_from("<I", data, off + 4)[0]
    return off + 12, size


# --- patch 1: PageDownloadInstall::LoaderInit, replace IL_001A..IL_0049 with 'ldsfld DlClientListLoader'
rva1 = 0x2DEB4
off1 = rva2off(rva1)
c1, n1 = il_code(off1)
print("LoaderInit: file_off=0x%X code_off=0x%X size=%d" % (off1, c1, n1))
expect = {0x1A: 0x72, 0x1F: 0x7E, 0x24: 0x2C, 0x26: 0x7E, 0x2B: 0x2B, 0x2D: 0x7E, 0x32: 0xFE,
          0x33: 0x06, 0x38: 0x73, 0x3D: 0x25, 0x3E: 0x80, 0x43: 0x14, 0x44: 0x18, 0x45: 0x73}
for k, v in expect.items():
    got = data[c1 + k]
    assert got == v, "IL_%04X expected 0x%02X got 0x%02X" % (k, v, got)
field_tok = 0x0400040F
new_bytes = bytes([0x7E]) + struct.pack("<I", field_tok) + b"\x00" * 43
assert len(new_bytes) == 0x4A - 0x1A
data[c1 + 0x1A: c1 + 0x4A] = new_bytes
print("patch1 applied: ldsfld 0x%08X + 43 nop" % field_tok)

# --- patch 2: PageDownloadInstall::LoadInstanceFinished, ldc.i4.2 -> ldc.i4.0 for CardMinecraft visibility
rva2 = 212908
off2 = rva2off(rva2)
c2, n2 = il_code(off2)
print("LoadInstanceFinished: file_off=0x%X code_off=0x%X size=%d" % (off2, c2, n2))
assert data[c2 + 0x012E] == 0x18, "expected ldc.i4.2 at IL_012E, got 0x%02X" % data[c2 + 0x012E]
assert data[c2 + 0x012F] == 0x6F, "expected callvirt at IL_012F, got 0x%02X" % data[c2 + 0x012F]
data[c2 + 0x012E] = 0x16
print("patch2 applied: ldc.i4.0 (Visible)")

open(dst, "wb").write(bytes(data))
print("written", dst, len(data), "bytes")
