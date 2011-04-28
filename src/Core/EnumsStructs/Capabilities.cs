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
    /// Describes OSCAR client capabilities
    /// </summary>
    public enum Capabilities
    {
        /// <summary>
        /// No capabilities
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// Client supports voice chat
        /// </summary>
        VoiceChat = 0x00000001,
        /// <summary>
        /// Client supports direct play service
        /// </summary>
        DirectPlay = 0x00000002,
        /// <summary>
        /// Client supports sendfile-style file transfer
        /// </summary>
        SendFiles = 0x00000004,
        /// <summary>
        /// Client supports getfile-style file transfer
        /// </summary>
        GetFiles = 0x00000008,
        /// <summary>
        /// Client supports route finder (ICQ clients only)
        /// </summary>
        RouteFinder = 0x00000010,
        /// <summary>
        /// Client supports DirectIM and IMImage
        /// </summary>
        DirectIM = 0x00000020,
        /// <summary>
        /// Client supports buddy icon service messages
        /// </summary>
        BuddyIcon = 0x00000040,
        /// <summary>
        /// Client supports the stock ticker add-in
        /// </summary>
        StocksAddIn = 0x00000080,
        /// <summary>
        /// Client supports channel 2 extended messages (ICQ only)
        /// </summary>
        Channel2Ext = 0x00000100,
        /// <summary>
        /// Client supports the games service
        /// </summary>
        Games = 0x00000200,
        /// <summary>
        /// Client supports buddy list transfers
        /// </summary>
        BuddyListTransfer = 0x00000400,
        /// <summary>
        /// Signals the server to allow AIM and ICQ interoperation
        /// </summary>
        AIMtoICQ = 0x00000800,
        /// <summary>
        /// Client supports UTF-8 encoded messages
        /// </summary>
        UTF8 = 0x00001000,
        /// <summary>
        /// Unknown capability (ICQ only)
        /// </summary>
        Unknown1 = 0x00002000,
        /// <summary>
        /// Unknown capability (ICQ only)
        /// </summary>
        Unknown2 = 0x00004000,
        /// <summary>
        /// Unknown capability (ICQ only)
        /// </summary>
        Unknown3 = 0x00008000,
        /// <summary>
        /// Unknown capability (ICQ only)
        /// </summary>
        Unknown4 = 0x00010000,
        /// <summary>
        /// Client supports chat service
        /// </summary>
        Chat = 0x00020000,
        /// <summary>
        /// Client supports SecureIM channel-2 messages (Trillian only)
        /// </summary>
        TrillianSecureIM = 0x00040000,
        /// <summary>
        /// SIM/Kopete clients set this capability to identify themselves
        /// </summary>
        SIMKopete = 0x00080000,
        /// <summary>
        /// OscarLib-based clients set this capability to identify themselves
        /// </summary>
        OscarLib = 0x00100000,
        /// <summary>
        /// iChat support? Okay then
        /// </summary>
        iChat = 0x00200000,
        /// <summary>
        /// Client supports RTF messages (ICQ only)
        /// </summary>
        RTF = 0x00400000
    }
}