/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Encapsulates a generic SSI item
    /// </summary>
    public class SSIItem
    {
        /// <summary>
        /// The item's Group ID
        /// </summary>
        public ushort GroupID;

        /// <summary>
        /// The item's Item ID
        /// </summary>
        public ushort ItemID;

        /// <summary>
        /// The type of the item
        /// </summary>
        public ushort ItemType;

        /// <summary>
        /// The name of this item
        /// </summary>
        public string Name;

        /// <summary>
        /// The TLVs included with this SSI item
        /// </summary>
        public TlvBlock Tlvs = null;
    }
}