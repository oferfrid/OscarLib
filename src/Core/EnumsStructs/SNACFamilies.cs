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
    internal enum SNACFamily
    {
        /// <summary>
        /// Basic control service - service negotiation, rate control, warnings
        /// </summary>
        BasicOscarService = 0x0001,
        /// <summary>
        /// This service is used to get/change user online data (like profile or capabilities or AIM away messages)
        /// </summary>
        LocationService = 0x0002,
        /// <summary>
        /// This service is used to manage old-style buddy lists and for presense online/offline/status notifications
        /// </summary>
        BuddyListManagementService = 0x0003,
        /// <summary>
        /// Used for Instant Messages (IM) and warnings
        /// </summary>
        ICBMService = 0x0004,
        /// <summary>
        /// This service is depricated. AOL clients currently use web services to get ad data (AIM Only)
        /// </summary>
        AdvertisementsService = 0x0005,
        /// <summary>
        /// This is used to send AIM service invitation email to a friend (AIM Only)
        /// </summary>
        InvitationService = 0x0006,
        /// <summary>
        /// This service is used to manage AIM user account data (screenname formating, email) and for account confirmation (AIM Only)
        /// </summary>
        AdministrativeService = 0x0007,
        /// <summary>
        /// Servers use this small family to popup a special message window on the client side (AIM Only)
        /// </summary>
        PopupNoticesService = 0x0008,
        /// <summary>
        /// Client use this message to manage its visible/invisible lists and user class permissions
        /// </summary>
        PrivacyManagementService = 0x0009,
        /// <summary>
        /// This service used by old AIM clients to search users by email (not used anymore) (AIM Only)
        /// </summary>
        UserLookupService = 0x000A,
        /// <summary>
        /// This is used by AOL to gather statistic information about client usage
        /// </summary>
        UsageStatsService = 0x000B,
        /// <summary>
        /// This service is depricated (AIM Only)
        /// </summary>
        TranslationService = 0x000C,
        /// <summary>
        /// AIM chat service (get/send messages, join/left notification, warnings, room update info, etc) (AIM Only)
        /// </summary>
        ChatService = 0x000E,
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
        /// <summary>
        /// This service is used for client registration/authorization
        /// </summary>
        AuthorizationRegistrationService = 0x0017,
        /// <summary>
        /// This service is completely undocumented in the Russian docs, but libfaim evidently does something with it
        /// </summary>
        EmailService = 0x0018,
        /// <summary>
        /// This is an IServerd extension to allow a client to send broadcasts. If a client has permissions to send broadcasts, it will get this family in SNAC(01,03)
        /// </summary>
        BroadcastService = 0x0085
    }
}