// A Better AssemblyInfo.cs based on better practices.
//
// This little guy sets up all the assembly level attributes
// on the assembly that you're compiling
//
// This version heavily influenced (read copied) from Jeffery Richter's
// Applied Microsoft.NET Framework Programming (pg. 93)

using System.Reflection;

#region Company Info

// Set the CompanyName, LegalCopyright, and LegalTrademark fields
// You shouldn't have to mess with these
[assembly: AssemblyCompany("blowery.org")]
[assembly: AssemblyCopyright("Copyright (c) 2002 Ben Lowery")]

#endregion

#region Product Info

// Set the ProductName and ProductVersion fields
// AssemblyProduct should be a descriptive name for the product that includes this assembly
//  If this is a generic library that's going to be used by a whole bunch of stuff,
//  just make the name the same as the Assembly's name.  This isn't really used by
//  anything except Windows Explorer's properties tab, so it's not a big deal if you mess it up
//  See pg. 60 of Richter for more info.
[assembly: AssemblyProduct("HttpCompressionModule")]
#endregion

#region Version and Identity Info

// AssemblyInformationalVersion should be the version of the product that is including this
//  assembly.  Again, if this assembly isn't being included in a "product", just make this 
//  the same as the FileVersion and be done with it.  See pg 60 of Richter for more info.
[assembly: AssemblyInformationalVersion("1.1.0.0")]

// Set the FileVersion, AssemblyVersion, FileDescription, and Comments fields
// AssemblyFileVersion is stored in the Win32 version resource.  It's ignored by the CLR,
//  we typically use it to store the build and revision bits every time a build
//  is performed.  Unfortunately, the c# compiler doesn't do this automatically,
//  so we'll have to work out another way to do it.
[assembly: AssemblyFileVersion("1.1.0.0")]

// AssemblyVersion is the version used by the CLR as part of the strong name for
//  an assembly.  You don't really want to mess with this unless you're
//  making changes that add / remove / change functionality within the library.
//  For bugfixes, don't mess with this.
//
//  Do this:
//   [assembly: AssemblyVersion("0.82.0.1709")]
//
//  Don't do this:
//   [assembly: AssemblyVersion("0.82.0.1709")]
//  as it breaks all the other assemblies that reference this one every time 
//  you build the project.  
[assembly: AssemblyVersion("0.82.0.1709")]

// Title is just for inspection utilities and isn't really used for much
//  Generally just set this to the name of the file containing the 
//  manifest, sans extension.  I.E. for BensLibrary.dll, name it BensLibrary
[assembly: AssemblyTitle("HttpCompressionModule")]

// Description is just for inspection utilities and isn't really that important.
//  It's generally a good idea to write a decent description so that you
//  don't end up looking like a tool when your stuff shows up in an inspector.
[assembly: AssemblyDescription("An HttpModule that compresses the output")]

#endregion

#region Culture Info

// Set the assembly's culture affinity.  Generally don't want to set this
// unless you're building an resource only assembly.  Assemblies that contain
// code should always be culture neutral
[assembly: AssemblyCulture("")]		

#endregion

#region Assembly Signing Info

#if !StronglyNamedAssembly

// Weakly named assemblies are never signed
[assembly: AssemblyDelaySign(false)]

#else

  // Strongly named assemblies are usually delay signed while building and
  // completely signed using sn.exe's -R or -Rc switch

  #if !SignedUsingACryptoServiceProvider

  // Give the name of the file that contains the public/private key pair.
  // If delay signing, only the public key is used
  [assembly: AssemblyKeyFile(THE_KEY_FILE_RELATIVE_TO_THIS_FILE)]

  // Note:  If AssemblyKeyFile and AssemblyKeyName are both specified,
  // here's what happens...
  // 1) If the container exists, the key file is ignored
  // 2) If the container doesn't exist, the keys from the key
  //    file are copied in the container and the assembly is signed

  #else
  
  // Give the name of the cryptographic service provider (CSP) container
  // that contains the public/private key pair
  // If delay signing, only the public key is used
  [assembly: AssemblyKeyName(THE_KEY_CONTAINER_IN_CSP)]

  #endif

#endif

#endregion
