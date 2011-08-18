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
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles chat room navigation and creation
    /// </summary>
    /// <remarks>This manager is responsible for processing SNACs from family 0x000D (ChatNav)
    /// and dispatching SNACs from family 0x000E to the correct <see cref="ChatRoom"/></remarks>
    public class ChatRoomManager : ISnacFamilyHandler
    {
        #region SNAC constants
        /// <summary>
        /// The SNAC family that handles chat room creation and information
        /// </summary>
        private const int SNAC_CHATNAV_FAMILY = 0x000D;
        /// <summary>
        /// The SNAC family used for chat room notifications and messages
        /// </summary>
        private const int SNAC_CHATROOM_FAMILY = 0x000E;
        /// <summary>
        /// The subtype used to indicate a SNAC-level error
        /// </summary>
        private const int ERROR = 0x0001;
        /// <summary>
        /// The subtype used to request chat room creation limitations
        /// </summary>
        private const int CHATNAV_PARAMETER_LIMITREQUEST = 0x0002;
        /// <summary>
        /// The subtype used to create a new chat room
        /// </summary>
        private const int CHATNAV_CREATE_ROOM = 0x0008;
        /// <summary>
        /// The subtype used to indicate a parameter response
        /// </summary>
        private const int CHATNAV_PARAMETER_REPLY = 0x0009;
        /// <summary>
        /// The subtype used to indicate an update to chat room information
        /// </summary>
        private const int CHATROOM_INFO_UPDATE = 0x0002;
        /// <summary>
        /// The subtype used to indicate a user has joined the chatroom
        /// </summary>
        private const int CHATROOM_USER_JOINED = 0x0003;
        /// <summary>
        /// The subtype used to indicate a user has left the chatroom
        /// </summary>
        private const int CHATROOM_USER_LEFT = 0x0004;
        /// <summary>
        /// The subtype used to indicate a chatroom member has sent a message
        /// </summary>
        private const int CHATROOM_MESSAGE_RECEIVED = 0x0006;
        #endregion

        private readonly Session parent;
        private readonly Dictionary<Cookie, ChatRoom> chatRoomInvitations =
            new Dictionary<Cookie, ChatRoom>();
        private readonly List<ChatRoom> waitingOnChatNavService =
            new List<ChatRoom>();
        /// <summary>
        /// A map of reasons for receiving chat nav parameters
        /// </summary>
        private readonly Dictionary<uint, string> chatParameterRequests =
            new Dictionary<uint, string>();
        /// <summary>
        /// A map of SNAC creation request IDs to chat room objects
        /// </summary>
        private readonly Dictionary<uint, ChatRoom> chatRoomCreationRequests =
            new Dictionary<uint, ChatRoom>();

        /// <summary>
        /// Initializes a new ChatRoomManager
        /// </summary>
        internal ChatRoomManager(Session parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_CHATNAV_FAMILY);
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_CHATROOM_FAMILY);
        }

        #region Public methods

        /// <summary>
        /// Creates a new chat room
        /// </summary>
        /// <param name="roomname">The name of the room to create</param>
        /// <param name="language">The language to be used in the room</param>
        /// <param name="charset">The character set to be used in the room</param>
        /// <remarks>
        /// <para>
        /// This method uses default values for the chat room exchange and instance parameters that
        /// will not allow entry to an AOL Members Only chat room.  To join an AOL Members chat room,
        /// use the <see cref="CreateChatRoom(string)"/> overload that takes an aol:// URL.</para>
        /// <para>Once the chat room has been created, you are automatically joined to it</para>
        /// </remarks>
        public void CreateChatRoom(string roomname, CultureInfo language, Encoding charset)
        {
            CreateChatRoom(roomname, 0x0004, 0xFFFF, charset, language);
        }

        /// <summary>
        /// Join a chat room using an aol:// URI
        /// </summary>
        /// <param name="uri">A URI using the aol:// scheme</param>
        public void CreateChatRoom(string uri)
        {
            CreateChatRoom(new ChatRoom(uri));
        }

        /// <summary>
        /// Creates a new chat room
        /// </summary>
        /// <param name="roomname">The name of the room to create</param>
        /// <param name="exchange">The exchange in which to create the room</param>
        /// <param name="instance">The instance number of the chat room</param>
        /// <param name="language">The language to be used in the room</param>
        /// <param name="charset">The character set to be used in the room</param>
        /// <remarks>
        /// <para>Once the chat room has been created, you are automatically joined to it</para>
        /// <para>This overload of CreateChatRoom allows the client to specify the exchange number.
        /// TODO:  explain this.</para>
        /// </remarks>
        public void CreateChatRoom(string roomname, ushort exchange, ushort instance, Encoding charset,
                                   CultureInfo language)
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            ChatRoom room = new ChatRoom(roomname, exchange, charset, language);
            ChatInvitationConnection.CacheChatRoomCreation(roomname, exchange);
            CreateChatRoom(room);
        }

        /// <summary>
        /// Accepts a chat room invite and joins the requested room
        /// </summary>
        /// <param name="invitationKey">The <see cref="Cookie"/> received with the invitation</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="invitationKey"/> does
        /// not describe an outstanding chat room invitation.</exception>
        public void JoinChatRoom(Cookie invitationKey)
        {
            if (!chatRoomInvitations.ContainsKey(invitationKey))
            {
                throw new ArgumentException("Not a valid chat room invitation key", "invitationKey");
            }

            ChatRoom chatRoom = chatRoomInvitations[invitationKey];
            chatRoomInvitations.Remove(invitationKey);

            if (parent.Connections.GetByFamily(SNAC_CHATNAV_FAMILY) == null)
            {
                waitingOnChatNavService.Add(chatRoom);
                RequestParameters();
            }
            else
            {
                RequestChatRoomConnection(chatRoom);
            }
        }

        /// <summary>
        /// Declines a chat room invite
        /// </summary>
        /// <param name="invitationKey">The <see cref="Cookie"/> received with the invitation</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="invitationKey"/> does
        /// not describe an outstanding chat room invitation.</exception>
        public void DeclineChatInvitation(Cookie invitationKey)
        {
            if (!chatRoomInvitations.ContainsKey(invitationKey))
            {
                throw new ArgumentException("Not a valid chat room invitation key", "invitationKey");
            }

            chatRoomInvitations.Remove(invitationKey);
            DirectConnection conn = parent.Connections.RemoveDirectConnection(invitationKey);
            parent.Messages.SendDirectConnectionCancellation(conn, "User declined invitation");
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Requests parameter settings for the ChatNav SNAC family
        /// </summary>
        internal void RequestParameters()
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_CHATNAV_FAMILY;
            sh.FamilySubtypeID = CHATNAV_PARAMETER_LIMITREQUEST;
            

            chatParameterRequests[sh.RequestID] = "parameters";

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, new ByteStream()));
        }

        /// <summary>
        /// Caches a chat room invitation
        /// </summary>
        /// <param name="invitationKey">The <see cref="Cookie"/> received with the invitation</param>
        /// <param name="chatRoom">The <see cref="ChatRoom"/> to cache</param>
        internal void CacheChatRoomInvitation(Cookie invitationKey, ChatRoom chatRoom)
        {
            chatRoomInvitations[invitationKey] = chatRoom;
        }

        /// <summary>
        /// Creates a new AIM chat room
        /// </summary>
        /// <param name="chatRoom">A <see cref="ChatRoom"/> object describing the room to create</param>
        /// <remarks>TODO:  I think this doesn't work, the fullname should be "create" maybe?</remarks>
        private void CreateChatRoom(ChatRoom chatRoom)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_CHATNAV_FAMILY;
            sh.FamilySubtypeID = CHATNAV_CREATE_ROOM;
            
            

            ByteStream stream = new ByteStream();
            chatRoom.WriteToByteStream(stream);

            chatRoomCreationRequests[sh.RequestID] = chatRoom;
            chatParameterRequests[sh.RequestID] = "create";

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        /// <summary>
        /// Sends a request for a new chat room connection -- SNAC(01,04)
        /// </summary>
        /// <param name="chatRoom">A <see cref="ChatRoom"/> object</param>
        internal void RequestChatRoomConnection(ChatRoom chatRoom)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = 0x0001;
            sh.FamilySubtypeID = 0x0004;
            
            

            ByteStream chatStream = new ByteStream();
            chatStream.WriteUshort(chatRoom.Exchange);
            chatStream.WriteByte((byte)Encoding.ASCII.GetByteCount(chatRoom.FullName));
            chatStream.WriteString(chatRoom.FullName, Encoding.ASCII);
            chatStream.WriteUshort(chatRoom.Instance);

            TlvBlock tlv = new TlvBlock();
            tlv.WriteByteArray(0x0001, chatStream.GetBytes());

            ByteStream mainStream = new ByteStream();
            mainStream.WriteUshort(0x000E);
            mainStream.WriteTlvBlock(tlv);

            parent.StoreRequestID(sh.RequestID, chatRoom);

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, mainStream);
            // This one is always sent on BOS
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Requests connections for any outstanding chat room invitations
        /// </summary>
        internal void OnChatNavServiceAvailable()
        {
            lock (waitingOnChatNavService)
            {
                foreach (ChatRoom chatRoom in waitingOnChatNavService)
                {
                    RequestChatRoomConnection(chatRoom);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ChatRoomJoined"/> event
        /// </summary>
        /// <param name="chatRoom">The chat room that has become available</param>
        internal void OnChatRoomConnectionAvailable(ChatRoom chatRoom)
        {
            if (ChatRoomJoined != null)
            {
                ChatRoomJoined(this, chatRoom);
            }
        }
        #endregion

        /// <summary>
        /// Occurs when a chat room has been successfully joined
        /// </summary>
        public event ChatRoomJoinedHandler ChatRoomJoined;

        /// <summary>
        /// Occurs when a chat room could not be created
        /// </summary>
        public event ChatRoomErrorHandler ChatRoomCreationFailed;

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/>
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(DataPacket dp)
        {
            if (dp.SNAC.FamilyServiceID == SNAC_CHATNAV_FAMILY)
            {
                switch (dp.SNAC.FamilySubtypeID)
                {
                    case ERROR:
                        ProcessChatRoomCreationError(dp);
                        break;
                    case CHATNAV_PARAMETER_REPLY:
                        ProcessChatRoomInformation(dp);
                        break;
                }
            }
            else if(dp.SNAC.FamilyServiceID == SNAC_CHATROOM_FAMILY)
            {
                ChatConnection conn = dp.ParentConnection as ChatConnection;
                if (conn != null)
                {
                    switch (dp.SNAC.FamilySubtypeID)
                    {
                        case CHATROOM_INFO_UPDATE:
                            conn.ChatRoom.ProcessRoomUpdate(dp);
                            break;
                        case CHATROOM_USER_JOINED:
                            conn.ChatRoom.ProcessUsersJoined(dp);
                            break;
                        case CHATROOM_USER_LEFT:
                            conn.ChatRoom.ProcessUsersLeft(dp);
                            break;
                        case CHATROOM_MESSAGE_RECEIVED:
                            conn.ChatRoom.ProcessIncomingMessage(dp);
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Handles an error occuring during chat room navigation
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0D,01)</param>
        public void ProcessChatRoomCreationError(DataPacket dp)
        {
            uint requestId = dp.SNAC.RequestID;
            if (chatRoomCreationRequests.ContainsKey(requestId))
            {
                if (ChatRoomCreationFailed != null)
                {
                    ChatRoomCreationFailed(this, chatRoomCreationRequests[requestId]);
                }
                chatRoomCreationRequests.Remove(requestId);
            }
            else
            {
                // What?
            }
        }

        /// <summary>
        /// Processes requested chat room information -- SNAC(0D,09)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0D,09)</param>
        public void ProcessChatRoomInformation(DataPacket dp)
        {
            ushort maxRooms = 0xFFFF;
            ReadOnlyCollection<Tlv> exchangeTlvs = null;
            ChatRoom newRoom = null;

            // Parse the response
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                maxRooms = tlvs.ReadByte(0x0002);
                if (tlvs.HasTlv(0x0003))
                {
                    exchangeTlvs = tlvs.ReadAllTlvs(0x0003);
                }
                if (tlvs.HasTlv(0x0004))
                {
                    newRoom = new ChatRoom(new ByteStream(tlvs.ReadByteArray(0x0004)));
                }
            }

            // If this packet was received in response to a chat room creation request,
            // request a new chat room connection from the BOS connection
            if (newRoom != null)
            {
                RequestChatRoomConnection(newRoom);
            }
        }

        private ChatExchangeInfo ParseChatExchangeInfo(ByteStream stream, int tlvLength)
        {
            ChatExchangeInfo retval = new ChatExchangeInfo();
            retval.Exchange = stream.ReadUshort();
            using (TlvBlock tlvs = new TlvBlock(stream.ReadByteArray(tlvLength)))
            {
                retval.ClassPermissions = tlvs.ReadUshort(0x0002);
                retval.Flags = tlvs.ReadUshort(0x00C9);
                retval.Name = tlvs.ReadString(0x00D3, Encoding.ASCII);
                retval.CreationPermissions = tlvs.ReadByte(0x00D5);
                retval.CharSet1 = tlvs.ReadString(0x00D6, Encoding.ASCII);
                retval.Language1 = tlvs.ReadString(0x00D7, Encoding.ASCII);
                retval.CharSet2 = tlvs.ReadString(0x00D8, Encoding.ASCII);
                retval.Language2 = tlvs.ReadString(0x00D9, Encoding.ASCII);
            }

            return retval;
        }
        #endregion
    }
}
