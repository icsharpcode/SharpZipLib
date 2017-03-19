--------A-HYP-------------------------------
The Hyper archiver is a very fast compression program by P. Sawatzki and K.P.
Nischke, which uses LZW compression techniques for compression. It is not very
widespread - in fact, I've yet to see a package distributed in this format.

OFFSET              Count TYPE   Description
0000h                   1 byte   ID=1Ah
0001h                   2 char   Compression method
                                 "HP" - compressed
                                 "ST" - stored
0003h                   1 byte   Version file was compressed by in BCD
0004h                   1 dword  Compressed file size
0008h                   1 dword  Original file size
000Ch                   1 dword  MS-DOS date and time of file (see table 0009)
0010h                   1 dword  CRC-32 of file
0014h                   1 byte   MS-DOS file attribute
0015h                   1 byte   Length of filename
                                 ="LEN"
0016h               "LEN" char   Filename

EXTENSION:HYP
OCCURENCES:PC
PROGRAMS:HYPER.EXE
