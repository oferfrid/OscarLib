namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0009 -- privacy management service
    /// </summary>
    internal static class SNAC09
    {
        /// <summary>
        /// Sends a request for parameter information -- SNAC(09,02)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        public static void RequestParametersList(Session sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.PrivacyManagementService;
            sh.FamilySubtypeID = (ushort) PrivacyManagementService.ServiceParametersRequest;
            

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        //KSD-SYSTEMS - added at 27.11.2009
        /// <summary>
        /// Processes a other clients request for parameter information -- SNAC(09,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(09,02)</param>
        public static void ProcessParametersListRequest(DataPacket dp)
        {
            // a client want receive information with your id on flap.Channel = 04 (= DisconnectFromServer = on this flap.Channel no Datapacket Handling)
            // dp.ParentSession.OnError(ServerErrorCode.ExternalClientRequest);
        }

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(09,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(09,03)</param>
        public static void ProcessParametersList(DataPacket dp)
        {
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                ushort max_visiblelist_size = tlvs.ReadUshort(0x0001);
                ushort max_invisiblelist_size = tlvs.ReadUshort(0x0002);

                dp.ParentSession.ParameterSetArrived();
            }
        }
    }
}