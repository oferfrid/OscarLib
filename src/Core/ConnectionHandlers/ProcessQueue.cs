/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections.Generic;
using System.Threading;
using System;

namespace csammisrun.OscarLib.Utility
{
    internal class ProcessQueue
    {
        private readonly Queue<DataPacket> processQueue = new Queue<DataPacket>();
        private readonly Timer processTimer;
        private bool hasTimerBeenSet;
        private readonly Session parent;

        public ProcessQueue(Session sess)
        {
            parent = sess;
            processTimer = new Timer(SendQueue);
        }

        /// <summary>
        /// Enqueues a data packet to be processed by SNAC handling functions
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        public void Enqueue(DataPacket dp)
        {
            lock (this)
            {
                processQueue.Enqueue(dp);
                if (!hasTimerBeenSet)
                {
                    SetDispatchTimer();
                }
            }
        }

        /// <summary>
        /// Sends the contents of the dispatch queue
        /// </summary>
        /// <param name="threadstate">Always <c>null</c></param>
        private void SendQueue(object threadstate)
        {
            List<DataPacket> dataPackets = null;

            // Dequeue any packets ready to be dispatched locally
            lock (this)
            {
                dataPackets = new List<DataPacket>(processQueue.Count);
                while (processQueue.Count > 0)
                {
                    dataPackets.Add(processQueue.Dequeue());
                }
            }

            // Dispatch the packets, either synchronously or asynchronously, depending on login status.
            // If the session hasn't logged in yet, packets are processed synchronously, because
            // it's a pretty rigorous call-response format.
            // This is outside a lock so synchronous dispatch doesn't stall things on a lot of processing.
            foreach(DataPacket dp in dataPackets)
            {
                if (parent.LoggedIn)
                {
                    ThreadPool.QueueUserWorkItem(DoDispatch, dp);
                }
                else
                {
                    DoDispatch(dp);
                }
            }

            // Allow the timer to be reset, or set the timer here to avoid packets getting
            // queued up without triggering the timer.
            lock (this)
            {
                if (processQueue.Count > 0)
                {
                    SetDispatchTimer();
                }
                else
                {
                    hasTimerBeenSet = false;
                }
            }
        }

        /// <summary>
        /// Sets the dispatch timer for a single event
        /// </summary>
        private void SetDispatchTimer()
        {
            processTimer.Change(100, Timeout.Infinite);
            hasTimerBeenSet = true;
        }

        /// <summary>
        /// Performs packet dispatching logic
        /// </summary>
        /// <param name="obj">A <see cref="DataPacket"/> to dispatch</param>
        private void DoDispatch(object obj)
        {
            DataPacket dp = obj as DataPacket;
            if (Logging.IsLoggingEnabled)
            {
                Logging.DumpDataPacket(dp);
            }

            try
            {
                if (!parent.Dispatcher.DispatchPacket(dp))
                {
                    SNACFunctions.ProcessSNAC(dp);
                }
            }
            catch (Exception ex)
            {
                parent.OnPacketDispatchException(ex, dp);
            }
        }
    }
}