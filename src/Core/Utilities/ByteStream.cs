/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// An exception caused by a bad read request from a <see cref="ByteStream"/>
    /// </summary>
    public class ByteStreamReadException : Exception
    {
        private const string MESSAGE_FORMAT = "ByteStreamReadException (Stream size: {0}, current position: {1}, requested read of {2} bytes)";

        /// <summary>
        /// The size of the ByteStream that threw the exception
        /// </summary>
        private readonly int streamSize;
        /// <summary>
        /// The read position in the ByteStream at the time the exception was thrown
        /// </summary>
        private readonly int currentPosition;
        /// <summary>
        /// The number of bytes that were requested to be read
        /// </summary>
        private readonly int requestedRead;

        /// <summary>
        /// Initializes a ByteStreamReadException
        /// </summary>
        /// <param name="streamSize">The size of the ByteStream that threw the exception</param>
        /// <param name="currentPosition">The read position in the ByteStream at the time the exception was thrown</param>
        /// <param name="requestedRead">The number of bytes that were requested to be read</param>
        public ByteStreamReadException(int streamSize, int currentPosition,
            int requestedRead)
            : base()
        {
            this.streamSize = streamSize;
            this.currentPosition = currentPosition;
            this.requestedRead = requestedRead;
        }

        /// <summary>
        /// Gets the size of the ByteStream that threw the exception
        /// </summary>
        public int StreamSize
        {
            get { return streamSize; }
        }

        /// <summary>
        /// Gets the read position in the ByteStream at the time the exception was thrown
        /// </summary>
        public int CurrentPosition
        {
            get { return currentPosition; }
        }

        /// <summary>
        /// Gets the number of bytes that were requested to be read
        /// </summary>
        public int RequestedRead
        {
            get { return requestedRead; }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return String.Format(MESSAGE_FORMAT, StreamSize, CurrentPosition, RequestedRead);
            }
        }
    }

    /// <summary>
    /// A utility class for reading and writing data to or from a byte array
    /// </summary>
    public class ByteStream : IDisposable
    {
        #region Data segment classes

        /// <summary>
        /// Defines an interface for a segment in a byte stream
        /// </summary>
        private interface ISegment
        {
            /// <summary>
            /// Get a byte array composed of the current segment
            /// </summary>
            byte[] GetBytes();
        }

        /// <summary>
        /// Defines a segment for a byte value
        /// </summary>
        private class ByteSegment : ISegment
        {
            private byte value;

            public ByteSegment(byte value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                return new byte[] { value };
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a ushort value
        /// </summary>
        private class UshortSegment : ISegment
        {
            private ushort value;

            public UshortSegment(ushort value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                byte[] retval = new byte[2];
                retval[0] = (byte)((value & 0xFF00) >> 8);
                retval[1] = (byte)(value & 0x00FF);
                return retval;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a uint value
        /// </summary>
        private class UintSegment : ISegment
        {
            private uint value;

            public UintSegment(uint value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                byte[] retval = new byte[4];
                retval[0] = (byte)((value & 0xFF000000) >> 24);
                retval[1] = (byte)((value & 0x00FF0000) >> 16);
                retval[2] = (byte)((value & 0x0000FF00) >> 8);
                retval[3] = (byte)((value & 0x000000FF));
                return retval;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a int value
        /// </summary>
        private class IntSegment : ISegment
        {
            private int value;

            public IntSegment(int value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                byte[] retval = System.BitConverter.GetBytes(value);
                Array.Reverse(retval);
                return retval;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a string value
        /// </summary>
        private class StringSegment : ISegment
        {
            private string value;
            private Encoding encoding;

            public StringSegment(string value, Encoding encoding)
            {
                this.value = value;
                this.encoding = encoding;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                return encoding.GetBytes(value);
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a <see cref="TlvBlock"/> value
        /// </summary>
        private class TlvBlockSegment : ISegment
        {
            private TlvBlock value;

            public TlvBlockSegment(TlvBlock value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                return value.GetBytes();
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a byte array
        /// </summary>
        private class ByteArraySegment : ISegment
        {
            private byte[] value;

            public ByteArraySegment(byte[] value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                return value;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a <see cref="FLAPHeader"/>
        /// </summary>
        private class FlapSegment : ISegment
        {
            private FLAPHeader value;

            public FlapSegment(FLAPHeader value)
            {
                this.value = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                byte[] buffer = new byte[6];
                buffer[0] = FLAPHeader.IDByte;
                buffer[1] = value.Channel;
                buffer[2] = (byte)((value.DatagramSequenceNumber & 0xFF00) >> 8);
                buffer[3] = (byte)(value.DatagramSequenceNumber & 0x00FF);
                buffer[4] = (byte)((value.DataSize & 0xFF00) >> 8);
                buffer[5] = (byte)(value.DataSize & 0x00FF);
                return buffer;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for a <see cref="SNACHeader"/>
        /// </summary>
        private class SnacSegment : ISegment
        {
            private SNACHeader header;

            public SnacSegment(SNACHeader value)
            {
                header = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                byte[] buffer = new byte[10];
                buffer[0] = (byte)((header.FamilyServiceID & 0xFF00) >> 8);
                buffer[1] = (byte)(header.FamilyServiceID & 0x00FF);
                buffer[2] = (byte)((header.FamilySubtypeID & 0xFF00) >> 8);
                buffer[3] = (byte)(header.FamilySubtypeID & 0x00FF);
                buffer[4] = (byte)((header.Flags & 0xFF00) >> 8);
                buffer[5] = (byte)(header.Flags & 0x00FF);
                buffer[6] = (byte)((header.RequestID & 0xFF000000) >> 24);
                buffer[7] = (byte)((header.RequestID & 0x00FF0000) >> 16);
                buffer[8] = (byte)((header.RequestID & 0x0000FF00) >> 8);
                buffer[9] = (byte)(header.RequestID & 0x000000FF);
                return buffer;
            }

            #endregion
        }

        /// <summary>
        /// Defines a segment for an <see cref="SSIItem"/>
        /// </summary>
        private class SSIItemSegment : ISegment
        {
            private readonly SSIItem item;

            public SSIItemSegment(SSIItem value)
            {
                item = value;
            }

            #region ISegment Members

            public byte[] GetBytes()
            {
                ByteStream buffer = new ByteStream();

                buffer.WriteUshort((ushort)Encoding.UTF8.GetByteCount(item.Name));
                buffer.WriteString(item.Name, Encoding.UTF8);
                buffer.WriteUshort(item.GroupID);
                buffer.WriteUshort(item.ItemID);
                buffer.WriteUshort(item.ItemType);
                if (item.Tlvs == null)
                    buffer.WriteUshort(0x000);
                else
                {
                    buffer.WriteUshort((ushort)item.Tlvs.GetByteCount());
                    buffer.WriteTlvBlock(item.Tlvs);
                }

                return buffer.GetBytes();
            }

            #endregion
        }

        private class BartIDSegment : ISegment
        {
            private readonly BartID item;

            public BartIDSegment(BartID value)
            {
                item = value;
            }

            #region ISegment Members

            byte[] ISegment.GetBytes()
            {
                using (ByteStream buffer = new ByteStream())
                {
                    buffer.WriteUshort((ushort)item.Type);
                    buffer.WriteByte((byte)item.Flags);
                    buffer.WriteByte((byte)item.Data.Length);
                    buffer.WriteByteArray(item.Data);
                    return buffer.GetBytes();
                }
            }

            #endregion
        }


        #endregion

        private List<ISegment> dataSegments;
        private int byteCount;

        private byte[] dataBuffer;
        private int dataIndex;

        #region Constructors / destructor

        /// <summary>
        /// Initializes a new <see cref="ByteStream"/> for writing
        /// </summary>
        public ByteStream()
        {
            dataSegments = new List<ISegment>();
        }

        /// <summary>
        /// Initializes a new <see cref="ByteStream"/> from a data buffer
        /// </summary>
        public ByteStream(byte[] data)
        {
            dataBuffer = data;
        }

        /// <summary>
        /// Disposes the <see cref="ByteStream"/>
        /// </summary>
        ~ByteStream()
        {
            Dispose();
        }

        #endregion

        #region Read data methods

        /// <summary>
        /// Creates a <see cref="DataPacket"/> from the beginning of the stream
        /// </summary>
        /// <returns>A new <see cref="DataPacket"/></returns>
        public DataPacket CreateDataPacket()
        {
            if (dataIndex != 0)
            {
                throw new Exception("A DataPacket can only be created from an unused ByteStream");
            }

            SNACHeader header = new SNACHeader(this);

            if ((header.Flags & 0x8000) != 0)
            {
                dataIndex += 2 + ReadUshort(); // Read past family version information
            }

            DataPacket retval = new DataPacket(this);
            retval.SNAC = header;
            return retval;
        }

        /// <summary>
        /// Advances the read index to a new position
        /// </summary>
        public void AdvanceToPosition(int newIndex)
        {
            dataIndex = newIndex;
        }

        /// <summary>
        /// Advances the read index by a specified amount
        /// </summary>
        public void AdvanceOffset(int offset)
        {
            dataIndex += offset;
        }

        /// <summary>
        /// Reads a byte from the stream
        /// </summary>
        public byte ReadByte()
        {
            if (dataIndex + 1 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 1);
            }

            byte retval = dataBuffer[dataIndex];
            dataIndex++;
            return retval;
        }

        /// <summary>
        /// Reads a 16-bit integer from the stream
        /// </summary>
        public ushort ReadUshort()
        {
            if (dataIndex + 2 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 2);
            }

            ushort retval = (ushort)((dataBuffer[dataIndex] << 8) | dataBuffer[dataIndex + 1]);
            dataIndex += 2;
            return retval;
        }

        /// <summary>
        /// Reads a 16-bit little-endian integer from the stream
        /// </summary>
        public ushort ReadUshortLE()
        {
            if (dataIndex + 2 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 2);
            }

            ushort retval = (ushort)((dataBuffer[dataIndex + 1] << 8) | dataBuffer[dataIndex]);
            dataIndex += 2;
            return retval;
        }

        /// <summary>
        /// Reads a 32-bit integer from the stream
        /// </summary>
        public uint ReadUint()
        {
            if (dataIndex + 4 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 4);
            }

            uint retval = (uint)((dataBuffer[dataIndex] << 24) |
                                  (dataBuffer[dataIndex + 1] << 16) |
                                  (dataBuffer[dataIndex + 2] << 8) |
                                  dataBuffer[dataIndex + 3]);
            dataIndex += 4;
            return retval;
        }

        /// <summary>
        /// Reads a 32-bit little-endian integer from the stream
        /// </summary>
        public uint ReadUintLE()
        {
            if (dataIndex + 4 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 4);
            }

            uint retval = (uint)(
                                     (dataBuffer[dataIndex + 3] << 24) |
                                     (dataBuffer[dataIndex + 2] << 16) |
                                     (dataBuffer[dataIndex + 1] << 8) |
                                     dataBuffer[dataIndex]);
            dataIndex += 4;
            return retval;
        }

        /// <summary>
        /// Reads an IP address from a 32-bit integer value in the stream
        /// </summary>
        public string ReadIPAddress()
        {
            if (dataIndex + 4 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 4);
            }

            String retval = String.Format("{0}.{1}.{2}.{3}",
                                          dataBuffer[dataIndex + 0], dataBuffer[dataIndex + 1],
                                          dataBuffer[dataIndex + 2], dataBuffer[dataIndex + 3]);
            dataIndex += 4;
            return retval;
        }

        /// <summary>
        /// Reads a string from the stream using the specified encoding
        /// </summary>
        public string ReadString(int length, Encoding encoding)
        {
            if (dataIndex + length > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, length);
            }

            string retval = encoding.GetString(dataBuffer, dataIndex, length).Trim(new char[] { ' ', '\0', '\r', '\n' });
            dataIndex += length;
            return retval;
        }

        /// <summary>
        /// Reads a buffer containing an ASCII null-terminated string from the byte stream
        /// </summary>
        /// <param name="length">The potential length of the string, including zero-padding</param>
        /// <returns>An ASCII string from the byte array</returns>
        public string ReadNullTerminatedString(int length)
        {
            if (dataIndex + length > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, length);
            }

            byte[] buffer = ReadByteArray(length);
            StringBuilder retval = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                // Friggin' file transfers and their "null padded strings"
                if (buffer[i] != 0x00)
                    retval.Append((char)buffer[i]);
            }
            return retval.ToString();
        }

        /// <summary>
        /// Reads a <see cref="UserInfo"/> from the stream
        /// </summary>
        public UserInfo ReadUserInfo()
        {
            UserInfo retval = new UserInfo();

            byte screenNameLength = ReadByte();
            if (screenNameLength == 0)
            {
                return null;
            }
            retval.ScreenName = ReadString(screenNameLength, Encoding.ASCII);
            retval.WarningLevel = ReadUshort();
            using (TlvBlock tlvBlock = ReadTlvBlock(ReadUshort()))
            {
                retval.Class = (UserClass)tlvBlock.ReadUshort(0x0001);
                retval.CreateTime = tlvBlock.ReadDateTime(0x0002);
                retval.SignonTime = tlvBlock.ReadDateTime(0x0003);
                retval.IdleTime = tlvBlock.ReadUshort(0x0004);
                if (retval.IdleTime == 0xFFFF)
                {
                    retval.IdleTime = 0;
                }
                retval.RegisterTime = tlvBlock.ReadDateTime(0x0005);
                retval.ICQUserStatus = tlvBlock.ReadUint(0x0006);
                retval.ExternalIPAddress = tlvBlock.ReadUint(0x000A);
                // Read the DC info from 0x000C
                retval.ClientCapabilities = CapabilityProcessor.ProcessCLSIDList(tlvBlock.ReadByteArray(0x000D));
                retval.OnlineTime = tlvBlock.ReadUint(0x000F);
                if (tlvBlock.HasTlv(0x001D))
                {
                    ReadIconInfo(tlvBlock.ReadByteArray(0x001D), retval);
                }
            }

            return retval;
        }

        /// <summary>
        /// Parses a byte buffer into an <see cref="BartID"/> object
        /// </summary>
        private void ReadIconInfo(byte[] buffer, UserInfo userinfo)
        {
            using (ByteStream iconStream = new ByteStream(buffer))
            {
                int iconStreamSize = iconStream.GetByteCount();

                while (iconStream.CurrentPosition + 4 <= iconStreamSize)
                {
                    BartID item = new BartID(iconStream);

                    // Find the end of the current data item in the stream
                    int endDataPosition = iconStream.CurrentPosition + item.Data.Length;

                    switch (item.Type)
                    {
                        case BartTypeId.BuddyIcon:
                            if (!GraphicsManager.IsBlankIcon(item.Data))
                            {
                                userinfo.Icon = item;
                            }
                            break;

                        case BartTypeId.StatusString: // Available message
                            using (ByteStream messageStream = new ByteStream(item.Data))
                            {
                                Encoding encoding = Encoding.UTF8;
                                byte[] amessage = new byte[0];

                                if (messageStream.HasMoreData)
                                {
                                    // Pull the message to a byte array, assume at first that the encoding
                                    // is UTF-8.  If existing encoding information exists, use that instead
                                    amessage = messageStream.ReadByteArray(messageStream.ReadByte());

                                    // Check if there's encoding information available
                                    if (messageStream.HasMoreData)
                                    {
                                        // Check to see if the encoding's been specified
                                        if (messageStream.ReadUshort() == 0x0001)
                                        {
                                            messageStream.AdvanceOffset(2);
                                            string encodingStr = messageStream.ReadString(messageStream.ReadUshort(), Encoding.ASCII);

                                            // Try to use the encoding from the byte stream
                                            try
                                            {
                                                encoding = Encoding.GetEncoding(encodingStr);
                                            }
                                            catch (ArgumentException)
                                            {
                                                Logging.WriteString(
                                                    "ReadIconInfo: Got unknown encoding for available message ("
                                                    + encodingStr + "), falling back to UTF-8");
                                                encoding = Encoding.UTF8;
                                            }
                                        }
                                    }
                                }

                                userinfo.AvailableMessage = Encoding.Unicode.GetString(
                                    Encoding.Convert(encoding, Encoding.Unicode, amessage));

                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Reads an <see cref="SSIItem"/> from the stream
        /// </summary>
        /// <returns>A populated <see cref="SSIItem"/></returns>
        public SSIItem ReadSSIItem()
        {
            SSIItem item = new SSIItem();
            item.Name = ReadString(ReadUshort(), Encoding.UTF8);
            item.GroupID = ReadUshort();
            item.ItemID = ReadUshort();
            item.ItemType = ReadUshort();
            item.Tlvs = new TlvBlock(ReadByteArray(ReadUshort()));

            return item;
        }

        /// <summary>
        /// Reads an <see cref="InterestItem"/> from the stream
        /// </summary>
        /// <returns>A populated <see cref="InterestItem"/></returns>
        public InterestItem ReadInterestItem()
        {
            InterestItem retval = new InterestItem();
            retval.Group = (ReadByte() == 0x01);
            retval.ID = ReadByte();
            retval.Name = ReadString(ReadUshort(), Encoding.ASCII);
            return retval;
        }

        /// <summary>
        /// Read a specified number of TLVs from the stream into a <see cref="TlvBlock"/>
        /// </summary>
        public TlvBlock ReadTlvBlock(int numberOfTlvs)
        {
            int tlvBlockLength = 0;
            int currentIndex = dataIndex;
            for (int i = 0; i < numberOfTlvs; i++)
            {
                // Skip the Type
                tlvBlockLength += 2;
                currentIndex += 2;
                // Calculate the size of the Value
                ushort currentTlvLength = (ushort)((dataBuffer[currentIndex] << 8) |
                                                    (dataBuffer[currentIndex + 1]));
                // Skip the Length and the size of the Value
                currentIndex += currentTlvLength + 2;
                tlvBlockLength += currentTlvLength + 2;
            }

            TlvBlock retval = new TlvBlock(dataBuffer, dataIndex, tlvBlockLength);
            dataIndex += tlvBlockLength;
            return retval;
        }

        /// <summary>
        /// Reads a tlv block 
        /// </summary>
        /// <param name="tlvBlockLength">the total byte length of the block</param>
        /// <returns>the tlv block</returns>
        public TlvBlock ReadTlvBlockByLength(int tlvBlockLength)
        {
            if (dataIndex + tlvBlockLength > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, tlvBlockLength);
            }

            TlvBlock retval = new TlvBlock(dataBuffer, dataIndex, tlvBlockLength);
            dataIndex += tlvBlockLength;
            return retval;
        }

        /// <summary>
        /// Reads an <see cref="SNACHeader"/> from the stream
        /// </summary>
        /// <returns>A populated <see cref="SNACHeader"/></returns>
        public SNACHeader ReadSnacHeader()
        {
            if (dataIndex + 10 > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length, dataIndex, 10);
            }

            SNACHeader sh = new SNACHeader(ReadUshort(), ReadUshort(), ReadUshort(), ReadUint());
            return sh;
        }

        /// <summary>
        /// Converts a 4-byte unsigned integer DateTime object
        /// </summary>
        /// <returns>A <see cref="DateTime"/> object representing the date in UTC</returns>
        /// <remarks>This method assumes the integer being read represents the UNIX time_t format:
        /// the number of seconds since the epoch (00:00:00 1 Jan 1970 GMT).</remarks>
        public DateTime ReadDateTime()
        {
            DateTime dateTime = new DateTime();
            dateTime = dateTime.AddYears(1969);
            dateTime = dateTime.AddSeconds(ReadUint());

            return dateTime;
        }

        /// <summary>
        /// Converts a 4-byte little-endian unsigned integer DateTime object
        /// </summary>
        /// <returns>A <see cref="DateTime"/> object representing the date in UTC</returns>
        /// <remarks>This method assumes the integer being read represents the UNIX time_t format:
        /// the number of seconds since the epoch (00:00:00 1 Jan 1970 GMT).</remarks>
        public DateTime ReadDateTimeLE()
        {
            DateTime dateTime = new DateTime();
            dateTime = dateTime.AddYears(1969);
            dateTime = dateTime.AddSeconds(ReadUintLE());

            return dateTime;
        }

        /// <summary>
        /// Reads file transfer information from the byte stream into the <see cref="FileTransferConnection"/>
        /// </summary>
        /// <param name="connection">The <see cref="FileTransferConnection"/> to populate with information</param>
        public void ReadFileTransferInformation(FileTransferConnection connection)
        {
            FileHeader fh = connection.FileHeader;

            fh.Cookie = ReadByteArray(fh.Cookie.Length);
            fh.Encryption = ReadUshort();
            fh.Compression = ReadUshort();
            connection.TotalFiles = ReadUshort();
            connection.FilesRemaining = ReadUshort();
            connection.TotalParts = ReadUshort();
            fh.PartsLeft = ReadUshort();
            connection.TotalFileSize = ReadUint();
            fh.Size = ReadUint();
            fh.modtime = ReadUint();
            fh.Checksum = ReadUint();
            fh.ResourceForkReceivedChecksum = ReadUint();
            fh.ResourceForkSize = ReadUint();
            fh.cretime = ReadUint();
            fh.ResourceForkChecksum = ReadUint();
            fh.nrecvd = ReadUint();
            fh.ReceivedChecksum = ReadUint();

            fh.IdString = ReadNullTerminatedString(32);
            fh.flags = ReadByte();
            fh.lnameoffset = ReadByte();
            fh.lsizeoffset = ReadByte();
            fh.dummy = ReadByteArray(fh.dummy.Length);
            fh.macfileinfo = ReadByteArray(fh.macfileinfo.Length);
            fh.nencode = ReadUshort();
            fh.nlanguage = ReadUshort();

            fh.Name = ReadNullTerminatedString(64);
        }

        /// <summary>
        /// Reads a byte array from the stream
        /// </summary>
        public byte[] ReadByteArray(int length)
        {
            if (length < 0 || length + dataIndex > dataBuffer.Length)
            {
                throw new ByteStreamReadException(dataBuffer.Length,
                    dataIndex, length);
            }

            byte[] retval = new byte[length];
            Array.Copy(dataBuffer, dataIndex, retval, 0, length);
            dataIndex += length;
            return retval;
        }

        /// <summary>
        /// Reads the remainder of the stream into a byte array
        /// </summary>
        public byte[] ReadByteArrayToEnd()
        {
            byte[] retval = new byte[dataBuffer.Length - dataIndex];
            Array.Copy(dataBuffer, dataIndex, retval, 0, retval.Length);
            dataIndex += retval.Length;
            return retval;
        }

        /// <summary>
        /// Gets a value indicating whether there remains data to be read
        /// </summary>
        /// <remarks>This method will return false if the stream was created for writing</remarks>
        public bool HasMoreData
        {
            get { return (dataBuffer == null) ? false : dataIndex < dataBuffer.Length; }
        }

        #endregion

        #region Write data methods

        /// <summary>
        /// Prepends a <see cref="FLAPHeader"/> and a <see cref="SNACHeader"/> into the byte stream
        /// </summary>
        public void PrependOscarHeaders(FLAPHeader flap, SNACHeader snac)
        {
            lock (this)
            {
                dataSegments.Insert(0, new FlapSegment(flap));
                byteCount += 6;
                if (snac != null)
                {
                    dataSegments.Insert(1, new SnacSegment(snac));
                    byteCount += 10;
                }
            }
        }

        /// <summary>
        /// Writes a byte into the stream
        /// </summary>
        public void WriteByte(byte value)
        {
            lock (this)
            {
                dataSegments.Add(new ByteSegment(value));
                byteCount += 1;
            }
        }

        /// <summary>
        /// Writes a 16-bit integer into the stream
        /// </summary>
        public void WriteUshort(ushort value)
        {
            lock (this)
            {
                dataSegments.Add(new UshortSegment(value));
                byteCount += 2;
            }
        }

        /// <summary>
        /// Writes a 16-bit little endian integer into the stream
        /// </summary>
        public void WriteUshortLE(ushort value)
        {
            ushort storeValue = (ushort)(((value & 0x00FF) << 8) | ((value & 0xFF00) >> 8));
            lock (this)
            {
                dataSegments.Add(new UshortSegment(storeValue));
                byteCount += 2;
            }
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer into the stream
        /// </summary>
        public void WriteUint(uint value)
        {
            lock (this)
            {
                dataSegments.Add(new UintSegment(value));
                byteCount += 4;
            }
        }

        /// <summary>
        /// Writes a 32-bit integer into the stream
        /// </summary>
        public void WriteInt(int value)
        {
            lock (this)
            {
                dataSegments.Add(new IntSegment(value));
                byteCount += 4;
            }
        }

        /// <summary>
        /// Writes a 32-bit little endian integer into the stream
        /// </summary>
        /// <param name="value"></param>
        public void WriteUintLE(uint value)
        {
            lock (this)
            {
                dataSegments.Add(new ByteArraySegment(BitConverter.GetBytes(value)));
                byteCount += 4;
            }
        }

        /// <summary>
        /// Writes a string into the stream using the specified encoding
        /// </summary>
        public void WriteString(string value, Encoding encoding)
        {
            lock (this)
            {
                dataSegments.Add(new StringSegment(value, encoding));
                byteCount += encoding.GetByteCount(value);
            }
        }

        /// <summary>
        /// Writes a <see cref="TlvBlock"/> into the stream
        /// </summary>
        public void WriteTlvBlock(TlvBlock value)
        {
            lock (this)
            {
                dataSegments.Add(new TlvBlockSegment(value));
                byteCount += value.GetByteCount();
            }
        }

        /// <summary>
        /// Writes a <see cref="SNACHeader"/> into the stream
        /// </summary>
        public void WriteSnacHeader(SNACHeader value)
        {
            lock (this)
            {
                dataSegments.Add(new UshortSegment(value.FamilyServiceID));
                dataSegments.Add(new UshortSegment(value.FamilySubtypeID));
                dataSegments.Add(new UshortSegment(value.Flags));
                dataSegments.Add(new UintSegment(value.RequestID));
                byteCount += 10;
            }
        }

        /// <summary>
        /// Writes a byte array into the stream
        /// </summary>
        public void WriteByteArray(byte[] value)
        {
            lock (this)
            {
                dataSegments.Add(new ByteArraySegment(value));
                byteCount += value.Length;
            }
        }

        /// <summary>
        /// Writes an <see cref="SSIItem"/> into the stream
        /// </summary>
        public void WriteSSIItem(SSIItem value)
        {
            lock (this)
            {
                dataSegments.Add(new SSIItemSegment(value));
                if (value.Tlvs == null)
                    byteCount += 10 + value.Name.Length;
                else
                    byteCount += 10 + value.Name.Length + value.Tlvs.GetByteCount();
            }
        }

        /// <summary>
        /// Writes a series of <see cref="SSIItem"/>s into the stream
        /// </summary>
        /// <param name="items"></param>
        public void WriteSSIItems(SSIItem[] items)
        {
            foreach (SSIItem item in items)
            {
                WriteSSIItem(item);
            }
        }

        /// <summary>
        /// Writes a <see cref="BartID"/> into the stream
        /// </summary>
        public void WriteBartID(BartID value)
        {
            lock (this)
            {
                dataSegments.Add(new BartIDSegment(value));
                byteCount += 4 + value.Data.Length;
            }
        }

        #endregion

        /// <summary>
        /// Gets a transmittable byte array representing the byte stream
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            byte[] retval = null;
            if (dataBuffer != null)
            {
                retval = dataBuffer;
            }
            else
            {
                retval = new byte[byteCount];
                int index = 0;
                lock (this)
                {
                    foreach (ISegment segment in dataSegments)
                    {
                        byte[] value = segment.GetBytes();
                        Array.Copy(value, 0, retval, index, value.Length);
                        index += value.Length;
                    }
                }
            }
            return retval;
        }

        /// <summary>
        /// Gets the current number of bytes in the stream
        /// </summary>
        /// <remarks>This method returns the full number of bytes in the stream,
        /// as opposed to the number of bytes remaining to be read.</remarks>
        public int GetByteCount()
        {
            if (dataBuffer != null)
            {
                return dataBuffer.Length;
            }
            else
            {
                return byteCount;
            }
        }

        /// <summary>
        /// Gets the number of bytes remaining to be read
        /// </summary>
        /// <returns>The byte count, if no data exists -1</returns>
        public int GetRemainingByteCount()
        {
            if (dataBuffer != null)
            {
                return dataBuffer.Length - CurrentPosition;
            }
            return -1;
        }

        /// <summary>
        /// Gets the number of bytes remaining to be read, equals <see cref="GetRemainingByteCount"/>
        /// </summary>
        public int RemainingBytes
        {
            get { return GetRemainingByteCount(); }
        }

        /// <summary>
        /// Gets the current read position in the stream
        /// </summary>
        public int CurrentPosition
        {
            get { return dataIndex; }
        }

        /// <summary>
        /// Converts a Microsoft DateTime object to the icq uint value
        /// </summary>
        /// <param name="dateTime">the Microsoft dateTime object</param>
        /// <param name="isLocalTime">Determines if the dateTime object has the local or the universal time format</param>
        /// <returns>the icq time format. past seconds since 1969</returns>
        public static uint ConvertDateTimeToUint(DateTime dateTime, bool isLocalTime)
        {
            if (isLocalTime)
                dateTime = dateTime.ToUniversalTime();

            DateTime _1969 = new DateTime();
            _1969 = _1969.AddYears(1969);
            TimeSpan span = dateTime.Subtract(_1969);

            uint lastModTimeValue = Convert.ToUInt32(span.TotalSeconds);
            return lastModTimeValue;
        }

        #region IDisposable Members
        /// <summary>
        /// Releases resources allocated by this ByteStream
        /// </summary>
        public void Dispose()
        {
            lock (this)
            {
                if (dataBuffer != null)
                {
                    dataBuffer = null;
                }
                if (dataSegments != null)
                {
                    dataSegments.Clear();
                }
            }
        }

        #endregion
    }
}