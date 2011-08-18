/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles screenname authorization functionality
    /// </summary>
    /// <remarks>This manager is responsible for processing SNACs from family 0x0017 (authorization).</remarks>
    sealed class AuthorizationManager : ISnacFamilyHandler
    {
        #region SNAC constants
        /// <summary>
        /// The SNAC family responsible for session authorization
        /// </summary>
        private const int SNAC_AUTH_FAMILY = 0x0017;
        /// <summary>
        /// The SNAC subtype for performing the login request
        /// </summary>
        private const int AUTH_LOGIN_REQUEST = 0x0002;
        /// <summary>
        /// The SNAC subtype containing a login response
        /// </summary>
        private const int AUTH_LOGIN_RESPONSE = 0x0003;
        /// <summary>
        /// The SNAC subtype for requesting an MD5 authorization key
        /// </summary>
        private const int AUTH_KEY_REQUEST = 0x0006;
        /// <summary>
        /// The SNAC subtype containing an MD5 authorization key
        /// </summary>
        private const int AUTH_KEY_RESPONSE = 0x0007;
        #endregion SNAC constants

        private readonly Session parent;

        /// <summary>
        /// Initializes a new AuthorizationManager
        /// </summary>
        internal AuthorizationManager(Session parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_AUTH_FAMILY);
        }

        #region Public methods
        /// <summary>
        /// Initiates the connection to the AIM/ICQ service
        /// </summary>
        public void LoginToService(string server, int port, bool ssl)
        {
            Connection loginConnection = parent.Connections.CreateNewConnection(SNAC_AUTH_FAMILY);
            loginConnection.ServerConnectionCompleted += new ServerConnectionCompletedHandler(loginConnection_ServerConnectionCompleted);
            loginConnection.Server = server;
            loginConnection.Port = port;
            loginConnection.Ssl = ssl;
            loginConnection.Cookie = null;
            loginConnection.ConnectToServer();
            parent.OnLoginStatusUpdate("Connecting to server", 0.00);
        }
        #endregion

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 0x0017
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(csammisrun.OscarLib.Utility.DataPacket dp)
        {
            switch (dp.SNAC.FamilySubtypeID)
            {
                case AUTH_KEY_RESPONSE:
                    SendAuthorizationRequest(dp);
                    break;
                case AUTH_LOGIN_RESPONSE:
                    ProcessLoginResponse(dp);
                    break;
            }
        }

        #endregion

        #region Processing methods
        /// <summary>
        /// Sends authorization request -- SNAC(17,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(17,07)</param>
        void SendAuthorizationRequest(DataPacket dp)
        {
            // Pull apart SNAC(17,07)
            byte[] key = dp.Data.ReadByteArray(dp.Data.ReadUshort());

            // Construct SNAC(17,02)
            SNACHeader header = new SNACHeader();
            header.FamilyServiceID = SNAC_AUTH_FAMILY;
            header.FamilySubtypeID = AUTH_LOGIN_REQUEST;

            OSCARIdentification id = parent.ClientIdentification;

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x0001, parent.ScreenName, Encoding.ASCII);
                tlvs.WriteString(0x0003, id.ClientName, Encoding.ASCII);
                tlvs.WriteString(0x000F, "en", Encoding.ASCII);
                tlvs.WriteString(0x000E, "us", Encoding.ASCII);
                tlvs.WriteUint(0x0014, id.ClientDistribution);
                tlvs.WriteUshort(0x0016, id.ClientId);
                tlvs.WriteUshort(0x0017, id.ClientMajor);
                tlvs.WriteUshort(0x0018, id.ClientMinor);
                tlvs.WriteUshort(0x0019, id.ClientLesser);
                tlvs.WriteUshort(0x001A, id.ClientBuild);
                tlvs.WriteByteArray(0x0025, parent.HashPassword(key));
                tlvs.WriteByte(0x004A, 0x01);
                tlvs.WriteEmpty(0x004C);
                stream.WriteTlvBlock(tlvs);
            }

            DataPacket newPacket = Marshal.BuildDataPacket(parent, header, stream);
            newPacket.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(newPacket);
        }

        /// <summary>
        /// Processes a login response -- SNAC(17,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(17,03)</param>
        internal void ProcessLoginResponse(DataPacket dp)
        {
            // Pull apart SNAC(17,03)
            Cookie cookie;
            string BOSaddress;

            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                if (tlvs.HasTlv(0x0008))
                {
                    ushort errorcode = tlvs.ReadUshort(0x0008);
                    parent.OnLoginFailed((LoginErrorCode)errorcode);
                    return;
                }

                BOSaddress = tlvs.ReadString(0x0005, Encoding.ASCII);
                cookie = Cookie.GetReceivedCookie(tlvs.ReadByteArray(0x0006));
            }

            // Shut down the authorization connection
            // Socket shutdown is initiated by the server
            parent.OnLoginStatusUpdate("Authorized", 0.33);
            dp.ParentConnection.DisconnectFromServer(false);

            // Create a new connection to the BOS server
            Connection newconn = parent.Connections.CreateNewConnection(0x0001);

            string[] bosinfo = BOSaddress.Split(':');

            newconn.ServerConnectionCompleted += delegate(Connection conn) { conn.ReadyForData = true; conn.ReadHeader(); };
            newconn.Server = bosinfo[0];
            if (bosinfo.Length == 2)
                newconn.Port = Int32.Parse(bosinfo[1]);
            else
                newconn.Port = dp.ParentConnection.Port;

            Logging.WriteString("Connect to Server after auth. Address from dp: {0} - Address to connect: {1}:{2}", BOSaddress, newconn.Server, newconn.Port);

            newconn.Cookie = cookie;
            newconn.ConnectToServer();

            // The login process continues when the server sends SNAC(01,03) on the new connection
        }

        /// <summary>
        /// Kicks off the authorization hash request when the initial connection is complete
        /// </summary>
        void loginConnection_ServerConnectionCompleted(Connection conn)
        {
            conn.ReadyForData = true;

            /*** Send SNAC(17,06) to get the auth key ***/
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_AUTH_FAMILY;
            sh.FamilySubtypeID = AUTH_KEY_REQUEST;

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x0001, parent.ScreenName, Encoding.ASCII);
                tlvs.WriteEmpty(0x004B);
                tlvs.WriteEmpty(0x005A);
                stream.WriteTlvBlock(tlvs);
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));

            conn.ReadHeader();
        }

        #endregion Processing methods
    }
}
