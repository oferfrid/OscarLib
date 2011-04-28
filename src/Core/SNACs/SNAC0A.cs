using System.Collections.Generic;
using System.Text;

// string encoding complete

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x000A -- User lookup service
    /// </summary>
    internal static class SNAC0A
    {
        /// <summary>
        /// Sends a request to find accounts by their associated email address -- SNAC(0A,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="email">The email address to query</param>
		public static void FindUsersByEmail(ISession sess, string email)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.UserLookupService;
            sh.FamilySubtypeID = (ushort) UserLookupService.FindUserByEmailRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteString(email, Encoding.ASCII);

            sess.StoreRequestID(sh.RequestID, email);

            DataPacket dp = Marshal.BuildDataPacket(sess, sh, stream);
            SNACFunctions.BuildFLAP(dp);
        }

        /// <summary>
        /// Processes the results of a search-by-email request -- SNAC(0A,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0A,03)</param>
        public static void ProcessSearchResults(DataPacket dp)
        {
            string email = (string) dp.ParentSession.RetrieveRequestID(dp.SNAC.RequestID);
            List<string> accts = new List<string>();

            while (dp.Data.HasMoreData)
            {
                ushort key = dp.Data.ReadUshort();
                accts.Add(dp.Data.ReadString(dp.Data.ReadUshort(), Encoding.ASCII));
            }

            dp.ParentSession.OnSearchByEmailResults(email, accts.ToArray());
        }
    }
}