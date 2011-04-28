using System;
using System.Collections;
using System.Text;

// string encoding complete

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0001 -- generic service controls
    /// </summary>
    internal static class SNAC01
    {
        private const int EXTSTATUS_AVAILMESSAGE = 0x0002;
        private const int EXTSTATUS_ICON1 = 0x0000;
        private const int EXTSTATUS_ICON2 = 0x0001;

        private const int ICONFLAGS_REQUESTICON = 0x81;
        private const int ICONFLAGS_UPLOADICON = 0x41;
        private const int ICONFLAGS_UPLOADSUCCESS = 0x01;
        private const int MOTD_MESSAGE = 0x000B;
        private const int MOTD_UNKNOWN_1 = 0x0002;
        private const int MOTD_UNKNOWN_2 = 0x0003;
        private const int SERVICE_RESPONSE_ADDRESS = 0x0005;
        private const int SERVICE_RESPONSE_COOKIE = 0x0006;
        private const int SERVICE_RESPONSE_FAMILY = 0x000D;
        private const int SERVICE_RESPONSE_VERSION = 0x0001;

        /// <summary>
        /// Notifies the BOS server that the client is ready to go online -- SNAC(01,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        /// <remarks>
        /// The client sends this SNAC to notify the server that it is ready to receive messages
        /// and online notifications.
        /// <para>
        /// This SNAC must be sent within 30 seconds of connection, or the connection
        /// will drop. This should not ever be a problem, barring a lost connection well below
        /// the level of this library.
        /// </para>
        /// </remarks>
        public static void SendReadyNotification(DataPacket dp)
        {
            // Build SNAC(01,02)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.ClientOnline;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ConnectionManager cm = dp.ParentSession.Connections;
            ushort[] families = cm.GetFamilies(dp.ParentConnection);
            FamilyManager fm = dp.ParentSession.Families;

            Array.Sort(families);
            Array.Reverse(families);

            bool isChatRoomConnection = false;
            bool isChatNavService = false;

            ByteStream stream = new ByteStream();
            DataPacket[][] delayedframes = new DataPacket[families.Length][];

            for (int i = 0; i < families.Length; i++)
            {
                ushort family = families[i];
                delayedframes[i] = dp.ParentSession.Connections.GetDelayedPackets(family);

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
            Session sess = dp.ParentSession;
            if (!sess.LoggedIn)
            {
                SetExtendedStatus(sess, sess.AvailableMessage, ICQFlags.Normal, ICQStatus.Online);
                SetExtendedStatus(sess, null, ICQFlags.Normal, ICQStatus.Online);
            }

            // The connection is done, so start sending keepalives
            dp.ParentConnection.Connecting = false;
            dp.ParentConnection.ReadyForData = true;

            // Build and send a new DataPacket instead of re-using the originator;
            // SNAC(01,02) was getting sent twice on child connections with the reuse
            DataPacket dp2 = Marshal.BuildDataPacket(dp.ParentSession, sh, stream);
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);

            /*
			 * If this is a new service connection, there is probably at least one
			 * delayed packet. Process those packets. Additionally, if the new connection is
             * a chatroom connection, query the Rendezvous manager to see if it is the result
             * of an explict room creation (or implict, as in accepting a chat invitation).
             * If it was explict, notify the user that it's done
			 */
            if (sess.LoggedIn)
            {
                foreach (DataPacket[] list in delayedframes)
                {
                    if (list == null) continue;
                    foreach (DataPacket dp_delay in list)
                    {
                        dp_delay.ParentConnection = dp.ParentConnection;
                        SNACFunctions.DispatchToRateClass(dp_delay);
                    }
                }

                if (isChatNavService)
                {
                    sess.ChatRooms.OnChatNavServiceAvailable();
                }
                else if (isChatRoomConnection)
                {
                    ChatRoom chatRoom = sess.Connections.GetChatByConnection((ChatConnection)dp.ParentConnection);
                    sess.ChatRooms.OnChatRoomConnectionAvailable(chatRoom);
                }
            }
        }

        /// <summary>
        /// Processes the server-supported family list -- SNAC(01,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,03)</param>
        public static void ProcessServicesList(DataPacket dp)
        {
            Session sess = dp.ParentSession;

            // Get the list of supported families and populate the session list
            while (dp.Data.HasMoreData)
            {
                ushort family = dp.Data.ReadUshort();
                // Add family to connection list, unless it is 0x0001, because all
                // connections (except authorization) handle this family
                if (family != (ushort)SNACFamily.BasicOscarService)
                {
                    sess.Connections.AssignFamily(family, dp.ParentConnection);
                }
            }

            // Request the server versions of these services
            RequestServiceVersions(dp);
        }

        /// <summary>
        /// Sends a request for a new service family -- SNAC(01,04)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="newfamily">The new family to request</param>
        public static void RequestNewService(Session sess, ushort newfamily)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.NewServiceRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(newfamily);

            // This SNAC expects a response in SNAC(01,05)
            sess.StoreRequestID(sh.RequestID, null);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            // This one is always sent on BOS
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes the server response to a new family request -- SNAC(01,05)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,05)</param>
        public static void ProcessNewServiceResponse(DataPacket dp)
        {
            int startIndex = 0;
            byte[] SNACData = dp.Data.ReadByteArrayToEnd();
            if (SNACData[0] == 0x00 && SNACData[1] == 0x06)
            {
                startIndex += 2; // What the heck is this...0x0006, some families, some of the time?
            }

            using (TlvBlock tlvs = new TlvBlock(SNACData, startIndex))
            {
                ushort family = tlvs.ReadUshort(SERVICE_RESPONSE_FAMILY);
                string BOSaddress = tlvs.ReadString(SERVICE_RESPONSE_ADDRESS, Encoding.ASCII);
                byte[] cookie = tlvs.ReadByteArray(SERVICE_RESPONSE_COOKIE);

                Connection newconn = null;
                object store = dp.ParentSession.RetrieveRequestID(dp.SNAC.RequestID);

                if (family != 0x000E)
                {
                    newconn = dp.ParentSession.Connections.CreateNewConnection(family);
                }
                else
                {
                    ChatRoom roominfo = (ChatRoom)store;
                    dp.ParentSession.Connections.CreateNewChatConnection(roominfo);
                    newconn = roominfo.Connection;
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

                newconn.ServerConnectionCompleted +=
                    new ServerConnectionCompletedHandler(newconn_ServerConnnectionCompleted);
                newconn.Server = bosinfo[0];
                newconn.Port = port;
                newconn.Cookie = new Cookie(cookie);
                newconn.ConnectToServer();
            }

            // The connection process continues when the server sends SNAC(01,03)
        }

        private static void newconn_ServerConnnectionCompleted(Connection conn)
        {
            conn.ReadHeader();
        }

        /// <summary>
        /// Sends a request for rate information -- SNAC(01,06)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        public static void RequestRateInformation(DataPacket dp)
        {
            Session sess = dp.ParentSession;

            // Construct SNAC (01,06)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (byte)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (byte)GenericServiceControls.RateLimitRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            DataPacket dp2 = Marshal.BuildDataPacket(sess, sh, new ByteStream());
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);
        }

        /// <summary>
        /// Processes the list of rate limitations -- SNAC(01,07)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(01,07)</param>
        public static void ProcessRateLimitations(DataPacket dp)
        {
            Session sess = dp.ParentSession;

            ushort num_classes = dp.Data.ReadUshort();
            Hashtable rcs = new Hashtable();
            ushort[] classes = new ushort[num_classes];

            RateClassManager rcm = sess.RateClasses;

            // Get the rate class attributes out of the SNAC
            for (int i = 0; i < num_classes; i++)
            {
                ushort id = dp.Data.ReadUshort();
                RateClass rc = sess.RateClasses.GetByID(id);
                rc.WindowSize = dp.Data.ReadUint();
                rc.ClearLevel = dp.Data.ReadUint();
                rc.AlertLevel = dp.Data.ReadUint();
                rc.LimitLevel = dp.Data.ReadUint();
                rc.DisconnectLevel = dp.Data.ReadUint();
                rc.CurrentLevel = dp.Data.ReadUint();
                rc.MaxLevel = dp.Data.ReadUint();
                if (dp.ParentSession.Families.GetFamilyVersion(0x0001) >= 3)
                {
                    rc.LastTime = dp.Data.ReadUint();
                    rc.CurrentState = dp.Data.ReadByte();
                }
                sess.RateClasses.SetByID(id, rc);
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

            AcknowledgeRateLimitations(dp, classes);
        }

        /// <summary>
        /// Sends an acknowledgement of the server's rate limitations -- SNAC(01,08)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        /// <param name="classes">The known rate classes to acknowledge</param>
        public static void AcknowledgeRateLimitations(DataPacket dp, ushort[] classes)
        {
            Session sess = dp.ParentSession;

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.AcknowledgeRateLimits;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            for (int i = 0; i < classes.Length; i++)
            {
                stream.WriteUshort(classes[i]);
            }

            DataPacket dp2 = Marshal.BuildDataPacket(sess, sh, stream);
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);

            sess.OnLoginStatusUpdate("Protocol negotiation complete", 0.66);

            /*
			 * If this is the initial services connection, we call the remaining SNACs
			 * in the login sequence.
			 * Otherwise, this is the last step in setting up a new service connection,
			 * and we send SNAC(01,02) here.
			 */
            if (!sess.LoggedIn)
            {
                // Start stage 3, services setup
                RequestOwnInformation(sess);
                SNAC13.RequestParametersList(sess);
                SNAC13.RequestInitialContactList(sess);
                sess.Statuses.RequestParameters();
                sess.Messages.RequestParametersList();
                SNAC09.RequestParametersList(sess);
                sess.Statuses.ReportClientCapabilities();
                sess.Statuses.SetProfile(sess.Statuses.Profile);
            }
            else
            {
                SendReadyNotification(dp);
            }
        }

        /// <summary>
        /// Processes a rate deletion message from the server -- SNAC(01,09)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,09)</param>
        public static void ProcessRateDeletion(DataPacket dp)
        {
        }

        /// <summary>
        /// Process a rate change message from the server -- SNAC(01,0A)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0A)</param>
        public static void ProcessRateChange(DataPacket dp)
        {
        }

        /// <summary>
        /// Processes a pause request from the server -- SNAC(01,0B)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0B)</param>
        public static void ProcessPauseRequest(DataPacket dp)
        {
            // I'm not going to support a bifurcated split, because it really screws with the
            // model I've got right now.

            SendPauseAcknowledgement(dp);
        }

        /// <summary>
        /// Sends an acknowledgement to the pause command -- SNAC(01,0C)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object to be paused</param>
        public static void SendPauseAcknowledgement(DataPacket dp)
        {
            Session sess = dp.ParentSession;

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.PauseResponse;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            ushort[] families = sess.Connections.GetFamilies(dp.ParentConnection);
            for (int i = 0; i < families.Length; i++)
            {
                stream.WriteUshort(families[i]);
            }

            DataPacket dp2 = Marshal.BuildDataPacket(sess, sh, stream);
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);
        }

        /// <summary>
        /// Processes a resume request from the server -- SNAC(01,0D)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0D)</param>
        public static void ProcessResumeRequest(DataPacket dp)
        {
            // Tell the connection it can start sending again
        }

        /// <summary>
        /// Requests the client's own online information -- SNAC(01,0E)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        public static void RequestOwnInformation(Session sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.OwnInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            // This SNAC expects a response in SNAC(01,0F)
            sess.StoreRequestID(sh.RequestID, null);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, new ByteStream());
            // Always gets sent on BOS
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes the client's own online information -- SNAC(01,0F)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,0F)</param>
        public static void ProcessOwnInformation(DataPacket dp)
        {
            UserInfo ui = dp.Data.ReadUserInfo();
            if (ui.Icon != null)
            {
                dp.ParentSession.Graphics.OwnBuddyIcon = ui.Icon;
            }
            dp.ParentSession.RetrieveRequestID(dp.SNAC.RequestID);
        }

        /// <summary>
        /// Processes a warning notification from the server -- SNAC(01,10)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,10)</param>
        public static void ProcessWarningMessage(DataPacket dp)
        {
            bool anonymous = false;
            UserInfo ui;

            ushort newwarning = dp.Data.ReadUshort();
            if (dp.Data.HasMoreData)
            {
                ui = dp.Data.ReadUserInfo();
                anonymous = false;
            }
            else
            {
                ui = new UserInfo();
                anonymous = true;
            }

            // Alert the client that it has been warned
            dp.ParentSession.OnWarningReceived(newwarning, anonymous, ui);
        }

        /// <summary>
        /// Sets the client's idle time -- SNAC(01,11)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="seconds">The client's idle time, in seconds</param>
        /// <remarks>
        /// <para>
        /// To remove idle status, set <paramref name="seconds"/> to zero.
        /// </para>
        /// <para>
        /// This SNAC should be sent only once to set the client as idle,
        /// or to remove idle status. The server updates the client's idle time
        /// automatically, there is no need to send multiple idle time messages.
        /// </para>
        /// </remarks>
        public static void SetIdleTime(Session sess, uint seconds)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.SetIdleTime;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUint(seconds);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            // Always gets sent on BOS
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes a server migration notification -- SNAC(01,12)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,12)</param>
        public static void ProcessMigrationNotice(DataPacket dp)
        {
            // Process migration
            dp.ParentSession.OnError(ServerErrorCode.OscarLibUnsupportedFunction);
        }

        /// <summary>
        /// Processes the Message Of The Day -- SNAC(01,13)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,13)</param>
        public static void ProcessMOTD(DataPacket dp)
        {
            ushort motdtype = dp.Data.ReadUshort();
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                string messagestring = tlvs.ReadString(MOTD_MESSAGE, Encoding.ASCII);

                // Signal the client that the MOTD has come through
                dp.ParentSession.OnMessageOfTheDayReceived(motdtype, messagestring);
            }
        }

        /// <summary>
        /// Sets the client's privacy settings -- SNAC(01,14)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="settings">The privacy flags to send to the server</param>
        public static void SetPrivacySettings(Session sess, uint settings)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.SetPrivacyFlags;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUint(settings);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            // Always gets sent on BOS
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// (Obsolete) Sends a keepalive no-op -- SNAC(01,16)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <remarks>
        /// This SNAC is not used anymore. Channel 5 FLAPs should be used
        /// for keepalive, and are sent automatically by the <see cref="Connection"/>
        /// objects.
        /// </remarks>
        public static void SendNoOp(Session sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.NoOp;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Sends service versions request -- SNAC(01,17)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        public static void RequestServiceVersions(DataPacket dp)
        {
            Session sess = dp.ParentSession;
            // Construct SNAC (01,17)
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.ServerServicesVersionRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            // Pack the family IDs and version numbers into a stream
            ushort[] families = sess.Connections.GetFamilies(dp.ParentConnection);

            Array.Sort(families);
            Array.Reverse(families);

            ByteStream stream = new ByteStream();
            foreach (ushort id in families)
            {
                ushort version = sess.Families.GetFamilyVersion(id);
                stream.WriteUshort(id);
                stream.WriteUshort(version);
            }

            DataPacket dp2 = Marshal.BuildDataPacket(sess, sh, stream);
            dp2.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(dp2);
        }

        /// <summary>
        /// Processes the server's service version list -- SNAC(01,18)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,18)</param>
        public static void ProcessVersionsList(DataPacket dp)
        {
            // This is pretty pointless, so we just move on
            RequestRateInformation(dp);
        }

        /// <summary>
        /// Sets the client's "Available" message -- SNAC(01,1E)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="availablemessage">The available message to set</param>
        public static void SetAvailableMessage(Session sess, string availablemessage)
        {
            SetExtendedStatus(sess, availablemessage, ICQFlags.Normal, ICQStatus.Online);
        }

        /// <summary>
        /// Sets the client's ICQ status -- SNAC(01,1E)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="flags">The ICQ flags to set</param>
        /// <param name="status">The ICQ status flags to set</param>
        public static void SetExtendedICQStatus(Session sess, ICQFlags flags, ICQStatus status)
        {
            SetExtendedStatus(sess, null, flags, status);
        }

        /// <summary>
        /// Sets the client's extended status -- SNAC(01,1E)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="availablemessage">The available message to set</param>
        /// <param name="flags">The ICQ flags to set</param>
        /// <param name="status">The ICQ status to set</param>
        /// <remarks>Either the available message or the flags/status can be set in one call to SetExtendedStatus</remarks>
        private static void SetExtendedStatus(Session sess, string availablemessage, ICQFlags flags, ICQStatus status)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.SetExtendedStatus;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

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

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes a request for client verification -- SNAC(01,1F)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,1F)</param>
        /// <remarks>See the entry for <see cref="SendVerificationResponse"/> for a description of
        /// how this SNAC is used.</remarks>
        public static void ProcessVerificationRequest(DataPacket dp)
        {
            SendVerificationResponse(dp.ParentSession);
        }

        /// <summary>
        /// Sends a response to a client verification request -- SNAC(01,20)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <remarks>
        /// SNAC(01,1F) and SNAC(01,20) are used to verify that the client connecting
        /// to the network was the official AOL Instant Messenger.  The server sends
        /// two integers, an offset and a length, in SNAC(01,1F).  These parameters are then used to
        /// get 16 bytes of data in the offical client's static memory region. However, after
        /// AOL begain using this authentication method, the servers never changed the requested
        /// offset and length. The expected response was the same 16 bytes every time, which made
        /// it a fairly ineffective deterrent to "unauthorized" clients. This SNAC pair not been
        /// reported as being used in years.
        /// </remarks>
        public static void SendVerificationResponse(Session sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.BasicOscarService;
            sh.FamilySubtypeID = (ushort)GenericServiceControls.ClientVertificationResponse;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUint(0x44a95d26);
            stream.WriteUint(0xd2490423);
            stream.WriteUint(0x93b8821f);
            stream.WriteUint(0x51c54b01);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            dp.ParentConnection = sess.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes the client's own extended status update sent by the server -- SNAC(01,21)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(01,21)</param>
        public static void ProcessExtendedStatus(DataPacket dp)
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
    }
}