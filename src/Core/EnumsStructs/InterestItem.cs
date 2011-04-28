/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Encapsulates an interest involved in user searching (SNAC family 0x000F)
    /// </summary>
    public struct InterestItem
    {
        /// <summary>
        /// <c>true</c> if this item is a group of interests, <c>false</c> if it is a single interest
        /// </summary>
        public bool Group;

        /// <summary>
        /// The ID of the group or, if this is a single interest, the ID of the parent group
        /// </summary>
        public byte ID;

        /// <summary>
        /// The name of the group or the interest
        /// </summary>
        public string Name;
    }
}