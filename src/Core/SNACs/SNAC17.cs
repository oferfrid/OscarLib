using System;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0017 -- authentication and registration
    /// </summary>
    internal static class SNAC17
    {
        /// <summary>
        /// Sends authorization request -- SNAC(17,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(17,07)</param>
        public static void SendAuthorizationRequest(DataPacket dp)
        {
            // Pull apart SNAC(17,07)
            byte[] key = dp.Data.ReadByteArray(dp.Data.ReadUshort());

            // Construct SNAC(17,02)
            SNACHeader header = new SNACHeader();
            header.FamilyServiceID = (ushort) SNACFamily.AuthorizationRegistrationService;
            header.FamilySubtypeID = (ushort) AuthorizationRegistrationService.LoginRequest;
            header.Flags = 0;
            header.RequestID = Session.GetNextRequestID();

            OSCARIdentification id = dp.ParentSession.ClientIdentification;

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x0001, dp.ParentSession.ScreenName, Encoding.ASCII);
                tlvs.WriteString(0x0003, id.ClientName, Encoding.ASCII);
                tlvs.WriteString(0x000F, "en", Encoding.ASCII);
                tlvs.WriteString(0x000E, "us", Encoding.ASCII);
                tlvs.WriteUint(0x0014, id.ClientDistribution);
                tlvs.WriteUshort(0x0016, id.ClientId);
                tlvs.WriteUshort(0x0017, id.ClientMajor);
                tlvs.WriteUshort(0x0018, id.ClientMinor);
                tlvs.WriteUshort(0x0019, id.ClientLesser);
                tlvs.WriteUshort(0x001A, id.ClientBuild);
                tlvs.WriteByteArray(0x0025, dp.ParentSession.HashPassword(key));
                tlvs.WriteByte(0x004A, 0x01);
                tlvs.WriteEmpty(0x004C);
                stream.WriteTlvBlock(tlvs);
            }

            DataPacket newPacket = Marshal.BuildDataPacket(dp.ParentSession, header, stream);
            newPacket.ParentConnection = dp.ParentConnection;
            SNACFunctions.BuildFLAP(newPacket);
        }

        /// <summary>
        /// Processes a login response -- SNAC(17,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(17,03)</param>
        public static void ProcessLoginResponse(DataPacket dp)
        {
            // Pull apart SNAC(17,03)
            Cookie cookie;
            string BOSaddress;

            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                if (tlvs.HasTlv(0x0008))
                {
                    ushort errorcode = tlvs.ReadUshort(0x0008);
                    dp.ParentSession.OnLoginFailed((LoginErrorCode)errorcode);
                    return;
                }

                BOSaddress = tlvs.ReadString(0x0005, Encoding.ASCII);
                cookie = Cookie.GetReceivedCookie(tlvs.ReadByteArray(0x0006));
            }

            // Shut down the authorization connection
            // Socket shutdown is initiated by the server
            dp.ParentSession.OnLoginStatusUpdate("Authorized", 0.33);
            dp.ParentConnection.DisconnectFromServer(false);

            // Create a new connection to the BOS server
            Connection newconn = dp.ParentSession.Connections.CreateNewConnection(0x0001);

            string[] bosinfo = BOSaddress.Split(':');

            newconn.ServerConnectionCompleted +=
                new ServerConnectionCompletedHandler(newconn_ServerConnnectionCompleted);
            newconn.Server = bosinfo[0];
            newconn.Port = Int32.Parse(bosinfo[1]);
            newconn.Cookie = cookie;
            newconn.ConnectToServer();

            // The connection process continues when the server sends SNAC(01,03)
        }

        // This is only attached to the BOS connection, which needs to be told it's ready for data
        // prior to receiving SNAC(01,02)
        private static void newconn_ServerConnnectionCompleted(Connection conn)
        {
            conn.ReadyForData = true;
            conn.ReadHeader();
        }

        /// <summary>
        /// Send MD5 key request -- SNAC(17,06)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void SendMD5Request(ISession sess)
        {
            /*** Send SNAC(17,06) to get the auth key ***/
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.AuthorizationRegistrationService;
            sh.FamilySubtypeID = (ushort) AuthorizationRegistrationService.MD5AuthkeyRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x0001, sess.ScreenName, Encoding.ASCII);
                tlvs.WriteEmpty(0x004B);
                tlvs.WriteEmpty(0x005A);
                stream.WriteTlvBlock(tlvs);
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }
    }
}