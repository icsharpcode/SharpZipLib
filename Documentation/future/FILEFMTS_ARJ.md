--------A-ARJ-------------------------------
The ARJ program by Robert K. Jung is a "newcomer" which compares well to PKZip
and LhArc in both compression and speed. An ARJ archive contains two types of
header blocks, one archive main header at the head of the archive and local
file headers before each archived file.

OFFSET              Count TYPE   Description
0000h                   1 word   ID=0EA60h
0002h                   1 word   Basic header size (0 if end of archive)
0004h                   1 byte   Size of header including extra data
0005h                   1 byte   Archiver version number
0006h                   1 byte   Minimum version needed to extract
0007h                   1 byte   Host OS (see table 0002)
0008h                   1 byte   Internal flags, bitmapped :
                                  0 - no password / password
                                  1 - reserved
                                  2 - file continues on next disk
                                  3 - file start position field is available
                                  4 - path translation ( "\" to "/" )
0009h                   1 byte   Compression method :
                                  0 - stored
                                  1 - compressed most
                                  2 - compressed
                                  3 - compressed faster
                                  4 - compressed fastest
                                 Methods 1 to 3 use Lempel-Ziv 77 sliding window
                                 with static Huffman encoding, method 4 uses
                                 Lempel-Ziv 77 sliding window with pointer/
                                 length unary encoding.
000Ah                   1 byte   File type :
                                  0 - binary
                                  1 - 7-bit text
                                  2 - comment header
                                  3 - directory
                                  4 - volume label
000Bh                   1 byte   reserved
000Ch                   1 dword  Date/Time of original file in MS-DOS format
0010h                   1 dword  Compressed size of file
0014h                   1 dword  Original size of file
0018h                   1 dword  Original file's CRC-32
001Ah                   1 word   Filespec position in filename
001Ch                   1 word   File attributes
001Eh                   1 word   Host data (currently not used)
?                       1 dword  Extended file starting position when used
                                 (see above)
                        ? char   ASCIIZ file name
                        ? char   Comment
????h                   1 dword  Basic header CRC-32
????h                   1 word   Size of first extended header (0 if none)
                                 ="SIZ"
????h+"SIZ"+2           1 dword  Extended header CRC-32
????h+"SIZ"+6           ? byte   Compressed file

(Table 0002)
ARJ HOST-OS types
  0 - MS-DOS
  1 - PRIMOS
  2 - UNIX
  3 - AMIGA
  4 - MAC-OS (System xx)
  5 - OS/2
  6 - APPLE GS
  7 - ATARI ST
  8 - NeXT
  9 - VAX VMS
EXTENSION:ARJ
OCCURENCES:PC
PROGRAMS:ARJ.EXE
REFERENCE:
SEE ALSO:
VALIDATION:
