﻿/*
* Demoder.PlanetMapViewer
* Copyright (C) 2012, 2013 Demoder (demoder@demoder.me)
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using RKNTracker.DataClasses;
using System.Threading;
using RKNTracker.Tools;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RKNTracker.AoHook;
using RKNTracker.AoHookBridge.AONative;

namespace RKNTracker.Helpers
{
    public class HookInfoTracker : IDisposable
    {
        private Dictionary<int, PlayerInfoKey> ProcessCharacterMap = new Dictionary<int, PlayerInfoKey>();

        public Provider Provider { get; internal set; }

        public event Action TrackedDimensionChanged;

		public event EventHandler DisplayEvent;
		public EventArgs e = null;

		public void FireDisplayEvent()
		{
			if (DisplayEvent != null)
			{
				DisplayEvent(this, e);
			}
		}

        public HookInfoTracker()
        {
            this.Provider = new Provider();
            this.Provider.CharacterPositionEvent += HandleCharacterPositionEvent;
            this.Provider.HookStateChangeEvent += HandleHookStateChangeEvent;
            this.Provider.DynelNameEvent += HandleDynelNameEvent;
            this.Provider.CharacterLogin += HandleCharacterLoginEvent;
            this.Provider.CharacterLogout += new CharacterLogoutEventDelegate(HandleCharacterLogoutEvent);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public PlayerInfoKey GetActiveCharacter()
        {
            var foregroundWindow = GetForegroundWindow();
            foreach (var kvp in this.ProcessCharacterMap.ToArray())
            {
                var proc = Process.GetProcessById(kvp.Key);
                if (proc.MainWindowHandle.ToInt64() == foregroundWindow.ToInt64())
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        #region AO Hook Provider

        private void HandleCharacterLoginEvent(Provider sender, RKNTracker.AoHook.Events.CharacterLoginEventArgs e)
        {
            PlayerInfoKey key = new PlayerInfoKey(e.ServerID, e.CharacterID);

            lock (this.ProcessCharacterMap)
            {
                if (!this.ProcessCharacterMap.ContainsKey(e.ProcessID))
                {
                    this.ProcessCharacterMap[e.ProcessID] = key;
                }
                else if (this.ProcessCharacterMap[e.ProcessID] != key)
                {
                    API.State.PlayerInfo[this.ProcessCharacterMap[e.ProcessID]].IsHooked = false;
                    this.ProcessCharacterMap[e.ProcessID] = key;
                }
            }

            lock(API.State.PlayerInfo)
            {
                if (API.State.PlayerInfo.ContainsKey(key))
                {
                    API.State.PlayerInfo[key].IsHooked = true;
                }
            }
        }


        private void HandleCharacterLogoutEvent(Provider sender, AoHook.Events.CharacterLogoutEventArgs e)
        {
            PlayerInfoKey key = new PlayerInfoKey(e.ServerID, e.CharacterID);
            lock (this.ProcessCharacterMap)
            {
                if (!this.ProcessCharacterMap.ContainsKey(e.ProcessID))
                {
                    return;
                }
                else
                {
                    if (API.State.PlayerInfo.ContainsKey(this.ProcessCharacterMap[e.ProcessID]))
                    {
                        API.State.PlayerInfo[this.ProcessCharacterMap[e.ProcessID]].IsHooked = false;
                    }
                    this.ProcessCharacterMap.Remove(e.ProcessID);
                }
            }
        }


        private void HandleDynelNameEvent(Provider sender, RKNTracker.AoHook.Events.DynelNameEventArgs e)
        {
            if (!e.IsSelf) { return; }
            bool changed = false;
            PlayerInfoKey key = new PlayerInfoKey(e.ServerID, e.DynelID);

            lock (API.State.PlayerInfo)
            {
                if (!API.State.PlayerInfo.ContainsKey(key))
                {
                    API.State.PlayerInfo[key] = new PlayerInfo();
                }

                var info = API.State.PlayerInfo[key];

                if (e.DynelID != uint.MinValue && e.DynelID != uint.MaxValue)
                {
                    if (info.ID != e.DynelID)
                    {
                        changed = true;
                    }
                    info.ID = e.DynelID;
                }
                if (e.DynelName != "NoName")
                {
                    if (e.DynelName != info.Name)
                    {
                        changed = true;
                    }
                    info.Name = e.DynelName;
                }
                if (e.ServerID != 0)
                {
                    if (e.ServerID != info.ServerID)
                    {
                        changed = true;
                    }
                    info.ServerID = e.ServerID;
                    info.Identity.Dimension = PlayerInfoKey.GetDimension(e.ServerID);
                }
            }
            if (changed)
            {
            }
        }


        private void HandleHookStateChangeEvent(Provider sender, RKNTracker.AoHook.Events.HookStateChangeEventArgs e)
        {
            lock (this.ProcessCharacterMap)
            {
                // We don't have anything to do if we're hooking.
                if (e.IsHooked) { return; }
                // We can't do anything if we don't know about the process
                if (!this.ProcessCharacterMap.ContainsKey(e.ProcessID)) { return; }

                // Update status of whether or not a player character is hooked.
                API.State.PlayerInfo[this.ProcessCharacterMap[e.ProcessID]].IsHooked = false;
                this.ProcessCharacterMap.Remove(e.ProcessID);
            }
        }

		private void HandleCharacterPositionEvent(Provider sender, RKNTracker.AoHook.Events.CharacterPositionEventArgs e)
		{
			// Ignore anything which isn't player characters.
			if (e.DynelType != 50000) { return; }

			// Don't update if we have invalid data (such as while zoning)
			if (e.ZoneID == 0) { return; }

			var update = false;
			var fireEvent = false;

			lock (API.State.PlayerInfo)
			{
				PlayerInfoKey key = new PlayerInfoKey(e.ServerID, e.DynelID);
				var info = API.State.PlayerInfo[key];

				if (info.Position.X != e.X || info.Position.Y != e.Y || info.Position.Z != e.Z)
				{
					fireEvent = true;
				}

				info.Position = new Vector3(e.X, e.Y, e.Z);
				info.Zone.ID = e.ZoneID;
				info.Zone.Name = e.ZoneName;

				if (info.InShadowlands != e.InShadowlands)
				{
					update = true;
				}

				info.InShadowlands = e.InShadowlands;

				if (info.ServerID != e.ServerID)
				{
					info.ServerID = e.ServerID;
					update = true;
					this.UpdateTrackedDimension();
				}

				if (!info.IsHooked)
				{
					info.IsHooked = true;
					update = true;
				}
			}
			if (update)
			{
			}

			if (fireEvent)
			{
				FireDisplayEvent();
			}
		}

        internal void UpdateTrackedDimension()
        {
            /*
			if (API.State.CameraControl == CameraControl.ActiveCharacter)
            {
                this.UpdateTrackerDimensionActiveCharacter();
                return;
            }
			*/
            lock (API.State.PlayerInfo)
            {
                var dict = new Dictionary<Dimension, int>();
                foreach (var val in (Dimension[])Enum.GetValues(typeof(Dimension)))
                {
                    dict[val] = 0;
                }
                var dimensions = from pi in API.State.PlayerInfo
                                 where pi.Value.IsHooked
                                 where pi.Value.IsTrackedByCamera
                                 select pi.Value.Dimension;

                if (dimensions.Count() == 0)
                {
                    dimensions = from pi in API.State.PlayerInfo
                                 where pi.Value.IsHooked
                                 select pi.Value.Dimension;
                }
                foreach (var dim in dimensions)
                {
                    dict[dim]++;
                }
                var newDim = dict.OrderByDescending(k => k.Value).First().Key;

                bool sendEvent = newDim != API.State.CurrentDimension;
                API.State.CurrentDimension = newDim;
                if (sendEvent)
                {
                    this.SendDimChanged();
                }
            }
        }

        private void UpdateTrackerDimensionActiveCharacter()
        {
            var id = API.AoHook.GetActiveCharacter();
            if (id == null)
            {
                return;
            }

            var dim = API.State.PlayerInfo[id].Dimension;
            if (dim != API.State.CurrentDimension)
            {
                bool sendEvent = dim != API.State.CurrentDimension;
                API.State.CurrentDimension = dim;
                if (sendEvent)
                {
                    this.SendDimChanged();
                }
            }
        }

        private void SendDimChanged()
        {
            if (this.TrackedDimensionChanged != null)
            {
                this.TrackedDimensionChanged();
            }
        }
        #endregion

        public void Dispose()
        {
            this.Provider.CharacterPositionEvent -= HandleCharacterPositionEvent;
            this.Provider.HookStateChangeEvent -= HandleHookStateChangeEvent;
            this.Provider.DynelNameEvent -= HandleDynelNameEvent;
        }
    }
}
