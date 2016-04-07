== Version History ==
1.1 ---------

  MAJOR CHANGES THAT WILL AFFECT YOU
    - the assembly is now named HttpCompressionModule.dll
    - the config section handler is now HttpCompressionModuleSectionHandler
    - YOU WILL NEED TO UPDATE YOUR CONFIG FILES
      FOR THIS VERSION TO WORK (see samples for direction)
  
  Other stuff
    - moved to SharpZipLib (formerly NZipLib) 0.31 which
      contains a bunch of bug fixes.  This means I just
      inherited a bunch of bug fixes.  yay!
    - updated the code to use the new ICSharpCode namespace
    - reworked the way the configuration works
      - no more generic http modules.  i'm only writing this
        one, so this made the code simpler
      - removed the Unspecified flag on the Enums.  Not needed.
        now, it defaults to Deflate + Normal
      - decided to not support config parenting, as it doesn't
        really make sense for this
    - pulled out some trace stuff from the DEBUG version that
      didn't need to be there
    - actually shipping compiled versions, both DEBUG and RELEASE
    - added examples.  
    

1.0 ---------
  - initial introduction

=== Introduction ==
Hey there,

Thanks for downloading my compressing filter for ASP.NET!  As
you can see, the full source is provided so you can 
understand how it works and modify it if you want.

If you don't have visual studio, no fear.  The whole project
lives in one directory, so csc *.cs should work, you just need
to add a reference to the supplied SharpZipLib.dll.

To get you going, read the docs on msdn regarding HttpModules.
That's how this thing works, as an HttpModule.  An HttpModule
is a little chunk of code that gets slipped into the HttpRuntime
via the web.config file.  It hooks an event in the HttpApplication
object and responds to it.  In this case, I'm hooking BeginRequest
and setting a new Stream on the HttpResponse.Filter to compress
the output.

For instructions on how to slip the HttpCompressionModule in,
see the provided web.config file.  It shows what entries have
to be added to the web.config to set things up.

So, to get things going, here's what you have to do:
1) compile the project into a library (or just use the
     version in /lib)
2) move the .dll that comes from compilation to the /bin directory of
   your asp.net web app
3) add the entries to the web.config of your asp.net app

That's it.  That should get you going.



--b