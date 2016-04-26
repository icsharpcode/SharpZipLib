LBR
===

The LBR files consist of a direcotry and one or more "members". The directory
contains from 4 to 256 entries and each entry describes one member.
The first directory entry describes the directory itself. All space allocations
are in terms of sectors, where a sector is 128 bytes long. Four directory
entries fit in one sector thus the number of directory entries is always evenly
divisible by 4. Different types of LBR files exist, all versions are discussed
here, the directory entry looks like this :

```
OFFSET              Count TYPE   Description
0000h                   1 byte   File status :
                                  0 - active
                                254 - deleted
                                255 - free
0001h                  11 char   File name in FCB format (8/3, blank padded),
                                 directory name is blanks for old LU,
                                 ID='********DIR'
                                 for LUPC
000Ch                   1 word   Offset to file data in sectors
000Eh                   1 word   Length of stored data in sectors

For the LUPC program, the remaining 16 bytes are used like this :
OFFSET              Count TYPE   Description
0000h                   8 char   ASCII date of creation (MM/DD/YY)
0008h                   8 char   ASCII time of creation (HH:MM:SS)

For the LU86 program, the remaining 16 bytes are used like this :
OFFSET              Count TYPE   Description
0000h                   1 word   CRC-16 or 0
0002h                   1 word   Creation date in CP/M format
0004h                   1 word   Creation time in DOS format
0006h                   1 word   Date of last modification, CP/M format
0008h                   1 word   Time of last modification, DOS format
000Ah                   1 byte   Number of bytes in last sector
000Bh                   5 byte   reserved (0)

EXTENSION:LBR
OCCURENCES:PC,CP/M
PROGRAMS:LU.COM, LUU.COM, LU86.COM
SEE ALSO:
```
