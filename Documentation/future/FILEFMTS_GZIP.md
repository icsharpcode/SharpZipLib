--------A-GZIP------------------------------
The GNU ZIP program is an archive program mostly for the UNIX machines developed
by the GNU project.
OFFSET              Count TYPE   Description
0000h                   2 char   ID='!',139
0002h                   1 byte   Method :
                                 0-7 - reserved
                                   8 - deflated
0003h                   1 byte   File flags :
                                   0 - ASCII-text
                                   1 - Multi-part file
                                   2 - Name present
                                   3 - Comment present
                                   4 - Encrypted
                                 5-8 - reserved
0004h                   1 dword  File date and time (see table 0009)
0008h                   1 byte   Extra flags
0009h                   1 byte   Target OS :
                                   0 - DOS
                                   1 - Amiga
                                   2 - VMS
                                   3 - Unix
                                   4 - ????
                                   5 - Atari
                                   6 - OS/2
                                   7 - MacOS
                                  10 - TOPS-20
                                  11 - Win/32
EXTENSION:ZIP
PROGRAMS:GNU gzip
