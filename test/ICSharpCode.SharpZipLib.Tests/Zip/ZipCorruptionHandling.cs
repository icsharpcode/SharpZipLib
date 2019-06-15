using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;

namespace ICSharpCode.SharpZipLib.Tests.Zip
{
    public class ZipCorruptionHandling
    {

        const string TestFileZeroCodeLength = "UEsDBBQA+AAIANwyZ0U5T8HwjQAAAL8AAAAIABUAbGltZXJpY2t" +
            "VVAkAAzBXXFR6LmBUVXgEAPQB9AEFjTEOwjAQBHu/YkVDg3gHoUaivjgHtmKfI5+D5d9zbndHM6/AldFJQTIJ" +
            "PrVkPOkgce9QlJFi5hr9rhD+cUUvZ9qgnuRuBAtId97Qw0AL1Kbw5h6MykeKdlyWdlWs7OlUdgsodRqKVo0v8" +
            "JWyGWZ6mLpuiii2t2Bl0mZ54QksOIpqXNPATF/eH1BLAQIXAxQAAgAIANxQZ0U5T8HwjQAAAL8AAAAIAA0AAA" +
            "AAAAEAAACggQAAAABsaW1lcgEAQwAAAMgAAAAAAA==";
		
        [Test]
		[Category("Zip")]
        public void ZeroCodeLengthZipFile()
        {
            Assert.Throws<SharpZipBaseException>(() => {
                Exception threadException = null;
                var testThread = new Thread(() => {
                    try {
                        var fileBytes = Convert.FromBase64String(TestFileZeroCodeLength);
                        using (var ms = new MemoryStream(fileBytes))
                        using (var zip = new ZipInputStream(ms))
                        {
                            while (zip.GetNextEntry() != null) { }
                        }
                    }
                    catch (Exception x) {
                        threadException = x;
                    }
                });

                testThread.Start();

                if(!testThread.Join(5000)){
                    // Aborting threads is deprecated in .NET Core, but if we don't do this,
                    // the poor CI will hang for 2 hours upon running this test
                    ThreadEx.Abort(testThread);
                    throw new TimeoutException("Timed out waiting for GetNextEntry() to return");
                }
                else  if(threadException != null){
                    throw threadException;
                }
            });
        }

    }

}