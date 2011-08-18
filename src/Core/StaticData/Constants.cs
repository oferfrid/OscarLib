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
    /// Summary description for Constants.
    /// </summary>
    internal class Constants
    {
        public const string AIM_MD5_STRING = "AOL Instant Messenger (SM)";
        public const ushort CLIENT_BUILD = 0x0BDC;
        public const uint CLIENT_DISTRIBUTION = 0x000000D2;

        // Commented values are "official", but the server doesn't seem to care
        public const ushort CLIENT_ID = 0x0109;
        public const ushort CLIENT_LESSER = 0x0000;
        public const ushort CLIENT_MAJOR = 0x0005;
        public const ushort CLIENT_MINOR = 0x0001;
        public const string CLIENT_NAME = "AOL Instant Messenger, version 5.1.3036/WIN32";
        public const uint PROTOCOL_VERSION = 0x00000001;
        public const uint STATUS_LOGGING_IN = 1;
        public const uint STATUS_NULL = 0;
        public const uint STATUS_WAITING_ON_SNAC = 2;
    }
}