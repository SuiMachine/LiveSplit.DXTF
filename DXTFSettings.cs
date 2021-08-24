using System;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace LiveSplit.DXTF
{
	[Serializable]
	public class MissionSplit
	{
		[Serializable]
		public class SubMissionSplit
		{
			[XmlAttribute] public string Name { get; set; }
			[XmlAttribute] public bool Split { get; set; }

			public SubMissionSplit(string Name, bool Split)
			{
				this.Name = Name;
				this.Split = Split;
			}

			public void Restart()
			{
				Split = false;
			}
		}

		[XmlAttribute] public string Name { get; set; }
		[XmlElement] public SubMissionSplit[] SubMissions { get; set; }

		public MissionSplit()
		{
			Name = "";
			SubMissions = new SubMissionSplit[0];
		}

		public MissionSplit(string Name, SubMissionSplit[] SubMissions)
		{
			this.Name = Name;
			this.SubMissions = SubMissions;
		}

		public void Restart()
		{
			foreach (var submission in SubMissions)
			{
				submission.Restart();
			}
		}

		public static MissionSplit[] GenerateSplitDefaults()
		{
			return new MissionSplit[]
			{
				new MissionSplit("M1 - Kill Mikhail Kontarsky", new SubMissionSplit[]
				{
					new SubMissionSplit("Enter Hotel Novoe Rostov", false),
					new SubMissionSplit("Unplug the decoy", false),
					new SubMissionSplit("Decoy", true)
				}),
				new MissionSplit("M2 - Investigate Tyrants", new SubMissionSplit[]
				{
					new SubMissionSplit("Find Neuropzyne", false),
					new SubMissionSplit("Contact Janus", false),
					new SubMissionSplit("Talk to Anna", true),
					new SubMissionSplit("Leave Costa Rica", true)
				}),
				new MissionSplit("M3 - The Killing Floor", new SubMissionSplit[]
				{
					new SubMissionSplit("Discover the truth", false),
					new SubMissionSplit("Confront Namir", false)
				}),
				new MissionSplit("M4 - Investigate Tyrants", new SubMissionSplit[]
				{
					new SubMissionSplit("Find Neuropzyne", false),
					new SubMissionSplit("Contact Janus", true),
					new SubMissionSplit("Talk to Anna", true),
					new SubMissionSplit("Leave Costa Rica", true)
				})
			};
		}
	}

	public partial class DXTFSettings : UserControl
	{
		public bool StartOnFirstLevelLoad { get; set; }
		public bool AutorestartOnFirstLevel { get; set; }

		private const bool DEFAULT_STARTONFIRSTLEVELLOAD = true;
		private const bool DEFAULT_AUTORESTARTONFIRSTLEVEL = true;

		public bool Split_00Moscow { get; set; }
		public bool Split_01CostaRica { get; set; }
		public bool Split_02Prologue { get; set; }
		public bool Split_03Panama1 { get; set; }
		public bool Split_04Panama2 { get; set; }
		public bool Split_05Panama3 { get; set; }
		public bool Split_06Panama4 { get; set; }
		public bool Split_07Panama5 { get; set; }
		public bool Split_08Panama6 { get; set; }
		public bool Split_09Panama7 { get; set; }

		public bool DEFAULT_SPLIT_00MOSCOW = true;
		public bool DEFAULT_SPLIT_01COSTARICA = true;
		public bool DEFAULT_SPLIT_02PROLOGUE = true;
		public bool DEFAULT_SPLIT_03PANAMA1 = true;
		public bool DEFAULT_SPLIT_04PANAMA2 = true;
		public bool DEFAULT_SPLIT_05PANAMA3 = true;
		public bool DEFAULT_SPLIT_06PANAMA4 = true;
		public bool DEFAULT_SPLIT_07PANAMA5 = true;
		public bool DEFAULT_SPLIT_08PANAMA6 = true;
		public bool DEFAULT_SPLIT_09PANAMA7 = true;

		public DXTFSettings()
		{
			InitializeComponent();

			this.CB_Autorestart.DataBindings.Add("Checked", this, "AutorestartOnFirstLevel", false, DataSourceUpdateMode.OnPropertyChanged);
			this.CB_Autostart.DataBindings.Add("Checked", this, "StartOnFirstLevelLoad", false, DataSourceUpdateMode.OnPropertyChanged);


			//defaults
			StartOnFirstLevelLoad = DEFAULT_STARTONFIRSTLEVELLOAD;
			AutorestartOnFirstLevel = DEFAULT_AUTORESTARTONFIRSTLEVEL;

			Split_00Moscow = DEFAULT_SPLIT_00MOSCOW;
			Split_01CostaRica = DEFAULT_SPLIT_01COSTARICA;
			Split_02Prologue = DEFAULT_SPLIT_02PROLOGUE;
			Split_03Panama1 = DEFAULT_SPLIT_03PANAMA1;
			Split_04Panama2 = DEFAULT_SPLIT_04PANAMA2;
			Split_05Panama3 = DEFAULT_SPLIT_05PANAMA3;
			Split_06Panama4 = DEFAULT_SPLIT_06PANAMA4;
			Split_07Panama5 = DEFAULT_SPLIT_07PANAMA5;
			Split_08Panama6 = DEFAULT_SPLIT_08PANAMA6;
			Split_09Panama7 = DEFAULT_SPLIT_09PANAMA7;
		}

		public XmlNode GetSettings(XmlDocument doc)
		{
			XmlElement settingNode = doc.CreateElement("Settings");
			settingNode.AppendChild(ToElement(doc, "StartOnFirstLevelLoad", this.StartOnFirstLevelLoad));
			settingNode.AppendChild(ToElement(doc, "AutorestartOnFirstLevel", this.AutorestartOnFirstLevel));
			settingNode.AppendChild(ToElement(doc, "Split_00Moscow", this.Split_00Moscow));
			settingNode.AppendChild(ToElement(doc, "Split_01CostaRica", this.Split_01CostaRica));
			settingNode.AppendChild(ToElement(doc, "Split_02Prologue", this.Split_02Prologue));
			settingNode.AppendChild(ToElement(doc, "Split_03Panama1", this.Split_03Panama1));
			settingNode.AppendChild(ToElement(doc, "Split_04Panama2", this.Split_04Panama2));
			settingNode.AppendChild(ToElement(doc, "Split_05Panama3", this.Split_05Panama3));
			settingNode.AppendChild(ToElement(doc, "Split_06Panama4", this.Split_06Panama4));
			settingNode.AppendChild(ToElement(doc, "Split_07Panama5", this.Split_07Panama5));
			settingNode.AppendChild(ToElement(doc, "Split_08Panama6", this.Split_08Panama6));
			settingNode.AppendChild(ToElement(doc, "Split_09Panama7", this.Split_09Panama7));

			return settingNode;
		}

		public void SetSettings(XmlNode settings)
		{
			this.StartOnFirstLevelLoad = ParseBool(settings, "StartOnFirstLevelLoad", DEFAULT_STARTONFIRSTLEVELLOAD);
			this.AutorestartOnFirstLevel = ParseBool(settings, "AutorestartOnFirstLevel", DEFAULT_AUTORESTARTONFIRSTLEVEL);

			this.Split_00Moscow = ParseBool(settings, "Split_00Moscow", DEFAULT_SPLIT_00MOSCOW);
			this.Split_01CostaRica = ParseBool(settings, "Split_01CostaRica", DEFAULT_SPLIT_01COSTARICA);
			this.Split_02Prologue = ParseBool(settings, "Split_02Prologue", DEFAULT_SPLIT_02PROLOGUE);
			this.Split_03Panama1 = ParseBool(settings, "Split_03Panama1", DEFAULT_SPLIT_03PANAMA1);
			this.Split_04Panama2 = ParseBool(settings, "Split_04Panama2", DEFAULT_SPLIT_04PANAMA2);
			this.Split_05Panama3 = ParseBool(settings, "Split_05Panama3", DEFAULT_SPLIT_05PANAMA3);
			this.Split_06Panama4 = ParseBool(settings, "Split_06Panama4", DEFAULT_SPLIT_06PANAMA4);
			this.Split_07Panama5 = ParseBool(settings, "Split_07Panama5", DEFAULT_SPLIT_07PANAMA5);
			this.Split_08Panama6 = ParseBool(settings, "Split_08Panama6", DEFAULT_SPLIT_08PANAMA6);
			this.Split_09Panama7 = ParseBool(settings, "Split_09Panama7", DEFAULT_SPLIT_09PANAMA7);
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
