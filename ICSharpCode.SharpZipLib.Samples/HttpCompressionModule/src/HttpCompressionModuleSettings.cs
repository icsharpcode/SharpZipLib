using System;
using System.Xml;

namespace blowery.Web.HttpModules {
  /// <summary>
  /// This class encapsulates the settings for an HttpCompressionModule
  /// </summary>
  internal class HttpCompressionModuleSettings {
    
    /// <summary>
    /// Create an HttpCompressionModuleSettings from an XmlNode
    /// </summary>
    /// <param name="node">The XmlNode to configure from</param>
    public HttpCompressionModuleSettings(XmlNode node) : this() {
      
      if(node == null) {
        return;
      }

      XmlAttribute preferredAlgorithm = node.Attributes["preferredAlgorithm"];
      if(preferredAlgorithm != null) {
        switch(preferredAlgorithm.Value.ToLower()) {
          case "gzip":
            _preferredAlgorithm = CompressionTypes.GZip;
            break;
          case "deflate":
          default:
            _preferredAlgorithm = CompressionTypes.Deflate;
            break;
        }
      }

      XmlAttribute compressionLevel = node.Attributes["compressionLevel"];
      if(compressionLevel != null) {
        switch(compressionLevel.Value.ToLower()) {
          case "high":
            _compressionLevel = CompressionLevels.High;
            break;
          case "low":
            _compressionLevel = CompressionLevels.Low;
            break;
          case "normal":
          default:
            _compressionLevel = CompressionLevels.Normal;
            break;
        }
      }
    }

    private HttpCompressionModuleSettings() {
      _preferredAlgorithm = CompressionTypes.Deflate;
      _compressionLevel = CompressionLevels.Normal;
    }


    /// <summary>
    /// Get the current settings from the xml config file
    /// </summary>
    public static HttpCompressionModuleSettings GetSettings() {
      HttpCompressionModuleSettings settings = (HttpCompressionModuleSettings)System.Configuration.ConfigurationSettings.GetConfig("blowery.web/httpCompressionModule");
      if(settings == null)
        return HttpCompressionModuleSettings.DEFAULT;
      else
        return settings;
    }

    /// <summary>
    /// The default settings.  Deflate + normal.
    /// </summary>
    public static readonly HttpCompressionModuleSettings DEFAULT = new HttpCompressionModuleSettings();

    /// <summary>
    /// The preferred algorithm to use for compression
    /// </summary>
    public CompressionTypes PreferredAlgorithm {
      get { return _preferredAlgorithm; }
    }
    private CompressionTypes _preferredAlgorithm;

    /// <summary>
    /// The preferred compression level
    /// </summary>
    public CompressionLevels CompressionLevel {
      get { return _compressionLevel; }
    }
    private CompressionLevels _compressionLevel;

    
  }

  /// <summary>
  /// The available types of compression to use with the HttpCompressionModule
  /// </summary>
  public enum CompressionTypes {
    /// <summary>
    /// Use the deflate algorithm
    /// </summary>
    Deflate,
    /// <summary>
    /// Use the gzip algorithm
    /// </summary>
    GZip
  }

  /// <summary>
  /// The level of compression to use with some algorithms
  /// </summary>
  public enum CompressionLevels {
    /// <summary>
    /// Use a normal level of compression.  Compromises between speed and size
    /// </summary>
    Normal,
    /// <summary>
    /// Use a high level of compression.  Sacrifices speed for size.
    /// </summary>
    High,
    /// <summary>
    /// Use a low level of compression.  Sacrifices size for speed.
    /// </summary>
    Low
  }

}

