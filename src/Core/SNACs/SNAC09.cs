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
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void RequestParametersList(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.PrivacyManagementService;
            sh.FamilySubtypeID = (ushort) PrivacyManagementService.ServiceParametersRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
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