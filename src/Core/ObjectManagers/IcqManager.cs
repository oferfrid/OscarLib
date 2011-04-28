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
    /// Contains information concerning a client adding the locally logged in UIN to their list
    /// </summary>
    public class AddedToRemoteListEventArgs : EventArgs
    {
        private string uin;

        /// <summary>
        /// Initializes a new AddedToRemoteListEventArgs
        /// </summary>
        public AddedToRemoteListEventArgs(String uin)
        {
            this.uin = uin;
        }

        /// <summary>
        /// Gets the UIN of the user that added the locally logged in UIN to their list
        /// </summary>
        public String Uin
        {
            get { return uin; }
        }
    }

    /// <summary>
    /// Handles the event of a remote ICQ user adding the locally logged in UIN to their list
    /// </summary>
    public delegate void AddedToRemoteListEventHandler(object sender,
                                                       AddedToRemoteListEventArgs e);

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
        #region Constants
        /// <summary>
        /// ICQ code for the end of the offline message sequence
        /// </summary>
        private const int END_OFFLINE_MESSAGES = 0x0042;

        /// <summary>
        /// ICQ code for received meta-information pertaining to an ICQ account
        /// </summary>
        private const int ICQ_INFORMATION_RECEIVED = 0x0003;

        /// <summary>
        /// ICQ code for a received offline message
        /// </summary>
        private const int OFFLINE_MESSAGE = 0x0041;

        /// <summary>
        /// SSI code for a contact adding the current UIN to their list
        /// </summary>
        private const int SSI_ADDED_TO_LIST = 0x001C;
        #endregion

        #region Fields

		private readonly ISession parent;

        #endregion Fields

        #region Enums

        private enum MetaRequestType : ushort
        {
            MetaDataRequest = 0x07d0
        }

        private enum MetaRequestSubType : ushort
        {
            Unknown_Search = 0x0fa0
        }

        private enum MetaResponseType : ushort
        {
            /// <summary>
            /// ICQ code for a received response for a meta data request
            /// </summary>
            MetaDataReply = 0x07DA,

            /// <summary>
            /// ICQ code for a received offline message
            /// </summary>
            OfflineMessage = 0x0041,

            /// <summary>
            /// ICQ code for the end of the offline message sequence
            /// </summary>
            EndOfflineMessage = 0x0042
        }

        private enum MetaResponseSubType : ushort
        {
            MetaProcessError = 0x0001,

            ServerSmsResponse = 0x0096,

            SearchResult = 0x0faa,

            LastSearchResult = 0x0fb4
        }

        #endregion Enums

        #region Constructor

        /// <summary>
        /// Initializes a new IcqManager
        /// </summary>
		internal IcqManager(ISession parent)
        {
            this.parent = parent;
            this.parent.Dispatcher.RegisterSnacFamilyHandler(this, 0x0015);
        }

        #endregion Constructor

        #region ISnacFamilyHandler Members

        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 15 (and one from 13)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(DataPacket dp)
        {
            Debug.Assert(dp.SNAC.FamilyServiceID == 0x0015 || dp.SNAC.FamilyServiceID == 0x0013);
            switch (dp.SNAC.FamilySubtypeID)
            {
                case ICQ_INFORMATION_RECEIVED:
                    ProcessMetaInformationResponse(dp);
                    break;
                case SSI_ADDED_TO_LIST:
                    ProcessRemoteListAddition(dp);
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
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();
            return sh;
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
                            this.ProcessMetaDataReply(uin, stream);
                            break;
                    }
                }
            }
        }

        private void ProcessMetaDataReply(string uin, ByteStream data)
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
                default:
                    this.ProcessSearchResult(uin, responseSubType, data);
                    break;
            }     

        }

        #endregion Common Functions        

        #region Added to remote list handling

        /// <summary>
        /// Raised when a remote ICQ user adds the locally logged in UIN to their list
        /// </summary>
        public event AddedToRemoteListEventHandler AddedToRemoteList;

        private void ProcessRemoteListAddition(DataPacket dp)
        {
            String uin = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            if (AddedToRemoteList != null)
            {
                AddedToRemoteList(this, new AddedToRemoteListEventArgs(uin));
            }
        }

        #endregion Added to remote list handling

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
            sh.Flags = 0x0000;
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

            stream.WriteUshortLE(0x07D0);
            stream.WriteUshortLE((ushort)sh.RequestID);
            stream.WriteUshortLE(0x1482);

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

    }
}