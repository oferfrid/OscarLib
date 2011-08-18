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
using System.Globalization;
using System.IO;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// A piece of data transmitted over a DirectIM connection
    /// </summary>
    public class Attachment
    {
        private byte[] _data = null;
        private int _id = 0;

        /// <summary>
        /// Initializes a new Attachment
        /// </summary>
        public Attachment(int id, byte[] data)
        {
            _id = id;
            _data = data;
        }

        /// <summary>
        /// Gets the ID of the attachment in the message
        /// </summary>
        public int ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets a new <see cref="MemoryStream"/> consisting of the attachment data
        /// </summary>
        public MemoryStream Data
        {
            get { return new MemoryStream(_data, false); }
        }
    }

    /// <summary>
    /// An instant message sent via a direct connection
    /// </summary>
    public class DirectIM : IM
    {
        private List<Attachment> _attachments = new List<Attachment>();
        private DirectIMConnection _connection;
        private Encoding encoding;


        /// <summary>
        /// Initializes a new Direct IM from a screenname
        /// </summary>
        internal DirectIM(string screenname, DirectIMConnection connection)
            : base(screenname)
        {
            _connection = connection;
        }

        /// <summary>
        /// Gets the connection that received the message
        /// </summary>
        internal DirectIMConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Gets or sets the received encoding of the message
        /// </summary>
        internal Encoding Encoding
        {
            get { return encoding; }
            set { encoding = value; }
        }

        /// <summary>
        /// Gets the list of attachments to this message
        /// </summary>
        public List<Attachment> Attachments
        {
            get { return _attachments; }
            internal set
            {
                if (value == null)
                {
                    _attachments = new List<Attachment>();
                }
                else
                {
                    _attachments = value;
                }
            }
        }

        /// <summary>
        /// Creates an ODC2 packet for sending
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            int headerlength = 76, index = 0;
            Encoding encoding = UtilityMethods.FindBestOscarEncoding(Message);
            int datalength = encoding.GetByteCount(Message);
            int buffersize = headerlength + datalength;
            byte[] packet = new byte[buffersize];
            ushort charset = 0, charsubset = 0;

            Marshal.InsertString(packet, "ODC2", Encoding.ASCII, ref index);
            Marshal.InsertUshort(packet, 0x004C, ref index);
            Marshal.InsertUint(packet, 0x00010006, ref index);
            index = 12;
            Marshal.CopyArray(Connection.Cookie.ToByteArray(), packet, 0, ref index);
            index = 28;
            Marshal.InsertUint(packet, (uint) (datalength + GetAttachmentDataLength()), ref index);
            GetCharsetCodesFromEncoding(encoding, out charset, out charsubset);
            Marshal.InsertUshort(packet, charset, ref index);
            Marshal.InsertUshort(packet, charsubset, ref index);
            Marshal.InsertUint(packet, 0x00000060, ref index);
            index = 44;
            Marshal.InsertString(packet, ScreenName, Encoding.ASCII, ref index);
            Marshal.CopyArray(encoding.GetBytes(Message), packet, 0, 76);
            return packet;
        }

        /// <summary>
        /// Returns the size of the attachment data
        /// </summary>
        public int GetAttachmentDataLength()
        {
            if (Attachments.Count == 0)
            {
                return 0;
            }

            int retval = "<binary></binary>".Length;
            foreach (Attachment attach in Attachments)
            {
                retval += attach.ID.ToString().Length;
                retval += (int) attach.Data.Length;
                retval += "<data id=\"\" size=\"\"></data>".Length;
            }

            return retval;
        }

        /// <summary>
        /// Returns a byte array consisting of the Direct IM attachments in ODC2 format
        /// </summary>
        public byte[] AttachmentsToByteArray()
        {
            int index = 0, size = 0;
            byte[] retval = new byte[GetAttachmentDataLength()];

            Marshal.InsertString(retval, "<BINARY>", Encoding.ASCII, ref index);
            foreach (Attachment attach in Attachments)
            {
                size = (int) attach.Data.Length;
                Marshal.InsertString(retval,
                                     String.Format("<DATA ID=\"{0}\" SIZE=\"{1}\">", attach.ID, size), Encoding.ASCII,
                                     ref index);
                attach.Data.Read(retval, index, size);
                index += size;
                Marshal.InsertString(retval, "</DATA>", Encoding.ASCII, ref index);
            }
            Marshal.InsertString(retval, "</BINARY>", Encoding.ASCII, ref index);
            return retval;
        }
    }

    /// <summary>
    /// An IM that was received offline
    /// </summary>
    public class OfflineIM : IM
    {
        private DateTime receivedOn = DateTime.Now;

        /// <summary>
        /// Initializes a new offline instant message from a screenname
        /// </summary>
        public OfflineIM(string screenname)
            : base(screenname)
        {
        }

        /// <summary>
        /// Initializes a new offline instant message received with a <see cref="UserInfo"/> block
        /// </summary>
        public OfflineIM(UserInfo userInfo)
            : base(userInfo)
        {
        }

        /// <summary>
        /// Initializes a new offline instant message from a realtime instant message
        /// </summary>
        public OfflineIM(IM message)
            : base(message.ScreenName)
        {
            icbmCookie = message.Cookie;
            isAutoResponse = message.IsAutoResponse;
            this.message = message.Message;
            userInfo = message.SenderInfo;
        }

        /// <summary>
        /// Gets the UTC-offset date at which this offline message was originally received
        /// </summary>
        public DateTime ReceivedOn
        {
            get { return receivedOn; }
            internal set { receivedOn = value; }
        }
    }

    /// <summary>
    /// A generic instant message
    /// </summary>
    public class IM
    {
        #region Static methods

        /// <summary>
        /// Gets an <see cref="Encoding"/> from an instant message's charset and language parameters
        /// </summary>
        public static Encoding GetEncodingFromCharset(ushort charset, ushort language)
        {
            // language is actually ignored at the moment

            switch (charset)
            {
                case 0x0000:
                    return Encoding.ASCII;
                case 0x0002:
                    return Encoding.BigEndianUnicode;
                case 0x0003:
                    goto default; // Should be LATIN-1
                default:
                    return Encoding.Default;
            }
        }

        /// <summary>
        /// Gets a charset and sub-charset code from an <see cref="Encoding"/>
        /// </summary>
        public static void GetCharsetCodesFromEncoding(Encoding encoding, out ushort charset, out ushort subset)
        {
            subset = 0;
            if (encoding == Encoding.ASCII)
            {
                charset = 0x0000;
            }
            else if (encoding == Encoding.BigEndianUnicode)
            {
                charset = 0x0002;
            }
            else
            {
                charset = 0x0000;
            }
        }

        #endregion

        /// <summary>
        /// The <see cref="Cookie"/> that uniquely identifies the message
        /// </summary>
        protected Cookie icbmCookie;
        /// <summary>
        /// A value indicating whether the message was received as an
        /// automated response
        /// </summary>
        protected bool isAutoResponse;
        /// <summary>
        /// The message that was received
        /// </summary>
        protected string message;
        /// <summary>
        /// The screen name that sent the message
        /// </summary>
        protected string screenName;
        /// <summary>
        /// A <see cref="UserInfo"/> object describing the message's sender
        /// </summary>
        protected UserInfo userInfo;

        /// <summary>
        /// Initializes a new instant message from a screenname
        /// </summary>
        public IM(string screenname)
        {
            screenName = screenname;
        }

        /// <summary>
        /// Initializes a new instant message received with a <see cref="UserInfo"/> block
        /// </summary>
        public IM(UserInfo userinfo)
        {
            userInfo = userinfo;
            screenName = userinfo.ScreenName;
        }

        #region Properties

        /// <summary>
        /// Gets the screenname that sent the message
        /// </summary>
        public string ScreenName
        {
            get { return screenName; }
            protected internal set { screenName = value; }
        }

        /// <summary>
        /// Gets the <see cref="UserInfo"/> block describing the message sender, if one exists
        /// </summary>
        public UserInfo SenderInfo
        {
            get { return userInfo; }
            protected internal set { userInfo = value; }
        }

        /// <summary>
        /// Gets the unique cookie identifying this message
        /// </summary>
        public Cookie Cookie
        {
            get { return icbmCookie; }
            protected internal set { icbmCookie = value; }
        }

        /// <summary>
        /// Gets a value indicating whether or not this message is an auto response (away message)
        /// </summary>
        public bool IsAutoResponse
        {
            get { return isAutoResponse; }
            protected internal set { isAutoResponse = value; }
        }

        /// <summary>
        /// Gets the message that has been received
        /// </summary>
        /// <remarks>This string is encoded in UTF16-BE</remarks>
        public string Message
        {
            get { return message; }
            protected internal set { message = value; }
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates a uniquely identifying cookie
    /// </summary>
    public class Cookie : IComparable
    {
        private byte[] byteArray;

        /// <summary>
        /// Initializes a new <see cref="Cookie"/> from a byte array
        /// </summary>
        /// <remarks>This constructor makes a new copy of the <paramref name="bytes"/> array</remarks>
        internal Cookie(byte[] bytes)
        {
            byteArray = new byte[bytes.Length];
            Array.Copy(bytes, 0, byteArray, 0, byteArray.Length);
        }

        /// <summary>
        /// Creates a new random <see cref="Cookie"/> suitable for transmission
        /// </summary>
        public static Cookie CreateCookieForSending()
        {
            byte[] cookie = new byte[8];
            Random generator = new Random();
            for (int i = 0; i < 8; i++)
            {
                cookie[i] = (byte) (generator.Next(25) + 'A');
            }
            return new Cookie(cookie);
        }

        /// <summary>
        /// Creates a new random <see cref="Cookie"/> suitable for transmission
        /// </summary>
        /// <remarks>The last byte in this Cookie is guaranteed to be 0x00</remarks>
        public static Cookie CreateNullTerminatedCookieForSending()
        {
            byte[] cookie = new byte[8];
            Random generator = new Random();
            for (int i = 0; i < 7; i++)
            {
                cookie[i] = (byte)(generator.Next(25) + 'A');
            }
            cookie[7] = 0x00;
            return new Cookie(cookie);
        }

        /// <summary>
        /// Creates a new <see cref="Cookie"/> from received data
        /// </summary>
        public static Cookie GetReceivedCookie(byte[] bytes)
        {
            return bytes != null ? new Cookie(bytes) : null;
        }

        /// <summary>
        /// Returns the number of bytes in the cookie
        /// </summary>
        /// <returns>The number of bytes in the cookie</returns>
        public int GetByteCount()
        {
            return byteArray.Length;
        }

        /// <summary>
        /// Returns the representation of the cookie as a byte array
        /// </summary>
        public byte[] ToByteArray()
        {
            byte[] retval = new byte[byteArray.Length];
            Array.Copy(byteArray, 0, retval, 0, retval.Length);
            return retval;
        }

        /// <summary>
        /// Returns the representation of the cookie as a string
        /// </summary>
        public override string ToString()
        {
            StringBuilder retval = new StringBuilder(byteArray.Length*2);
            for (int i = 0; i < byteArray.Length; i++)
            {
                retval.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", byteArray[i]);
            }
            return retval.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Cookie))
            {
                return false;
            }

            Cookie other = obj as Cookie;
            if (this.byteArray.Length != other.byteArray.Length)
            {
                return false;
            }
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] != other.byteArray[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return byteArray.GetHashCode();
        }

        #region IComparable Members
        /// <summary>
        /// Oye...
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return this.Equals(obj) ? 0 : -1;
        }

        #endregion
    }
}