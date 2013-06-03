using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using RKNTracker.DataClasses;
using RKNTracker.Helpers;
using RKNTracker;

namespace RKNTrackerService
{
	public partial class TrackerService : ServiceBase
	{
		public delegate void InvokeDelegate();

		private RKNMapService.Map Service = new RKNMapService.Map();
		private EventLog eventLog = new System.Diagnostics.EventLog();

		private long LastRKNUpdate = 0;
		private long RKNUpdateDelay = 10000000;

		public TrackerService()
		{
			this.ServiceName = "RKN Tracker";
	
			this.eventLog.Source = this.ServiceName;
			this.eventLog.Log = "Application";

			((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
			if (!EventLog.SourceExists(this.eventLog.Source))
			{
				EventLog.CreateEventSource(this.eventLog.Source, this.eventLog.Log);
			}
			((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			//Debugger.Launch();
			InitializeAOHook();
		}

		protected override void OnStop()
		{
		}

		public void InitializeAOHook()
		{
			try
			{
				API.AoHook = new HookInfoTracker();
				API.AoHook.Provider.HookAo();
				API.AoHook.DisplayEvent += new EventHandler(DisplayEvent);
			}
			catch (Exception e)
			{
				eventLog.WriteEntry("Error: " + e.Message, EventLogEntryType.Error);
			}
		}

		void DisplayEvent(object sender, EventArgs e)
		{
			UpdateRKNLocation();
		}

		private void UpdateRKNLocation()
		{
			if (DateTime.Now.Ticks > (LastRKNUpdate + RKNUpdateDelay))
			{
				PlayerInfo[] Characters = API.State.PlayerInfo.Values.ToArray();

				List<RKNMapService.Character> RKNCharList = new List<RKNMapService.Character>();

				foreach (PlayerInfo PlayerInfo in Characters)
				{
					RKNMapService.Character Character = new RKNMapService.Character();

					if (PlayerInfo.IsHooked)
					{
						Character.NickName = PlayerInfo.Name;
						Character.X = PlayerInfo.Position.X;
						Character.Y = PlayerInfo.Position.Z;
						Character.Zone = Convert.ToInt32(PlayerInfo.Zone.ID);

						RKNCharList.Add(Character);
					}
				}

				if (RKNCharList.Count > 0)
				{
					try
					{
						Service.UpdateCharacterLocation(RKNCharList.ToArray());
					}
					catch (Exception e)
					{
						eventLog.WriteEntry("Error: " + e.Message, EventLogEntryType.Error);
					}
				}

				LastRKNUpdate = DateTime.Now.Ticks;
			}
		}
	}
}
