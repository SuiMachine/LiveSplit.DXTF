using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveSplit.ComponentUtil;


namespace LiveSplit.DXTF
{
    class GameMemory
    {
        public event EventHandler OnLoadStarted;
        public event EventHandler OnLoadFinished;
        public event EventHandler OnLevelChanged;
        public event EventHandler OnFirstLevelLoad;
        public event EventHandler OnFirstLevelAutostart;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private SynchronizationContext _uiThread;
        private List<int> _ignorePIDs;

        private DeepPointer _MJustLoadedLevel;
        private DeepPointer _MLevelToLoadPtr;


        private enum ExpectedDllSizes
        {
            DXTF = 11948032,
        }

        public void resetSplitStates()
        {
        }

        public GameMemory(DXTFSettings componentSettings)
        {
            resetSplitStates();

            _MJustLoadedLevel = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x164, 0x13);
            _MLevelToLoadPtr = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x164, 0x8, 0xC);


            _ignorePIDs = new List<int>();
        }

        public void StartMonitoring()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException();
            }
            if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
            {
                throw new InvalidOperationException("SynchronizationContext.Current is not a UI thread.");
            }

            _uiThread = SynchronizationContext.Current;
            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(MemoryReadThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null || _thread.Status != TaskStatus.Running)
            {
                return;
            }

            _cancelSource.Cancel();
            _thread.Wait();
        }

        void MemoryReadThread()
        {
            Debug.WriteLine("[NoLoads] MemoryReadThread");

            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Debug.WriteLine("[NoLoads] Waiting for DeusEx_steam.exe...");
                    uint frameCounter = 0;
                    
                    Process game;
                    while ((game = GetGameProcess()) == null)
                    {
                        Thread.Sleep(250);
                        if (_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    Debug.WriteLine("[NoLoads] Got games process!");

                    bool prevIsLoading = false;
                    bool loadingStarted = false;
                    string prevLevelName = "";

                    while (!game.HasExited)
                    {
                        bool isLoading = _MJustLoadedLevel.Deref<bool>(game);
                        string levelName = _MLevelToLoadPtr.DerefString(game, ReadStringType.UTF16, 40, "");

                        if (isLoading != prevIsLoading)
                        {
                            if (isLoading)
                            {
                                Debug.WriteLine(String.Format("[NoLoads] Load Start - {0}", frameCounter));

                                loadingStarted = true;

                                // pause game timer
                                _uiThread.Post(d =>
                                {
                                    if (this.OnLoadStarted != null)
                                    {
                                        this.OnLoadStarted(this, EventArgs.Empty);
                                    }
                                }, null);
                            }
                            else
                            {
                                Debug.WriteLine(String.Format("[NoLoads] Load End - {0}", frameCounter));

                                if (loadingStarted)
                                {
                                    loadingStarted = false;

                                    // unpause game timer
                                    _uiThread.Post(d =>
                                    {
                                        if (this.OnLoadFinished != null)
                                        {
                                            this.OnLoadFinished(this, EventArgs.Empty);
                                        }
                                    }, null);
                                }

                                if (levelName == "CostaRicaSafehouse")
                                {
                                    //Autorestart + autostart invoke event
                                    _uiThread.Post(d =>
                                    {
                                        if (this.OnFirstLevelLoad != null)
                                        {
                                            this.OnFirstLevelLoad(this, EventArgs.Empty);
                                        }
                                    }, null);
                                }
                            }
                        }

                        if(levelName != prevLevelName && prevLevelName != "" && levelName != "")
						{
                            if(levelName == "CostaRicaSafehouse")
							{
                                //Autorestart + autostart invoke event
                                _uiThread.Post(d =>
                                {
                                    if (this.OnFirstLevelAutostart != null)
                                    {
                                        this.OnFirstLevelAutostart(this, EventArgs.Empty);
                                    }
                                }, null);
                            }

                            Debug.WriteLine($"Level changed: {prevLevelName} -> {levelName}");
						}

                        prevIsLoading = isLoading;
                        prevLevelName = levelName;

                        frameCounter++;

                        Thread.Sleep(15);

                        if (_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }

        Process GetGameProcess()
        {
            Process game = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.ToLower() == "deusex_steam" && !p.HasExited && !_ignorePIDs.Contains(p.Id));
            if (game == null)
            {
                return null;
            }

            if (game.MainModuleWow64Safe().ModuleMemorySize != (int)ExpectedDllSizes.DXTF )
            {
                _ignorePIDs.Add(game.Id);
                _uiThread.Send(d => MessageBox.Show("Unexpected game version. Deus Ex The Fall (1.1) is required.", "LiveSplit.DXTF",
                    MessageBoxButtons.OK, MessageBoxIcon.Error), null);
                return null;
            }

            return game;
        }
    }
}
