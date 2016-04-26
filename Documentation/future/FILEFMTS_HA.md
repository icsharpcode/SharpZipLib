--------A-HA--------------------------------
HA files (not to be confused with HamarSoft's HAP files [3]) contain a
small archive header with a word count of the number of files in the
archive. The constituent files stored sequentially with a header followed
by the compressed data, as is with most archives.

The main file header is formatted as follows:
OFFSET              Count TYPE   Description
0000h                   2 char   ID='HA'
0002h                   1 word   Number of files in archive

Every compressed file has a header before it, like this :

OFFSET              Count TYPE   Description
0000h                   1 byte   Version & compression type
0001h                   1 dword  Compressed file size
0005h                   1 dword  Original file size
0009h                   1 dword  CCITT CRC-32 (same as ZModem/PkZIP)
000Dh                   1 dword  File time-stamp (Unix format)
   ?                    ? char   ASCIIZ pathname
   ?                    ? char   ASCIIZ filename
????h                   1 byte   Length of machine specific information
                        ? byte   Machine specific information

Note that the path separator for pathnames is the 0FFh (255) character.

The high nybble of the version & compression type field contains the
version information (0=HA 0.98), the low nybble is the compression type :

(Table 0012)
HA compression types
    0           "CPY"           File is stored (no compression)
    1           "ASC"           Default compression method, using a sliding
                                window dictionary with an arithmetic coder.
    2           "HSC"           Compression using a "finite context [sic]
                                model and arithmetic coder"
   14           "DIR"           Directory entry
   15           "SPECIAL"       Used with HA 0.99B (?)


Machine specific information known:

    1 byte      Machine type (Host-OS)

                        1 = MS DOS
                        2 = Linux (Unix)

    ? bytes     Information (currently only file-attribute info)

EXTENSION:HA
OCCURENCES:PC, Linux
PROGRAMS:HA
REFERENCE:
