namespace BlogEngine.Core.DataStore
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    using BlogEngine.Core.Providers;

    /// <summary>
    /// Class to encapsulate saving and retreaving 
    ///     XML documents to and from data storage
    /// </summary>
    public class XmlDocumentBehavior : ISettingsBehavior
    {
        #region Implemented Interfaces

        #region ISettingsBehavior

        /// <summary>
        /// Gets settings from data store
        /// </summary>
        public object GetSettings(ExtensionType extensionType, string extensionId)
        {
            WidgetData widgetData;
            var xml = new XmlDocument();

            var isXmlProvider = BlogService.Provider is XmlBlogProvider;

            if (isXmlProvider)
            {
                var stm = (Stream)BlogService.LoadFromDataStore(extensionType, extensionId);
                if (stm != null)
                {
                    var x = new XmlSerializer(typeof(XmlDocument));
                    xml = (XmlDocument)x.Deserialize(stm);
                    stm.Close();
                }
            }
            else
            {
                var o = BlogService.LoadFromDataStore(extensionType, extensionId);
                if (!string.IsNullOrEmpty((string)o))
                {
                    var serializer = new XmlSerializer(typeof(WidgetData));
                    using (var reader = new StringReader((string)o))
                    {
                        widgetData = (WidgetData)serializer.Deserialize(reader);
                    }

                    if (widgetData.Settings.Length > 0)
                    {
                        xml.InnerXml = widgetData.Settings;
                    }
                }
            }

            return xml;
        }

        /// <summary>
        /// Saves XML document to data storage
        /// </summary>
        public bool SaveSettings(ExtensionType extensionType, string extensionId, object settings)
        {
            var xml = (XmlDocument)settings;

            var isXmlProvider = BlogService.Provider is XmlBlogProvider;

            if (isXmlProvider)
            {
                BlogService.SaveToDataStore(extensionType, extensionId, xml);
            }
            else
            {
                var wd = new WidgetData { Settings = xml.InnerXml };
                BlogService.SaveToDataStore(extensionType, extensionId, wd);
            }

            return true;
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Wrap around xml document
    /// </summary>
    [Serializable]
    public class WidgetData
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref = "WidgetData" /> class.
        /// </summary>
        public WidgetData()
        {
            this.Settings = string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public string Settings { get; set; }

        #endregion
    }
}
