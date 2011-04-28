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
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles the basic OSCAR connection and service request functionality
    /// </summary>
    /// <remarks>This manager is responsible for processing SNACs from family 0x0001 (basic service).</remarks>
    public class ServiceManager : ISnacFamilyHandler
    {
        /// <summary>
        /// The SNAC family responsible for basic OSCAR services
        /// </summary>
        private const int SNAC_BOS_FAMILY = 0x0001;

        #region SNAC subtype constants
        /// <summary>
        /// BOS code for a SNAC level error
        /// </summary>
        private const int BOS_SNAC_ERROR = 0x0001;
        /// <summary>
        /// BOS code for signalling that a client is ready to go online
        /// </summary>
        private const int BOS_CONNECTION_READY = 0x0002;
        /// <summary>
        /// BOS code for the list of families supported on a connection
        /// </summary>
        private const int BOS_SUPPORTED_FAMILIES = 0x0003;
        /// <summary>
        /// BOS code for requesting a new service connection
        /// </summary>
        private const int BOS_REQUEST_NEW_SERVICE = 0x0004;
        /// <summary>
        /// BOS code for new service connection parameters
        /// </summary>
        private const int BOS_NEW_SERVICE = 0x0005;
        /// <summary>
        /// BOS code for requesting rate limit information
        /// </summary>
        private const int BOS_RATE_LIMIT_REQUST = 0x0006;
        /// <summary>
        /// BOS code for initial rate limit information
        /// </summary>
        private const int BOS_RATE_LIMIT_INFORMATION = 0x0007;
        /// <summary>
        /// BOS code for acknowledging rate limitation
        /// </summary>
        private const int BOS_ACKNOWLEDGE_RATE_LIMITS = 0x0008;
        /// <summary>
        /// BOS code for information about a rate class update
        /// </summary>
        private const int BOS_RATE_CHANGE_NOTIFICATION = 0x001A;
        /// <summary>
        /// BOS code for initiating a server migration sequence
        /// </summary>
        private const int BOS_PAUSE_CONNECTION_REQUEST = 0x000B;
        /// <summary>
        /// BOS code for responding to a connection pause request
        /// </summary>
        private const int BOS_PAUSE_CONNECTION_RESPONSE = 0x000C;
        /// <summary>
        /// BOS code for notifying that server migration is complete
        /// </summary>
        private const int BOS_RESUME_CONNECTION = 0x000D;
        /// <summary>
        /// BOS code for requesting our own online information
        /// </summary>
        private const int BOS_REQUEST_OWN_INFORMATION = 0x000E;
        /// <summary>
        /// BOS code for receiving our own online information
        /// </summary>
        private const int BOS_OWN_ONLINE_INFORMATION = 0x000F;
        /// <summary>
        /// BOS code for a OSCAR server migration notification
        /// </summary>
        private const int BOS_SERVER_MIGRATION_NOTICE = 0x0012;
        /// <summary>
        /// BOS code for requesting the list of SNAC family versions
        /// </summary>
        private const int BOS_FAMILY_VERSION_REQUEST = 0x0017;
        /// <summary>
        /// BOS code for the list of SNAC family versions
        /// </summary>
        private const int BOS_FAMILY_VERSIONS_RESPONSE = 0x0018;
        /// <summary>
        /// BOS code for setting extended ICQ statuses
        /// </summary>
        private const int BOS_SET_EXTENDED_STATUS = 0x001E;
        /// <summary>
        /// BOS code for a request for client verification
        /// </summary>
        private const int BOS_CLIENT_VERIFICATION_REQUEST = 0x001F;
        /// <summary>
        /// BOS code for a response to a client verification request
        /// </summary>
        private const int BOS_CLIENT_VERIFICATION_RESPONSE = 0x0020;
        /// <summary>
        /// BOS code for a reply to our own extended status
        /// </summary>
        private const int BOS_PROCESS_EXTENDED_STATUS = 0x0021;
        #endregion SNAC subtype constants

        #region TLV typecodes
        private const int NEW_SERVICE_ADDRESS = 0x0005;
        private const int NEW_SERVICE_COOKIE = 0x0006;
        private const int NEW_SERVICE_FAMILY = 0x000D;
        private const int NEW_SERVICE_VERSION = 0x0001;

        private const int EXTSTATUS_AVAILMESSAGE = 0x0002;
        private const int EXTSTATUS_ICON1 = 0x0000;
        private const int EXTSTATUS_ICON2 = 0x0001;
        #endregion

		private readonly ISession parent;

        /// <summary>
        /// Initializes a new ServiceManager
        /// </summary>
		internal ServiceManager(ISession parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_BOS_FAMILY);
        }

        #region Public methods
        /// <summary>
        /// Sends a request for a new service family
        /// </summary>
        /// <param name="newfamily">The new family to request</param>
        public void RequestNewService(ushort newfamily)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_REQUEST_NEW_SERVICE;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(newfamily);

            // This SNAC expects a response in SNAC(01,05)
            parent.StoreRequestID(sh.RequestID, null);

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            // This one is always sent on BOS
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Requests the version of SNAC services provided to this connection
        /// </summary>
        /// <param name="connection">The <see cref="Connection"/> for which to retrieve service information</param>
        public void RequestServiceVersions(Connection connection)
        {
            // Pack the family IDs and version numbers into a stream
            ushort[] families = parent.Connections.GetFamilies(connection);

            Array.Sort(families);
            Array.Reverse(families);

            ByteStream stream = new ByteStream();
            foreach (ushort id in families)
            {
                ushort version = parent.Families.GetFamilyVersion(id);
                stream.WriteUshort(id);
                stream.WriteUshort(version);
            }

            // Construct SNAC (01,17)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_FAMILY_VERSION_REQUEST;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            dp.ParentConnection = connection; // Always send this on the same connection it was received
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Requests the client's own online information -- SNAC(01,0E)
        /// </summary>
        public void RequestOnlineInformation()
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_REQUEST_OWN_INFORMATION;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();


            DataPacket dp = Marshal.BuildDataPacket(parent, sh, new ByteStream());
            // Always gets sent on BOS
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Notifies the OSCAR server that the new connection is ready to receive service-specific data
        /// </summary>
        /// <remarks>
        /// This SNAC must be sent within 30ish seconds of connection, or the connection will drop.
        /// This should not ever be a problem, barring a lost connection well below the level of this library.
        /// </remarks>
        public void SendReadyNotification(Connection conn)
        {
            // Build SNAC(01,02)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_CONNECTION_READY;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ushort[] families = parent.Connections.GetFamilies(conn);
            FamilyManager fm = parent.Families;

            Array.Sort(families);
            Array.Reverse(families);

            bool isChatRoomConnection = false;
            bool isChatNavService = false;

            ByteStream stream = new ByteStream();
            DataPacket[][] delayedframes = new DataPacket[families.Length][];

            for (int i = 0; i < families.Length; i++)
            {
                ushort family = families[i];
                delayedframes[i] = parent.Connections.GetDelayedPackets(family);

                stream.WriteUshort(family);
                stream.WriteUshort(fm.GetFamilyVersion(family));
                stream.WriteUshort(fm.GetFamilyToolID(family));
                stream.WriteUshort(fm.GetFamilyToolVersion(family));

                if (family == 0x000D)
                {
                    isChatNavService = true;
                }
                else if (family == 0x000E)
                {
                    isChatRoomConnection = true;
                }
            }

            /*
             * The initial service connection has to send SNAC(01,1E) before it actually
             * sends SNAC(01,02), thus the check for the initial connection here
             * and after it gets sent.
            */
            if (!parent.LoggedIn)
            {
                SetExtendedStatus(parent.AvailableMessage, ICQFlags.Normal, ICQStatus.Online);
                SetExtendedStatus(null, ICQFlags.Normal, ICQStatus.Online);
            }

            // The connection is done, so start sending keepalives
            conn.Connecting = false;
            conn.ReadyForData = true;

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            dp.ParentConnection = conn;
            SNACFunctions.BuildFLAP(dp);

            /*
			 * If this is a new service connection, there is probably at least one
			 * delayed packet. Process those packets. Additionally, if the new connection is
             * a chatroom connection, query the Rendezvous manager to see if it is the result
             * of an explict room creation (or implict, as in accepting a chat invitation).
             * If it was explict, notify the user that it's done
			 */
            if (parent.LoggedIn)
            {
                foreach (DataPacket[] list in delayedframes)
                {
                    if (list == null) continue;
                    foreach (DataPacket dp_delay in list)
                    {
                        dp_delay.ParentConnection = conn;
                        SNACFunctions.DispatchToRateClass(dp_delay);
                    }
                }

                if (isChatNavService)
                {
                    parent.ChatRooms.OnChatNavServiceAvailable();
                }
                else if (isChatRoomConnection)
                {
                    ChatRoom chatRoom = parent.Connections.GetChatByConnection((ChatConnection)conn);
                    parent.ChatRooms.OnChatRoomConnectionAvailable(chatRoom);
                }
            }
        }

        /// <summary>
        /// Sets the client's "Available" message -- SNAC(01,1E)
        /// </summary>
        /// <param name="availablemessage">The available message to set</param>
        /// <remarks>TODO:  This probably belongs in the status manager</remarks>
        public void SetAvailableMessage(string availablemessage)
        {
            SetExtendedStatus(availablemessage, ICQFlags.Normal, ICQStatus.Online);
        }

        /// <summary>
        /// Sets the client's ICQ status -- SNAC(01,1E)
        /// </summary>
        /// <param name="flags">The ICQ flags to set</param>
        /// <param name="status">The ICQ status flags to set</param>
        /// <remarks>TODO:  This probably belongs in the status manager</remarks>
        public void SetExtendedICQStatus(ICQFlags flags, ICQStatus status)
        {
            SetExtendedStatus(null, flags, status);
        }

        /// <summary>
        /// Sends a response to a client verification request -- SNAC(01,20)
        /// </summary>
        /// <remarks>
        /// SNAC(01,1F) and SNAC(01,20) are used to verify that the client connecting
        /// to the network was the official AOL Instant Messenger.  The server sends
        /// two integers, an offset and a length, in SNAC(01,1F).  These parameters are then used to
        /// get 16 bytes of data in the offical client's static memory region. However, after
        /// AOL begain using this authentication method, the servers never changed the requested
        /// offset and length. The expected response was the same 16 bytes every time, which made
        /// it a fairly ineffective deterrent to "unauthorized" clients.
        /// </remarks>
        public void SendVerificationResponse()
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_CLIENT_VERIFICATION_RESPONSE;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUint(0x44a95d26);
            stream.WriteUint(0xd2490423);
            stream.WriteUint(0x93b8821f);
            stream.WriteUint(0x51c54b01);

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }
        #endregion Public methods

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 1
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(csammisrun.OscarLib.Utility.DataPacket dp)
        {
            switch (dp.SNAC.FamilySubtypeID)
            {
                case BOS_SNAC_ERROR:
                    SNACFunctions.ProcessErrorNotification(dp);
                    break;
                case BOS_SUPPORTED_FAMILIES:
                    ProcessServicesList(dp);
                    break;
                case BOS_NEW_SERVICE:
                    ProcessNewServiceResponse(dp);
                    break;
                case BOS_RATE_LIMIT_INFORMATION:
                    ProcessRateLimitations(dp);
                    break;
                case BOS_FAMILY_VERSIONS_RESPONSE:
                    ProcessVersionsListAndGetRateLimits(dp);
                    break;
                case BOS_RATE_CHANGE_NOTIFICATION:
                    ProcessRateChange(dp);
                    break;
                case BOS_PAUSE_CONNECTION_REQUEST:
                    ProcessPauseRequest(dp);
                    break;
                case BOS_RESUME_CONNECTION:
                    ProcessResumeRequest(dp);
                    break;
                case BOS_OWN_ONLINE_INFORMATION:
                    ProcessOwnInformation(dp);
                    break;
                case BOS_SERVER_MIGRATION_NOTICE:
                    ProcessMigrationNotice(dp);
                    break;
                case BOS_PROCESS_EXTENDED_STATUS:
                    ProcessExtendedStatus(dp);
                    break;
                case BOS_CLIENT_VERIFICATION_REQUEST:
                    ProcessVerificationRequest(dp);
                    break;
            }
        }

        #endregion ISnacFamilyHandler Members

        #region Handlers
        /// <summary>
        /// Processes the client's own online information -- SNAC(01,0F)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0F)</param>
        private void ProcessOwnInformation(DataPacket dp)
        {
            UserInfo ui = dp.Data.ReadUserInfo();
            if (ui.Icon != null)
            {
                parent.Graphics.OwnBuddyIcon = ui.Icon;
            }
        }

        /// <summary>
        /// Processes the server-supported family list -- SNAC(01,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,03)</param>
        private void ProcessServicesList(DataPacket dp)
        {
            // Get the list of supported families and populate the session list
            while (dp.Data.HasMoreData)
            {
                ushort family = dp.Data.ReadUshort();
                // Add family to connection list, unless it is 0x0001, because all
                // connections (except authorization) handle this family
                if (family != SNAC_BOS_FAMILY)
                {
                    parent.Connections.AssignFamily(family, dp.ParentConnection);
                }
            }

            // Request the server versions of these services
            RequestServiceVersions(dp.ParentConnection);
        }

        /// <summary>
        /// Processes the server response to a new family request -- SNAC(01,05)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,05)</param>
        private void ProcessNewServiceResponse(DataPacket dp)
        {
            int startIndex = 0;
            byte[] SNACData = dp.Data.ReadByteArrayToEnd();
            if (SNACData[0] == 0x00 && SNACData[1] == 0x06)
            {
                startIndex += 2; // What the heck is this...0x0006, some families, some of the time?
            }

            using (TlvBlock tlvs = new TlvBlock(SNACData, startIndex))
            {
                ushort family = tlvs.ReadUshort(NEW_SERVICE_FAMILY);
                string BOSaddress = tlvs.ReadString(NEW_SERVICE_ADDRESS, Encoding.ASCII);
                byte[] cookie = tlvs.ReadByteArray(NEW_SERVICE_COOKIE);

                Connection newconn = null;
                object store = dp.ParentSession.RetrieveRequestID(dp.SNAC.RequestID);

                if (family != 0x000E)
                {
                    newconn = dp.ParentSession.Connections.CreateNewConnection(family);
                }
                else
                {
                    ChatRoom roominfo = (ChatRoom)store;
                    newconn = dp.ParentSession.Connections.CreateNewChatConnection(roominfo);
                }

                string[] bosinfo = BOSaddress.Split(':');
                int port = 0;
                if (bosinfo.Length == 2)
                {
                    port = Int32.Parse(bosinfo[1]);
                }
                else
                {
                    port = dp.ParentSession.LoginPort;
                }

                newconn.ServerConnectionCompleted += delegate { newconn.ReadHeader(); };
                newconn.Server = bosinfo[0];
                newconn.Port = port;
                newconn.Cookie = new Cookie(cookie);
                newconn.ConnectToServer();
            }

            // The connection process continues when the server sends SNAC(01,03)
        }

        /// <summary>
        /// Sends SNAC(01,06) in response to SNAC(01,18)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,18)</param>
        /// <remarks>OscarLib doesn't store the version list, so this method just sends SNAC(01,06) for rate limit info</remarks>
        private void ProcessVersionsListAndGetRateLimits(DataPacket dp)
        {
            // Construct SNAC (01,06)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_RATE_LIMIT_REQUST;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            DataPacket dp2 = Marshal.BuildDataPacket(parent, sh, new ByteStream());
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);
        }

        /// <summary>
        /// Processes the list of rate limitations -- SNAC(01,07)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,07)</param>
        /// <remarks>This is the last step in setting up a secondary service connection,
        /// and the trigger to initialize basic services for the primary connection.</remarks>
        private void ProcessRateLimitations(DataPacket dp)
        {
            ushort num_classes = dp.Data.ReadUshort();
            ushort[] classes = new ushort[num_classes];

            RateClassManager rcm = parent.RateClasses;

            // Get the rate class attributes out of the SNAC
            for (int i = 0; i < num_classes; i++)
            {
                ushort id = dp.Data.ReadUshort();
                RateClass rc = rcm.GetByID(id);
                rc.WindowSize = dp.Data.ReadUint();
                rc.ClearLevel = dp.Data.ReadUint();
                rc.AlertLevel = dp.Data.ReadUint();
                rc.LimitLevel = dp.Data.ReadUint();
                rc.DisconnectLevel = dp.Data.ReadUint();
                rc.CurrentLevel = dp.Data.ReadUint();
                rc.MaxLevel = dp.Data.ReadUint();
                if (parent.Families.GetFamilyVersion(0x0001) >= 3)
                {
                    rc.LastTime = dp.Data.ReadUint();
                    rc.CurrentState = dp.Data.ReadByte();
                }
                rcm.SetByID(id, rc);
                rc.StartLimitedTransmission();

                classes[i] = id;
            }

            // Register rates with the session's ConnectionList
            for (int i = 0; i < num_classes; i++)
            {
                ushort id = dp.Data.ReadUshort();
                ushort num_pairs = dp.Data.ReadUshort();

                for (int j = 0; j < num_pairs; j++)
                {
                    ushort family = dp.Data.ReadUshort();
                    ushort subtype = dp.Data.ReadUshort();
                    rcm.SetRateClassKey(family, subtype, id);
                }
            }

            // Construct SNAC(01,08)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_ACKNOWLEDGE_RATE_LIMITS;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            for (int i = 0; i < classes.Length; i++)
            {
                stream.WriteUshort(classes[i]);
            }

            DataPacket dp2 = Marshal.BuildDataPacket(parent, sh, stream);
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);

            parent.OnLoginStatusUpdate("Protocol negotiation complete", 0.66);

            /*
			 * If this is the initial services connection, we call the remaining SNACs
			 * in the login sequence.
			 * Otherwise, this is the last step in setting up a new service connection,
			 * and we send SNAC(01,02) here.
			 */
            if (!parent.LoggedIn)
            {
                // Start stage 3, services setup
                RequestOnlineInformation();
                SNAC13.RequestParametersList(parent);
                SNAC13.RequestInitialContactList(parent);
                parent.Statuses.RequestParameters();
                parent.Messages.RequestParametersList();
                SNAC09.RequestParametersList(parent);
                parent.Statuses.ReportClientCapabilities();
                parent.Statuses.SetProfile(parent.Statuses.Profile);
            }
            else
            {
                SendReadyNotification(dp.ParentConnection);
            }
        }

        /// <summary>
        /// Process a rate change message from the server -- SNAC(01,0A)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0A)</param>
        private void ProcessRateChange(DataPacket dp)
        {
        }  

        /// <summary>
        /// Processes a pause request from the server -- SNAC(01,0B)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0B)</param>
        private void ProcessPauseRequest(DataPacket dp)
        {
            // TODO:  Actually tell the connection to stop sending in preparation for the migration
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_PAUSE_CONNECTION_RESPONSE;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            DataPacket dp2 = Marshal.BuildDataPacket(parent, sh, new ByteStream());
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);
        }

        /// <summary>
        /// Processes a resume request from the server -- SNAC(01,0D)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0D)</param>
        private void ProcessResumeRequest(DataPacket dp)
        {
            // TODO: Tell the connection it can start sending again
        }

        /// <summary>
        /// Processes a server migration notification -- SNAC(01,12)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,12)</param>
        private void ProcessMigrationNotice(DataPacket dp)
        {
            // Process migration
            dp.ParentSession.OnError(ServerErrorCode.OscarLibUnsupportedFunction);
        }

        /// <summary>
        /// Processes the client's own extended status update sent by the server -- SNAC(01,21)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,21)</param>
        private void ProcessExtendedStatus(DataPacket dp)
        {
            // Messages are starting with a 0x0004...what the heck?
            dp.Data.ReadUshort();
            ushort type = dp.Data.ReadUshort();
            byte flags = dp.Data.ReadByte();
            byte length = dp.Data.ReadByte();

            if (type == EXTSTATUS_ICON1 || type == EXTSTATUS_ICON2)
            {
                // TODO:  Some icon bullcrap, I don't know
            }
            else // Available message
            {
            }
        }

        /// <summary>
        /// Processes a request for client verification -- SNAC(01,1F)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,1F)</param>
        /// <remarks>See the entry for <see cref="SendVerificationResponse"/> for a description of
        /// how this SNAC is used.</remarks>
        private void ProcessVerificationRequest(DataPacket dp)
        {
            SendVerificationResponse();
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Sets the client's extended status -- SNAC(01,1E)
        /// </summary>
        /// <param name="availablemessage">The available message to set</param>
        /// <param name="flags">The ICQ flags to set</param>
        /// <param name="status">The ICQ status to set</param>
        /// <remarks>Either the available message or the flags/status can be set in one call to SetExtendedStatus</remarks>
        private void SetExtendedStatus(string availablemessage, ICQFlags flags, ICQStatus status)
        {
            ByteStream stream = new ByteStream();
            if (availablemessage != null)
            {
                stream.WriteUshort(0x001D);
                stream.WriteUshort((ushort)(availablemessage.Length + 8));
                stream.WriteUshort(0x0002);
                stream.WriteByte(0x04);
                stream.WriteByte((byte)(Encoding.ASCII.GetByteCount(availablemessage) + 4));
                stream.WriteUshort((ushort)Encoding.ASCII.GetByteCount(availablemessage));
                stream.WriteString(availablemessage, Encoding.ASCII);
                stream.WriteUshort(0x0000);
            }
            else
            {
                uint stat = (uint)((ushort)flags << 16);
                stat |= (ushort)status;
                stream.WriteUint(0x00060004);
                stream.WriteUint(stat);
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BOS_FAMILY;
            sh.FamilySubtypeID = BOS_SET_EXTENDED_STATUS;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }
        #endregion
    }
}
