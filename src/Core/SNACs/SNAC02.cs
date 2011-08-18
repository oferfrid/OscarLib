using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0002 -- location services
    /// </summary>
    internal static class SNAC02
    {
        private const int DIRECTORY_ADDRESS = 0x0021;
        private const int DIRECTORY_CITY = 0x0008;
        private const int DIRECTORY_COUNTRY = 0x0006;
        private const int DIRECTORY_FIRSTNAME = 0x0001;
        private const int DIRECTORY_LASTNAME = 0x0003;
        private const int DIRECTORY_MAIDENNAME = 0x0004;
        private const int DIRECTORY_MIDDLENAME = 0x0002;
        private const int DIRECTORY_NICKNAME = 0x000C;
        private const int DIRECTORY_STATE = 0x0007;
        private const int DIRECTORY_ZIPCODE = 0x000D;

        private const int INTERESTS_ALLOWSEARCH = 0x001A;
        private const int INTERESTS_INTEREST = 0x000B;
        private const int PARAMETER_MAXCAPABILITIES = 0x0002;
        private const int PARAMETER_PROFILELENGTH = 0x0001;


        /// <summary>
        /// Sets the user's directory information -- SNAC(02,09)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="allow"><c>true</c> if other users may search this information, <c>false</c> if not</param>
        /// <param name="firstname">A first name</param>
        /// <param name="middlename">A middle name</param>
        /// <param name="lastname">A last name</param>
        /// <param name="maidenname">A maiden name</param>
        /// <param name="nickname">A nickname</param>
        /// <param name="city">A city</param>
        /// <param name="state">A state</param>
        /// <param name="country">A country (two-letter code)</param>
        /// <param name="zip">A ZIP code</param>
        /// <param name="address">An address</param>
        public static void SetDirectoryInformation(
            Session sess, bool allow,
            string firstname, string middlename, string lastname, string maidenname, string nickname,
            string city, string state, string country, string zip, string address)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.LocationService;
            sh.FamilySubtypeID = (ushort) LocationServices.UpdateDirectoryInfoRequest;
            
            

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteUshort(0x001A, (ushort) ((allow) ? 0x0001 : 0x0000));
                tlvs.WriteString(DIRECTORY_FIRSTNAME, firstname, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_MIDDLENAME, middlename, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_LASTNAME, lastname, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_MAIDENNAME, maidenname, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_COUNTRY, country, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_STATE, state, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_CITY, city, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_NICKNAME, nickname, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_ZIPCODE, zip, Encoding.ASCII);
                tlvs.WriteString(DIRECTORY_ADDRESS, address, Encoding.ASCII);
                stream.WriteByteArray(tlvs.GetBytes());
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes a reply to a directory or interests information update -- SNAC(02,0A) and SNAC(02,10)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(02,0A)</param>
        public static void ProcessUpdateResult(DataPacket dp)
        {
            ushort result = dp.Data.ReadUshort();
            dp.ParentSession.OnDirectoryUpdateAcknowledged((result == 1));
        }

        /// <summary>
        /// Registers the user's screenname with the Location service -- SNAC(02,0B)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <remarks>
        /// The function of this SNAC is unknown, but the official AIM client (5.9.3861) sends it sometime
        /// after SNAC(01,02) is sent.
        /// </remarks>
        public static void SetSelfLocation(Session sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.LocationService;
            sh.FamilySubtypeID = (ushort) 0x000B;
            
            

            ByteStream stream = new ByteStream();
            stream.WriteByte((byte) Encoding.ASCII.GetByteCount(sess.ScreenName));
            stream.WriteString(sess.ScreenName, Encoding.ASCII);

            // This SNAC expects a response in SNAC(02,0C)
            sess.StoreRequestID(sh.RequestID, null);
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes a reply to SNAC(02,0B) -- SNAC(02,0C)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(02,0C)</param>
        public static void ProcessSelfLocationReply(DataPacket dp)
        {
            dp.ParentSession.RetrieveRequestID(dp.SNAC.RequestID);
        }


        /// <summary>
        /// Sets the user's interests list -- SNAC(02,0F)
        /// </summary>
        /// <param name="sess">A <see cref="Session"/> object</param>
        /// <param name="allow"><c>true</c> if other users may search this information, <c>false</c> if not</param>
        /// <param name="interests">An array of interest names</param>
        /// <remarks>
        /// OSCAR allows a user to set up to five interests. If <paramref name="interests"/> contains
        /// more than five items, only the first five are used.
        /// </remarks>
        public static void SetInterestsInformation(Session sess, bool allow, string[] interests)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.LocationService;
            sh.FamilySubtypeID = (ushort) LocationServices.UpdateInterestsRequest;
            
            

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteUshort(INTERESTS_ALLOWSEARCH, (ushort) ((allow) ? 0x0001 : 0x0000));
                for (int i = 0; i < interests.Length && i < 5; i++)
                {
                    tlvs.WriteString(INTERESTS_INTEREST, interests[i], Encoding.ASCII);
                }
                stream.WriteByteArray(tlvs.GetBytes());
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }
    }
}