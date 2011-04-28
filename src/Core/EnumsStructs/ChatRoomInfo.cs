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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib.Utility
{
    internal class ChatExchangeInfo
    {
        public string CharSet1 = "";
        public string CharSet2 = "";
        public ushort ClassPermissions = 0x0000;
        public byte CreationPermissions = 0x00;
        public ushort Exchange = 0x0000;
        public ushort Flags = 0x0000;
        public string Language1 = "";
        public string Language2 = "";
        public string Name = "";
    }
}

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles a chat room change event
    /// </summary>
    public delegate void ChatRoomChangedHandler(object sender, ChatRoomChangedEventArgs e);

    /// <summary>
    /// Describes the properties of an OSCAR chat room
    /// </summary>
    public class ChatRoom
    {
        /// <summary>
        /// A default exchange number to use when none is specified
        /// </summary>
        public const int DEFAULT_EXCHANGE = 0x0004;

        /// <summary>
        /// Defines a regular expression to parse an aol:// URI
        /// </summary>
        private static Regex AolUriParser =
            new Regex(@"aol://\d*?:(?<instance>\d*?)-(?<exchange>\d*?)-(?<roomname>.*?)",
                      RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private Encoding charSet1 = Encoding.BigEndianUnicode;
        private Encoding charSet2;

        internal ChatConnection Connection;
        private string contentType = "text/x-aolrtf";
        private byte creationPermissions = 0xFF;
        private DateTime creationTime = DateTime.Now;
        private byte detailLevel;

        // These members will always be populated
        private string displayName;
        private ushort exchangeNumber;
        private ushort flags = 0xFFFF;
        private string fullName;
        private ushort instance = 0xFFFF;
        private string language1 = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private string language2;
        private ushort maxMessageLength = 0xFFFF;
        private ushort maxOccupants = 0xFFFF;

        private List<UserInfo> roomMembers = new List<UserInfo>();
        private ushort unknownCode;

        #region TLV type constants

        private const int CHATMESSAGE_CHARSET = 0x0002;
        private const int CHATMESSAGE_CONTENTTYPE = 0x0004;
        private const int CHATMESSAGE_ENCODING = 0x0005;
        private const int CHATMESSAGE_LANGUAGE = 0x0003;
        private const int CHATMESSAGE_MESSAGE = 0x0001;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_CHARSET1 = 0x00D6;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_CHARSET2 = 0x00D8;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_CONTENTTYPE = 0x00DB;

        /// <summary>
        /// Value: uint
        /// </summary>
        private const int CHATROOM_CREATION_TIME = 0x00CA;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_DISPLAYNAME = 0x00D3;

        /// <summary>
        /// Value: ushort
        /// </summary>
        private const int CHATROOM_FLAGS = 0x00C9;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_LANGUAGE1 = 0x00D7;

        /// <summary>
        /// Value:  ASCII string
        /// </summary>
        private const int CHATROOM_LANGUAGE2 = 0x00D9;

        /// <summary>
        /// Value: ushort
        /// </summary>
        private const int CHATROOM_MAX_OCCUPANTS = 0x00D2;

        /// <summary>
        /// Value: ushort
        /// </summary>
        private const int CHATROOM_MESSAGE_LENGTH = 0x00D1;

        /// <summary>
        /// Value:  byte
        /// </summary>
        private const int CHATROOM_PERMISSIONS = 0x00D5;

        #endregion

        /// <summary>
        /// Raised when a user joins the chat room
        /// </summary>
        public event ChatRoomChangedHandler UserJoined;

        /// <summary>
        /// Raised when a user leaves the chat room
        /// </summary>
        public event ChatRoomChangedHandler UserLeft;

        /// <summary>
        /// Raised when a message is received by the chat room
        /// </summary>
        public event MessageReceivedHandler MessageReceived;

        #region Constructors

        /// <summary>
        /// Create a new <see cref="ChatRoom"/> from an aol:// URI
        /// </summary>
        /// <param name="uri">A URI using the aol:// scheme</param>
        public ChatRoom(string uri)
        {
            Match match = AolUriParser.Match(uri);
            if (!match.Success)
            {
                throw new ArgumentException("The URI must use a valid aol:// scheme", "uri");
            }

            fullName = uri;
            //displayName = UtilityMethods.DeHexUri(match.Groups["roomname"].Value);
            exchangeNumber = (ushort) UtilityMethods.ParseInt(match.Groups["exchange"].Value, NumberStyles.None);
            instance = (ushort) UtilityMethods.ParseInt(match.Groups["instance"].Value, NumberStyles.None);
            int lastDashBeforeName = fullName.IndexOf('-', 16);
            displayName = fullName.Substring(lastDashBeforeName + 1);
        }

        /// <summary>
        /// Creates a new ChatRoomInfo
        /// </summary>
        /// <param name="roomName">The name of the chat room</param>
        /// <param name="exchange">The exchange on which to create the room</param>
        public ChatRoom(string roomName, ushort exchange)
            : this(roomName, exchange, Encoding.ASCII, CultureInfo.CurrentUICulture)
        {
        }

        /// <summary>
        /// Creates a new ChatRoomInfo
        /// </summary>
        /// <param name="roomName">The name of the chat room</param>
        /// <param name="exchange">The exchange on which to create the room</param>
        /// <param name="charset">The character set used by the room</param>
        /// <param name="language">The language used by the room</param>
        public ChatRoom(string roomName, ushort exchange, Encoding charset, CultureInfo language)
        {
            displayName = fullName = roomName;
            exchangeNumber = exchange;
            charSet1 = charset;
            language1 = language.TwoLetterISOLanguageName;
        }

        /// <summary>
        /// Creates a new <see cref="ChatRoom"/> from a received <see cref="ByteStream"/>
        /// </summary>
        public ChatRoom(ByteStream stream)
        {
            exchangeNumber = stream.ReadUshort();
            fullName = stream.ReadString(stream.ReadByte(), Encoding.ASCII);
            instance = stream.ReadUshort();

            // A small chat room info block will only contain the bare essentials:
            // exchange number, chat room name, and instance number.
            // The chat room class really wants a display name, so parse one here.
            if (stream.HasMoreData)
            {
                detailLevel = stream.ReadByte();
                unknownCode = stream.ReadUshort(); // No idea what this is

                using (TlvBlock block = new TlvBlock(stream.ReadByteArrayToEnd()))
                {
                    flags = block.ReadUshort(CHATROOM_FLAGS);
                    if (block.HasTlv(CHATROOM_CREATION_TIME))
                    {
                        creationTime = block.ReadDateTime(CHATROOM_CREATION_TIME);
                    }
                    maxMessageLength = block.ReadUshort(CHATROOM_MESSAGE_LENGTH);
                    maxOccupants = block.ReadUshort(CHATROOM_MAX_OCCUPANTS);
                    displayName = block.ReadString(CHATROOM_DISPLAYNAME, Encoding.ASCII);
                    creationPermissions = block.ReadByte(CHATROOM_PERMISSIONS);

                    string charset = block.ReadString(CHATROOM_CHARSET1, Encoding.ASCII);
                    if (!String.IsNullOrEmpty(charset))
                    {
                        charSet1 = Encoding.GetEncoding(charset);
                    }
                    language1 = block.ReadString(CHATROOM_LANGUAGE1, Encoding.ASCII);

                    charset = block.ReadString(CHATROOM_CHARSET2, Encoding.ASCII);
                    if (!String.IsNullOrEmpty(charset))
                    {
                        charSet2 = Encoding.GetEncoding(charset);
                    }
                    language2 = block.ReadString(CHATROOM_LANGUAGE2, Encoding.ASCII);

                    contentType = block.ReadString(CHATROOM_CONTENTTYPE, Encoding.ASCII);
                }
            }

            // Make sure there's a display name to show
            if (String.IsNullOrEmpty(displayName))
            {
                Match match = AolUriParser.Match(fullName);
                if (match.Success)
                {
                    //displayName = UtilityMethods.DeHexUri(match.Groups["roomname"].Value);
                    int lastDashBeforeName = fullName.IndexOf('-', 16);
                    displayName = fullName.Substring(lastDashBeforeName + 1);
                }
                else
                {
                    displayName = fullName;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The fully-qualified name of the chat room
        /// </summary>
        public string FullName
        {
            get { return fullName; }
        }

        /// <summary>
        /// The name of the chat room that is fit to display to the client
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        /// <summary>
        /// The exchange number of the chat room
        /// </summary>
        /// <remarks>Ideally, neither the exchange nor the <see cref="Instance"/> number
        /// should be exposed to the user</remarks>
        public ushort Exchange
        {
            get { return exchangeNumber; }
            set { exchangeNumber = value; }
        }

        /// <summary>
        /// The instance number of the chat room
        /// </summary>
        /// <remarks>Ideally, neither the instance nor the <see cref="Exchange"/> number
        /// should be exposed to the user</remarks>
        public ushort Instance
        {
            get { return instance; }
            set { instance = value; }
        }

        /// <summary>
        /// The GMT creation time of the chat room
        /// </summary>
        public DateTime CreationTime
        {
            get { return creationTime; }
        }

        /// <summary>
        /// A value indicating whether or not new chat rooms can be created
        /// </summary>
        public byte CreationPermissions
        {
            get { return creationPermissions; }
            set { creationPermissions = value; }
        }

        /// <summary>
        /// The flags of the chat room
        /// </summary>
        public ushort Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        /// <summary>
        /// The maximum message length that can be sent to the room
        /// </summary>
        public ushort MaxMessageLength
        {
            get { return maxMessageLength; }
            set { maxMessageLength = value; }
        }

        /// <summary>
        /// The maximum number of users that can join the chat room
        /// </summary>
        public ushort MaxOccupants
        {
            get { return maxOccupants; }
            set { maxOccupants = value; }
        }

        /// <summary>
        /// Gets the list of current room members
        /// </summary>
        public ReadOnlyCollection<UserInfo> Occupants
        {
            get { return new ReadOnlyCollection<UserInfo>(roomMembers); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Disconnects from the chat room
        /// </summary>
        public void LeaveRoom()
        {
            if (Connection != null && Connection.Connected)
            {
                Connection.DisconnectFromServer(false);
            }
        }

        /// <summary>
        /// Send a message to this chatroom using the room's established character set and language
        /// </summary>
        public void SendMessage(string message)
        {
            SendMessage(message, charSet1, language1);
        }

        /// <summary>
        /// Send a message to this chatroom using a specific character set and language
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="charset">The character set in which to encode the message</param>
        /// <param name="language">The language of the message</param>
        public void SendMessage(string message, Encoding charset, CultureInfo language)
        {
            SendMessage(message, charset, language.TwoLetterISOLanguageName);
        }

        /// <summary>
        /// Send a message to this chatroom using a specific character set and language
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="charset">The character set in which to encode the message</param>
        /// <param name="language">The two-letter code of the language of the message</param>
        private void SendMessage(string message, Encoding charset, string language)
        {
            if (!Connection.Connected)
            {
                // TODO:  The semantics here aren't right
                throw new NotLoggedInException();
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ChatService;
            sh.FamilySubtypeID = (ushort) ChatService.MessageFromClient;
            sh.RequestID = Session.GetNextRequestID();

            byte[] cookie = new byte[8];
            Random r = new Random();
            r.NextBytes(cookie);

            ByteStream stream = new ByteStream();
            stream.WriteByteArray(cookie);
            stream.WriteUshort(0x0003);

            TlvBlock tlvs = new TlvBlock();
            using (TlvBlock messageBlock = new TlvBlock())
            {
                Encoding messageEncoding = UtilityMethods.FindBestOscarEncoding(message);

                messageBlock.WriteString(CHATMESSAGE_MESSAGE, message, messageEncoding);
                messageBlock.WriteString(CHATMESSAGE_CHARSET,
                    UtilityMethods.OscarEncodingToString(messageEncoding), Encoding.ASCII);
                messageBlock.WriteString(CHATMESSAGE_LANGUAGE, language, Encoding.ASCII);
                messageBlock.WriteString(CHATMESSAGE_CONTENTTYPE, "text/x-aolrtf", Encoding.ASCII);
                messageBlock.WriteString(CHATMESSAGE_ENCODING, "binary", Encoding.ASCII);

                tlvs.WriteByteArray(0x0005, messageBlock.GetBytes());
            }
            tlvs.WriteEmpty(0x0001);

            stream.WriteTlvBlock(tlvs);

            DataPacket dp = Marshal.BuildDataPacket(Session.CurrentSession, sh, stream);
            dp.ParentConnection = Connection;
            SNACFunctions.BuildFLAP(dp);
        }

        #endregion

        #region Internal methods
        /// <summary>
        /// Processes an update to the chat room information -- SNAC(0E,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0E,02)</param>
        internal void ProcessRoomUpdate(DataPacket dp)
        {
            exchangeNumber = dp.Data.ReadUshort();
            fullName = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            instance = dp.Data.ReadUshort();
            detailLevel = dp.Data.ReadByte();


            ushort tlvcount = dp.Data.ReadUshort();
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                displayName = tlvs.ReadString(CHATROOM_DISPLAYNAME, Encoding.ASCII);
                flags = tlvs.ReadUshort(CHATROOM_FLAGS);
                creationTime = tlvs.ReadDateTime(CHATROOM_CREATION_TIME);
                maxMessageLength = tlvs.ReadUshort(CHATROOM_MESSAGE_LENGTH);

                string charset = tlvs.ReadString(CHATROOM_CHARSET1, Encoding.ASCII);
                if (!String.IsNullOrEmpty(charset))
                {
                    charSet1 = Encoding.GetEncoding(charset);
                }
                language1 = tlvs.ReadString(CHATROOM_LANGUAGE1, Encoding.ASCII);

                charset = tlvs.ReadString(CHATROOM_CHARSET2, Encoding.ASCII);
                if (!String.IsNullOrEmpty(charset))
                {
                    charSet2 = Encoding.GetEncoding(charset);
                }
                language2 = tlvs.ReadString(CHATROOM_LANGUAGE2, Encoding.ASCII);

                contentType = tlvs.ReadString(CHATROOM_CONTENTTYPE, Encoding.ASCII);

                ushort maxvisiblemessagelength = tlvs.ReadUshort(0x00DA);
                string description = tlvs.ReadString(0x00D3, Encoding.ASCII);
            }
        }

        /// <summary>
        /// Processes a list of users who have joined a chat room -- SNAC(0E,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0E,03)</param>
        internal void ProcessUsersJoined(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                UserInfo ui = dp.Data.ReadUserInfo();
                if (ui != null)
                {
                    roomMembers.Add(ui);
                    if (UserJoined != null)
                    {
                        UserJoined(this, new ChatRoomChangedEventArgs(ui));
                    }
                }
            }
        }

        /// <summary>
        /// Processes a list of users who have left a chat room -- SNAC(0E,04)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0E,04)</param>
        internal void ProcessUsersLeft(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                UserInfo ui = dp.Data.ReadUserInfo();
                if (ui != null)
                {
                    roomMembers.Remove(ui);
                    if (UserLeft != null)
                    {
                        UserLeft(this, new ChatRoomChangedEventArgs(ui));
                    }
                }
            }
        }

        /// <summary>
        /// Processes a message received from a chat room -- SNAC(0E,06)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0E,06)</param>
        internal void ProcessIncomingMessage(DataPacket dp)
        {
            UserInfo sender = new UserInfo();
            byte[] message;
            Encoding encoding = Encoding.ASCII;
            string language = "";

            byte[] cookie = dp.Data.ReadByteArray(8);
            ushort channel = dp.Data.ReadUshort();
            using (TlvBlock outerTlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                using (ByteStream userStream = new ByteStream(outerTlvs.ReadByteArray(0x0003)))
                {
                    sender = userStream.ReadUserInfo();
                }

                using (TlvBlock innerTlvs = new TlvBlock(outerTlvs.ReadByteArray(0x0005)))
                {
                    message = innerTlvs.ReadByteArray(0x0001);
                    encoding = Marshal.AolMimeToEncoding(innerTlvs.ReadString(0x0002, Encoding.ASCII));
                    language = innerTlvs.ReadString(0x0003, Encoding.ASCII);
                }
            }

            if (MessageReceived != null)
            {
                IM msg = new IM(sender);
                msg.Message = Encoding.Unicode.GetString(Encoding.Convert(encoding, Encoding.Unicode, message));
                msg.Cookie = Cookie.GetReceivedCookie(cookie);
                MessageReceived(this, new MessageReceivedEventArgs(msg));
            }
        }

        /// <summary>
        /// Writes the ChatRoomInfo's parameters to a byte stream for transmission
        /// </summary>
        internal void WriteToByteStream(ByteStream stream)
        {
            // Write the basic info header
            stream.WriteUshort(exchangeNumber);
            stream.WriteByte((byte) "create".Length);
            stream.WriteString("create", Encoding.ASCII);
            stream.WriteUshort(instance);

            // Write details
            stream.WriteByte(0x01); // Detail level
            stream.WriteUshort(0x0003); // An unknown code, AIM 5.9 sets this to 0x0003
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(CHATROOM_CHARSET1, UtilityMethods.OscarEncodingToString(charSet1), Encoding.ASCII);
                tlvs.WriteString(CHATROOM_DISPLAYNAME, displayName, Encoding.ASCII);
                tlvs.WriteString(CHATROOM_LANGUAGE1, language1, Encoding.ASCII);

                // Everything else is optional
                if (contentType != "text/x-aolrtf")
                {
                    tlvs.WriteString(CHATROOM_CONTENTTYPE, contentType, Encoding.ASCII);
                }

                if (Flags != 0xFFFF)
                {
                    tlvs.WriteUshort(CHATROOM_FLAGS, Flags);
                }
                if (MaxMessageLength != 0xFFFF)
                {
                    tlvs.WriteUshort(CHATROOM_MESSAGE_LENGTH, MaxMessageLength);
                }
                if (MaxOccupants != 0xFFFF)
                {
                    tlvs.WriteUshort(CHATROOM_MAX_OCCUPANTS, MaxOccupants);
                }
                if (CreationPermissions != 0xFF)
                {
                    tlvs.WriteByte(CHATROOM_PERMISSIONS, CreationPermissions);
                }
                if (charSet2 != null)
                {
                    tlvs.WriteString(CHATROOM_CHARSET2, charSet2.WebName, Encoding.ASCII);
                }
                if (!String.IsNullOrEmpty(language2))
                {
                    tlvs.WriteString(CHATROOM_LANGUAGE2, language2, Encoding.ASCII);
                }

                //stream.WriteUshort((ushort) tlvs.GetByteCount());
                stream.WriteTlvBlock(tlvs);
            }
        }

        #endregion
    }
}