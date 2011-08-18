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
        private static uint snacRequestID = 0;
        private static object idLocker = new object();
        /// <summary>
        /// Gets the next SNAC request ID in the sequence 0 through 2^32 - 1, inclusive
        /// </summary>
        /// <returns>The next SNAC request ID</returns>
        /// <remarks>The request ID sequence wraps around if it is about to overflow</remarks>
        private static uint GetNextRequestID()
        {
            lock (idLocker)
            {
                if (snacRequestID == uint.MaxValue) snacRequestID = 0;
                return snacRequestID++;
            }
        }

        private ushort familyServiceID;
        private ushort familySubtypeID;
        private ushort flags;
        private readonly uint requestID;

        /// <summary>
        /// Initializes a new SNACHeader from a bytestream
        /// </summary>
        /// <param name="byteStream"></param>
        internal SNACHeader(ByteStream byteStream)
        {
            FamilyServiceID = byteStream.ReadUshort();
            FamilySubtypeID = byteStream.ReadUshort();
            Flags = byteStream.ReadUshort();
            requestID = byteStream.ReadUint();
        }

        /// <summary>
        /// Initializes a new SNACHeader with a unique request ID
        /// </summary>
        internal SNACHeader()
        {
            requestID = GetNextRequestID();
        }

        /// <summary>
        /// Initializes a new SNACHeader with a unique request ID and the specified SNAC IDs
        /// </summary>
        internal SNACHeader(ushort familyServiceId, ushort familySubtypeId)
            : this()
        {
            FamilyServiceID = familyServiceId;
            FamilySubtypeID = familySubtypeId;
        }

        internal SNACHeader(ushort familyServiceId, ushort familySubtypeId, ushort flag, uint requestId)
        {
            FamilyServiceID = familyServiceId;  // Family
            FamilySubtypeID = familySubtypeId;  // Subtype
            Flags = flag;                       // Flags
            requestID = requestId;              // RequestId((short) sequence) + ((short) command)
        }


        /// <summary>
        /// Identifies which service group this SNAC belongs to
        /// </summary>
        public ushort FamilyServiceID
        {
            get { return familyServiceID; }
            set { familyServiceID = value; }
        }

        /// <summary>
        /// Further divides the service group identified by FamilyServiceID
        /// </summary>
        public ushort FamilySubtypeID
        {
            get { return familySubtypeID; }
            set { familySubtypeID = value; }
        }

        /// <summary>
        /// Gets or sets the value of the SNAC header flags
        /// </summary>
        public ushort Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        /// <summary>
        /// Used in request-response exchanges, the client sets the RequestID and the server responds with a SNAC having an identical RequestID
        /// </summary>
        public uint RequestID
        {
            get { return requestID; }
        }
    }
}