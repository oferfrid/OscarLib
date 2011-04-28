using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x000F -- Directory search service
    /// </summary>
    internal static class SNAC0F
    {
        /// <summary>
        /// Performs a directory search by email address -- SNAC(0F,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="email">The email address to search for</param>
		public static void SearchByEmail(ISession sess, string email)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.DirectoryUserSearch;
            sh.FamilySubtypeID = (ushort) DirectorySearch.SearchUserRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x001C, "us-ascii", Encoding.ASCII);
                tlvs.WriteUshort(0x000A, 0x0001);
                tlvs.WriteString(0x0005, email, Encoding.ASCII);
                stream.WriteTlvBlock(tlvs);
            }
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Performs a directory search by personal information -- SNAC(0F,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="items">The number of non-null search terms</param>
        /// <param name="firstname">A first name</param>
        /// <param name="middlename">A middle name</param>
        /// <param name="lastname">A last name</param>
        /// <param name="maidenname">A maiden name</param>
        /// <param name="nickname">A nickname</param>
        /// <param name="city">A city</param>
        /// <param name="state">A state</param>
        /// <param name="country">A country (two letter code)</param>
        /// <param name="zip">A ZIP code</param>
        /// <param name="address">An address</param>
        /// <remarks>
        /// <para>If a search term is to be ignored, it must be set to <c>null</c>.</para>
        /// <para>There must be at least one non-null search term.</para></remarks>
        public static void SearchByInfo(
			ISession sess, int items,
            string firstname, string middlename, string lastname, string maidenname, string nickname,
            string city, string state, string country, string zip, string address)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.DirectoryUserSearch;
            sh.FamilySubtypeID = (ushort) DirectorySearch.SearchUserRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x001C, "us-ascii", Encoding.ASCII);
                tlvs.WriteUshort(0x000A, 0x0000);

                if (firstname != null)
                    tlvs.WriteString(0x0001, firstname, Encoding.ASCII);
                if (middlename != null)
                    tlvs.WriteString(0x0002, middlename, Encoding.ASCII);
                if (lastname != null)
                    tlvs.WriteString(0x0003, lastname, Encoding.ASCII);
                if (maidenname != null)
                    tlvs.WriteString(0x0004, maidenname, Encoding.ASCII);
                if (country != null)
                    tlvs.WriteString(0x0006, country, Encoding.ASCII);
                if (state != null)
                    tlvs.WriteString(0x0007, state, Encoding.ASCII);
                if (city != null)
                    tlvs.WriteString(0x0008, city, Encoding.ASCII);
                if (nickname != null)
                    tlvs.WriteString(0x000C, nickname, Encoding.ASCII);
                if (zip != null)
                    tlvs.WriteString(0x000D, zip, Encoding.ASCII);
                if (address != null)
                    tlvs.WriteString(0x0021, address, Encoding.ASCII);

                stream.WriteTlvBlock(tlvs);
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Performs a directory search by interest -- SNAC(0F,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="interest">The interest to search for</param>
		public static void SearchByInterest(ISession sess, string interest)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.DirectoryUserSearch;
            sh.FamilySubtypeID = (ushort) DirectorySearch.SearchUserRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            using (TlvBlock tlvs = new TlvBlock())
            {
                tlvs.WriteString(0x001C, "us-ascii", Encoding.ASCII);
                tlvs.WriteUshort(0x000A, 0x0001);
                tlvs.WriteString(0x0001, interest, Encoding.ASCII);
                stream.WriteTlvBlock(tlvs);
            }
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(0F,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0F,03)</param>
        public static void ProcessSearchResults(DataPacket dp)
        {
            // Skip some introductory data
            dp.Data.ReadUshort();
            ushort tmp = dp.Data.ReadUshort();
            dp.Data.ReadByteArray(tmp);

            ushort numresults = dp.Data.ReadUshort();
            DirectoryEntry[] results = new DirectoryEntry[numresults];
            for (int i = 0; i < numresults; i++)
            {
                results[i] = new DirectoryEntry();
                ushort key = dp.Data.ReadUshort();
                ushort keylength = dp.Data.ReadUshort();
                switch (key)
                {
                    case 0x0001:
                        results[i].FirstName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0002:
                        results[i].LastName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0003:
                        results[i].MiddleName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0004:
                        results[i].MaidenName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0005:
                        results[i].Email = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0006:
                        results[i].Country = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0007:
                        results[i].State = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0008:
                        results[i].City = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0009:
                        results[i].ScreenName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x000B:
                        results[i].Interest = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x000C:
                        results[i].NickName = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x000D:
                        results[i].ZIPCode = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x001C:
                        results[i].Region = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    case 0x0021:
                        results[i].Address = dp.Data.ReadString(keylength, Encoding.ASCII);
                        break;
                    default:
                        dp.Data.ReadByteArray(keylength);
                        break;
                }
            }

            dp.ParentSession.OnSearchResults(results);
        }

        /// <summary>
        /// Requests a list of known interests -- SNAC(0F,04)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void RequestInterestList(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.DirectoryUserSearch;
            sh.FamilySubtypeID = (ushort) DirectorySearch.InterestsListRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Processes a list of known interests from the server -- SNAC(0F,05)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0F,05)</param>
        /// <remarks>
        /// libfaim doesn't implement the interest list fetching, so this one's thanks to Mark Doliner's
        /// (thekingant@users.sourceforge.net) documentation.
        /// </remarks>
        public static void ProcessInterestList(DataPacket dp)
        {
            dp.Data.ReadUshort(); // First two bytes: 0x0001
            ushort num_items = dp.Data.ReadUshort();
            InterestItem[] interests = new InterestItem[num_items];
            for (int i = 0; i < num_items; i++)
            {
                interests[i] = dp.Data.ReadInterestItem();
            }

            dp.ParentSession.OnInterestsReceived(interests);
        }
    }
}