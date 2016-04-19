--------A-ZOO-------------------------------
The ZOO archive program by Raoul Dhesi is a file compression program now
superceeded in both compression and speed by most other compression programs.
The archive header looks like this :
OFFSET              Count TYPE   Description
0000h                  20 char   Archive header text, ^Z terminated, null padded
0014h                   1 dword  ID=0FDC4A7DCh
0018h                   1 dword  Offset of first file in archive
001Ch                   1 dword  Offset of ????
0020h                   1 byte   Version archive was made by
0021h                   1 byte   Minimum version needed to extract

Each stored file has its own header, which looks like this :
OFFSET              Count TYPE   Description
0000h                   1 dword  ID=0FDC4A7DCh
0004h                   1 byte   Type of directory entry
0005h                   1 byte   Compression method :
                                 0 - stored
                                 1 - Crunched : LZW, 4K buffer,
                                                 var len (9-13 bits)
0006h                   1 dword  Offset of next directory entry
000Ah                   1 dword  Offset of next header
000Dh                   1 word   Original date / time of file (see table 0009)
0012h                   1 word   CRC-16 of file
0014h                   1 dword  Uncompressed size of file
0018h                   1 dword  Compressed size of file
001Ch                   1 byte   Version this file was compressed by
001Dh                   1 byte   Minimum version needed to extract
001Eh                   1 byte   Deleted flag
                                 0 - file in archive
                                 1 - file is considered deleted
001Fh                   1 dword  Offset of comment field, 0 if none
0023h                   1 word   Length of comment field
0025h                   ? char   ASCIIZ path / filename

EXTENSION:ZOO
OCCURENCES:PC
PROGRAMS:ZOO.EXE
REFERENCE:
VALIDATION:
