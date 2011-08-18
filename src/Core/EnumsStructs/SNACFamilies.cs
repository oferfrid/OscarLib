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
    /// The main SNAC families
    /// </summary>
    internal enum SNACFamily : ushort
    {
        /// <summary>
        /// This service is used to get/change user online data (like profile or capabilities or AIM away messages)
        /// </summary>
        LocationService = 0x0002,
        /// <summary>
        /// Client use this message to manage its visible/invisible lists and user class permissions
        /// </summary>
        PrivacyManagementService = 0x0009,
        /// <summary>
        /// This service is used by modern AIM clients for user search (by email, by name/details, by interests) (AIM Only)
        /// </summary>
        DirectoryUserSearch = 0x000F,
        /// <summary>
        /// This service allows clients to store their contact list data locally (buddies, groups, comments, visibility, invisibility, ignore, perms mask) on the server
        /// </summary>
        SSIService = 0x0013,
        /// <summary>
        /// This service is used by ICQ clients for compatibility with the old Mirabilis ICQ server database engine (info, search, offline messages, sms and other) (ICQ Only)
        /// </summary>
        ICQExtensionsService = 0x0015,
    }
}