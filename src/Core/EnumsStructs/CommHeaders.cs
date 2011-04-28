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
    /// Represents a FLAP header. FLAP is the lowest level of communication in the OSCAR protocol
    /// </summary>
    public struct FLAPHeader
    {
        /// <summary>
        /// IDByte is the frame-start sign, and is always 0x2A
        /// </summary>
        public const byte IDByte = 0x2A;

        /// <summary>
        /// Channels are used to identify different types of communication on a single wire
        /// </summary>
        public byte Channel;

        /// <summary>
        /// Datagram sequence number, used for error detection
        /// </summary>
        public ushort DatagramSequenceNumber;

        /// <summary>
        /// The size, in bytes, of the data which follows the header
        /// </summary>
        public ushort DataSize;
    }

    /// <summary>
    /// The five possible FLAP communication channels
    /// </summary>
    internal enum FLAPChannels
    {
        NewContentNegotiation = 0x01,
        SNACData = 0x02,
        Error = 0x03,
        CloseContentNegotiation = 0x04,
        KeepAlive = 0x05,
    }

    /// <summary>
    /// Represents a SNAC header. A SNAC is the basic communication unit sent between client and server
    /// </summary>
    public class SNACHeader
    {
        /// <summary>
        /// Identifies which service group this SNAC belongs to
        /// </summary>
        public ushort FamilyServiceID;

        /// <summary>
        /// Further divides the service group identified by FamilyServiceID
        /// </summary>
        public ushort FamilySubtypeID;

        /// <summary>
        /// General SNAC properties
        /// </summary>
        public ushort Flags;

        /// <summary>
        /// Used in request-response exchanges, the client sets the RequestID and the server responds with a SNAC having an identical RequestID
        /// </summary>
        public uint RequestID;
    }
}