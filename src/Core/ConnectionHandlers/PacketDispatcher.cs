/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections.Generic;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides a dispatch point for received <see cref="DataPacket"/>s
    /// </summary>
    public class PacketDispatcher
    {
        private Dictionary<ushort, ISnacFamilyHandler> handlers =
            new Dictionary<ushort, ISnacFamilyHandler>();

        /// <summary>
        /// Register a <see cref="ISnacFamilyHandler"/> for a SNAC family
        /// </summary>
        /// <param name="handler">The handler to register</param>
        /// <param name="snacFamily">The SNAC family handled by <paramref name="handler"/></param>
        public void RegisterSnacFamilyHandler(ISnacFamilyHandler handler, ushort snacFamily)
        {
            handlers[snacFamily] = handler;
        }

        /// <summary>
        /// Dispatches a newly received <see cref="DataPacket"/> according to its SNAC family
        /// </summary>
        /// <param name="dp">The packet to dispatch</param>
        /// <returns><c>true</c> if the packet was handled, <c>false</c> if no handlers were registered for its SNAC family</returns>
        public bool DispatchPacket(DataPacket dp)
        {
            bool handled = false;
            if (handlers.ContainsKey(dp.SNAC.FamilyServiceID))
            {
                if (handlers[dp.SNAC.FamilyServiceID] != null)
                {
                    handlers[dp.SNAC.FamilyServiceID].ProcessIncomingPacket(dp);
                    handled = true;
                }
            }
            return handled;
        }
    }
}