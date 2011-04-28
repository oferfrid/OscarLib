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
    /// Encapsulates identification information needed by an OSCAR client
    /// </summary>
    public class OSCARIdentification
    {
        /// <summary>
        /// Build number
        /// </summary>
        public ushort ClientBuild; //0x0BDC;

        /// <summary>
        /// Client distribution number
        /// </summary>
        public uint ClientDistribution; //0x000000D2;

        /// <summary>
        /// The client ID number
        /// </summary>
        public ushort ClientId; //0x0109;

        /// <summary>
        /// Lesser version number
        /// </summary>
        public ushort ClientLesser; //0x0000;

        /// <summary>
        /// Major version number
        /// </summary>
        public ushort ClientMajor; //0x0005;

        /// <summary>
        /// Minor version number
        /// </summary>
        public ushort ClientMinor; //0x0001;

        /// <summary>
        /// The name of the client
        /// </summary>
        public string ClientName;
    }
}