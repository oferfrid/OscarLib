using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0006 -- invitation service
    /// </summary>
    internal static class SNAC06
    {
        /// <summary>
        /// Sends an AIM invitation to a receipient -- SNAC(06,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="email">The email address of the person to invite</param>
        /// <param name="text">The text of the invitation</param>
		public static void SendInvitiationRequest(ISession sess, string email, string text)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.PrivacyManagementService;
            sh.FamilySubtypeID = (ushort) PrivacyManagementService.ServiceParametersRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x0011, email, Encoding.ASCII);
                tlvs.WriteString(0x0015, text, Encoding.ASCII);
                stream.WriteTlvBlock(tlvs);
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(06,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(06,03) </param>
        public static void ProcessInvitationConfirmation(DataPacket dp)
        {
            // Perhaps a user callback?
        }
    }
}