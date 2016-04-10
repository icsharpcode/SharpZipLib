<%@ WebHandler Language="C#" Class="ExistingImage" %>

using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class ExistingImage : IHttpHandler
{
    
    public void ProcessRequest (HttpContext context)
    {
        context.Response.ContentType = "image/gif";
        context.Response.WriteFile("blowery.gif");
    }
   

    public bool IsReusable
    {
        get { return true; }
    }
}