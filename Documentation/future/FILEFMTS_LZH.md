LZH
===

The LHArc/LHA archiver is a multi platform archiver made by Haruyasu Yoshizaki,
which has a relatively good compression. It uses more or less the same
technology like the ZIP programs by Phil Katz. There was a hack named "ICE",
which had only the graphic characters displayed on decompression changed.

```
OFFSET              Count TYPE   Description
0000h                   1 byte   Size of archived file header
0001h                   1 byte   Checksum of remaining bytes
0002h                   3 char   ID='-lh'
                                 ID='-lz'
0005h                   1 char   Compression methods used (see table 0005)
0006h                   1 char   ID='-'
0007h                   1 dword  Compressed size
000Bh                   1 dword  Uncompressed size
000Fh                   1 dword  Original file date/time (see table 0009)
0013h                   1 word   File attribute
0015h                   1 byte   Filename / path length in bytes
                                 ="LEN"
0016h               "LEN" char   Filename / path
0018h                   1 word   CRC-16 of original file
+"LEN"

(Table 0005)
LHArc compression types
  "0" - No compression
  "1" - LZW, 4K buffer, Huffman for upper 6 bits of position
  "2" - unknown
  "3" - unknown
  "4" - LZW, Arithmetic Encoding
  "5" - LZW, Arithmetic Encoding
  "s" - LHa 2.x archive?
  "\" - LHa 2.x archive?
  "d" - LHa 2.x archive?

EXTENSION:LZH,ICE
OCCURENCES:PC
PROGRAMS:LHArc.EXE, LHA.EXE
```
