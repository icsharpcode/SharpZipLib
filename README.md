# SharpZipLib [![Join the chat at https://gitter.im/icsharpcode/SharpZipLib](https://badges.gitter.im/icsharpcode/SharpZipLib.svg)](https://gitter.im/icsharpcode/SharpZipLib?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Stories in Ready](https://badge.waffle.io/icsharpcode/SharpZipLib.svg?label=ready&title=Ready)](http://waffle.io/icsharpcode/SharpZipLib) [![Coverage Status](https://coveralls.io/repos/github/McNeight/SharpZipLib/badge.svg?branch=master)](https://coveralls.io/github/McNeight/SharpZipLib?branch=master) [![Coverity Scan Build Status](https://scan.coverity.com/projects/8519/badge.svg)](https://scan.coverity.com/projects/mcneight-sharpziplib)

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
    <td style="text-align:center" rowspan="3">Bitrise</td>
    <td>OSX</td>
    <td>Xamarin.iOS</td>
    <td style="text-align:center" rowspan="3">
    <a href="https://www.bitrise.io/app/e085f985c0c29473"><img src="https://www.bitrise.io/app/e085f985c0c29473.svg?token=TKMy51lbK4ZU0N2lQi5WNg&branch=master" alt="Bitrise Build Status" /></a></td>
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

This software is now released under the [MIT License](https://opensource.org/licenses/MIT). Please see [issue #103](https://github.com/icsharpcode/SharpZipLib/issues/103) for more information on the relicensing effort.

Previous versions were released under the [GNU General Public License, version 2](http://www.gnu.org/licenses/old-licenses/gpl-2.0.en.html) with an [exception](http://www.gnu.org/software/classpath/license.html) which allowed linking with non-GPL programs.

Namespace layout
----------------

| Module | Namespace |
|:----------------:|:-----------------------------|
|BZip2 implementation|ICSharpCode.SharpZipLib.BZip2.\*|
|Checksum implementation|ICSharpCode.SharpZipLib.Checksums.\*|
|Core utilities / interfaces|ICSharpCode.SharpZipLib.Core.\*|
|Encryption implementation|ICSharpCode.SharpZipLib.Encryption.\*|
|GZip implementation|ICSharpCode.SharpZipLib.GZip.\*|
|LZW implementation|ICSharpCode.SharpZipLib.Lzw.\*|
|Tar implementation|ICSharpCode.SharpZipLib.Tar.\*|
|ZIP implementation|ICSharpCode.SharpZipLib.Zip.\*|
|Inflater/Deflater|ICSharpCode.SharpZipLib.Zip.Compression.\*|
|Inflater/Deflater streams|ICSharpCode.SharpZipLib.Zip.Compression.Streams.\*|

Credits
-------

SharpZipLib was initially developed by [Mike Krüger](http://www.icsharpcode.net/pub/relations/krueger.aspx). Past maintainers are John Reilly and David Pierson. The current maintainer is Neil McNeight.

And thanks to all the people that contributed features, bug fixes and issue reports.

Metrics
-------
[![Throughput Graph](https://graphs.waffle.io/icsharpcode/SharpZipLib/throughput.svg)](https://waffle.io/icsharpcode/SharpZipLib/metrics/throughput)
