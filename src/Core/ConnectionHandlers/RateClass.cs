/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Encapsulates one of the rate classes
    /// </summary>
    /// <remarks>
    /// <para>If a SNAC belongs to a rate class, the RateClass object is the one responsible for actually sending it.</para>
    /// <para>
    /// The rate calculation code was taken from Kopete, KDE's native instant messanger program: 
    /// http://kopete.kde.org/
    /// </para>
    /// </remarks>
    public class RateClass
    {
        private const int RATE_SAFETY_DANCE = 50;

        private uint _alertlevel;
        private uint _clearlevel;

        private uint _currentlevel;

        private byte _currentstate;
        private uint _disconnectlevel;

        private uint _initiallevel;

        private uint _lasttime;

        private uint _limitlevel;
        private uint _maxlevel;
        private Queue<DataPacket> _sendqueue = new Queue<DataPacket>();

        private Timer _sendtimer = null;
        private int _starttime = 0;
        private uint _windowsize;

        /// <summary>
        /// Creates a new rate class
        /// </summary>
        public RateClass()
        {
            _sendtimer = new Timer(new TimerCallback(SetTimer), null, Timeout.Infinite, Timeout.Infinite);
            _starttime = Environment.TickCount;
        }

        /// <summary>
        /// Enqueues a DataPacket to be sent asynchronously
        /// </summary>
        public void Enqueue(DataPacket dp)
        {
            lock (this)
            {
                _sendqueue.Enqueue(dp);
            }
        }

        /// <summary>
        /// Begin sending DataPackets in server-limited time slices
        /// </summary>
        public void StartLimitedTransmission()
        {
            int tts = TimeToNextSend();
            _sendtimer.Change(tts, tts);
        }

        /// <summary>
        /// Calculates the time to the next packet send, in milliseconds
        /// </summary>
        /// <returns>The time to the next packet send, in milliseconds</returns>
        /// <remarks>See the remarks for <see cref="RateClass"/> for code credit.</remarks>
        private int TimeToNextSend()
        {
            int newlevel = 0;
            int timediff = Environment.TickCount - _starttime;
            newlevel = CalculateNewLevel(timediff);

            int maxpacket = (int) _alertlevel + RATE_SAFETY_DANCE;
            if (newlevel < maxpacket || newlevel < _disconnectlevel)
            {
                // Warn the parent to stop sending stuff so damn fast!
                int waittime = (int) ((_windowsize*maxpacket) - ((_windowsize - 1)*_currentlevel));
                return waittime;
            }

            return RATE_SAFETY_DANCE;
        }

        /// <summary>
        /// Calculates the sender's new rate level
        /// </summary>
        /// <param name="timediff">The time in milliseconds since the last packet send</param>
        /// <returns>The sender's new rate level</returns>
        /// <remarks>See the remarks for <see cref="RateClass"/> for code credit.</remarks>
        private int CalculateNewLevel(int timediff)
        {
            int retval = (int) ((((_windowsize - 1)*_currentlevel) + timediff)/_windowsize);

            /* This, this right here, makes NO SENSE -- never raise the level above the current? */
            if (retval > _initiallevel)
                retval = (int) _initiallevel;
            return retval;
        }

        /// <summary>
        /// Updates the sender's level and resets the last-sent time
        /// </summary>
        /// <remarks>See the remarks for <see cref="RateClass"/> for code credit.</remarks>
        private void UpdateRateInfo()
        {
            int timediff = Environment.TickCount - _starttime;
            int newlevel = CalculateNewLevel(timediff);
            _currentlevel = (uint) newlevel;
            _starttime = Environment.TickCount;
        }

        /// <summary>
        /// Sets the timer for the necessary wait period, or if there is none,
        /// send a packet immediately
        /// </summary>
        /// <remarks>See the remarks for <see cref="RateClass"/> for code credit.</remarks>
        private void SetTimer(object threadstate)
        {
            lock (this)
            {
                if (SendOne())
                {
                    StartLimitedTransmission();
                }
            }
        }

        /// <summary>
        /// Sends a single packet from the queue
        /// </summary>
        /// <returns>Returns a value indicating whether or not a packet was actually sent</returns>
        /// <remarks>See the remarks for <see cref="RateClass"/> for code credit.</remarks>
        private bool SendOne()
        {
            if (_sendqueue.Count != 0)
            {
                DataPacket dp = _sendqueue.Dequeue();
                if (dp != null && dp.ParentConnection != null)
                {
                    dp.ParentConnection.SendFLAP(dp.Data.GetBytes());
                }
                else
                {
                    // ...huh? 
                }

                UpdateRateInfo();
            }

            return _sendqueue.Count != 0;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the size of the window used in calculating the average time between packets
        /// </summary>
        /// <remarks>
        /// For a screen name that has never sent any packets, the server sets this value
        /// to 0.
        /// </remarks>
        public uint WindowSize
        {
            get { return _windowsize; }
            set { _windowsize = value; }
        }

        /// <summary>
        /// Gets or sets the level at which the client is free to resume sending data
        /// </summary>
        /// <remarks>
        /// As of this writing, this value is set by the server to 0x0C00
        /// </remarks>
        public uint ClearLevel
        {
            get { return _clearlevel; }
            set { _clearlevel = value; }
        }

        /// <summary>
        /// Gets or sets the level at which the client is being warned about sending data
        /// too fast, but is not yet being limited
        /// </summary>
        /// <remarks>
        /// As of this writing, this value is set by the server to 0x0900
        /// </remarks>
        public uint AlertLevel
        {
            get { return _alertlevel; }
            set { _alertlevel = value; }
        }

        /// <summary>
        /// Gets or sets the level at which the client is being limited and cannot send data
        /// without being disconnected from the server
        /// </summary>
        /// <remarks>
        /// As of this writing, this value is set by the server to 0x0700
        /// </remarks>
        public uint LimitLevel
        {
            get { return _limitlevel; }
            set { _limitlevel = value; }
        }

        /// <summary>
        /// Gets or sets the level at which the client will be disconnected from the server
        /// </summary>
        /// <remarks>This value is not used by the client because, by the time it becomes
        /// relevant, the client is already disconnected.</remarks>
        /// <remarks>
        /// As of this writing, this value is set by the server to 0x0500
        /// </remarks>
        public uint DisconnectLevel
        {
            get { return _disconnectlevel; }
            set { _disconnectlevel = value; }
        }

        /// <summary>
        /// Gets or sets the client's current rate level
        /// </summary>
        /// <remarks>
        /// For a screen name that has never sent any packets, the server sets this value
        /// to 0x0D00.
        /// </remarks>
        public uint CurrentLevel
        {
            get { return _currentlevel; }
            set { _currentlevel = _initiallevel = value; }
        }

        /// <summary>
        /// Gets or sets the maximum calculable rate level
        /// </summary>
        /// <remarks>
        /// As of this writing, this value is set by the server to 0x1100
        /// </remarks>
        public uint MaxLevel
        {
            get { return _maxlevel; }
            set { _maxlevel = value; }
        }

        /// <summary>
        /// Gets or sets a value in milliseconds of the last time a packet was sent by the client
        /// </summary>
        /// <remarks>
        /// For a screen name that has never sent any packets, the server sets this value
        /// to 0x0900.
        /// </remarks>
        public uint LastTime
        {
            get { return _lasttime; }
            set { _lasttime = value; }
        }

        /// <summary>
        /// Gets or sets the client's current packet sending state
        /// </summary>
        /// <remarks>
        /// For a screen name that has never sent any packets, the server sets this value
        /// to 0.
        /// </remarks>
        public byte CurrentState
        {
            get
            {
                //SetCurrentState();
                return _currentstate;
            }
            set { _currentstate = value; }
        }

        #endregion
    }
}