# SharpZipLib

[![Build Status](https://travis-ci.org/McNeight/SharpZipLib.svg?branch=build)](https://travis-ci.org/McNeight/SharpZipLib)
[![Build status](https://ci.appveyor.com/api/projects/status/oe7kwnaib3qscm8l/branch/build?svg=true)](https://ci.appveyor.com/project/McNeight/SharpZipLib/branch/build)

\#ziplib (SharpZipLib, formerly NZipLib) is a **Zip, GZip, Tar and BZip2 library** written entirely in C\# for the .NET framework. It is implemented as an assembly (installable in the GAC), and thus can easily be incorporated into other projects (in any .NET language). The creator of #ziplib put it this way: "I've ported the zip library over to C\# because I needed gzip/zip compression and I didn't want to use libzip.dll or something like this. I want all in pure C\#."

Introduction
------------

SharpZipLib was originally ported from the [GNU Classpath](http://www.gnu.org/software/classpath/) java.util.zip library for use with [SharpDevelop](http://www.icsharpcode.net/OpenSource/SD), which needed gzip/zip compression. bzip2 compression and tar archiving were added later due to popular demand.

There is a web site from which You can download the assembly and/or the source code (<http://icsharpcode.net/OpenSource/SharpZipLib>). A forum is also available at http://community.sharpdevelop.net/forums/12/ShowForum.aspx.

Please see the [\#ziplib homepage](http://icsharpcode.github.io/SharpZipLib/) for precompiled downloads, license information, link to the forum (support), release history, samples and more.

License
-------

This software is released under the [GNU General Public License, version 2](http://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html) with an exception which allows linking with non GPL programs. The exception to the GPL is as follows:

>Linking this library statically or dynamically with other modules is making a combined work based on this library. Thus, the terms and conditions of the GNU General Public License cover the whole combination.

>As a special exception, the copyright holders of this library give you permission to link this library with independent modules to produce an executable, regardless of the license terms of these independent modules, and to copy and distribute the resulting executable under terms of your choice, provided that you also meet, for each linked independent module, the terms and conditions of the license of that module.  An independent module is a module which is not derived from or based on this library.  If you modify this library, you may extend this exception to your version of the library, but you are not obligated to do so. If you do not wish to do so, delete this exception statement from your version.

Building the library
--------------------

There are multiple ways to build this library, however the library build is tested with the following methods:

[AppVeyor](https://ci.appveyor.com/project/McNeight/SharpZipLib/branch/build)

This builds SharpZipLib on Windows useing Visual Studio 2013 and Visual Studio 2015

[Travis CI](https://travis-ci.org/McNeight/SharpZipLib)

This builds SharpZipLib on Linux with Mono 2.10.8, 3.12.1, and the latest version available

Namespace layout
----------------

| Module | Namespace |
|:----------------:|:-----------------------------|
|BZip2 implementation|ICSharpCode.SharpZipLib.BZip2.\*|
|Checksum implementation|ICSharpCode.SharpZipLib.Checksums.\*|
|Core utilities / interfaces|ICSharpCode.SharpZipLib.Core.\*|
|Encryption implementation|ICSharpCode.SharpZipLib.Encryption.\*|
|GZip implementation|ICSharpCode.SharpZipLib.GZip.\*|
|Tar implementation|ICSharpCode.SharpZipLib.Tar.\*|
|ZIP implementation|ICSharpCode.SharpZipLib.Zip.\*|
|Inflater/Deflater|ICSharpCode.SharpZipLib.Zip.Compression.\*|
|Inflater/Deflater streams|ICSharpCode.SharpZipLib.Zip.Compression.Streams.\*|

Reporting Bugs/Submit Patches
-----------------------------

Credits
-------

\#ziplib was initially been developed by [Mike Krueger](http://www.icsharpcode.net/pub/relations/krueger.aspx), however, much existing Java code helped a lot in speeding the creation of this library. Therefore credits fly out to other people.

The current maintainer of \#ziplib is David Pierson. Please contact him regarding features, bugs etc via the [forum](http://community.sharpdevelop.net/forums/12.aspx).

Zip/Gzip implementation:

A Java version of the zlib which was originally created by the [Free Software Foundation (FSF)](http://www.fsf.org). So all credits should go to the FSF and the authors who have worked on this piece of code.

Without the zlib authors the Java zlib wouldn't be possible:
[Jean-loup Gailly](http://gailly.net/), [Mark Adler](http://en.wikipedia.org/wiki/Mark_Adler), and contributors of zlib.

[Julian R Seward](julian@bzip.org) for the bzip2 implementation, and the Java port by [Keiron Liddle](keiron@aftexsw.com) of Aftex Software.

Credits for the tar implementation fly out to :
[Timothy Gerard Endres](time@gjt.org)

Special thanks to [Christoph Wille](http://www.icsharpcode.net/pub/relations/wille.aspx) for beta testing, suggestions, the setup of the web site, and for his tireless efforts at cat herding.
