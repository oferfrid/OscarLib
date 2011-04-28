using System;
using System.Text;

// string encoding complete

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0015 -- ICQ-specific functions
    /// </summary>
    /// <remarks>Good news, everyone! ICQ-specific messages use little-endian byte storage,
    /// making every existing marshaling function completely irrelevant. Note that I'm extending the
    /// custom Marshal functions instead of using the BitConverter class, because I don't know how
    /// it would behave on systems where Mono is compiled to big-endian processors. I doubt this
    /// will ever be a problem, but what the hell, basically no trouble for me.</remarks>
    internal static class SNAC15
    {
        /// <summary>
        /// Hides the client's IP address from view
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void HidePublicIP(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x0424);
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x0001);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Changes the password on an ICQ account
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="newpassword">The new password</param>
        /// <remarks>If the new password is longer than 8 characters, it is automatically
        /// truncated to 8 characters by the server.</remarks>
		public static void ChangeICQPassword(ISession sess, string newpassword)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x042E);
            stream.WriteUshort((ushort) (newpassword.Length + 1));
            stream.WriteString(newpassword, Encoding.ASCII);
            stream.WriteByte(0x00);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Requests a full set of information about an ICQ account
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="screenname">The account for which to retrieve information</param>
		public static void GetAllICQInfo(ISession sess, string screenname)
        {
            if (!ScreennameVerifier.IsValidICQ(screenname))
            {
                throw new ArgumentException(screenname + " is not a valid ICQ screenname", "screenname");
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x04B2);
            stream.WriteUint(uint.Parse(screenname));

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Requests a minimal set of information about an ICQ account
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="screenname">The account for which to retrieve information</param>
		public static void GetSimpleICQInfo(ISession sess, string screenname)
        {
            if (!ScreennameVerifier.IsValidICQ(screenname))
            {
                throw new ArgumentException(screenname + " is not a valid ICQ screenname", "screenname");
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x051F);
            stream.WriteUint(uint.Parse(screenname));

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Requests the alias assigned to an ICQ account
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="screenname">The account for which to retrieve information</param>
		public static void GetAlias(ISession sess, string screenname)
        {
            if (!ScreennameVerifier.IsValidICQ(screenname))
            {
                throw new ArgumentException(screenname + " is not a valid ICQ screenname", "screenname");
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x04BA);
            stream.WriteUint(uint.Parse(screenname));

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Sends an XML string
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="xml">The contents of an XML document</param>
        /// <remarks>I have no idea how to use this.</remarks>
		public static void SendXmlRequest(ISession sess, string xml)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x0998);
            stream.WriteUshort((ushort) Encoding.ASCII.GetByteCount(xml));
            stream.WriteString(xml, Encoding.ASCII);
            stream.WriteByte(0x00);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Sends an out-of-the-country text message
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="number">The number to which to send the message</param>
        /// <param name="message">The message to send</param>
        /// <param name="alias">The sender's alias</param>
        /// <remarks><paramref name="message"/> must be in codepage 1252. A delivery receipt
        /// is automatically requested by this method.</remarks>
		public static void SendSMSMessage(ISession sess, string number, string message, string alias)
        {
            string xmlformat = "<icq_sms_message>\n" +
                               "\t<destination>{0}</destination>\n" +
                               "\t<text>{1}</text>\n" +
                               "\t<codepage>1252</codepage>\n" +
                               "\t<senders_UIN>{2}</senders_UIN>\n" +
                               "\t<senders_name>{3}</senders_name>\n" +
                               "\t<delivery_receipt>Yes</delivery_receipt>\n" +
                               "\t<time>{4}</time>\n" +
                               "</icq_sms_message>\n";

            string xml = String.Format(xmlformat,
                                       number,
                                       message,
                                       sess.ScreenName,
                                       alias,
                                       DateTime.Now.ToString("r"));

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort) ICQExtensionsService.MetaInformationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshort(0x0008);
            stream.WriteUint(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort) sh.RequestID);
            stream.WriteUshort(0x8214);
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x0016);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);
            stream.WriteUshort(0x0000);
            stream.WriteUshort((ushort) Encoding.ASCII.GetByteCount(xml));
            stream.WriteString(xml, Encoding.ASCII);
            stream.WriteByte(0x00);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        public static void ProcessInfoResponse(DataPacket dp)
        {
            // The first four bytes of this are the TLV 0x0001
            // header which encapsulates the rest of the data
            int index = 4;

            ushort cmdlength = dp.Data.ReadUshortLE();
            uint my_uin = dp.Data.ReadUintLE();
            ushort command = dp.Data.ReadUshortLE();
            ushort requestID = dp.Data.ReadUshortLE();

            Encoding enc = Encoding.ASCII;

            switch (command)
            {
                case 0x0041:
                    break;
                case 0x0042:
                    // Gaim does a callback here
                    break;
                case 0x07DA:
                    ICQInfo info = new ICQInfo();
                    // RecallICQInfo(requestid)
                    ushort subtype = dp.Data.ReadUshortLE();
                    index++; // 0x0A
                    switch (subtype)
                    {
                        case 0x00A0:
                            break;
                        case 0x00AA:
                            break;
                        case 0x00C8:
                            info.Nickname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Firstname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Lastname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Email = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeCity = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeState = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomePhone = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeFax = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeAddress = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.MobilePhone = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeZip = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.HomeCountry = dp.Data.ReadUshortLE();
                            break;
                        case 0x00DC:
                            info.Age = dp.Data.ReadByte();
                            index++; // unknown
                            info.Gender = dp.Data.ReadByte();
                            info.PersonalURL = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.BirthYear = dp.Data.ReadUshortLE();
                            info.BirthMonth = dp.Data.ReadByte();
                            info.BirthDay = dp.Data.ReadByte();
                            info.Language1 = dp.Data.ReadByte();
                            info.Language2 = dp.Data.ReadByte();
                            info.Language3 = dp.Data.ReadByte();
                            break;
                        case 0x00D2:
                            info.WorkCity = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkState = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkPhone = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkFax = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkAddress = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkZip = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkCountry = dp.Data.ReadUshortLE();
                            info.WorkCompany = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkDivision = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.WorkPosition = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            index += 2;
                            info.WorkWebsite = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            break;
                        case 0x00E6:
                            info.Information = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            break;
                        case 0x00EB:
                            ushort numaddresses = dp.Data.ReadUshortLE();
                            info.EmailAddresses = new string[numaddresses];
                            for (int i = 0; i < numaddresses; i++)
                            {
                                info.EmailAddresses[i] = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                                if (i + 1 != numaddresses)
                                    index++;
                            }
                            break;
                        case 0x00F0:
                            break;
                        case 0x00FA:
                            break;
                        case 0x0104:
                            info.Nickname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Firstname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Lastname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);

                            break;
                        case 0x010E:
                            break;
                        case 0x019A:
                            index += 2;
                            info.Screenname = dp.Data.ReadUintLE().ToString();
                            info.Nickname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Firstname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Lastname = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            info.Email = dp.Data.ReadString(dp.Data.ReadUshortLE(), Encoding.ASCII);
                            break;
                    } // End the subtype switch statement
                    break;

                    // Do some crazy multipart snac stuff
                default:
                    index += cmdlength;
                    break;
            }
        }


    }

}