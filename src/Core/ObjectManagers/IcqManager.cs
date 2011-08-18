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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles the event if a search result for an icq user search has been received
    /// </summary>
    /// <param name="sender">the sender, that fired this event</param>
    /// <param name="searchResult">the searchresult object</param>
    public delegate void SearchResultReceivedEventHandler(object sender, object searchResult);

    /// <summary>
    /// Handles the event of SMS message sent successfully.
    /// </summary>
    /// <param name="sender">sender of event</param>
    /// <param name="messageId">SMS message id</param>
    public delegate void SmsMessageSentHandler(object sender, string messageId);

    /// <summary>
    /// Handles the event of SMS message error.
    /// </summary>
    /// <param name="sender">sender of event</param>
    /// <param name="error">error description.</param>    
    /// <param name="param">error parameter.</param>
    public delegate void SmsMessageErrorHandler(object sender, string error, string param);

    public delegate void ShortUserInfoReceivedHandler(object sender, ShortUserInfo ui);
    public delegate void FullUserInfoReceivedHandler(object sender, FullUserInfo ui);
    
    /// <summary>
    /// An interface implemented by manager objects that handle incoming data packets
    /// </summary>
    public interface ISnacFamilyHandler
    {
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/>
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        void ProcessIncomingPacket(DataPacket dp);
    }

    /// <summary>
    /// Performs the special functions for that special protocol, ICQ
    /// </summary>
    public class IcqManager : ISnacFamilyHandler
    {
        #region Fields

        private readonly Session parent;

        #endregion Fields

        #region Enums

        private enum MetaRequestType : ushort
        {
            OfflineMessageRequest = 0x003c,

            DeleteOfflineMessageRequest = 0x003e,

            MetaInfoRequest = 0x07d0
        }

        private enum MetaInfoRequestType : ushort
        {
            SetPassword = 0x042e,

            ShortUserInfoRequest = 0x04BA,

            DirectoryQueryRequest = 0x0FA0,

            DirectoryUpdateRequest = 0x0FD2,

            SendSmsRequest = 0x1482
        }

        private enum DirectoryRequestType : ushort
        {
            QueryInfoRequest = 0x02,

            SetInfoRequest = 0x03
        }

        private enum MetaResponseType : ushort
        {
            /// <summary>
            /// ICQ code for a received offline message
            /// </summary>
            OfflineMessage = 0x0041,

            /// <summary>
            /// ICQ code for the end of the offline message sequence
            /// </summary>
            EndOfflineMessage = 0x0042,

            /// <summary>
            /// ICQ code for a received response for a meta data request
            /// </summary>
            MetaDataReply = 0x07DA
        }

        private enum MetaResponseSubType : ushort
        {
            MetaProcessError = 0x0001,

            ServerSmsResponse = 0x0096,

            SetPasswordAck = 0x00aa,

            SearchResult = 0x0faa,

            LastSearchResult = 0x0fb4,

            ShortUserInfoResponse = 0x0104,

            DirectoryQueryData = 0x0FAA,
            DirectoryQueryResponse = 0x0FB4,

            DirectoryUpdateAck = 0x0FDC
        }

        private enum DirecotyQueryType : ushort
        {
            InfoOwner = 0x0001,

            InfoUser = 0x0002,

            InfoMulti = 0x0003,

            Search = 0x0004
        }

        #endregion Enums

        #region Constructor

        /// <summary>
        /// Initializes a new IcqManager
        /// </summary>
        internal IcqManager(Session parent)
        {
            this.parent = parent;
            this.parent.Dispatcher.RegisterSnacFamilyHandler(this, (ushort)SNACFamily.ICQExtensionsService);
        }

        #endregion Constructor

        #region ISnacFamilyHandler Members

        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 15 (and one from 13)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(DataPacket dp)
        {
            Debug.Assert(dp.SNAC.FamilyServiceID == 0x0015);
            switch (dp.SNAC.FamilySubtypeID)
            {
                case (ushort)ICQExtensionsService.ClientServerError:
                    ProcessExtensionError(dp);
                    break;
                case (ushort)ICQExtensionsService.MetaInformationResponse:
                    ProcessMetaInformationResponse(dp);
                    break;
            }
        }

        #endregion

        #region Common Functions

        /// <summary>
        /// Creates a byte stream with a common ICQ prefix
        /// </summary>
        /// <param name="uin">An ICQ UIN</param>
        internal static ByteStream BeginIcqByteStream(String uin)
        {
            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x000A);
            stream.WriteUshortLE(0x0008);
            stream.WriteUintLE(uint.Parse(uin));
            return stream;
        }

        /// <summary>
        /// Creates a SNAC header with the common ICQ properties
        /// </summary>
        internal static SNACHeader CreateIcqMetaHeader()
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort)ICQExtensionsService.MetaInformationRequest;


            return sh;
        }

        /// <summary>
        /// Creates a SNAC header with the common ICQ properties
        /// </summary>
        private static ByteStream CreateMetaInfoHeader(Session sess, SNACHeader sh, MetaInfoRequestType type, ByteStream data)
        {
            ByteStream stream = new ByteStream();
            int size = data.GetByteCount();

            stream.WriteUshort(0x0001);
            stream.WriteUshort((ushort)(size + 12));        // size

            stream.WriteUshortLE((ushort)(size + 10));      // size
            stream.WriteUintLE(uint.Parse(sess.ScreenName));
            stream.WriteUshortLE((ushort)MetaRequestType.MetaInfoRequest);
            stream.WriteUshortLE((ushort)sh.RequestID);
            stream.WriteUshortLE((ushort)type);

            stream.WriteByteArray(data.GetBytes());
            return stream;
        }
        private static ByteStream CreateDirectoryHeader(DirectoryRequestType type, ByteStream data)
        {
            ByteStream stream = new ByteStream();
            int size = data.GetByteCount();

            stream.WriteUshortLE((ushort)(size + 26));       // size
            SNACHeader shsub = new SNACHeader(0x05b9, (ushort)type, 0x8000, 0);     // Sub Snac Header
            stream.WriteSnacHeader(shsub);

            stream.WriteUshort(0x0006);             // Snac Extra Bytes (length)
            stream.WriteUshort(0x0001);             // TLV - T
            stream.WriteUshort(0x0002);             // TLV - L
            stream.WriteUshort(0x0002);             // TLV - V; Versions Nr

            stream.WriteUshort(0x0000);
            stream.WriteUshort((ushort)Encoding.Default.CodePage);
            stream.WriteUint((uint)2);

            stream.WriteByteArray(data.GetBytes());
            return stream;
        }

        private void ProcessExtensionError(DataPacket dp)
        {
            ushort errorcode, subcode;
            SNACFunctions.GetSNACErrorCodes(dp, out errorcode, out subcode);

            parent.OnError((ServerErrorCode)errorcode, dp);
        }

        private void ProcessMetaInformationResponse(DataPacket dp)
        {
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                if (tlvs.HasTlv(0x0001))
                {
                    ByteStream stream = new ByteStream(tlvs.ReadByteArray(0x0001));
                    ushort cmdLength = stream.ReadUshortLE();
                    String uin = stream.ReadUintLE().ToString();
                    ushort command = stream.ReadUshortLE();
                    ushort requestId = stream.ReadUshortLE();

                    MetaResponseType responseType = (MetaResponseType)command;
                    switch (responseType)
                    {
                        case MetaResponseType.OfflineMessage:
                            parent.Messages.ReadIcqOfflineMessage(uin, stream);
                            break;
                        case MetaResponseType.EndOfflineMessage:
                            parent.Messages.EndOfflineMessageSequence(uin, true);
                            break;

                        case MetaResponseType.MetaDataReply:
                            this.ProcessMetaDataReply(uin, dp, stream);
                            break;
                    }
                }
            }
        }

        private void ProcessMetaDataReply(string uin, DataPacket dp, ByteStream data)
        {
            MetaResponseSubType responseSubType = (MetaResponseSubType)data.ReadUshortLE();

            switch (responseSubType)
            {
                case MetaResponseSubType.MetaProcessError:
                    data.AdvanceOffset(1); // skip status byte
                    // get error message
                    string errorDesc = data.ReadNullTerminatedString(data.GetByteCount() - data.CurrentPosition);
                    SmsMessageError(this, "46", errorDesc);
                    break;
                case MetaResponseSubType.ServerSmsResponse:
                    this.ProcessServerSmsResponse(data);
                    break;
                case MetaResponseSubType.SetPasswordAck:
                    this.ProcessSetPasswordAck(dp, data);
                    break;
                case MetaResponseSubType.ShortUserInfoResponse:
                    this.ProcessShortUserInfoResponse(dp, data);
                    break;
                case MetaResponseSubType.DirectoryQueryData:
                case MetaResponseSubType.DirectoryQueryResponse:
                    this.ProcessDirectoryQueryResponse(dp, data);
                    break;
                case MetaResponseSubType.DirectoryUpdateAck:
                    this.ProcessDirectoryUpdateAck(dp, data);
                    break;
                default:
                    this.ProcessSearchResult(uin, responseSubType, data);
                    break;
            }

        }

        #endregion Common Functions

        #region Search Handling
        public void SendSearchRequest(string nickname, string firstname, string lastname, ushort fromAge, ushort toAge,
            string gender, string language, string country, string online, bool multiPageSearch)
        {
            // TODO Send Search Request
        }

        private void ProcessSearchResult(string uin, MetaResponseSubType responseType, ByteStream data)
        {
            switch (responseType)
            {
                default:
                    return;

                case MetaResponseSubType.SearchResult:
                case MetaResponseSubType.LastSearchResult:
                    {

                        // TODO Handle search result 

                        break;
                    }
            }

        }
        #endregion Search Handling

        #region SMS Handling

        /// <summary>
        /// Raised when an SMS message was successfully sent.
        /// </summary>
        public event SmsMessageSentHandler SmsMessageSent;

        /// <summary>
        /// Raised when an SMS message failed to be sent.
        /// </summary>
        public event SmsMessageErrorHandler SmsMessageError;

        void ProcessServerSmsResponse(ByteStream data)
        {
            data.AdvanceOffset(1); // skip success byte
            data.AdvanceOffset(8); // skip unknown fields

            // length of xml response
            ushort xmlen = data.ReadUshort();
            // read response
            string xml = data.ReadString(xmlen, Encoding.ASCII);
            // parse xml
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            System.Xml.XmlNode node = doc.SelectSingleNode("/sms_response/deliverable");
            bool deliverable = false;
            // check if sms delivered
            if (node != null && node.InnerText.ToLower() == "yes")
                deliverable = true;

            // sms info
            string messageId = null;
            string error = null;
            string param = null;

            if (deliverable)
            {
                node = doc.SelectSingleNode("/sms_response/message_id");
                if (node != null)
                    messageId = node.InnerText;
            }
            else
            {
                node = doc.SelectSingleNode("/sms_response/error/id");
                error = node.InnerText;
                node.SelectSingleNode("/params/param");
                param = node.InnerText;
            }

            doc = null;

            if (deliverable)
                SmsMessageSent(this, messageId);
            else
                SmsMessageError(this, error, param);
        }

        /// <summary>
        /// Sends an out-of-the-country text message
        /// </summary>
        /// <param name="number">The number to which to send the message</param>
        /// <param name="message">The message to send</param>
        /// <param name="alias">The sender's alias</param>
        /// <remarks><paramref name="message"/> must be in codepage 1252. A delivery receipt
        /// is automatically requested by this method.</remarks>
        public void SendSMSMessage(string number, string message, string alias)
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            string xmlformat = "<icq_sms_message>" +
              "<destination>{0}</destination>" +
              "<text>{1}</text>" +
              "<codepage>1252</codepage>" +
              "<encoding>utf8</encoding>" +
              "<senders_UIN>{2}</senders_UIN>" +
              "<senders_name>{3}</senders_name>" +
              "<delivery_receipt>Yes</delivery_receipt>" +
              "<time>{4}</time>" +
              "</icq_sms_message>";

            string xml = String.Format(xmlformat,
              number,
              message,
              parent.ScreenName,
              alias,
              DateTime.Now.ToString("r"));


            /*SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.ICQExtensionsService;
            sh.FamilySubtypeID = (ushort)ICQExtensionsService.MetaInformationRequest;
            
            sh.RequestID = sess.GetNextRequestID();*/
            SNACHeader sh = CreateIcqMetaHeader();

            Encoding enc = Encoding.GetEncoding("utf-8");
            ushort xmllength = (ushort)(enc.GetByteCount(xml) + 1);
            byte[] buffer = new byte[40 + xmllength];

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0001);
            stream.WriteUshort((ushort)(buffer.Length - 4));
            stream.WriteUshortLE((ushort)(buffer.Length - 6));
            stream.WriteUintLE(uint.Parse(parent.ScreenName));

            stream.WriteUshortLE((ushort)MetaRequestType.MetaInfoRequest);
            stream.WriteUshortLE((ushort)sh.RequestID);
            stream.WriteUshortLE((ushort)MetaInfoRequestType.SendSmsRequest);

            stream.WriteUshort(0x0001);
            stream.WriteUshort(0x0016);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);
            stream.WriteUint(0x00000000);

            stream.WriteUshort(0x0000);
            stream.WriteUshort(xmllength);
            stream.WriteString(xml, enc);
            stream.WriteByte(0x00);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));

        }

        #endregion SMS Functions

        #region User Details

        #region META_SET_PASSWORD  -  META_SET_PASSWORD_ACK
        /// <summary>
        /// Set's a new Password for current Account.
        /// </summary>
        /// <param name="password">New Password</param>
        [Obsolete("This method is obsolete and Icq-Server sends a 'Not supported by server'-Response.")]
        public void SendNewPassword(string password)
        {
            ByteStream stream = new ByteStream();
            stream.WriteUshortLE((ushort)password.Length);
            stream.WriteString(password, Encoding.ASCII);

            /* * * * * Header * * * * */
            SNACHeader sh = CreateIcqMetaHeader();
            stream = CreateMetaInfoHeader(parent, sh, MetaInfoRequestType.SetPassword, stream);

            //parent.StoreRequestID(sh.RequestID, screenname);
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }
        private void ProcessSetPasswordAck(DataPacket dp, ByteStream data)
        {
            Byte success = data.ReadByte();     // 0x0a
        }
        #endregion

        #region META_DIRECTORY_UPDATE  -  META_DIRECTORY_UPDATE_ACK
        /// <summary>
        /// Sets a new pending main Email, also you get to this Email-Address a activation Email
        /// </summary>
        /// <param name="email">A valid email address</param>
        /// <returns>The error code</returns>
        /// <remarks><para>Known error codes are:</para><para>0 = success, 11 = invalid session key, 12 = invalid screenname, 13 = invalid email-domain</para></remarks>
        public uint SendUserEmail(string email)
        {
            string url = "http://www.icq.com";
            string path = "/register/email_activation/sendEmailActivation.php";
            string get = "?uin=" + parent.ScreenName + "&email=" + System.Web.HttpUtility.UrlEncode(email) + "&sessionKey="+ parent.Services.AuthCookie +"&version=605"; //&lang_id=de-de
            string regex = "(<answer)(.)*(code=\")(?<errorcode>[0-9]*)(\")(.)*(/>)";

            System.Net.WebRequest req = System.Net.WebRequest.Create(url + path + get);

            System.IO.Stream stream = req.GetResponse().GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(stream);
            string doc = reader.ReadToEnd();

            System.Text.RegularExpressions.Regex error = new System.Text.RegularExpressions.Regex(regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return uint.Parse(error.Match(doc).Groups["errorcode"].Value);
        }

        /// <summary>
        /// Set my icq contact informations.
        /// </summary>
        /// <param name="ui">My Details</param>
        /// <remarks>
        /// <para>The following properties can't be set:</para>
        /// <para><seealso cref="FullUserInfo.Screenname"/>, <seealso cref="FullUserInfo.Email"/> (use <seealso cref="SendUserEmail"/> instead), <seealso cref="FullUserInfo.Age"/>, <seealso cref="FullUserInfo.CodePage"/>, 
        /// <seealso cref="FullUserInfo.Timezone"/></para>
        /// </remarks>
        public void SendUserInfo(FullUserInfo ui)
        {
            TlvBlock tlvs = new TlvBlock();

            // Names
            tlvs.WriteString(0x64, ui.Firstname, Encoding.UTF8);
            tlvs.WriteString(0x6e, ui.Lastname, Encoding.UTF8);
            tlvs.WriteString(0x78, ui.Nickname, Encoding.UTF8);

            // Home Address
            tlvs.WriteByteArray(0x96, ProcessDirectoryUpdateRequestUserDetails(0x96, ui));
            // Origin Address
            tlvs.WriteByteArray(0xa0, ProcessDirectoryUpdateRequestUserDetails(0xa0, ui));
            // Phone Numbers
            tlvs.WriteByteArray(0xc8, ProcessDirectoryUpdateRequestUserDetails(0xc8, ui));
            // Emails
            tlvs.WriteByteArray(0x8c, ProcessDirectoryUpdateRequestUserDetails(0x8c, ui));
            // Work
            tlvs.WriteByteArray(0x118, ProcessDirectoryUpdateRequestUserDetails(0x118, ui));
            // Education
            tlvs.WriteByteArray(0x10e, ProcessDirectoryUpdateRequestUserDetails(0x10e, ui));
            // Interests
            tlvs.WriteByteArray(0x122, ProcessDirectoryUpdateRequestUserDetails(0x122, ui));

            // Timezone
            TimeSpan ts = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
                ts -= TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year).Delta;
            tlvs.WriteSshort(0x17c, (short)(ts.TotalMinutes / -30));

            switch (ui.Gender.ToString().ToUpper())
            {
                case "F": tlvs.WriteByte(0x82, 1); break;
                case "M": tlvs.WriteByte(0x82, 2); break;
                default: tlvs.WriteByte(0x82, 0); break;
            }

            tlvs.WriteString(0xfa, ui.Website, Encoding.ASCII);

            if (ui.Birthday == DateTime.MinValue)
                tlvs.WriteDouble(0x1a4, 0);
            else
                tlvs.WriteDouble(0x1a4, ui.Birthday.ToOADate() - 2);

            tlvs.WriteUshort(0xaa, (ushort)ui.Language1);
            tlvs.WriteUshort(0xb4, (ushort)ui.Language2);
            tlvs.WriteUshort(0xbe, (ushort)ui.Language3);

            tlvs.WriteUshort(0x12c, (ushort)ui.MaritalStatus);
            tlvs.WriteString(0x186, ui.About, Encoding.UTF8);

            tlvs.WriteString(0x226, ui.StatusNote, Encoding.UTF8);
            tlvs.WriteUshort(0x1f9, ui.PrivacyLevel);
            tlvs.WriteUshort(0x19a, ui.Auth);
            tlvs.WriteByte(0x212, ui.WebAware);
            tlvs.WriteByte(0x1ea, ui.AllowSpam);
            tlvs.WriteUshort(0x1c2, (ushort)Encoding.Default.CodePage);     //ui.CodePage

            ByteStream stream = new ByteStream();
            stream.WriteUshort(0x0003);
            stream.WriteUshort((ushort)tlvs.GetByteCount());
            stream.WriteByteArray(tlvs.GetBytes());

            /* * * * * Header * * * * */
            SNACHeader sh = CreateIcqMetaHeader();
            stream = CreateDirectoryHeader(DirectoryRequestType.SetInfoRequest, stream);
            stream = CreateMetaInfoHeader(parent, sh, MetaInfoRequestType.DirectoryUpdateRequest, stream);

            //parent.StoreRequestID(sh.RequestID, DirecotyQueryType.InfoUser);
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        private byte[] ProcessDirectoryUpdateRequestUserDetails(int typecode, FullUserInfo ui)
        {
            List<TlvBlock> tlvlist = new List<TlvBlock>();
            ByteStream bstream = null;

            switch (typecode)
            {
                case 0x96:  // Home Address
                    tlvlist.Add(new TlvBlock());
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.HomeAddress, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x6e, ui.HomeCity, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x78, ui.HomeState, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x82, ui.HomeZip, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteUint(0x8c, (uint)ui.HomeCountry);
                    break;
                case 0xa0:  // Origin Address
                    tlvlist.Add(new TlvBlock());
                    tlvlist[tlvlist.Count - 1].WriteString(0x6e, ui.OriginCity, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x78, ui.OriginState, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteUint(0x8c, (uint)ui.OriginCountry);
                    break;
                case 0xc8:  // Phone Numbers
                    tlvlist.Add(new TlvBlock()); 
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, 1);
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.HomePhone, Encoding.ASCII);

                    tlvlist.Add(new TlvBlock()); 
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, 2);
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.WorkPhone, Encoding.ASCII);

                    tlvlist.Add(new TlvBlock()); 
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, 3);
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.MobilePhone, Encoding.ASCII);

                    tlvlist.Add(new TlvBlock()); 
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, 4);
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.HomeFax, Encoding.ASCII);

                    tlvlist.Add(new TlvBlock()); 
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, 5);
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.WorkFax, Encoding.ASCII);
                    break;
                case 0x8c:  // Emails
                    if (ui.EmailAddresses != null)
                    {
                        foreach (string email in ui.EmailAddresses)
                        {
                            tlvlist.Add(new TlvBlock());
                            tlvlist[tlvlist.Count - 1].WriteUshort(0x78, 0);
                            tlvlist[tlvlist.Count - 1].WriteString(0x64, email, Encoding.ASCII);
                        }
                    }
                    break;
                case 0x118: // Work
                    tlvlist.Add(new TlvBlock());
                    tlvlist[tlvlist.Count - 1].WriteString(0x64, ui.WorkPosition, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x6e, ui.WorkCompany, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x7d, ui.WorkDepartment, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x78, ui.WorkWebsite, Encoding.ASCII);
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x82, (ushort)ui.WorkIndustry);

                    tlvlist[tlvlist.Count - 1].WriteString(0xaa, ui.WorkAddress, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0xb4, ui.WorkCity, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0xbe, ui.WorkState, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0xc8, ui.WorkZip, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteUint(0xd2, (uint)ui.WorkCountry);
                    break;
                case 0x10e: // Education
                    tlvlist.Add(new TlvBlock());
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x64, (ushort)ui.StudyLevel);
                    tlvlist[tlvlist.Count - 1].WriteString(0x6e, ui.StudyInstitute, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteString(0x78, ui.StudyDegree, Encoding.UTF8);
                    tlvlist[tlvlist.Count - 1].WriteUshort(0x8c, (ushort)ui.StudyYear);
                    break;
                case 0x122: // Interests
                    if (ui.InterestInfos != null)
                    {
                        foreach (FullUserInfo.InterestInfo info in ui.InterestInfos)
                        {
                            tlvlist.Add(new TlvBlock());
                            tlvlist[tlvlist.Count - 1].WriteUshort(0x6e, (ushort)info.Category);
                            tlvlist[tlvlist.Count - 1].WriteString(0x64, info.Info, Encoding.UTF8);
                        }
                    }
                    break;
            }

            switch (typecode)
            {
                case 0x96:  // Home Address
                    bstream = writeTlvList(tlvlist, 1);
                    break;
                case 0xa0:  // Origin Address
                    bstream = writeTlvList(tlvlist, 1);
                    break;
                case 0xc8:  // Phone Numbers
                    bstream = writeTlvList(tlvlist, 5);
                    break;
                case 0x8c:  // Emails
                    bstream = writeTlvList(tlvlist, 4);
                    break;
                case 0x118: // Work
                    bstream = writeTlvList(tlvlist, 1);
                    break;
                case 0x10e: // Education
                    bstream = writeTlvList(tlvlist, 1);
                    break;
                case 0x122: // Interests
                    bstream = writeTlvList(tlvlist, 4);
                    break;
            }

            return bstream.GetBytes();
        }

        void ProcessDirectoryUpdateAck(DataPacket dp, ByteStream data)
        {
            //DirecotyQueryType responseType = (DirecotyQueryType)parent.RetrieveRequestID(dp.SNAC.RequestID);

            Byte success = data.ReadByte();     // 0x0a

            ushort responseLength = data.ReadUshortLE();
            Debug.Assert(responseLength == data.RemainingBytes);

            // Read Sub Snac Header
            SNACHeader shsub = data.ReadSnacHeader();

            // Reads Snac Extra Bytes
            ushort snacExtraBytesLength = data.ReadUshort();
            if (snacExtraBytesLength > 0)
            {
                // Version or Anything
                if (snacExtraBytesLength == 6)
                {
                    ushort snacExtraBytesTyp = data.ReadUshort();
                    ushort snacExtraBytesLen = data.ReadUshort();
                    ushort snacExtraBytesValue = data.ReadUshort(); // VersionsNr
                }
                else
                {
                    data.ReadByteArray(snacExtraBytesLength);
                }
            }

            Byte result = data.ReadByte();      // 0x01
            if (result != 1)
            {
                Logging.WriteString("Error: Directory update request failed, status {0}", result);
                return;
            }

            ushort errorLength = data.ReadUshort();
            if (errorLength > 0)
            {
                data.ReadByteArray(errorLength);
                Logging.WriteString("Warning: Data in error message present!");
            }
        }
        #endregion

        #region META_REQUEST_SHORT_INFO  -  META_SHORT_USERINFO
        /// <summary>
        /// Raised when an user info is received.
        /// </summary>
        public event ShortUserInfoReceivedHandler ShortUserInfoReceived;

        /// <summary>
        /// <para>Request short user informations from Server. Strings are ASCII.</para>
        /// <para>Works only with ICQ-Contacts not e.g. AOL-Contacts.</para>
        /// </summary>
        /// <param name="screenname">The screenname to get information about, see <see cref="Utility.ScreennameVerifier.IsValidICQ"/></param>
        /// <remarks>Results are returned by the <see cref="ShortUserInfoReceived"/> event</remarks>
        public void RequestShortUserInfo(string screenname)
        {
            if (!ScreennameVerifier.IsValidICQ(screenname)) return;

            ByteStream stream = new ByteStream();
            stream.WriteUintLE(uint.Parse(screenname));

            /* * * * * Header * * * * */
            SNACHeader sh = CreateIcqMetaHeader();
            stream = CreateMetaInfoHeader(parent, sh, MetaInfoRequestType.ShortUserInfoRequest, stream);

            parent.StoreRequestID(sh.RequestID, screenname);
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        private void ProcessShortUserInfoResponse(DataPacket dp, ByteStream data)
        {
            ushort length;

            ShortUserInfo ui = new ShortUserInfo();
            ui.Screenname = parent.RetrieveRequestID(dp.SNAC.RequestID) as string;

            Byte success = data.ReadByte();     // 0x0a

            length = data.ReadUshortLE();
            ui.Nickname = data.ReadString(length, Encoding.ASCII);

            length = data.ReadUshortLE();
            ui.Firstname = data.ReadString(length, Encoding.ASCII);

            length = data.ReadUshortLE();
            ui.Lastname = data.ReadString(length, Encoding.ASCII);

            length = data.ReadUshortLE();
            ui.Email = data.ReadString(length, Encoding.ASCII);

            Byte authflag = data.ReadByte();
            Byte unknown = data.ReadByte();
            Byte gender = data.ReadByte();

            if (ShortUserInfoReceived != null)
                ShortUserInfoReceived(this, ui);
        }
        #endregion META_REQUEST_SHORT_INFO META_SHORT_USERINFO

        #region META_DIRECTORY_QUERY  -  META_DIRECTORY_RESPONSE
        /// <summary>
        /// Raised when an user info is received.
        /// </summary>
        public event FullUserInfoReceivedHandler FullUserInfoReceived;

        /// <summary>
        /// <para>Request full user informations from Server. Strings are ASCII and UTF8.</para>
        /// <para>Works only with ICQ-Contacts not e.g. AOL-Contacts.</para>
        /// </summary>
        /// <param name="screenname">The screenname to get information about, see <see cref="Utility.ScreennameVerifier.IsValidICQ"/></param>
        /// <remarks>Results are returned by the <see cref="FullUserInfoReceived"/> event.</remarks>
        public void RequestFullUserInfo(string screenname)
        {
            if (!ScreennameVerifier.IsValidICQ(screenname)) return;

            ByteStream stream = new ByteStream();

            // Metatoken
            SSIBuddy buddy = parent.SSI.GetBuddyByName(screenname);
            byte[] metatoken = (buddy != null ? buddy.MetaInfoToken : null);

            // Length
            ushort len_token = (ushort)(metatoken != null ? metatoken.Length + 4 : 0);
            ushort len_name = (ushort)(Encoding.ASCII.GetByteCount(screenname) + 4);

            /* * * * * User * * * * */
            stream.WriteUshort(0x0003);
            stream.WriteUint((uint)1);
            stream.WriteUshort((ushort)(len_name + len_token));         // size to end

            // MetaToken for extendet userinfo
            if (metatoken != null)
            {
                stream.WriteUshort(0x003c);
                stream.WriteUshort((ushort)metatoken.Length);
                stream.WriteByteArray(metatoken);
            }

            // TLV Block
            stream.WriteUshort(0x0032);
            stream.WriteUshort((ushort)Encoding.ASCII.GetByteCount(screenname));
            stream.WriteString(screenname, Encoding.ASCII);

            /* * * * * Header * * * * */
            SNACHeader sh = CreateIcqMetaHeader();
            stream = CreateDirectoryHeader(DirectoryRequestType.QueryInfoRequest, stream);
            stream = CreateMetaInfoHeader(parent, sh, MetaInfoRequestType.DirectoryQueryRequest, stream);

            parent.StoreRequestID(sh.RequestID, DirecotyQueryType.InfoUser);
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        void ProcessDirectoryQueryResponse(DataPacket dp, ByteStream data)
        {
            DirecotyQueryType responseType = (DirecotyQueryType)parent.RetrieveRequestID(dp.SNAC.RequestID);

            Byte success = data.ReadByte();     // 0x0a

            ushort responseLength = data.ReadUshortLE();
            Debug.Assert(responseLength == data.RemainingBytes);

            // Read Sub Snac Header
            SNACHeader shsub = data.ReadSnacHeader();

            // Reads Snac Extra Bytes
            ushort snacExtraBytesLength = data.ReadUshort();
            if (snacExtraBytesLength > 0)
            {
                // Version or Anything
                if (snacExtraBytesLength == 6)
                {
                    ushort snacExtraBytesTyp = data.ReadUshort();
                    ushort snacExtraBytesLen = data.ReadUshort();
                    ushort snacExtraBytesValue = data.ReadUshort(); // VersionsNr
                }
                else
                {
                    data.ReadByteArray(snacExtraBytesLength);
                }
            }

            Byte result = data.ReadByte();      // 0x01

            ushort errorLength = data.ReadUshort();
            if (errorLength > 0)
            {
                data.ReadByteArray(errorLength);
                Logging.WriteString("Warning: Data in error message present!");
            }

            if (data.RemainingBytes <= 0x16)
            {
                parent.OnError(ServerErrorCode.InvalidSNACFormat, dp);
                return;
            }

            // Unknown Stuff
            data.ReadByteArray(0x10);

            // TODO: check itemcount, pagecount against the cookie data ???
            uint itemcount = data.ReadUint();
            ushort pagecount = data.ReadUshort();

            if (responseType == DirecotyQueryType.Search)
                Logging.WriteString("Directory Search: %d contacts found (%u pages)", itemcount, pagecount);

            if (data.RemainingBytes <= 2)
            {
                parent.OnError(ServerErrorCode.InvalidSNACFormat, dp);
                return;
            }

            // Maybe the bock count of the following data
            ushort dataAvailable = data.ReadUshort();
            if (dataAvailable == 0)
            {
                parent.OnError(ServerErrorCode.InvalidSNACFormat, dp);
                return;
            }

            ushort dataLength = data.ReadUshort();
            Debug.Assert(dataLength == data.RemainingBytes);

            if (dataAvailable != 1 || dataLength != data.RemainingBytes)
            {
                parent.OnError(ServerErrorCode.InvalidSNACFormat, dp);
                return;
            }

            using (TlvBlock tlvs = new TlvBlock(data.ReadByteArrayToEnd()))
            {
                switch (responseType)
                {
                    case DirecotyQueryType.InfoOwner:
                    case DirecotyQueryType.InfoUser:
                    case DirecotyQueryType.InfoMulti:
                        ProcessDirectoryQueryResponseUserDetails(tlvs);
                        break;
                    case DirecotyQueryType.Search:
                        break;
                }
            }
        }

        private void ProcessDirectoryQueryResponseUserDetails(TlvBlock tlvs)
        {
            FullUserInfo ui = new FullUserInfo();

            // Email
            ui.Email = tlvs.ReadString(0x50, Encoding.ASCII);       // Verified e-mail
            if (string.IsNullOrEmpty(ui.Email))
                ui.Email = tlvs.ReadString(0x55, Encoding.ASCII);   // Pending e-mail

            // Names
            ui.Screenname = tlvs.ReadString(0x32, Encoding.ASCII);
            ui.Firstname = tlvs.ReadString(0x64, Encoding.UTF8);
            ui.Lastname = tlvs.ReadString(0x6e, Encoding.UTF8);
            ui.Nickname = tlvs.ReadString(0x78, Encoding.UTF8);

            // Home Address
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0x96), ref ui);
            // Origin Address
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0xa0), ref ui);
            // Phone Numbers
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0xc8), ref ui);
            // Emails
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0x8c), ref ui);
            // Work
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0x118), ref ui);
            // Education
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0x10e), ref ui);
            // Interests
            ProcessDirectoryQueryResponseUserDetails(tlvs.ReadTlv(0x122), ref ui);

            ui.Timezone = tlvs.ReadSshort(0x17c, 0);

            switch (tlvs.ReadByte(0x82))
            {
                case 1: ui.Gender = 'F'; break;
                case 2: ui.Gender = 'M'; break;
                default: ui.Gender = '\0'; break;
            }

            ui.Website = tlvs.ReadString(0xfa, Encoding.ASCII);

            double birthday = tlvs.ReadDouble(0x1A4, 0);
            if (birthday != 0)
            {
                ui.Birthday = DateTime.FromOADate(birthday + 2);
                ui.Age = (byte)(DateTime.Now.Year - ui.Birthday.Year);
                if (ui.Birthday.DayOfYear > DateTime.Now.DayOfYear)
                    ui.Age--;
            }

            ui.Language1 = (LanguageList)tlvs.ReadUshort(0xaa, 0);
            ui.Language2 = (LanguageList)tlvs.ReadUshort(0xb4, 0);
            ui.Language3 = (LanguageList)tlvs.ReadUshort(0xbe, 0);

            ui.MaritalStatus = (MartialList)tlvs.ReadUshort(0x12c, 0);
            ui.About = tlvs.ReadString(0x186, Encoding.UTF8);

            ui.StatusNote = tlvs.ReadString(0x226, Encoding.UTF8);
            ui.PrivacyLevel = tlvs.ReadUshort(0x1f9, 0);
            ui.Auth = tlvs.ReadUshort(0x19a, 0);
            ui.WebAware = tlvs.ReadByte(0x212, 0);
            ui.AllowSpam = tlvs.ReadByte(0x1ea, 0);
            ui.CodePage = tlvs.ReadUshort(0x1c2, 0);

            if (FullUserInfoReceived != null)
                FullUserInfoReceived(this, ui);
        }

        private void ProcessDirectoryQueryResponseUserDetails(Tlv t, ref FullUserInfo ui)
        {
            List<TlvBlock> tlvlist = null;
            ByteStream bstream = new ByteStream(t.Data);

            switch (t.TypeNumber)
            {
                case 0x96:  // Home Address
                    tlvlist = readTlvList(bstream, 1);
                    break;
                case 0xa0:  // Origin Address
                    tlvlist = readTlvList(bstream, 1);
                    break;
                case 0xc8:  // Phone Numbers
                    tlvlist = readTlvList(bstream, 5);
                    break;
                case 0x8c:  // Emails
                    tlvlist = readTlvList(bstream, 4);
                    break;
                case 0x118: // Work
                    tlvlist = readTlvList(bstream, 1);
                    break;
                case 0x10e: // Education
                    tlvlist = readTlvList(bstream, 1);
                    break;
                case 0x122: // Interests
                    tlvlist = readTlvList(bstream, 4);
                    break;
            }

            if (tlvlist != null)
            {
                ushort count = 0;
                foreach (TlvBlock block in tlvlist)
                {
                    count++;
                    switch (t.TypeNumber)
                    {
                        case 0x96:  // Home Address
                            ui.HomeAddress = block.ReadString(0x64, Encoding.UTF8);
                            ui.HomeCity = block.ReadString(0x6e, Encoding.UTF8);
                            ui.HomeState = block.ReadString(0x78, Encoding.UTF8);
                            ui.HomeZip = block.ReadString(0x82, Encoding.UTF8);
                            ui.HomeCountry = (CountryList)block.ReadUint(0x8c, 0);
                            break;
                        case 0xa0:  // Origin Address
                            ui.OriginCity = block.ReadString(0x6e, Encoding.UTF8);
                            ui.OriginState = block.ReadString(0x78, Encoding.UTF8);
                            ui.OriginCountry = (CountryList)block.ReadUint(0x8c, 0);
                            break;
                        case 0xc8:  // Phone Numbers
                            switch (count)
                            {
                                case 1: ui.HomePhone = block.ReadString(0x64, Encoding.ASCII); break;
                                case 2: ui.HomeFax = block.ReadString(0x64, Encoding.ASCII); break;
                                case 3: ui.MobilePhone = block.ReadString(0x64, Encoding.ASCII); break;
                                case 4: ui.WorkPhone = block.ReadString(0x64, Encoding.ASCII); break;
                                case 5: ui.WorkFax = block.ReadString(0x64, Encoding.ASCII); break;
                                default: return;
                            }
                            break;
                        case 0x8c:  // Emails
                            string email = block.ReadString(0x64, Encoding.ASCII);
                            if (!string.IsNullOrEmpty(email))
                            {
                                if (ui.EmailAddresses == null)
                                    ui.EmailAddresses = new string[0];

                                Array.Resize<string>(ref ui.EmailAddresses, ui.EmailAddresses.Length + 1);
                                ui.EmailAddresses[ui.EmailAddresses.Length - 1] = email;
                            }
                            break;
                        case 0x118: // Work
                            ui.WorkPosition = block.ReadString(0x64, Encoding.UTF8);
                            ui.WorkCompany = block.ReadString(0x6e, Encoding.UTF8);
                            ui.WorkDepartment = block.ReadString(0x7d, Encoding.UTF8);
                            ui.WorkWebsite = block.ReadString(0x78, Encoding.ASCII);
                            ui.WorkIndustry = (IndustryList)block.ReadUshort(0x82, 0);

                            ui.WorkAddress = block.ReadString(0xaa, Encoding.UTF8);
                            ui.WorkCity = block.ReadString(0xb4, Encoding.UTF8);
                            ui.WorkState = block.ReadString(0xbe, Encoding.UTF8);
                            ui.WorkZip = block.ReadString(0xc8, Encoding.UTF8);
                            ui.WorkCountry = (CountryList)block.ReadUint(0xd2, 0);
                            break;
                        case 0x10e: // Education
                            ui.StudyLevel = (StudyLevelList)block.ReadUshort(0x64, 0);
                            ui.StudyInstitute = block.ReadString(0x6e, Encoding.UTF8);
                            ui.StudyDegree = block.ReadString(0x78, Encoding.UTF8);
                            ui.StudyYear = block.ReadUshort(0x8c, 0);
                            break;
                        case 0x122: // Interests
                            string info = block.ReadString(0x64, Encoding.UTF8);
                            ushort cat = block.ReadUshort(0x6e, 0);
                            if (!string.IsNullOrEmpty(info) || cat > 0)
                            {
                                if (ui.InterestInfos == null)
                                    ui.InterestInfos = new FullUserInfo.InterestInfo[0];

                                Array.Resize<FullUserInfo.InterestInfo>(ref ui.InterestInfos, ui.InterestInfos.Length + 1);

                                ui.InterestInfos[ui.InterestInfos.Length - 1].Info = info;
                                ui.InterestInfos[ui.InterestInfos.Length - 1].Category = (InterestList)cat;
                            }
                            break;
                    }
                }
            }
        }

        #endregion META_DIRECTORY_QUERY  -  META_DIRECTORY_RESPONSE


        private List<TlvBlock> readTlvList(ByteStream data, ushort max)
        {
            List<TlvBlock> tlvlist = new List<TlvBlock>();
            ushort count = data.ReadUshort();
            ushort size;

            for (int i = 0; i < count; i++)
            {
                if (i == max) break;
                if (data.RemainingBytes <= 2) break;

                size = data.ReadUshort();
                tlvlist.Add(new TlvBlock(data.ReadByteArray(size)));
            }

            return tlvlist;
        }

        private ByteStream writeTlvList(List<TlvBlock> tlvlist, ushort max)
        {
            ByteStream data = new ByteStream();
            ushort count = 0;

            data.WriteUshort((ushort)(tlvlist.Count > max ? max : tlvlist.Count));
            foreach (TlvBlock block in tlvlist)
            {
                if (count == max) break;
                count++;
              
                data.WriteUshort((ushort)block.GetByteCount());
                data.WriteByteArray(block.GetBytes());
            }

            return data;
        }

        #endregion User Details
    }
}