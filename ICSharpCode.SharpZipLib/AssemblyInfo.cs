// AssemblyInfo.cs
//
// Copyright © 2000-2016 AlphaSierraPapa for the SharpZipLib Team
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
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
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

// Version name | Version number | Release date
// 1.0 RTM | 1.0.2268.0 | 2002 late
// 1.0 SP1 | 1.0.3111.0 | 2003
// 1.0 SP2 | 1.0.3316.0 | unknown
// 1.0 SP3 | 1.0.4292.0 | 2005 January
// 2.0 RTM | 2.0.5238.0 | 2005 October
// 2.0 SP1 | 2.0.6129.0 | 2006 June
// 2.0 SP2 | 2.0.7045.0 | 2007 March

#if (NET_1_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Framework 1.0")]
#elif (NET_1_1)
[assembly: AssemblyTitle("SharpZipLib for .NET Framework 1.1")]
#elif (NETCF_1_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Compact Framework 1.0")]
#elif (NETCF_2_0)
[assembly: AssemblyTitle("SharpZipLib for .NET Compact Framework 2.0")]
#else
[assembly: AssemblyTitle("SharpZipLib")]
#endif

[assembly: AssemblyDescription("A free C# compression library")]
[assembly: AssemblyProduct("#ZipLib")]
[assembly: AssemblyDefaultAlias("SharpZipLib")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif


[assembly: AssemblyCompany("AlphaSierraPapa")]
[assembly: AssemblyCopyright("Copyright © 2000-2016 AlphaSierraPapa for the SharpZipLib Team")]
[assembly: AssemblyTrademark("")]

[assembly: AssemblyVersion("0.87.*")]
[assembly: AssemblyInformationalVersionAttribute("0.87.*")]


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
