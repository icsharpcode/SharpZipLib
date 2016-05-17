using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCompany("ICSharpCode")]
[assembly: AssemblyProduct("SharpZipLib (#ziplib)")]
[assembly: AssemblyCopyright("Copyright © 2000-2016 SharpZipLib Contributors")]
[assembly: AssemblyTrademark("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

// https://blogs.msdn.microsoft.com/carloc/2008/02/22/version-numbers-in-a-compiled-assembly/
//
// Build: the number of days since 1.1.2000 (you can modify this start date by setting Software\\Microsoft\\ALink\\VersionStartDate).  i.e. 1879 = 02.22.2005
//
// Revision: the number of two second intervals since midnight in local time; i.e. 31308 =  17:23:36 local time.

// Specifies the version of the assembly being attributed.
[assembly: AssemblyVersion("1.0.*")]

// Instructs a compiler to use a specific version number for the Win32 file version resource.
// The Win32 file version is not required to be the same as the assembly's version number.
[assembly: AssemblyFileVersion("1.0")]

// Defines additional version information for an assembly manifest.
[assembly: AssemblyInformationalVersion("1.0")]
