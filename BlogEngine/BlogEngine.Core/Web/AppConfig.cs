using System;
using System.IO;
using System.Xml;

namespace BlogEngine.Core.Web
{
    /// <summary>
    /// Helper class for reading and modifying appSettings in the XML config file.
    /// Replaces the legacy AppSettingsReader base class (System.Configuration).
    /// </summary>
    public class AppConfig
    {
        private XmlDocument cfgDoc = new XmlDocument();
        public string docName = "web.config";
        private XmlNode node = null;

        /// <summary>
        /// Creates a new AppConfig instance.
        /// </summary>
        public static AppConfig Instance()
        {
            return new AppConfig();
        }

        /// <summary>
        /// Sets a value in the appSettings section of the config file.
        /// </summary>
        public bool SetValue(string key, string value)
        {
            object _lock = new object();

            lock (_lock)
            {
                loadConfigDoc();
                node = cfgDoc.SelectSingleNode("//appSettings");

                if (node == null)
                {
                    throw new InvalidOperationException("appSettings section not found");
                }

                try
                {
                    XmlElement addElem = (XmlElement)node.SelectSingleNode("//add[@key='" + key + "']");
                    if (addElem != null)
                    {
                        addElem.SetAttribute("value", value);
                    }
                    else
                    {
                        XmlElement entry = cfgDoc.CreateElement("add");
                        entry.SetAttribute("key", key);
                        entry.SetAttribute("value", value);
                        node.AppendChild(entry);
                    }
                    saveConfigDoc(docName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void saveConfigDoc(string cfgDocPath)
        {
            try
            {
                XmlTextWriter writer = new XmlTextWriter(cfgDocPath, null);
                writer.Formatting = Formatting.Indented;
                cfgDoc.WriteTo(writer);
                writer.Flush();
                writer.Close();
                return;
            }
            catch (Exception e)
            {
                throw new FileLoadException("Unable to load the web.config file for modification", e.InnerException);
            }
        }

        /// <summary>
        /// Removes an element from the appSettings section.
        /// </summary>
        public bool removeElement(string elementKey)
        {
            try
            {
                loadConfigDoc();
                node = cfgDoc.SelectSingleNode("//appSettings");
                if (node == null)
                {
                    throw new InvalidOperationException("appSettings section not found");
                }
                node.RemoveChild(node.SelectSingleNode("//add[@key='" + elementKey + "']"));
                saveConfigDoc(docName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void loadConfigDoc()
        {
            docName = BlogConfig.MapPath(docName);
            cfgDoc.Load(docName);
            return;
        }
    }
}
