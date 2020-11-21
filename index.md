\#ziplib (SharpZipLib, formerly NZipLib) is a **Zip, GZip, Tar and BZip2 library** written entirely in C# for the .NET platform. It is implemented as an assembly (installable in the GAC), and thus can easily be incorporated into other projects (in any .NET language). The creator of \#ziplib put it this way: "I've ported the zip library over to C# because I needed gzip/zip compression and I didn't want to use libzip.dll or something like this. I want all in pure C\#." 

## Download

Releases of v1.3.1 is now available as a [NuGet Package](https://www.nuget.org/packages/SharpZipLib/)

Install using
```.ps1
Install-Package SharpZipLib
```
or
```.sh
dotnet add package SharpZipLib
```

[Release notes for v1.3.1](https://github.com/icsharpcode/SharpZipLib/wiki/Release-1.3.1)

### Legacy

Legacy versions of SharpZipLib can be found on the [legacy page](legacy)

**Note:**  They are not supported anymore and is released under a stricter license ([GPL with linking exception](legacy-license))

## License

The library is released under the MIT license:

> Copyright © 2002-2020 SharpZipLib Contributors
> 
> Permission is hereby granted, free of charge, to any person obtaining a copy of this
> software and associated documentation files (the "Software"), to deal in the Software
> without restriction, including without limitation the rights to use, copy, modify, merge,
> publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
> to whom the Software is furnished to do so, subject to the following conditions:
> 
> The above copyright notice and this permission notice shall be included in all copies or
> substantial portions of the Software.
> 
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
> INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
> PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
> FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
> OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
> DEALINGS IN THE SOFTWARE.

## Credits
\#ziplib has intially been developed by Mike Krueger, however, much existing Java code helped a lot in speeding the creation of this library. Therefore credits fly out to other people.

## Change Log
The changes are documented in the [release history](https://github.com/icsharpcode/SharpZipLib/wiki/Release-History) that can be found in our [project wiki](https://github.com/icsharpcode/SharpZipLib/wiki). 
