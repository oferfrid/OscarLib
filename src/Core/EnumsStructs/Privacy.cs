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
    /// Describes the available privacy settings
    /// </summary>
    public enum PrivacySetting
    {
        /// <summary>
        /// All users are allowed to contact this client
        /// </summary>
        AllowAllUsers = 0x01,
        /// <summary>
        /// No users are allowed to contact this client
        /// </summary>
        BlockAllUsers = 0x02,
        /// <summary>
        /// Only the users on this client's permit list are allowed to make contact
        /// </summary>
        AllowOnlyPermitList = 0x03,
        /// <summary>
        /// Only the users on this client's block list cannot make contact
        /// </summary>
        BlockOnlyDenyList = 0x04,
        /// <summary>
        /// Only the users on this client's buddy list are allowed to make contact
        /// </summary>
        AllowOnlyBuddyList = 0x05
    }
}