<%@ WebHandler Language="C#" Class="ImageMaker" %>

using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class ImageMaker : IHttpHandler
{
    public void ProcessRequest (HttpContext context)
    {
        using(Bitmap myFoo = new Bitmap(200,200,PixelFormat.Format24bppRgb)) {
          using(Graphics g = Graphics.FromImage(myFoo)) {
          
            g.FillRectangle(Brushes.Gray,0,0,200,200);
            g.FillPie(Brushes.Yellow,100,100,100,100,0,90);
          
          }
          context.Response.ContentType = "image/png";
          
          // have to do this crap because saving
          // png directly to HttpResponse.OutputStream
          // is broken in the 1.0 bits (at least up to sp2)
          // should just be
          //  myFoo.Save(context.Response.OutputStream, ImageFormat.Png);
          MemoryStream ms = new MemoryStream();
          myFoo.Save(ms, ImageFormat.Png);
          context.Response.OutputStream.Write(ms.ToArray(), 0, (int)ms.Length);
        }
    }

    public bool IsReusable
    {
        get { return true; }
    }
}