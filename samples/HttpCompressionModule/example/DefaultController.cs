using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Example {
  /// <summary>
  /// This class acts as a controller for the default.aspx page.  
  /// It handles Page events and maps events from
  /// the classes it contains to event handlers.
  /// </summary>
  public class DefaultController : Page {
    
    /// <summary>
    /// A label on the form
    /// </summary>
    protected Label MyLabel;
    
    /// <summary>
    /// Override of OnLoad that adds some processing.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnLoad(EventArgs e) {
      try {
        MyLabel.Text = "Right Now: " + DateTime.Now.ToString();
      }finally {
        base.OnLoad(e);  // be sure to call base to fire the event
      }
    }

    
  }
}
