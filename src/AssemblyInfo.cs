// AssemblyInfo.cs
//
// Copyright (C) 2001 Mike Krueger
// Copyright 2004 John Reilly
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// Linking this library statically or dynamically with other modules is
// making a combined work based on this library.  Thus, the terms and
// conditions of the GNU General Public License cover the whole
// combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module.  An independent module is a module which is not derived from
// or based on this library.  If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so.  If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if (NET_1_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Framework 1.0")]
#elif (NET_1_1)
[assembly: AssemblyTitle("SharpZipLib for .NET Framework 1.1")]
#elif (NET_2_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Framework 2.0")]
#elif (NETCF_1_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Compact Framework 1.0")]
#elif (NETCF_2_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Compact Framework 2.0")]
#elif (MONO_1_0)
[assembly: AssemblyTitle("SharpZipLib for Mono 1.0")]
#elif (MONO_2_0)
[assembly: AssemblyTitle("SharpZipLib for Mono 2.0")]
#else
[assembly: AssemblyTitle("SharpZipLibrary unlabelled version")]
#endif

[assembly: AssemblyDescription("A free C# compression library")]
[assembly: AssemblyProduct("#ZipLibrary")]
[assembly: AssemblyDefaultAlias("SharpZipLib")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif


[assembly: AssemblyCompany("ICSharpCode.net")]
[assembly: AssemblyCopyright("Copyright 2001-2007 Mike Krueger, John Reilly")]
[assembly: AssemblyTrademark("Copyright 2001-2007 Mike Krueger, John Reilly")]

[assembly: AssemblyVersion("0.85.4.369")]
[assembly: AssemblyInformationalVersionAttribute("0.85.4")]


[assembly: CLSCompliant(true)]

#if (!NETCF)
//
// If #Zip is strongly named it still allows partially trusted callers
//
[assembly: System.Security.AllowPartiallyTrustedCallers]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
#if (CLI_1_0 || NET_1_0 || NET_1_1 || NETCF_1_0 || SSCLI)
[assembly: AssemblyDelaySign(false)]
#if VSTUDIO
[assembly: AssemblyKeyFile("../../ICSharpCode.SharpZipLib.key")]
#elif AUTOBUILD
[assembly: AssemblyKeyFile("ICSharpCode.SharpZipLib.key")]
#else
[assembly: AssemblyKeyFile("../ICSharpCode.SharpZipLib.key")]
#endif
#endif


