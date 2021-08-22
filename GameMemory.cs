//#define LevelChangeDebugMsg
//#define NoLoadsDebugMsg

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
	#region Game Data
	public enum GameState
	{
		Startup,
		FadeStaticOut,
		FadeKeyArtIn,
		FadeKeyArtOut,
		ObbDownload,
		LoadLocalization,
		PreMenu,
		Menu,
		FadeBlackToGameLoad,
		FadeInGameLoad,
		FadeOutGameLoad,
		WaitingOnLoad,
		Game,
		Paused,
		FadeBlackToMenuLoad,
		FadeInMenuLoad,
		FramePadding,
		LoadMenuFromGame,
		SetupMenuFromGame,
		FadeOutMenuLoad
	}

	public enum MissionStatus
	{
		Unknown,
		Acquired,
		Completed_Success,
		Completed_Fail
	}

	public enum Missions
	{
		Main_Moscow_KillKontrasky,
		CostaRica1_ConspiracyConfrontNamir,
		Prologue_PanamaShadowAugs,
		Main_Panama1_LocateAlvarezAraujo,
		Main_Panama2_SecureNeuropozne,
		Panama3_ShadowAugs,
		Side_Panama4_DrugRunner,
		Side_Panama5_MissingJunkie,
		Side_Panama6_DirtyDeeds,
		Panama7_RattingOut,
		Panama8_Stalkers,
		Total //There is a total of 16 missions in the code, but 6 of them are in New York that never got released
			  //A lot of these are not actually used by the game - episode 2 that we never got.
	}

	public struct MissionStruct
	{
		public MissionStatus missionStatus;
		public MissionStatus[] subMissionStatues;

		public MissionStruct(int subMissionLenght)
		{
			missionStatus = MissionStatus.Unknown;
			subMissionStatues = new MissionStatus[subMissionLenght];
		}
	}
	#endregion


	class GameMemory
	{
		public event EventHandler OnLoadStarted;
		public event EventHandler OnLoadFinished;
		public event EventHandler<int> OnLevelChanged;
		public event EventHandler OnFirstLevelLoad;
		public event EventHandler OnFirstLevelAutostart;

		private Task _thread;
		private CancellationTokenSource _cancelSource;
		private SynchronizationContext _uiThread;
		private List<int> _ignorePIDs;

		private DeepPointer _MGameState;
		private DeepPointer _MJustLoadedLevel;
		private DeepPointer _MLevelToLoadPtr;
		private DeepPointer _MMissions;


		private enum ExpectedDllSizes
		{
			DXTF = 11948032,
		}

		public GameMemory(DXTFSettings componentSettings)
		{
			_MJustLoadedLevel = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x164, 0x13);
			_MGameState = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x164, 0x0, 0x58);
			_MLevelToLoadPtr = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x164, 0x8, 0xC);
			_MMissions = new DeepPointer(0x009E2A64, 0x50, 0x8, 0x50, 0x158, 0x1a8);

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

					GameState prevGameState = GameState.Startup;
					bool prevIsJustLoadedLevel = false;
					bool loadingStarted = false;
					string prevLevelName = "";
					MissionStruct[] storyProgression = new MissionStruct[(int)Missions.Total]
					{
						new MissionStruct(3),
						new MissionStruct(4),
						new MissionStruct(2),
						new MissionStruct(4),
						new MissionStruct(5),
						new MissionStruct(5),
						new MissionStruct(3),
						new MissionStruct(2),
						new MissionStruct(2),
						new MissionStruct(3),
						new MissionStruct(2)
					};

					MissionStruct[] previousStoryProgression = new MissionStruct[storyProgression.Length];
					for (int i = 0; i < storyProgression.Length; i++)
					{
						previousStoryProgression[i] = storyProgression[i];
					}

					while (!game.HasExited)
					{
						bool isJustLoadedLevel = _MJustLoadedLevel.Deref<bool>(game);
						GameState gameState = _MGameState.Deref<GameState>(game);
						string levelName = _MLevelToLoadPtr.DerefString(game, ReadStringType.UTF16, 40, "");
						var missionPtrArray = (IntPtr)_MMissions.Deref<int>(game);
						if (missionPtrArray != null)
						{
							var missionAmounts = new DeepPointer(missionPtrArray + 0xC).Deref<int>(game);

							for (int i = 0; i < storyProgression.Length; i++)
							{
								var missionPointer = (IntPtr)new DeepPointer(missionPtrArray + 0x10 + i * 4).Deref<int>(game);
								storyProgression[i].missionStatus = new DeepPointer(missionPointer + 0x24).Deref<MissionStatus>(game);

								if (storyProgression[i].missionStatus == MissionStatus.Completed_Success)
								{
									for (int j = 0; j < storyProgression[i].subMissionStatues.Length; j++)
									{
										storyProgression[i].subMissionStatues[j] = MissionStatus.Completed_Success;
									}
								}
								else if (storyProgression[i].missionStatus == MissionStatus.Completed_Fail)
								{
									for (int j = 0; j < storyProgression[i].subMissionStatues.Length; j++)
									{
										storyProgression[i].subMissionStatues[j] = MissionStatus.Completed_Fail;
									}
								}
								else if (storyProgression[i].missionStatus == MissionStatus.Unknown)
								{
									for (int j = 0; j < storyProgression[i].subMissionStatues.Length; j++)
									{
										storyProgression[i].subMissionStatues[j] = MissionStatus.Unknown;
									}
								}
								else if (storyProgression[i].missionStatus == MissionStatus.Acquired)
								{
									for (int j = 0; j < storyProgression[i].subMissionStatues.Length; j++)
									{
										var subMissionStatues = new DeepPointer(missionPointer + 0xC, 0x10 + i * 4, 0x10).Deref<MissionStatus>(game);
										storyProgression[i].subMissionStatues[j] = subMissionStatues;
										if (subMissionStatues > MissionStatus.Completed_Fail)
											Debug.WriteLine($"Something is wrong with mission {(Missions)i}, submission {j}.");
									}
								}


							}
						}


						if (gameState != prevGameState)
						{
							switch (gameState)
							{
								case GameState.FadeInGameLoad:
								case GameState.WaitingOnLoad:
								case GameState.FadeOutGameLoad:
									{
#if NoLoadsDebugMsg
                                        Debug.WriteLine(String.Format("[NoLoads] Load Start - {0}", frameCounter));
#endif

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
									break;
								default:
									{
#if NoLoadsDebugMsg
                                        Debug.WriteLine(String.Format("[NoLoads] Load End - {0}", frameCounter));
#endif

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
									}
									break;
							}

#if NoLoadsDebugMsg
                            Debug.WriteLine($"Game state changed from {prevGameState} to {gameState}");
#endif
						}


						if (isJustLoadedLevel != prevIsJustLoadedLevel)
						{
							if (!isJustLoadedLevel)
							{
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
									//Autostart invoke event
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

						if (levelName != prevLevelName && prevLevelName != "" && levelName != "")
						{
							if (levelName == "CostaRicaSafehouse" && prevLevelName == "Menu")
							{
								//Autorestart
								_uiThread.Post(d =>
								{
									if (this.OnFirstLevelAutostart != null)
									{
										this.OnFirstLevelAutostart(this, EventArgs.Empty);
									}
								}, null);
							}

#if LevelChangeDebugMsg
                            Debug.WriteLine($"Level changed: {prevLevelName} -> {levelName}");
#endif
						}

						bool alreadySplit = false;
						for (int i = 0; i < storyProgression.Length; i++)
						{
							if (storyProgression[i].missionStatus != previousStoryProgression[i].missionStatus)
							{
								if (!alreadySplit && storyProgression[i].missionStatus == MissionStatus.Completed_Success)
								{
									alreadySplit = true;
									_uiThread.Send(d =>
									{
										if (this.OnLevelChanged != null)
										{
											this.OnLevelChanged(this, i);
										}
									}, null);
								}
#if DEBUG
								Debug.WriteLine($"Story progression for {(Missions)i} changed {previousStoryProgression[i]} -> {storyProgression[i]}");
#endif
							}
						}

						prevGameState = gameState;
						prevLevelName = levelName;
						prevIsJustLoadedLevel = isJustLoadedLevel;

						for (int i = 0; i < storyProgression.Length; i++)
						{
							previousStoryProgression[i] = storyProgression[i];
						}

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

			if (game.MainModuleWow64Safe().ModuleMemorySize != (int)ExpectedDllSizes.DXTF)
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
