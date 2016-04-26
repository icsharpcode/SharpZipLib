Future Work
===========

C# implementation of older archive file formats.

Documentation from http://www.corion.net/fileformats/index.html

File format list        Release 3.00             Last change 02/04/96
This compilation is Copyright (c) 1994,2002 Max Maischein

## CONTACT_INFO

If you notice any mistakes or omissions, please let me know!  It is only
with YOUR help that the list can continue to grow.  Please send
all changes to me rather than distributing a modified version of the list.

This file has been authored in the style of the INTERxxy.* file list
by Ralf Brown, and uses almost the same format.

Please read the file FILEFMTS.1ST before asking me any questions. You may find
that they have already been addressed.

         Max Maischein

corion@corion.net
Corion on #coders@IRC

## DISCLAIMER

DISCLAIMER:  THIS MATERIAL IS PROVIDED "AS IS".  I verify the information
contained in this list to the best of my ability, but I cannot be held
responsible for any problems caused by use or misuse of the information,
especially for those file formats foreign to the PC, like AMIGA or SUN file
formats. If an information it is marked "guesswork" or undocumented, you
should check it carefully to make sure your program will not break with
an unexpected value (and please let me know whether or not it works
the same way).

Information marked with "???" is known to be incomplete or guesswork.

Some file formats were not released by their creators, others are regarded
as proprietary, which means that if your programs deal with them, you might
be looking for trouble. I don't care about this.

## FLAGS

One or more letters may follow the file format ID; they have the following
meanings:

```
 Cx - Charset used :
      7 - Unix 7-bit characters
      A - Amiga charset (if there is one)
      E - EBDIC character format
      U - Unicode character set
      W - Windows char set
      Default is the 8-Bit IBM PC-II Charset. Note that Microsoft
      introduced codepages which might be relevant with other
      programs.
 G  - guesswork, incomplete, unreliable etc.
 M  - Motorola byte order
      Default is Intel byte order
 O  - obsolete, valid only for version noted below
 X  - Synonym topic. See topic named under see also.
```

## CATEGORIES

The ninth column of the divider line preceding an entry usually contains a
classification code for the application that uses those files.

The codes currently in use are:
```
! - User information ( not really a file format )
A - Archives (ARC,LZH,ZIP,...)
a - Animations (CEL, FLI, FLT,...)
B - Binary files for compilers etc. (OBJ,TPU)
H - Help file (HLP,NG)
I - Images, bit maps (GIF,BMP,TIFF,...)
D - Data support files (CPI,FON,...)
E - Executable files (EXE,PIF)
f - Generic file format. RIFF and IFF are generic file formats.
F - Font files (TTF)
G - General graphics file
M - Module music file (MIDI,MOD,S3M,...)
R - Resource data files (RES)
S - Sound files (WAV,VOC,ZYX)
T - Text files (DOC,TXT)
W - Spreadsheet and related (WKS)
X - Database files (DBF)
```

## FIELDS

After a format description, you will sometimes find other keywords. The
meanings of these are :
### EXTENSION:
    This is the default extension of files of the given type.
    On DOS systems, most files have a 3 letter extension.
    On Amiga systems, the files are prefixed with something.
    The DOS extensions are all uppercase, extensions for other systems
    are in lower case chars. On other systems, which do not have the con-
    cept of extensions, as the MAC, this is the file type.
### OCCURENCES:
Where you are likely to encounter those files. This specifies
machines (like PC,AMIGA) or operating systems (like UNIX).
### PROGRAMS:
Programs which either create, use or convert files of this format.
Some might be used for validation or conversion.
### REFERENCE:
A reference to a file or an article in a magazine which is mandatory
or recommended for further understanding of the matter.
### SEE ALSO:
A cross reference to a topic which might be interesting as well.
### VALIDATION:
Methods to validate that the file you have is not corrupt. Normally
this is a method to check the theoretical file size against the
real filesize. Some file formats allow no reliable validation.

## FORMAT

The block oriented files are organized in some other fashion, since the
order of blocks is at best marginally obligatory.

Each block type starts with the block ID (eg. RIFFblock for a RIFF file) and
in square brackets the character value of the ID field (eg. [WAVE] for RIFF
WAVe sound files). The block itself is descripted in the format description,
that means you will have to look after RIFF or FORM. In the record
description, the header information is omitted !

If a record is descripted, the record ends when the next offset is given.

Bitmapped values have a description for each bit. The value left of the
slash ("/") is for the bit not set (=0), the right sided value applies
if the bit is set.

A note on the tables section. The tables were added as they were introduced
into Ralf Browns interrupt list - so not everything was pressed into a table.
The tables (should) have unique numbers, but they sure are out of order !

## MACHINES

Machines that use Intel byte ordering
* PC

Machines that use Motorola byte ordering
* AMIGA
* ATARI ST
* MAC
* SUN

## HISTORY

History is kept within this file for convenience whilst editing ...
Date format is european/german, just for my convenience.
```
Date     Who            What
14.03.95 MM             Introduced tables
                        Last table number=0012
05.06.95 MM             + PTM format
25.07.95 MM             + PIF format
                        + Paradox format description
11.08.95 MM             + MS Compress variants
18.11.95 MM             + ARC enhancements, caveats
                        + HA files
22.11.95 MM             + Parts of the .CRD files
01.02.96 MM             + PNG structure
02.02.96 MM             + More on JPEG
                        + TARGA entry created
```
