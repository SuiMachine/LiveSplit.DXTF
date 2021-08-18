using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Xml;
using System.Windows.Forms;
using System.Diagnostics;

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

        public DXTFComponent(LiveSplitState state, bool isLayoutComponent)
        {
            _state = state;
            this.IsLayoutComponent = isLayoutComponent;

            _timer = new TimerModel { CurrentState = state };
            _timer.CurrentState.OnStart += timer_OnStart;

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

        private void _gameMemory_OnLevelChanged(object sender, EventArgs e)
        {
            if(this.Settings.SplitOnLevelChange)
			{
                _timer.Split();
			}
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
            _gameMemory.resetSplitStates();
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
