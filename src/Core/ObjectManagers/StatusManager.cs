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
    /// Handles online and offline notifications, away messages, available messages, and profiles
    /// for the local and remote users.
    /// </summary>
    /// <remarks>This manager is responsible for processing SNACs from families 0x0002
    /// (Location) and 0x0003 (Buddy Management).</remarks>
    public class StatusManager : ISnacFamilyHandler
    {
        #region SNAC constants
        /// <summary>
        /// The SNAC family that handles location information
        /// </summary>
        private const int SNAC_LOCATION_FAMILY = 0x0002;
        /// <summary>
        /// The subtype for requesting location parameter information
        /// </summary>
        private const int LOCATION_PARAMETER_REQUEST = 0x0002;
        /// <summary>
        /// The subtype for location parameter information
        /// </summary>
        private const int LOCATION_PARAMETER_LIST = 0x0003;
        /// <summary>
        /// The subtype for setting user information
        /// </summary>
        private const int LOCATION_PARAMETER_USERINFO = 0x0004;
        /// <summary>
        /// The subtype for user information notification
        /// </summary>
        private const int LOCATION_USER_INFO = 0x0006;
        /// <summary>
        /// The SNAC family that handles buddy state information
        /// </summary>
        private const int SNAC_BUDDY_FAMILY = 0x0003;
        /// <summary>
        /// The subtype for requesting buddy parameter information
        /// </summary>
        private const int BUDDY_PARAMETER_REQUEST = 0x0002;
        /// <summary>
        /// The subtype for buddy parameter information
        /// </summary>
        private const int BUDDY_PARAMETER_LIST = 0x0003;
        /// <summary>
        /// The subtype for an online notification
        /// </summary>
        private const int BUDDY_ONLINE_NOTIFICATION = 0x000B;
        /// <summary>
        /// The subtype for an offline notification
        /// </summary>
        private const int BUDDY_OFFLINE_NOTIFICATION = 0x000C;
        /// <summary>
        /// The maximum number of parameters that can be reported
        /// </summary>
        private const int PARAMETER_MAXCAPABILITIES = 0x0002;
        /// <summary>
        /// The maximum length of a user profile in bytes
        /// </summary>
        private const int PARAMETER_PROFILELENGTH = 0x0001;
        /// <summary>
        /// The subtype for sending an away message to the server
        /// </summary>
        private const int LOCATION_AWAYMESSAGE = 0x0004;
        /// <summary>
        /// The subtype for sending an away message's text encoding to the server
        /// </summary>
        private const int LOCATION_AWAYMESSAGE_ENCODING = 0x0003;
        /// <summary>
        /// The subtype for sending a set of capabilities to the server
        /// </summary>
        private const int LOCATION_CAPABILITIES = 0x0005;
        /// <summary>
        /// The subtype for sending a profile to the server
        /// </summary>
        private const int LOCATION_PROFILE = 0x0002;
        /// <summary>
        /// The subtype for sending a profile's text encoding to the server
        /// </summary>
        private const int LOCATION_PROFILE_ENCODING = 0x0001;
        /// <summary>
        /// The maximum number of buddies in an old-style contact list
        /// </summary>
        private const int PARAMETER_MAXBUDDIES = 0x0001;
        /// <summary>
        /// TODO:  What the hell is this?
        /// </summary>
        private const int PARAMETER_MAXNOTIFICATIONS = 0x0003;
        /// <summary>
        /// The maximum number of entries in a Watcher list
        /// </summary>
        private const int PARAMETER_MAXWATCHERS = 0x0002;
        #endregion

		private readonly ISession parent;
        private int maxBuddyListEntries;
        private int maxWatcherListEntries;
        private int maxNotifications;
        private int maxCapabilities;
        private int maxProfileLength;

        private string localProfile;

        /// <summary>
        /// Initializes a new StatusManager
        /// </summary>
		internal StatusManager(ISession parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_LOCATION_FAMILY);
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_BUDDY_FAMILY);
        }

        /// <summary>
        /// Gets or sets the client's profile
        /// </summary>
        /// <remarks><para>The <paramref name="profile"/> is assumed to be encoded as <see cref="Encoding.Unicode"/></para></remarks>
        public string Profile
        {
            get { return localProfile; }
            set
            {
                localProfile = value;
                SetProfile(localProfile);
            }
        }

        #region Public methods
        /// <summary>
        /// Sets the client's idle time.
        /// </summary>
        /// <param name="seconds">The client's idle time, in seconds</param>
        /// <remarks>
        /// <para>
        /// To remove idle status, set <paramref name="seconds"/> to zero.
        /// </para>
        /// <para>
        /// This method should be used only once to set the client as idle,
        /// or to remove idle status. The server updates the client's idle time
        /// automatically.  So, for example, if the client application wants the
        /// user to appear idle after ten minutes, the application should call
        /// this method after 10 minutes of being idle:
        /// <code>Session s;
        /// ...
        /// s.SetIdleTime(600);</code>
        /// The next time this method is called, it should be to remove idle status:
        /// <code>s.SetIdleTime(0);</code>
        /// </para>
        /// </remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
        public void SetIdleTime(uint seconds)
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = 0x0001;
            sh.FamilySubtypeID = 0x0011;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUint(seconds);

            // Always gets sent on BOS
            DataPacket dp = Marshal.BuildDataPacket(parent, sh, stream);
            dp.ParentConnection = parent.Connections.BOSConnection;
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Sets the client's profile
        /// </summary>
        /// <param name="profile">The profile to set</param>
        /// <remarks><para>The <paramref name="profile"/> is assumed to be encoded as <see cref="Encoding.Unicode"/></para></remarks>
        internal void SetProfile(string profile)
        {
            if (parent.LoggedIn ||
                (parent.Connections.BOSConnection != null && parent.Connections.BOSConnection.Connecting))
            {
                SetAwayMessageProfileInternal(null, profile);
            }
        }

        /// <summary>
        /// Sets the client's away message
        /// </summary>
        /// <param name="awayMessage">The away message to set</param>
        /// <remarks>
        /// <para>
        /// The session must be logged in for this function to succeed. Calling SetAwayMessage
		/// before a successful login is achieved (i.e., when the <see cref="ISession.LoginCompleted"/> event
        /// is raised) results in an exception.
        /// </para>
        /// <para>The <paramref name="awayMessage"/> is assumed to be encoded as <see cref="Encoding.Unicode"/></para>
        /// </remarks>
        /// <exception cref="NotLoggedInException">Thrown when the client is not logged in</exception>
        public void SetAwayMessage(string awayMessage)
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            SetAwayMessageProfileInternal(awayMessage, null);
        }

        /// <summary>
        /// Drops the client's away message
        /// </summary>
        /// <exception cref="NotLoggedInException">Thrown when the client is not logged in</exception>
        public void DropAwayMessage()
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            SetAwayMessageProfileInternal("", null);
        }

        /// <summary>
        /// Requests parameter settings for the Location and Buddy Management SNAC families
        /// </summary>
        /// <remarks>This method sends both SNAC(0x02,0x02) and SNAC(0x03,0x02)</remarks>
        internal void RequestParameters()
        {
            SNACHeader header1 = new SNACHeader();
            header1.FamilyServiceID = SNAC_LOCATION_FAMILY;
            header1.FamilySubtypeID = LOCATION_PARAMETER_REQUEST;
            header1.RequestID = Session.GetNextRequestID();
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, header1, new ByteStream()));

            SNACHeader header2 = new SNACHeader();
            header2.FamilyServiceID = SNAC_BUDDY_FAMILY;
            header2.FamilySubtypeID = BUDDY_PARAMETER_REQUEST;
            header2.RequestID = Session.GetNextRequestID();
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, header2, new ByteStream()));
        }

        /// <summary>
        /// Sends a list of the client's capabilities to the server
        /// </summary>
        /// <remarks>This method sends SNAC(0x02,0x04)</remarks>
        public void ReportClientCapabilities()
        {
            if (parent.ClientCapabilities != Capabilities.None)
            {
                SNACHeader sh = new SNACHeader();
                sh.FamilyServiceID = SNAC_LOCATION_FAMILY;
                sh.FamilySubtypeID = LOCATION_PARAMETER_USERINFO;
                sh.Flags = 0x0000;
                sh.RequestID = Session.GetNextRequestID();

                // Build the capabilities list, TLV type 0x0005
                byte[] caps = CapabilityProcessor.GetCapabilityArray(parent.ClientCapabilities);
                ByteStream stream = new ByteStream();
                stream.WriteUshort(LOCATION_CAPABILITIES);
                stream.WriteUshort((ushort)caps.Length);
                stream.WriteByteArray(caps);

                SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
            }
        }
        #endregion

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/>
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(DataPacket dp)
        {
            if (dp.SNAC.FamilyServiceID == SNAC_BUDDY_FAMILY)
            {
                switch (dp.SNAC.FamilySubtypeID)
                {
                    case BUDDY_PARAMETER_LIST:
                        ProcessBuddyParameterList(dp);
                        break;
                    case BUDDY_ONLINE_NOTIFICATION:
                        ProcessOnlineNotification(dp);
                        break;
                    case BUDDY_OFFLINE_NOTIFICATION:
                        ProcessOfflineNotification(dp);
                        break;
                }
            }
            else if (dp.SNAC.FamilyServiceID == SNAC_LOCATION_FAMILY)
            {
                switch (dp.SNAC.FamilySubtypeID)
                {
                    case LOCATION_PARAMETER_LIST:
                        ProcessLocationParameterList(dp);
                        break;
                    case LOCATION_USER_INFO:
                        ProcessUserInfo(dp);
                        break;
                }
            }
        }

        #endregion

        /// <summary>
        /// Occurs when a user status notification is received
        /// </summary>
        /// <remarks>The first receipt of this event for a remote contact
        /// indicates that the user has come online.  Subsequent receipts will
        /// indicate that the user has changed idle or away states, or updated their profile.</remarks>
        public event UserStatusReceivedHandler UserStatusReceived;

        /// <summary>
        /// Occurs when a user on the client's contact list has gone offline
        /// </summary>
        public event UserOfflineHandler UserOffline;

        /// <summary>
        /// Occurs when a response to a request for user information is received
        /// </summary>
        public event UserInfoReceivedHandler UserInfoReceived;

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(02,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(02,03)</param>
        private void ProcessLocationParameterList(DataPacket dp)
        {
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                maxCapabilities = tlvs.ReadUshort(PARAMETER_MAXCAPABILITIES);
                maxProfileLength = tlvs.ReadUshort(PARAMETER_PROFILELENGTH);
            }

            ReportClientCapabilities();
            parent.ParameterSetArrived();
        }

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(03,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(03,03)</param>
        private void ProcessBuddyParameterList(DataPacket dp)
        {
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                maxBuddyListEntries = tlvs.ReadUshort(PARAMETER_MAXBUDDIES);
                maxWatcherListEntries = tlvs.ReadUshort(PARAMETER_MAXWATCHERS);
                maxNotifications = tlvs.ReadUshort(PARAMETER_MAXNOTIFICATIONS);
            }

            parent.ParameterSetArrived();
        }

        /// <summary>
        /// Processes a user status notification sent by the server -- SNAC(03,0B)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(03,0B)</param>
        private void ProcessOnlineNotification(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                UserInfo ui = dp.Data.ReadUserInfo();
                if (ui != null && UserStatusReceived != null)
                {
                    UserStatusReceived(this, ui);
                }
            }
        }

        /// <summary>
        /// Processes a user status notification sent by the server -- SNAC(03,0C)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(03,0C)</param>
        private void ProcessOfflineNotification(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                UserInfo ui = dp.Data.ReadUserInfo();
                if (ui != null && UserOffline != null)
                {
                    UserOffline(this, ui);
                }
            }
        }

        /// <summary>
        /// Processes user information sent by the server -- SNAC(02,06)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(02,06)</param>
        private void ProcessUserInfo(DataPacket dp)
        {
            if (dp.SNAC.Flags != 0)
            {
                Logging.DumpFLAP(dp.Data.GetBytes(), "You've got to be shitting me");
            }

            // Apparently, the userinfo block will always be first,
            // and then possibly TLVs 0x0001 - 0x0005, depending on the request
            byte[] awaymessage = null;
            Encoding awaymessageencoding = Encoding.ASCII;
            byte[] profile = null;
            Encoding profileencoding = Encoding.ASCII;
            Capabilities caps = Capabilities.None;

            UserInfo ui = dp.Data.ReadUserInfo();
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                profileencoding = Marshal.AolMimeToEncoding(tlvs.ReadString(LOCATION_PROFILE_ENCODING, Encoding.ASCII));
                profile = tlvs.ReadByteArray(LOCATION_PROFILE);
                awaymessageencoding =
                    Marshal.AolMimeToEncoding(tlvs.ReadString(LOCATION_AWAYMESSAGE_ENCODING, Encoding.ASCII));
                awaymessage = tlvs.ReadByteArray(LOCATION_AWAYMESSAGE);
                caps = CapabilityProcessor.ProcessCLSIDList(tlvs.ReadByteArray(LOCATION_CAPABILITIES));
            }

            UserInfoResponse uir = new UserInfoResponse();
            uir.Info = ui;
            if (profile != null)
            {
                uir.Profile = profileencoding.GetString(profile, 0, profile.Length);
                uir.ProfileEncoding = profileencoding;
            }
            if (awaymessage != null)
            {
                uir.AwayMessage = awaymessageencoding.GetString(awaymessage, 0, awaymessage.Length);
                uir.AwayMessageEncoding = awaymessageencoding;
            }
            uir.ClientCapabilities = caps;

            if (UserInfoReceived != null)
            {
                UserInfoReceived(this, uir);
            }
        }

        /// <summary>
        /// Sets the away message and/or profile of the client
        /// </summary>
        /// <param name="awayMessage">The away message to set</param>
        /// <param name="profile">The profile to set</param>
        private void SetAwayMessageProfileInternal(string awayMessage, string profile)
        {
            // Build the SNAC header
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_LOCATION_FAMILY;
            sh.FamilySubtypeID = LOCATION_PARAMETER_USERINFO;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                if (profile != null)
                {
                    Encoding profileEncoding = UtilityMethods.FindBestOscarEncoding(profile);
                    tlvs.WriteString(LOCATION_PROFILE_ENCODING, Marshal.EncodingToAolMime(profileEncoding), Encoding.ASCII);
                    tlvs.WriteString(LOCATION_PROFILE, profile, profileEncoding);
                }
                if (awayMessage != null)
                {
                    Encoding awayMessageEncoding = UtilityMethods.FindBestOscarEncoding(awayMessage);
                    tlvs.WriteString(LOCATION_AWAYMESSAGE_ENCODING, Marshal.EncodingToAolMime(awayMessageEncoding),
                                     Encoding.ASCII);
                    tlvs.WriteString(LOCATION_AWAYMESSAGE, awayMessage, awayMessageEncoding);
                }
                stream.WriteByteArray(tlvs.GetBytes());
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }
    }
}
