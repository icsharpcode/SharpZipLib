using System;
using System.Xml;
using System.Xml.Serialization;
using System.Configuration;

namespace blowery.Web.HttpModules
{
	/// <summary>
	/// This class acts as a factory for the configuration settings.
	/// </summary>
	internal class HttpCompressionModuleSectionHandler : IConfigurationSectionHandler
	{
    
    /// <summary>
    /// Create a new config section handler.  This is of type <see cref="HttpCompressionModuleSettings"/>
    /// </summary>
    object IConfigurationSectionHandler.Create(object parent, object configContext, XmlNode configSection) {
      return new HttpCompressionModuleSettings(configSection);
    }
	}

}
