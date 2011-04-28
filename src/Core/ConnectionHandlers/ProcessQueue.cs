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
        private Queue<DataPacket> _processqueue = new Queue<DataPacket>();
        private Timer _processtimer;
		private ISession _sess;

		public ProcessQueue(ISession sess)
        {
            _sess = sess;
            _processtimer = new Timer(
                new TimerCallback(SendQueue),
                null, 100, 100);
        }

        /// <summary>
        /// Enqueues a data packet to be processed by SNAC handling functions
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        public void Enqueue(DataPacket dp)
        {
            lock (this)
            {
                _processqueue.Enqueue(dp);
            }
        }

        /// <summary>
        /// Sends the contents of the dispatch queue
        /// </summary>
        /// <param name="threadstate">Always <c>null</c></param>
        protected void SendQueue(object threadstate)
        {
            lock (this)
            {
                while (_processqueue.Count > 0)
                {
                    DataPacket dp = _processqueue.Dequeue();
                    if (_sess.LoggedIn)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(DoDispatch), dp);
                    }
                    else
                    {
                        // We have to synchronously process login packets
                        // because it's a pretty rigorous call-response format
                        DoDispatch(dp);
                    }
                }
            }
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
                if (!_sess.Dispatcher.DispatchPacket(dp))
                {
                    SNACFunctions.ProcessSNAC(dp);
                }
            }
            catch (Exception ex)
            {
                _sess.OnPacketDispatchException(ex, dp);
            }
        }
    }
}