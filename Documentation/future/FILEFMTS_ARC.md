ARC
===

The ARC files are archive files created by the SEA ARC program. The compression
has been superceded by more recent compression programs. Similar archives can
be created by the PAK and PkPAK programs. There have been many variations
and enhancements to the ARC format though not as many as to the TIFF format.

You may have to use some (paranoid) checks to ensure that you actually are
processing an ARC file, since other archivers also adopted the idea of putting
a 01Ah byte at offset 0, namely the Hyper archiver. To check if you have a
Hyper-archive, check the next two bytes for "HP" or "ST" (or look below for
"HYP"). Also the ZOO archiver also does put a 01Ah at the start of the file,
see the ZOO entry below.
```
OFFSET              Count TYPE   Description
0000h                   1 byte   ID=1Ah
0001h                   1 byte   Compression method (see table 0001)
0002h                  12 char   File name
000Fh                   1 dword  Compressed file size
0013h                   1 dword  File date in MS-DOS format (see table 0009)
0017h                   1 word   16-bit CRC
0019h                   1 dword  Original file size
                                 ="SIZ"
```

| (Table 0001) | ARC compression types |
|-----------:|:----------------------|
|    0 | End of archive marker
|    1 | unpacked (obsolete) | ARC 1.0 ?
|    2 | unpacked | ARC 3.1
|    3 | packed (RLE encoding)
|    4 | squeezed (after packing)
|    5 | crunched (obsolete) | ARC 4.0
|    6 | crunched (after packing) (obsolete) | ARC 4.1
|    7 | crunched (after packing, using faster hash algorithm) | ARC 4.6
|    8 | crunched (after packing, using dynamic LZW variations) | ARC 5.0
|    9 | Squashed c/o Phil Katz (no packing) (var. on crunching)
|   10 | crushed (PAK only)
|   11 | distilled (PAK only)
|12-19 |  to 19 unknown (ARC 6.0 or later) | ARC 7.0 (?)
|20-29 | ?informational items? | ARC 6.0
|30-39 | ?control items? | ARC 6.0
|  40+ | reserved

According to SEA's technical memo, the information and control items
were added to ARC 6.0. Information items use the same headers as archived
files, although the original file size (and name?) can be ignored.

OFFSET              Count TYPE   Description
0000h                   2 byte   Length of header (includes "length"
                                 and "type"?)
0002h                   1 byte   (sub)type
0003h                   ? byte   data

Informational item types as used by ARC 6.0 :

Block type    Subtype   Description
   20                   archive info
                0       archive description (ASCIIZ)
                1       name of creator program (ASCIIZ)
                2       name of modifier program (ASCIIZ)

   21                   file info
                0       file description (ASCIIZ)
                1       long name (if not MS-DOS "8.3" filename)
                2       extended date-time info (reserved)
                3       icon (reserved)
                4       file attributes (ASCIIZ)

                        Attributes use an uppercase letter to signify the
                        following:

                                R       read access
                                W       write access
                                H       hidden file
                                S       system file
                                N       network shareable

   22                   operating system info (reserved)

(Table 0009)
Format of the MS-DOS time stamp (32-bit)
The MS-DOS time stamp is limited to an even count of seconds, since the
count for seconds is only 5 bits wide.
```
  31 30 29 28 27 26 25 24 23 22 21 20 19 18 17 16
 |<---- year-1980 --->|<- month ->|<--- day ---->|

  15 14 13 12 11 10  9  8  7  6  5  4  3  2  1  0
 |<--- hour --->|<---- minute --->|<- second/2 ->|

EXTENSION:ARC,PAK
OCCURENCES:PC
PROGRAMS:SEA ARC,PAK,PkPAK
SEE ALSO:HYP,ZOO
VALIDATION:FileSize="SIZ"
```
