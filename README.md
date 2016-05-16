[![Stories in Ready](https://badge.waffle.io/McNeight/SharpZipLib.png?label=ready&title=Ready)](https://waffle.io/McNeight/SharpZipLib)
# SharpZipLib

[![Join the chat at https://gitter.im/icsharpcode/SharpZipLib](https://badges.gitter.im/icsharpcode/SharpZipLib.svg)](https://gitter.im/icsharpcode/SharpZipLib?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

<table>
  <tr>
    <th style="text-align:center">Build Server</th>
    <th>Operating System</th>
    <th>Framework</th>
    <th style="text-align:center">Status</th>
  </tr>
  <tr>
    <td style="text-align:center">AppVeyor</td>
    <td>Windows Server 2012</td>
    <td>.NET Framework 4.5</td>
    <td style="text-align:center"><a href="https://ci.appveyor.com/project/McNeight/SharpZipLib/branch/master"><img src="https://ci.appveyor.com/api/projects/status/oe7kwnaib3qscm8l/branch/master?svg=true" alt="AppVeyor build status" /></a></td>
  </tr>
  <tr>
    <td style="text-align:center" rowspan="2">Travis</td>
    <td>Ubuntu 12.04.5 LTS</td>
    <td>Mono 4.2.3</td>
    <td style="text-align:center" rowspan="2">
    <a href="https://travis-ci.org/McNeight/SharpZipLib"><img src="https://travis-ci.org/McNeight/SharpZipLib.svg?branch=master" alt="Travis build status" /></a></td>
  </tr>
  <tr>
    <td>MacOSX 13.4.0.0</td>
    <td>Mono 4.2.3</td>
  </tr>
  <tr>
    <td style="text-align:center" rowspan="3">Bitrise (Soon)</td>
    <td>OSX</td>
    <td>Xamarin.iOS</td>
    <td style="text-align:center" rowspan="3"></td>
  </tr>
  <tr>
    <td>OSX</td>
    <td>Xamarin.Android</td>
  </tr>
  <tr>
    <td>OSX</td>
    <td>Xamarin.Mac</td>
  </tr>
</table>

Introduction
------------

SharpZipLib (\#ziplib, formerly NZipLib) is a compression library that supports Zip files using both stored and deflate compression methods, PKZIP 2.0 style and AES encryption, tar with GNU long filename extensions, GZip, zlib and raw deflate, as well as BZip2. Zip64 is supported while Deflate64 is not yet supported. It is implemented as an assembly (installable in the GAC), and thus can easily be incorporated into other projects (in any .NET language). The creator of SharpZipLib put it this way: "I've ported the zip library over to C\# because I needed gzip/zip compression and I didn't want to use libzip.dll or something like this. I want all in pure C\#."

SharpZipLib was originally ported from the [GNU Classpath](http://www.gnu.org/software/classpath/) java.util.zip library for use with [SharpDevelop](http://www.icsharpcode.net/OpenSource/SD), which needed gzip/zip compression. bzip2 compression and tar archiving were added later due to popular demand.

The [SharpZipLib homepage](http://icsharpcode.github.io/SharpZipLib/) has precompiled libraries available for download, [a link to the forum for support](http://community.sharpdevelop.net/forums/12/ShowForum.aspx), [release history](https://github.com/icsharpcode/SharpZipLib/wiki/Release-History), samples and more.

License
-------

This software is released under the [GNU General Public License, version 2](http://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html) with an exception which allows linking with non GPL programs. The exception to the GPL is as follows:

>Linking this library statically or dynamically with other modules is making a combined work based on this library. Thus, the terms and conditions of the GNU General Public License cover the whole combination.

>As a special exception, the copyright holders of this library give you permission to link this library with independent modules to produce an executable, regardless of the license terms of these independent modules, and to copy and distribute the resulting executable under terms of your choice, provided that you also meet, for each linked independent module, the terms and conditions of the license of that module.  An independent module is a module which is not derived from or based on this library.  If you modify this library, you may extend this exception to your version of the library, but you are not obligated to do so. If you do not wish to do so, delete this exception statement from your version.

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

Credits
-------

SharpZipLib was initially developed by [Mike Krüger](http://www.icsharpcode.net/pub/relations/krueger.aspx). Past maintainers are John Reilly and David Pierson. The current maintainer is Neil McNeight.

Much existing Java code helped to speed the creation of this library. Therefore credits fly out to other people.

Zip/GZip implementation:

A Java version of the zlib which was originally created by the [Free Software Foundation (FSF)](http://www.fsf.org). So all credits should go to the FSF and the authors who have worked on this piece of code.

Without the zlib authors the Java zlib wouldn't be possible:
[Jean-loup Gailly](http://gailly.net/), [Mark Adler](http://en.wikipedia.org/wiki/Mark_Adler), and contributors of zlib.

[Julian R Seward](julian@bzip.org) for the bzip2 implementation, and the Java port by [Keiron Liddle](keiron@aftexsw.com) of Aftex Software.

Credits for the tar implementation fly out to :
[Timothy Gerard Endres](time@gjt.org)

Special thanks to [Christoph Wille](http://www.icsharpcode.net/pub/relations/wille.aspx) for beta testing, suggestions, the setup of the web site, and for his tireless efforts at cat herding.
