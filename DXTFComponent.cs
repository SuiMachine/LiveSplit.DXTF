using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.DXTF
{
    class DXTFComponent : LogicComponent
    {
        public override string ComponentName
        {
            get { return "DXTF"; }
        }

        public DXTFSettings Settings { get; set; }

        public bool Disposed { get; private set; }
        public bool IsLayoutComponent { get; private set; }

        private TimerModel _timer;
        private GameMemory _gameMemory;
        private LiveSplitState _state;
        private bool[] missionSplits;

        public DXTFComponent(LiveSplitState state, bool isLayoutComponent)
        {
            _state = state;
            this.IsLayoutComponent = isLayoutComponent;

            _timer = new TimerModel { CurrentState = state };
            _timer.CurrentState.OnStart += timer_OnStart;

            missionSplits = new bool[(int)Missions.Total];
            this.Settings = new DXTFSettings();

            _gameMemory = new GameMemory(this.Settings);
			_gameMemory.OnFirstLevelAutostart += _gameMemory_OnFirstLevelAutostart;
            _gameMemory.OnLoadStarted += gameMemory_OnLoadStarted;
            _gameMemory.OnLoadFinished += gameMemory_OnLoadFinished;
			_gameMemory.OnLevelChanged += _gameMemory_OnLevelChanged;
			_gameMemory.OnFirstLevelLoad += _gameMemory_OnFirstLevelLoad;

            state.OnStart += State_OnStart;
            _gameMemory.StartMonitoring();
        }

		private void _gameMemory_OnLevelChanged(object sender, int mission)
        {
            var missionEnum = (Missions)mission;
            switch (missionEnum)
            {
                case Missions.Main_Moscow_KillKontrasky:
                    if (!missionSplits[mission] && Settings.Split_00Moscow)
                        _timer.Split();
                    break;
                case Missions.CostaRica1_ConspiracyConfrontNamir:
                    if (!missionSplits[mission] && Settings.Split_01CostaRica)
                        _timer.Split();
                    break;
                case Missions.Prologue_PanamaShadowAugs:
                    if (!missionSplits[mission] && Settings.Split_02Prologue)
                        _timer.Split();
                    break;
                case Missions.Main_Panama1_LocateAlvarezAraujo:
                    if (!missionSplits[mission] && Settings.Split_03Panama1)
                        _timer.Split();
                    break;
                case Missions.Main_Panama2_SecureNeuropozne:
                    if (!missionSplits[mission] && Settings.Split_04Panama2)
                        _timer.Split();
                    break;
                case Missions.Panama3_ShadowAugs:
                    if (!missionSplits[mission] && Settings.Split_05Panama3)
                        _timer.Split();
                    break;
                case Missions.Side_Panama4_DrugRunner:
                    if (!missionSplits[mission] && Settings.Split_06Panama4)
                        _timer.Split();
                    break;
                case Missions.Side_Panama5_MissingJunkie:
                    if (!missionSplits[mission] && Settings.Split_07Panama5)
                        _timer.Split();
                    break;
                case Missions.Side_Panama6_DirtyDeeds:
                    if (!missionSplits[mission] && Settings.Split_08Panama6)
                        _timer.Split();
                    break;
                case Missions.Panama7_RattingOut:
                    if (!missionSplits[mission] && Settings.Split_09Panama7)
                        _timer.Split();
                    break;
            }
            missionSplits[mission] = true;
        }

        private void _gameMemory_OnFirstLevelAutostart(object sender, EventArgs e)
		{
            if(this.Settings.AutorestartOnFirstLevel)
			{
                _timer.Reset();
			}
		}

        public override void Dispose()
        {
            this.Disposed = true;
            _timer.CurrentState.OnStart -= timer_OnStart;
            _gameMemory.OnFirstLevelAutostart -= _gameMemory_OnFirstLevelAutostart;
            _gameMemory.OnLoadStarted -= gameMemory_OnLoadStarted;
            _gameMemory.OnLoadFinished -= gameMemory_OnLoadFinished;
            _gameMemory.OnLevelChanged -= _gameMemory_OnLevelChanged;
            _gameMemory.OnFirstLevelLoad -= _gameMemory_OnFirstLevelLoad;

            _state.OnStart -= State_OnStart;

            if (_gameMemory != null)
            {
                _gameMemory.Stop();
            }

        }
        private void timer_OnStart(object sender, EventArgs e)
        {
            _timer.InitializeGameTime();
        }

        private void _gameMemory_OnFirstLevelLoad(object sender, EventArgs e)
		{
            if (this.Settings.StartOnFirstLevelLoad)
            {
                _timer.Start();
            }
        }

        void State_OnStart(object sender, EventArgs e)
        {
            for(int i=0; i<missionSplits.Length; i++)
			{
                missionSplits[i] = false;
			}
        }

        void gameMemory_OnLoadStarted(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = true;
        }

        void gameMemory_OnLoadFinished(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = false;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return this.Settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return this.Settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            this.Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
        //public override void RenameComparison(string oldName, string newName) { }
    }
}
