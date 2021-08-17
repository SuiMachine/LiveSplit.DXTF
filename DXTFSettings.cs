using System;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.DXTF
{
    public partial class DXTFSettings : UserControl
    {
        public bool StartOnFirstLevelLoad { get; set; }
        public bool AutorestartOnFirstLevel { get; set; }

        public bool SplitOnLevelChange { get; set; }

        private const bool DEFAULT_STARTONFIRSTLEVELLOAD = true;
        private const bool DEFAULT_AUTORESTARTONFIRSTLEVEL = true;
        private const bool DEFAULT_SPLITONLEVELCHANGE = true;

        public DXTFSettings()
        {
            InitializeComponent();

            //defaults
            StartOnFirstLevelLoad = DEFAULT_STARTONFIRSTLEVELLOAD;
            AutorestartOnFirstLevel = DEFAULT_AUTORESTARTONFIRSTLEVEL;
            SplitOnLevelChange = DEFAULT_SPLITONLEVELCHANGE;
        }

        public XmlNode GetSettings(XmlDocument doc)
		{
            XmlElement settingNode = doc.CreateElement("Settings");
            settingNode.AppendChild(ToElement(doc, "StartOnFirstLevelLoad", this.StartOnFirstLevelLoad));
            settingNode.AppendChild(ToElement(doc, "AutorestartOnFirstLevel", this.AutorestartOnFirstLevel));
            settingNode.AppendChild(ToElement(doc, "SplitOnLevelChange", this.SplitOnLevelChange));
            return settingNode;
		}

        public void SetSettings(XmlNode settings)
        {
            this.StartOnFirstLevelLoad = ParseBool(settings, "StartOnFirstLevelLoad", DEFAULT_STARTONFIRSTLEVELLOAD);
            this.AutorestartOnFirstLevel = ParseBool(settings, "AutorestartOnFirstLevel", DEFAULT_AUTORESTARTONFIRSTLEVEL);
            this.SplitOnLevelChange = ParseBool(settings, "SplitOnLevelChange", DEFAULT_SPLITONLEVELCHANGE);
        }

        static bool ParseBool(XmlNode settings, string setting, bool default_ = false)
        {
            bool val;
            return settings[setting] != null ?
                (Boolean.TryParse(settings[setting].InnerText, out val) ? val : default_)
                : default_;
        }

        static int ParseInt(XmlNode settings, string setting, int default_ = 0)
        {
            int val;
            return settings[setting] != null ?
                (int.TryParse(settings[setting].InnerText, out val) ? val : default_)
                : default_;
        }

        static XmlElement ToElement<T>(XmlDocument document, string name, T value)
        {
            XmlElement str = document.CreateElement(name);
            str.InnerText = value.ToString();
            return str;
        }
    }
}
