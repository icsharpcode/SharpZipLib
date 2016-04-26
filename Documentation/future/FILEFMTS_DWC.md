DWC
===

The DWC archives seem to be a relict from ancient computing times - I've never
seen any program that dealt with them or could create them. They are yet
included in this compilation for reasons I don't know. But maybe one of you
stumbles over such a file, he might find this documentation helpful.
The DWC archives consist of single file entries with one archive trailer. The
archive entries seem to be at the start of the archive, but maybe they are
stored at the end of the archive, before the trailer. Each file header has the
following format :

```
OFFSET              Count TYPE   Description
0000h                  13 char   Name of the original file in ASCIIZ.
000Dh                   1 dword  Size of the original file
0011h                   1 dword  MS-DOS date and time of the original file
0015h                   1 dword  Size of the compressed file
0019h                   1 dword  Offset of compressed data in archive file
001Dh                   3 byte   reserved
0020h                   1 byte   Method :
                                  1 - crunched
                                  2 - stored

The trailer at the end of each archive has the following format :
OFFSET              Count TYPE   Description
0000h                   1 word   Length of trailer (=27)
0002h                   1 word   Size of the directory entries (=34)??
0004h                  16 byte   reserved
0014h                   1 dword  Count of the directory entries
0018h                   3 char   ID="DWC"

EXTENSION:DWC??
OCCURENCES:PC??
PROGRAMS:DWC.EXE??
```
