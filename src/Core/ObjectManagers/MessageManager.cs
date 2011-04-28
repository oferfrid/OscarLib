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
using System.Text;
using csammisrun.OscarLib.Utility;
using System.Collections.ObjectModel;

namespace csammisrun.OscarLib
{
	/// <summary>
	/// Handles all incoming and outgoing peer-to-peer messages
	/// </summary>
	/// <remarks>
	/// <para>This manager is primarily responsible for handling the SNAC family 0x0004.
	/// The session's <see cref="IcqManager"/> and any <see cref="DirectIMConnection"/>s process their messages
	/// and dispatch them to this manager for distribution to the client, though the raw packets are
	/// processed elsewhere.</para>
	/// <para>Chat room messages are not handled by this manager.</para>
	/// </remarks>
	public class MessageManager : ISnacFamilyHandler
	{
		/// <summary>
		/// The SNAC family responsible for sending and receiving instant messages
		/// </summary>
		private const int SNAC_ICBM_FAMILY = 0x0004;

		#region SNAC subtype constants

		/// <summary>
		/// ICBM code for an incoming message
		/// </summary>
		private const int ICBM_INCOMING_MESSAGE = 0x0007;

		/// <summary>
		/// ICBM code for sending a warning to a remote client
		/// </summary>
		private const int ICBM_SEND_WARNING = 0x0008;

		/// <summary>
		/// ICBM code for an outgoing message
		/// </summary>
		private const int ICBM_OUTGOING_MESSAGE = 0x0006;

		/// <summary>
		/// ICBM code for a notification of delivered messages
		/// </summary>
		private const int ICBM_MESSAGE_DELIVERED = 0x000C;

		/// <summary>
		/// ICBM code for an error packet sent by the server
		/// </summary>
		private const int ICBM_ERROR = 0x0001;

		/// <summary>
		/// ICBM code for setting the SNAC family parameters
		/// </summary>
		private const int ICBM_SET_PARAMETERS = 0x0002;

		/// <summary>
		/// ICBM code for a received parameter list
		/// </summary>
		private const int ICBM_PARAMETER_LIST = 0x0005;

		/// <summary>
		/// ICBM code for requesting message parameters
		/// </summary>
		private const int ICBM_REQUEST_PARAMETERS = 0x0004;

		/// <summary>
		/// ICBM code for received typing notifications
		/// </summary>
		private const int ICBM_TYPING_NOTIFICATION = 0x0014;

		/// <summary>
		/// ICBM code for a notification of undeliverable messages
		/// </summary>
		private const int ICBM_UNDELIVERABLE_MESSAGE = 0x000A;

		/// <summary>
		/// ICBM code for the acknowledgement of a previously sent warning
		/// </summary>
		private const int ICBM_WARNING_ACKNOWLEDGED = 0x0009;

		/// <summary>
		/// ICBM code for requesting delivery of offline messages
		/// </summary>
		private const int ICBM_RETRIEVE_OFFLINE_MESSAGES = 0x0010;

		/// <summary>
		/// ICQ code for requesting delivery of offline messages
		/// </summary>
		private const int ICQ_RETRIEVE_OFFLINE_MESSAGES = 0x003C;

		/// <summary>
		/// ICQ code for deleting offline messages
		/// </summary>
		private const int ICQ_DELETE_OFFLINE_MESSAGES = 0x003E;

		/// <summary>
		/// ICBM code sent when the server has finished sending offline messages
		/// </summary>
		private const int ICBM_OFFLINE_MESSAGES_SENT = 0x0017;

		#endregion

		#region TLV type constants

		/// <summary>
		/// The client IP address of a direct connection
		/// </summary>
		private const int DC_CLIENT_IP_ADDRESS = 0x0003;

		/// <summary>
		/// The message to send with a direct connection invitation
		/// </summary>
		private const int DC_MESSAGE = 0x000C;

		/// <summary>
		/// The port of a direct connection
		/// </summary>
		private const int DC_PORT = 0x0005;

		/// <summary>
		/// The compliment of a direct connection's port
		/// </summary>
		private const int DC_PORT_COMPLIMENT = 0x0017;

		/// <summary>
		/// The proxy IP address of a direct connection
		/// </summary>
		private const int DC_PROXY_IP_ADDRESS = 0x0002;

		/// <summary>
		/// The compliment of a direct connection's proxy IP address
		/// </summary>
		private const int DC_PROXY_IP_ADDRESS_COMPLIMENT = 0x0016;

		/// <summary>
		/// The sequence number of a direct connection
		/// </summary>
		private const int DC_SEQUENCE_NUMBER = 0x000A;

		/// <summary>
		/// A flag for a direct connection to use a proxy
		/// </summary>
		private const int DC_USE_PROXY_FLAG = 0x0010;

		/// <summary>
		/// The verified IP address of a direct connection
		/// </summary>
		private const int DC_VERIFIED_IP_ADDRESS = 0x0004;

		/// <summary>
		/// A SNAC error code indicating that the recepient of a message is offline
		/// </summary>
		private const int ERROR_CODE_BUDDYOFFLINE = 0x0004;

		/// <summary>
		/// A SNAC error subcode indicating that the intended recepient of an offline message cannot accept them
		/// </summary>
		private const int ERROR_SUBCODE_OFFLINEMSGNOTSUPPORTED = 0x000E;

		/// <summary>
		/// A SNAC error subcode indicating that the intended recepient (?) of an offline message has exceeded their storage
		/// </summary>
		private const int ERROR_SUBCODE_OFFLINESTORAGEFULL = 0x000F;

		#endregion

		[Flags]
		enum ICBMParameters
		{
			/// <summary>
			/// No parameters are supported
			/// </summary>
			None = 0x00,
			/// <summary>
			/// Client wants basic ICBM messages
			/// </summary>
			Basic = 0x01,
			/// <summary>
			/// Client wants missed call (undeliverable message) notifications
			/// </summary>
			MissedCalls = 0x02,
			/// <summary>
			/// Client wants client event notifications
			/// </summary>
			ClientEvents = 0x08,
			/// <summary>
			/// Client wants to send SMS messages
			/// </summary>
			SMSMessaging = 0x10,
			/// <summary>
			/// Client supports offline messages
			/// </summary>
			OfflineMessaging = 0x100,
			/// <summary>
			/// A combination of all ICBM parameter flags
			/// </summary>
			All = Basic | MissedCalls | ClientEvents | SMSMessaging | OfflineMessaging
		}

		#region Events
		/// <summary>
		/// Occurs when a new message is received
		/// </summary>
		public event MessageReceivedHandler MessageReceived;

		/// <summary>
		/// Occurs when offline messages have been retrieved from the server
		/// </summary>
		public event OfflineMessagesReceivedEventHandler OfflineMessagesReceived;

		/// <summary>
		/// Occurs when the server sends a typing notification
		/// </summary>
		public event TypingNotificationEventHandler TypingNotification;

		/// <summary>
		/// Occurs when a message delivery update is available
		/// </summary>
		public event MessageDeliveryUpdateEventHandler MessageDeliveryUpdate;
		#endregion

		/// <summary>
		/// A map of message IDs and their destinations
		/// </summary>
		private Dictionary<uint, MessageStatusEventArgs> messageStatuses =
			new Dictionary<uint, MessageStatusEventArgs>();

		private bool isRetrievingOfflineMessages;
		/// <summary>
		/// The list of offline messages collected by this manager
		/// </summary>
		private readonly Dictionary<string, Collection<OfflineIM>> offlineMessages =
			new Dictionary<string, Collection<OfflineIM>>();

		private readonly ISession parent;

		/// <summary>
		/// Initializes a new MessageManager
		/// </summary>
		internal MessageManager(ISession parent)
		{
			this.parent = parent;
			parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_ICBM_FAMILY);
		}

		#region Public methods

		/// <summary>
		/// Sends an instant message
		/// </summary>
		/// <param name="screenName">The screenname to receive the IM</param>
		/// <param name="message">The message to send</param>
		/// <remarks>Delivery confirmation results or sending errors will be returned in the <see cref="MessageDeliveryUpdate"/> event.</remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		public Cookie SendMessage(String screenName, String message)
		{
			return SendMessage(screenName, message, OutgoingMessageFlags.None, null);
		}

		/// <summary>
		/// Sends an instant message
		/// </summary>
		/// <param name="screenName">The screenname to receive the IM</param>
		/// <param name="message">The message to send</param>
		/// <param name="flags">A <see cref="OutgoingMessageFlags"/> enumeration specifying what
		/// additional information should be sent with the message</param>
		/// <remarks>Delivery confirmation results or sending errors will be returned in the <see cref="MessageDeliveryUpdate"/> event.</remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		public Cookie SendMessage(String screenName, String message, OutgoingMessageFlags flags)
		{
			return SendMessage(screenName, message, flags, null);
		}

		/// <summary>
		/// Sends an instant message
		/// </summary>
		/// <param name="screenName">The screenname to receive the IM</param>
		/// <param name="message">The message to send</param>
		/// <param name="flags">A <see cref="OutgoingMessageFlags"/> enumeration specifying what
		/// additional information should be sent with the message</param>
		/// <param name="attachments">A list of <see cref="Attachment"/>s to send with the message</param>
		/// <remarks>
		/// <para>Delivery confirmation results or sending errors will be returned in the <see cref="MessageDeliveryUpdate"/> event.</para>
		/// <para>If any attachments are specified in the <paramref name="attachments"/> list, this method will
		/// attempt to open a Direct Connection, a peer-to-peer connection that could expose the local
		/// IP address to the other participant in the conversation.  If a Direct Connection cannot be established,
		/// this method will not attempt to send the message without the attachments.  The <see cref="ISession.DirectIMSessionCancelled"/>
		/// event will be raised, and the client should attempt to send the message without attachments manually.</para></remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		public Cookie SendMessage(String screenName, String message, OutgoingMessageFlags flags,
		                          List<Attachment> attachments)
		{
			if (!parent.LoggedIn)
			{
				throw new NotLoggedInException();
			}

			// See if there is an existing Direct Connection for this screen name.
			// If there is, send the message by it.  If there isn't and there are
			// attachments to send along, create a new DC
			DirectIMConnection conn = parent.Connections.GetDirectIMByScreenname(screenName);
			if (conn != null || (attachments != null && attachments.Count > 0))
			{
				// Make sure the connection hasn't been disconnected on the other side
				if (conn != null && conn.Connected == false)
				{
					// If there are no attachments, it can be sent through the server,
					// otherwise the DC will attempt reconnection
					if (attachments == null || attachments.Count == 0)
					{
						// No attachments, just send it through the server
						return SendMessageThroughServer(screenName, message, flags);
					}
				}

				if (conn == null)
				{
					conn = parent.Connections.CreateNewDirectIMConnection(DirectConnectionMethod.Direct,
					                                                      DirectConnectRole.Initiator);
					RequestDirectConnection(conn);
				}

				DirectIM dim = new DirectIM(screenName, conn);
				dim.Cookie = Cookie.CreateCookieForSending();
				dim.Attachments = attachments;
				dim.Message = message;
				dim.IsAutoResponse = (flags & OutgoingMessageFlags.AutoResponse) != 0;
				conn.SendMessage(dim);

				return dim.Cookie;
			}
			else
			{
				return SendMessageThroughServer(screenName, message, flags);
			}
		}

		/// <summary>
		/// Begin retrieving any offline messages stored on the server
		/// </summary>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		public void RetrieveOfflineMessages()
		{
			if (!parent.LoggedIn)
			{
				throw new NotLoggedInException();
			}

			lock (this)
			{
				if (isRetrievingOfflineMessages)
				{
					return;
				}
				isRetrievingOfflineMessages = true;
			}

			SNACHeader sh = null;
			ByteStream stream = null;
			// Decide whether we should use the AIM or ICQ method
			if (ScreennameVerifier.IsValidICQ(parent.ScreenName))
			{
				// Build an ICQ packet to request the offline messages
				sh = IcqManager.CreateIcqMetaHeader();
				stream = IcqManager.BeginIcqByteStream(parent.ScreenName);
				stream.WriteUshortLE(ICQ_RETRIEVE_OFFLINE_MESSAGES);
				stream.WriteUshortLE((ushort)sh.RequestID);
			}
			else if (ScreennameVerifier.IsValidAIM(parent.ScreenName))
			{
				// Build SNAC(04,10)
				sh = new SNACHeader();
				sh.FamilyServiceID = SNAC_ICBM_FAMILY;
				sh.FamilySubtypeID = ICBM_RETRIEVE_OFFLINE_MESSAGES;
				sh.Flags = 0x0000;
				sh.RequestID = Session.GetNextRequestID();
				stream = new ByteStream();
			}
			else
			{
				// What?
				isRetrievingOfflineMessages = false;
				return;
			}

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Sends a typing notification
		/// </summary>
		/// <param name="screenName">The screenname to receive the typing notification</param>
		/// <param name="tn">A <see cref="TypingNotification"/> enumeration specifying what
		/// notification to send</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		public void SendTypingNotification(string screenName, TypingNotification tn)
		{
			if (!parent.LoggedIn)
			{
				throw new NotLoggedInException();
			}

			DirectIMConnection conn = parent.Connections.GetDirectIMByScreenname(screenName);
			if (conn != null)
			{
				if (conn.Connected)
				{
					// TODO:  send DIM typing notifications
				}
			}

			// Construct SNAC(04,14)
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_TYPING_NOTIFICATION;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			ByteStream stream = new ByteStream();
			stream.WriteByteArray(new byte[] {0, 0, 0, 0, 0, 0, 0, 0});
			stream.WriteUshort(0x0001);
			stream.WriteByte((byte) Encoding.ASCII.GetByteCount(screenName));
			stream.WriteString(screenName, Encoding.ASCII);
			stream.WriteUshort((ushort) tn);

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Invites an AIM user to an AOL chatroom
		/// </summary>
		/// <param name="chatroom">The <see cref="ChatRoom"/> describing the chatroom</param>
		/// <param name="screenName">The screenname of the user to invite</param>
		/// <param name="message">A message to send along with the invitation</param>
		public Cookie InviteToChatRoom(ChatRoom chatroom, string screenName, string message)
		{
			if (!parent.LoggedIn)
			{
				throw new NotLoggedInException();
			}

			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_OUTGOING_MESSAGE;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			Cookie cookie = Cookie.CreateCookieForSending();

			Encoding enc = Encoding.ASCII;
			byte destlength = (byte) enc.GetByteCount(screenName);
			ushort messagelength = (ushort) enc.GetByteCount(message);
			byte roomnamelength = (byte) enc.GetByteCount(chatroom.FullName);

			ByteStream stream = new ByteStream();
			InsertIcbmHeader(stream, cookie, 0x0002, screenName);

			ByteStream header = new ByteStream();
			header.WriteUshort(0x0000);
			header.WriteByteArray(cookie.ToByteArray());
			header.WriteUint(0x748F2420);
			header.WriteUint(0x628711D1);
			header.WriteUint(0x82224445);
			header.WriteUint(0x53540000);

			using (TlvBlock tlv05 = new TlvBlock())
			{
				tlv05.WriteUshort(0x000A, 0x0001);
				tlv05.WriteEmpty(0x000F);
				tlv05.WriteString(0x000C, message, enc);

				ByteStream tlv2711 = new ByteStream();
				tlv2711.WriteUshort(chatroom.Exchange);
				tlv2711.WriteByte(roomnamelength);
				tlv2711.WriteString(chatroom.FullName, enc);
				tlv2711.WriteUshort(chatroom.Instance);

				tlv05.WriteByteArray(0x2711, tlv2711.GetBytes());

				header.WriteByteArray(tlv05.GetBytes());
			}

			stream.WriteUshort(0x0005);
			stream.WriteUshort((ushort) header.GetByteCount());
			stream.WriteByteArray(header.GetBytes());

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));

			return cookie;
		}

		/// <summary>
		/// Sends a direct connection request
		/// </summary>
		/// <param name="conn">A <see cref="DirectConnection"/> object that will handle the request</param>
		public void RequestDirectConnection(DirectConnection conn)
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_OUTGOING_MESSAGE;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			ByteStream stream = new ByteStream();
			InsertIcbmHeader(stream, conn.Cookie, 0x0002, conn.Other.ScreenName);
			using (ByteStream tlv05 = new ByteStream())
			{
				tlv05.WriteUshort(RendezvousData.UshortFromType(RendezvousType.Invite));
				tlv05.WriteByteArray(conn.Cookie.ToByteArray());
				tlv05.WriteByteArray(CapabilityProcessor.GetCapabilityArray(conn.Capability));

				using (TlvBlock tlvs = new TlvBlock())
				{
					tlvs.WriteUshort(DC_SEQUENCE_NUMBER, RendezvousData.UshortFromSequence(conn.Sequence));
					if (conn.Sequence == RendezvousSequence.DirectOrStage1)
					{
						tlvs.WriteEmpty(0x000F);
					}
					if (!String.IsNullOrEmpty(conn.Message))
					{
						tlvs.WriteString(DC_MESSAGE, conn.Message, Encoding.ASCII);
					}

					uint ipaddress = 0;
					if (conn.Method == DirectConnectionMethod.Proxied)
					{
						ipaddress = ConvertIPAddress(conn.ProxyIP);
					}
					else
					{
						ipaddress = ConvertIPAddress(conn.ClientIP);
					}

					tlvs.WriteUint(DC_PROXY_IP_ADDRESS, ipaddress);
					tlvs.WriteUint(DC_PROXY_IP_ADDRESS_COMPLIMENT, ~ipaddress);

					if (conn.Sequence == RendezvousSequence.DirectOrStage1)
					{
						tlvs.WriteUint(DC_CLIENT_IP_ADDRESS, ConvertIPAddress(conn.ClientIP));
					}

					if (conn.Sequence != RendezvousSequence.Stage3)
					{
						tlvs.WriteUshort(DC_PORT, (ushort)conn.Port);
						tlvs.WriteUshort(DC_PORT_COMPLIMENT, (ushort)(~conn.Port));
					}

					if (conn.Method == DirectConnectionMethod.Proxied)
					{
						tlvs.WriteEmpty(DC_USE_PROXY_FLAG);
					}

					if (conn is FileTransferConnection && conn.Sequence == RendezvousSequence.DirectOrStage1)
					{
						FileTransferConnection ftc = conn as FileTransferConnection;
						using (ByteStream tlv2711 = new ByteStream())
						{
							tlv2711.WriteUshort((ushort)((ftc.TotalFiles > 1) ? 0x0002 : 0x0001));
							tlv2711.WriteUshort((ushort)ftc.TotalFiles);
							tlv2711.WriteUint(ftc.TotalFileSize);
							tlv2711.WriteString(ftc.FileHeader.Name, Encoding.ASCII);
							tlv2711.WriteByte(0x00);
							tlvs.WriteByteArray(0x2711, tlv2711.GetBytes());
						}
						tlvs.WriteString(0x2712, ftc.LocalFileNameEncoding.WebName, Encoding.ASCII);
					}

					tlv05.WriteByteArray(tlvs.GetBytes());
				}

				stream.WriteUshort(0x0005);
				stream.WriteUshort((ushort)tlv05.GetByteCount());
				stream.WriteByteArray(tlv05.GetBytes());
			}

			// Acknowledgement request
			stream.WriteUint(0x00030000);

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Cancel a direct connection attempt
		/// </summary>
		public void SendDirectConnectionCancellation(DirectConnection conn, string reason)
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_OUTGOING_MESSAGE;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			ByteStream stream = new ByteStream();
			InsertIcbmHeader(stream, conn.Cookie, 0x0002, conn.Other.ScreenName);
			using (ByteStream tlv05 = new ByteStream())
			{
				tlv05.WriteUshort(RendezvousData.UshortFromType(RendezvousType.Cancel));
				tlv05.WriteByteArray(conn.Cookie.ToByteArray());
				tlv05.WriteByteArray(CapabilityProcessor.GetCapabilityArray(conn.Capability));

				using (TlvBlock tlvs = new TlvBlock())
				{
					tlvs.WriteUshort(0x000B, 0x0001);
					tlvs.WriteString(0x000C, reason, Encoding.ASCII);
					tlvs.WriteEmpty(0x0003);
					tlv05.WriteByteArray(tlvs.GetBytes());
				}

				stream.WriteUshort(0x0005);
				stream.WriteUshort((ushort)tlv05.GetByteCount());
				stream.WriteByteArray(tlv05.GetBytes());
			}

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Accept a direct connection attempt
		/// </summary>
		public void SendDirectConnectionAccept(DirectConnection conn)
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_OUTGOING_MESSAGE;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			ByteStream stream = new ByteStream();
			InsertIcbmHeader(stream, conn.Cookie, 0x0002, conn.Other.ScreenName);
			using (ByteStream tlv05 = new ByteStream())
			{
				tlv05.WriteUshort(RendezvousData.UshortFromType(RendezvousType.Accept));
				tlv05.WriteByteArray(conn.Cookie.ToByteArray());
				tlv05.WriteByteArray(CapabilityProcessor.GetCapabilityArray(conn.Capability));
				tlv05.WriteUint(0x00030000);

				stream.WriteUshort(0x0005);
				stream.WriteUshort((ushort)tlv05.GetByteCount());
				stream.WriteByteArray(tlv05.GetBytes());
			}

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Sends a warning to a remote client -- SNAC(04,08)
		/// </summary>
		/// <param name="screenname">The screenname of the client to warn</param>
		/// <param name="anonymous">Send the warning as anonymous or as yourself</param>
		public void SendWarning(string screenname, bool anonymous)
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_SEND_WARNING;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			ByteStream stream = new ByteStream();
			stream.WriteUshort((ushort)((anonymous) ? 1 : 0));
			stream.WriteByte((byte)Encoding.ASCII.GetByteCount(screenname));
			stream.WriteString(screenname, Encoding.ASCII);

			// Save the screenname so the handler for the reply SNAC can retrieve it
			parent.StoreRequestID(sh.RequestID, screenname);

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		/// <summary>
		/// Sends a request for parameter information -- SNAC(04,04)
		/// </summary>
		internal void RequestParametersList()
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_REQUEST_PARAMETERS;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, new ByteStream()));
		}

		/// <summary>
		/// Sets ICBM parameters to more reasonable values than the defaults -- SNAC(04,02)
		/// </summary>
		private void UpdateParameters()
		{
			// Construct the SNAC header
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_SET_PARAMETERS;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			// Put the ICBM parameters into a byte array
			ByteStream stream = new ByteStream();
			// Channel to setup -- should always be 0
			stream.WriteUshort(0x0000);
			// Message flags
			stream.WriteUint((uint)ICBMParameters.All);
			// Maximum message size
			parent.Limits.MaxMessageSize = 8000;
			// Maximum sender evil level
			parent.Limits.MaxSenderWarningLevel = 999;
			// Maximum receiver evil level
			parent.Limits.MaxReceiverWarningLevel = 999;
			// Send the limits to the server
			stream.WriteUshort(parent.Limits.MaxMessageSize);
			stream.WriteUshort(parent.Limits.MaxSenderWarningLevel);
			stream.WriteUshort(parent.Limits.MaxReceiverWarningLevel);
			// Minimum message interval, in seconds
			stream.WriteUint(0x00000000);

			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
		}

		#endregion

		#region ISnacFamilyHandler Members

		/// <summary>
		/// Process an incoming <see cref="DataPacket"/> from SNAC family 4
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
		public void ProcessIncomingPacket(DataPacket dp)
		{
			switch (dp.SNAC.FamilySubtypeID)
			{
				case ICBM_ERROR:
					ProcessIcbmError(dp);
					break;
				case ICBM_PARAMETER_LIST:
					ProcessParametersList(dp);
					break;
				case ICBM_INCOMING_MESSAGE:
					ProcessMessage(dp);
					break;
				case ICBM_WARNING_ACKNOWLEDGED:
					ProcessWarningAcknowledgement(dp);
					break;
				case ICBM_UNDELIVERABLE_MESSAGE:
					ProcessUndeliverableMessage(dp);
					break;
				case ICBM_MESSAGE_DELIVERED:
					ProcessServerAcknowledgement(dp);
					break;
				case ICBM_TYPING_NOTIFICATION:
					ProcessTypingNotification(dp);
					break;
				case ICBM_OFFLINE_MESSAGES_SENT:
					ProcessOfflineMessagesSent(dp);
					break;
			}
		}

		#endregion

		#region Handlers
		/// <summary>
		/// Proceses an error packet -- SNAC(04,01)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,01)</param>
		private void ProcessIcbmError(DataPacket dp)
		{
			if (messageStatuses.ContainsKey(dp.SNAC.RequestID))
			{
				MessageStatusEventArgs e = messageStatuses[dp.SNAC.RequestID];
				messageStatuses.Remove(dp.SNAC.RequestID);
				e.Status = MessageStatus.UnknownError;

				ushort errorcode, subcode;
				SNACFunctions.GetSNACErrorCodes(dp, out errorcode, out subcode);

				switch(errorcode)
				{
					case ERROR_CODE_BUDDYOFFLINE:
						e.Status = MessageStatus.BuddyOffline;
						break;
				}

				switch (subcode)
				{
					case ERROR_SUBCODE_OFFLINEMSGNOTSUPPORTED:
						e.Status = MessageStatus.OfflineMessagesNotSupported;
						break;
					case ERROR_SUBCODE_OFFLINESTORAGEFULL:
						e.Status = MessageStatus.OfflineStorageFull;
						break;
				}

				if (MessageDeliveryUpdate != null)
				{
					MessageDeliveryUpdate(this, e);
				}
			}
		}

		/// <summary>
		/// Processes the parameter information sent by the server -- SNAC(04,05)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,05)</param>
		private void ProcessParametersList(DataPacket dp)
		{
			ushort max_channel = dp.Data.ReadUshort();
			uint flags = dp.Data.ReadUint();
			ushort snac_size = dp.Data.ReadUshort();
			ushort sender_evil = dp.Data.ReadUshort();
			ushort receiver_evil = dp.Data.ReadUshort();
			ushort message_interval = dp.Data.ReadUshort();
			ushort unknown_data = dp.Data.ReadUshort();

			// TODO:  Do something with these capabilities?
			parent.ParameterSetArrived();

			// Send SNAC(04,02) to rid ourselves of these foolish default settings
			UpdateParameters();
		}

		/// <summary>
		/// Processes the acknowledgement of a warning sent by the server -- SNAC(04,09)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,09)</param>
		private void ProcessWarningAcknowledgement(DataPacket dp)
		{
			ushort warning_level_gain = dp.Data.ReadUshort();
			ushort new_warning_level = dp.Data.ReadUshort();
			// TODO:  We don't really do anything with this
			//parent.OnWarningAcknowledgement(dp.SNAC.RequestID, warning_level_gain, new_warning_level);
		}

		/// <summary>
		/// Processes an undeliverable message notification sent by the server -- SNAC(04,0A)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,0A)</param>
		private void ProcessUndeliverableMessage(DataPacket dp)
		{
			while (dp.Data.HasMoreData)
			{
				ushort messagechannel = dp.Data.ReadUshort();
				UserInfo ui = dp.Data.ReadUserInfo();
				ushort numbermissed = dp.Data.ReadUshort();
				UndeliverableMessageReason reason = (UndeliverableMessageReason) dp.Data.ReadUshort();
			}
		}

		/// <summary>
		/// Processes an "message accepted" notification sent by the server -- SNAC(04,0C)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,0C)</param>
		private void ProcessServerAcknowledgement(DataPacket dp)
		{
			if (messageStatuses.ContainsKey(dp.SNAC.RequestID))
			{
				MessageStatusEventArgs e = messageStatuses[dp.SNAC.RequestID];
				messageStatuses.Remove(dp.SNAC.RequestID);
				e.Status = MessageStatus.AcceptedForDelivery;
				if (MessageDeliveryUpdate != null)
				{
					MessageDeliveryUpdate(this, e);
				}
			}
		}

		/// <summary>
		/// Processes a typing notification sent by the server -- SNAC(04,14)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,14)</param>
		private void ProcessTypingNotification(DataPacket dp)
		{
			byte[] cookie = dp.Data.ReadByteArray(8);
			ushort notification_channel = dp.Data.ReadUshort();
			string screenname = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
			TypingNotification tn = (TypingNotification) dp.Data.ReadUshort();

			if (TypingNotification != null)
			{
				TypingNotification(this, new TypingNotificationEventArgs(screenname, tn));
			}
		}

		/// <summary>
		/// Processes a notification that all offline messages have been retrieved -- SNAC(04,17)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,17), always empty</param>
		private void ProcessOfflineMessagesSent(DataPacket dp)
		{
			EndOfflineMessageSequence(parent.ScreenName, false);
		}

		#region SNAC(04,07) -- Receive ICBM

		/// <summary>
		/// Processes an ICBM message sent by the server -- SNAC(04,07)
		/// </summary>
		/// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(04,07)</param>
		private void ProcessMessage(DataPacket dp)
		{
			// Pull apart the fixed part of the message
			byte[] message_cookie = dp.Data.ReadByteArray(8);
			ushort channel = dp.Data.ReadUshort();
			UserInfo ui = dp.Data.ReadUserInfo();

			IM message = new IM(ui);
			message.Cookie = Cookie.GetReceivedCookie(message_cookie);

			// End of the fixed part. Pull apart the channel-specific data
			switch (channel)
			{
				case 0x0001:
					ProcessChannelOneMessage(dp.Data, ui);
					break;
				case 0x0002:
					ProcessChannelTwoMessage(dp.Data, ui);
					break;
				case 0x0004: //SMS From ISRAEL
					ProcessChannelFourMessage(dp.Data, ui);
					break;
				default:
					parent.OnWarning(ServerErrorCode.UnknownMessageChannel);
					break;
			}
		}

		#region SNAC(04,07):01

		/// <summary>
		/// Processes an incoming ICBM message on channel 1 -- SNAC(04,07)
		/// </summary>
		/// <param name="stream">A received <see cref="ByteStream"/></param>
		/// <param name="ui">The UserInfo block that came with this message</param>
		private void ProcessChannelOneMessage(ByteStream stream, UserInfo ui)
		{
			IM message = new IM(ui);
			using (TlvBlock tlvs = new TlvBlock(stream.ReadByteArrayToEnd()))
			{
				message.IsAutoResponse = tlvs.HasTlv(0x0004);
				// If this message was received offline, cast it to an OfflineIM
				if (tlvs.HasTlv(0x0006))
				{
					message = new OfflineIM(message);
					if (tlvs.HasTlv(0x0016))
					{
						((OfflineIM)message).ReceivedOn = tlvs.ReadDateTime(0x0016);
					}
				}
				GetChannelOneMessage(new ByteStream(tlvs.ReadByteArray(0x0002)), ref message);
			}

			// Figure out what to do with it
			if (message is OfflineIM)
			{
				OfflineIM offlineMessage = message as OfflineIM;
				if (isRetrievingOfflineMessages)
				{
					// Queue it for delivery
					AcceptIcbmOIM(offlineMessage);
				}
				else
				{
					// A single offline message?  Okay then
					if (OfflineMessagesReceived != null)
					{
						List<OfflineIM> tmpList = new List<OfflineIM>(1);
						tmpList.Add(offlineMessage);
						OfflineMessagesReceived(this, new OfflineMessagesReceivedEventArgs(parent.ScreenName,
						                                                                   new Collection<OfflineIM>(tmpList)));
					}
				}
				// Offline messages don't get delivered via OnMessageReceived - if the offline messages event
				// isn't hooked up, tough stuff.
				return;
			}
			else
			{
				OnMessageReceived(message);
			}
		}

		/// <summary>
		/// Raises the <see cref="MessageReceived"/> event
		/// </summary>
		/// <param name="message">The <see cref="IM"/> that was received</param>
		protected internal void OnMessageReceived(IM message)
		{
			// Check to see if there is a DirectIM connection that has been superceded by this message
			DirectIMConnection conn = parent.Connections.GetDirectIMByScreenname(message.ScreenName);
			if (conn != null && !conn.Connected)
			{
				parent.Connections.RemoveDirectConnection(conn.Cookie);
				parent.OnDirectIMSessionClosed(conn.Other, conn.Cookie);
				conn = null;
			}

			// Alert the user of the message
			if (MessageReceived != null)
			{
				MessageReceived(this, new MessageReceivedEventArgs(message));
			}
		}

		/// <summary>
		/// Retrieves the message text from SNAC(04,07) TLV 02
		/// </summary>
		/// <param name="stream">A received <see cref="ByteStream"/></param>
		/// <param name="message">An <see cref="IM"/> object to be populated</param>
		private void GetChannelOneMessage(ByteStream stream, ref IM message)
		{
			while (stream.HasMoreData)
			{
				byte identifier = stream.ReadByte();
				byte version = stream.ReadByte();
				ushort length = stream.ReadUshort();
				switch (identifier)
				{
					case 0x01: // Message text
						ushort charset = stream.ReadUshort();
						ushort charsubset = stream.ReadUshort();
						Encoding incoming = IM.GetEncodingFromCharset(charset, charsubset);
						message.Message = Encoding.Unicode.GetString(
							Encoding.Convert(incoming, Encoding.Unicode, stream.ReadByteArray(length - 4)));

						break;
					default: // Unhandled case
						stream.AdvanceOffset(length);
						break;
				}
			}
		}

		#endregion

		#region SNAC(04,07):02

		/// <summary>
		/// Process a ICBM that was sent over channel two - rich text messages, chat/filetransfer invites, buddy icons
		/// </summary>
		private void ProcessChannelTwoMessage(ByteStream stream, UserInfo ui)
		{
			TlvBlock tlvs = new TlvBlock(stream.ReadByteArrayToEnd());
			ushort errorCode = tlvs.ReadUshort(0x000B);
			DirectConnection conn = ProcessChannel2Tlv05(new ByteStream(tlvs.ReadByteArray(0x0005)), ui);
		}

		/// <summary>
		/// Processes the inner TLV list in TLV 0x0005 and returns a new DirectConnection
		/// </summary>
		private DirectConnection ProcessChannel2Tlv05(ByteStream stream, UserInfo ui)
		{
			byte[] invitemessage;
			Encoding encoding = Encoding.ASCII;
			string language = "en";

			DirectConnection directconn = null;

			// Pull the type, cookie, and capability array
			RendezvousType rtype = RendezvousData.TypeFromUshort(stream.ReadUshort());
			Cookie cookie = Cookie.GetReceivedCookie(stream.ReadByteArray(8));
			byte[] capabilitiesArray = stream.ReadByteArray(16);
			Capabilities capabilities = CapabilityProcessor.ProcessCLSIDList(capabilitiesArray);

			// Create the correct type of connection based on the capability
			if (capabilities == Capabilities.SendFiles)
			{
				if ((directconn = parent.Connections.GetDirectConnectionByCookie(cookie)) == null)
				{
					directconn = parent.Connections.CreateNewFileTransferConnection(
						DirectConnectionMethod.Direct, DirectConnectRole.Receiver);
				}
			}
			else if (capabilities == Capabilities.DirectIM)
			{
				if ((directconn = parent.Connections.GetDirectConnectionByCookie(cookie)) == null)
				{
					directconn = parent.Connections.CreateNewDirectIMConnection(
						DirectConnectionMethod.Direct, DirectConnectRole.Receiver);
				}
			}
			else if (capabilities == Capabilities.Chat)
			{
				directconn = parent.Connections.CreateNewChatInvitationConnection(DirectConnectRole.Receiver);
			}
			else
			{
				// Currently unsupported
				parent.OnWarning(ServerErrorCode.UnknownRendezvousChannel);
				return null;
			}

			directconn.Other = ui;
			directconn.Cookie = cookie;
			directconn.Type = rtype;

			ByteStream serviceData = null;
			Encoding serviceDataEncoding = Encoding.ASCII;

			// Process the inner TLV list
			using (TlvBlock tlvs = new TlvBlock(stream.ReadByteArrayToEnd()))
			{
				directconn.ProxyIP = tlvs.ReadIPAddress(DC_PROXY_IP_ADDRESS);
				directconn.ClientIP = tlvs.ReadIPAddress(DC_CLIENT_IP_ADDRESS);
				directconn.VerifiedIP = tlvs.ReadIPAddress(0x0004);
				directconn.Port = tlvs.ReadUshort(DC_PORT);
				directconn.Sequence = RendezvousData.SequenceFromUshort(tlvs.ReadUshort(DC_SEQUENCE_NUMBER));
				invitemessage = tlvs.ReadByteArray(DC_MESSAGE);
				if (tlvs.HasTlv(0x000D))
				{
					encoding = Encoding.GetEncoding(tlvs.ReadString(0x000D, Encoding.ASCII));
				}
				language = tlvs.ReadString(0x000E, Encoding.ASCII);
				directconn.Method = (tlvs.HasTlv(DC_USE_PROXY_FLAG))
					?
					DirectConnectionMethod.Proxied
					: DirectConnectionMethod.Direct;

				serviceData = new ByteStream(tlvs.ReadByteArray(0x2711));
				if (tlvs.HasTlv(0x2712))
				{
					serviceDataEncoding = Encoding.GetEncoding(tlvs.ReadString(0x2712, Encoding.ASCII));
				}
			}

			if (invitemessage != null)
			{
				directconn.Message = encoding.GetString(invitemessage, 0, invitemessage.Length);
			}

			// Process the extra data, if necessary
			if (directconn is FileTransferConnection || directconn is DirectIMConnection)
			{
				ProcessDirectConnectionRequest(directconn, serviceData);
			}
			else if (directconn is ChatInvitationConnection)
			{
				ChatInvitationConnection cic = directconn as ChatInvitationConnection;
				cic.ChatInvite = new ChatInvitation();
				cic.ChatInvite.Message = directconn.Message;
				cic.ChatInvite.Encoding = encoding;
				cic.ChatInvite.Language = language;
				ProcessChatInvitationRequest(cic, serviceData);
			}

			return directconn;
		}

		/// <summary>
		/// Performs processing on the 0x2711 TLV of a chat invitation request
		/// </summary>
		private void ProcessChatInvitationRequest(ChatInvitationConnection conn, ByteStream serviceData)
		{
			if (conn.Type == RendezvousType.Accept)
			{
				// Accepting chat invitation. Does this ever get received?
			}
			else if (conn.Type == RendezvousType.Cancel)
			{
				// Cancelling chat invitation. Jerks.
			}
			else if (conn.Type == RendezvousType.Invite && serviceData.HasMoreData)
			{
				conn.ChatRoom = new ChatRoom(serviceData);
				parent.ChatRooms.CacheChatRoomInvitation(conn.Cookie, conn.ChatRoom);

				parent.OnChatInvitationReceived(conn.Other, conn.ChatRoom.DisplayName,
				                                conn.ChatInvite.Message, conn.ChatInvite.Encoding,
				                                conn.ChatInvite.Language,
				                                conn.Cookie);
			}
		}

		/// <summary>
		/// Performs TLV 0x2711 processing for direct connect (sendfiles, DirectIM) negotiation
		/// </summary>
		private void ProcessDirectConnectionRequest(DirectConnection conn, ByteStream stream)
		{
			if (conn.Type == RendezvousType.Accept)
			{
				// They're accepting, which means we're going to get a connection on the
				// listener socket set up in FileTransferManager. Do nothing here
			}
			else if (conn.Type == RendezvousType.Cancel)
			{
				DirectConnection cancelled = parent.Connections.GetDirectConnectionByCookie(conn.Cookie);

				if (cancelled != null)
				{
					if (cancelled is FileTransferConnection)
					{
						(cancelled as FileTransferConnection).CancelFileTransfer(
							"Remote user cancelled direct connection");
					}
					else if (cancelled is DirectIMConnection)
					{
						parent.OnDirectIMSessionCancelled(cancelled, "Remote user cancelled direct connection");
					}
				}

				return;
			}
			else if (conn.Type == RendezvousType.Invite)
			{
				// AIM sends a type 0x0000 when accepting...huh.

				if (conn.Sequence == RendezvousSequence.DirectOrStage1)
				{
					if (stream.HasMoreData && conn is FileTransferConnection)
					{
						FileTransferConnection ftconn = conn as FileTransferConnection;
						ftconn.SubType = stream.ReadUshort();
						ftconn.TotalFiles = stream.ReadUshort();
						ftconn.TotalFileSize = stream.ReadUint();

						int strlen = 0;
						byte[] servicedata = stream.ReadByteArrayToEnd();
						// The filename in an RD invite is null-terminated ASCII
						while (servicedata[strlen++] != 0x00) ;
						ftconn.FileHeader.Name = Encoding.ASCII.GetString(servicedata, 0, strlen - 1);
					}

					parent.OnDirectConnectionRequestReceived(conn.Cookie);
				}
				else if (conn.Sequence == RendezvousSequence.Stage2)
				{
					// The receipient of a previous invite wants a stage 2 proxy redirection
					// Shut down the server socket, we won't be getting a connection on it
					conn.StopListeningSocket();
					SendDirectConnectionAccept(conn);
					conn.StartSendThroughStage2Proxy();
				}
				else if (conn.Sequence == RendezvousSequence.Stage3)
				{
					// Direct connection and proxy 2 failed, the sender's trying to proxy it now
					conn.Method = DirectConnectionMethod.Proxied;
					conn.ConnectToServer();
				}
			}
		}

		// End region SNAC(04,07):02

		#endregion

		#region SNAC(04,07):04

		/// <summary>
		/// Processes an incoming ICBM message on channel 4 (SMS response from ISRAEL)-- SNAC(04,07)
		/// </summary>
		/// <param name="stream">A received <see cref="ByteStream"/></param>
		/// <param name="ui">The UserInfo block that came with this message</param>
		private void ProcessChannelFourMessage(ByteStream stream, UserInfo ui)
		{
			IM message = new IM(ui);
			using (TlvBlock tlvs = new TlvBlock(stream.ReadByteArrayToEnd()))
			{
				message.IsAutoResponse = tlvs.HasTlv(0x0004);
				// If this message was received offline, cast it to an OfflineIM
				if (tlvs.HasTlv(0x0006))
				{
					message = new OfflineIM(message);
					if (tlvs.HasTlv(0x0016))
					{
						((OfflineIM)message).ReceivedOn = tlvs.ReadDateTime(0x0016);
					}
				}
				GetChannelfourMessage(new ByteStream(tlvs.ReadByteArray(0x0005)), ref message);
			}

			// Figure out what to do with it
			if (message is OfflineIM)
			{
				OfflineIM offlineMessage = message as OfflineIM;
				if (isRetrievingOfflineMessages)
				{
					// Queue it for delivery
					AcceptIcbmOIM(offlineMessage);
				}
				else
				{
					// A single offline message?  Okay then
					if (OfflineMessagesReceived != null)
					{
						List<OfflineIM> tmpList = new List<OfflineIM>(1);
						tmpList.Add(offlineMessage);
						OfflineMessagesReceived(this, new OfflineMessagesReceivedEventArgs(parent.ScreenName,
						                                                                   new Collection<OfflineIM>(tmpList)));
					}
				}
				// Offline messages don't get delivered via OnMessageReceived - if the offline messages event
				// isn't hooked up, tough stuff.
				return;
			}
			else
			{
				OnMessageReceived(message);
			}
		}

		/// <summary>
		/// Retrieves the message text from SNAC(04,07) TLV 05
		/// </summary>
		/// <param name="stream">A received <see cref="ByteStream"/></param>
		/// <param name="message">An <see cref="IM"/> object to be populated</param>
		private void GetChannelfourMessage(ByteStream stream, ref IM message)
		{
						
			stream.ReadByteArray(46); //fixed unknon part 
			ushort length = stream.ReadUshort();
			
			ushort charset = stream.ReadUshort();
			stream.ReadByte();
			ushort charsubset = (ushort)0;//stream.ReadUshort();
			Encoding	 incoming = IM.GetEncodingFromCharset(charset, charsubset);
			string xml = Encoding.Unicode.GetString(
				Encoding.Convert(incoming, Encoding.Unicode, stream.ReadByteArray(length)));
			// parse xml
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			doc.LoadXml(xml);
			System.Xml.XmlNode node = doc.SelectSingleNode("/sms_message/text");
			if (node != null)
			{
				message.Message = node.InnerText;
			}
			
			node = doc.SelectSingleNode("/sms_message/sender");
			if (node != null)
			{
				message.ScreenName  = node.InnerText;
			}
			
			
		}

		#endregion
		
		// End region SNAC(04,07)

		#endregion

		#endregion Handlers

		#region ICQ Offline message processing helpers
		/// <summary>
		/// Processes a newly received offline message for the specified UIN
		/// </summary>
		/// <remarks>This is called from the ICQ manager, which receives the incoming
		/// offline message packet and sends it here for processing</remarks>
		internal void ReadIcqOfflineMessage(String uin, ByteStream stream)
		{
			String sender = stream.ReadUintLE().ToString();

			// Read in the date information (GMT)
			ushort year = stream.ReadUshortLE();
			byte month = stream.ReadByte();
			byte day = stream.ReadByte();
			byte hourGmt = stream.ReadByte();
			byte minute = stream.ReadByte();
			DateTime received = new DateTime(year, month, day, hourGmt, minute, 0, DateTimeKind.Utc);

			// Read in message type information
			byte messageType = stream.ReadByte();
			byte messageFlags = stream.ReadByte();
			Encoding messageEncoding = GetOfflineMessageEncoding(messageType);

			OfflineIM im = new OfflineIM(sender);
			im.ReceivedOn = received;
			im.Message = Encoding.Unicode.GetString(
				Encoding.Convert(messageEncoding, Encoding.Unicode, stream.ReadByteArray(stream.ReadUshortLE() - 1)));
			im.IsAutoResponse = (messageFlags == 0x03) || (messageType == 0xE8);

			// Store it for delivery to the client
			AcceptIcbmOIM(im);
		}

		/// <summary>
		/// Processes a message from the server that offline message retrival is finished
		/// </summary>
		internal void EndOfflineMessageSequence(String screenname, bool deleteIcqMessages)
		{
			Collection<OfflineIM> oims = new Collection<OfflineIM>();
			if (offlineMessages.ContainsKey(screenname))
			{
				oims = offlineMessages[screenname];
				if (deleteIcqMessages)
				{
					DeleteIcqOfflineMessages();
				}
			}

			isRetrievingOfflineMessages = false;

			// Raise the offline messages received event
			if (OfflineMessagesReceived != null)
			{
				OfflineMessagesReceived(this, new OfflineMessagesReceivedEventArgs(screenname, oims));
			}
		}

		/// <summary>
		/// Deletes offline messages belonging to the user
		/// </summary>
		/// <remarks>This method also removes any offline messages locally cached by the manager.</remarks>
		private void DeleteIcqOfflineMessages()
		{
			if (!ScreennameVerifier.IsValidICQ(parent.ScreenName))
			{
				throw new Exception("Can't retrieve offline messages for non-ICQ accounts");
			}

			offlineMessages.Remove(parent.ScreenName);

			SNACHeader header = IcqManager.CreateIcqMetaHeader();
			ByteStream stream = IcqManager.BeginIcqByteStream(parent.ScreenName);
			stream.WriteUshortLE(ICQ_DELETE_OFFLINE_MESSAGES);
			stream.WriteUshortLE((ushort)header.RequestID);
			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, header, stream));
		}

		/// <summary>
		/// Caches an offline message until the entire collection has been retrieved
		/// </summary>
		private void AcceptIcbmOIM(OfflineIM message)
		{
			if (!offlineMessages.ContainsKey(parent.ScreenName))
			{
				offlineMessages.Add(parent.ScreenName, new Collection<OfflineIM>());
			}

			offlineMessages[parent.ScreenName].Add(message);
		}
		#endregion ICQ Offline message processing helpers

		#region Helper methods
		/// <summary>
		/// Sends an ICBM on channel 1 -- SNAC(04,06)
		/// </summary>
		/// <param name="destination">The screenname to receive the IM</param>
		/// <param name="message">The message to send</param>
		/// <param name="flags">A <see cref="OutgoingMessageFlags"/> enumeration specifying what
		/// additional information should be sent with the message</param>
		private Cookie SendMessageThroughServer(string destination, string message, OutgoingMessageFlags flags)
		{
			SNACHeader sh = new SNACHeader();
			sh.FamilyServiceID = SNAC_ICBM_FAMILY;
			sh.FamilySubtypeID = ICBM_OUTGOING_MESSAGE;
			sh.Flags = 0x0000;
			sh.RequestID = Session.GetNextRequestID();

			byte[] default_features = new byte[] { 0x01, 0x01, 0x01, 0x02 };
			Cookie cookie = Cookie.CreateCookieForSending();
			Encoding encoding = UtilityMethods.FindBestOscarEncoding(message);

			ByteStream stream = new ByteStream();

			InsertIcbmHeader(stream, cookie, 0x0001, destination);
			using (TlvBlock tlvs = new TlvBlock())
			{
				// Write in TLV 0x0002: a text message
				using (TlvBlock tlv02 = new TlvBlock())
				{
					tlv02.WriteByteArray(0x0501, default_features);
					ByteStream feature0101 = new ByteStream();
					feature0101.WriteUshort(Marshal.EncodingToCharset(encoding));
					feature0101.WriteUshort(0x0000);
					feature0101.WriteString(message, encoding);
					tlv02.WriteByteArray(0x0101, feature0101.GetBytes());
					tlvs.WriteByteArray(0x0002, tlv02.GetBytes());
				}

				// Pack in optional TLVs
				if ((flags & OutgoingMessageFlags.AutoResponse) != 0)
				{
					tlvs.WriteEmpty(0x0004);
				}
				else
				{
					// Request a notification of delivery - note that this can't happen
					// with an AutoResponse flag set, otherwise the message is bounced
					tlvs.WriteEmpty(0x0003);
				}

				if ((flags & OutgoingMessageFlags.DeliverOffline) != 0
				    && (flags & OutgoingMessageFlags.AutoResponse) == 0)
				{
					// Try to store if the user is offline
					tlvs.WriteEmpty(0x0006);
				}

				// Add the TLVs to the byte stream
				stream.WriteByteArray(tlvs.GetBytes());
			}

			// Cache the message for delivery updates
			messageStatuses.Add(sh.RequestID, new MessageStatusEventArgs(cookie, destination));

			// Send that sucker off
			SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
			return cookie;
		}

		/// <summary>
		/// Inserts an ICBM header
		/// </summary>
		private static void InsertIcbmHeader(ByteStream stream, Cookie cookie, ushort channel, string screenName)
		{
			stream.WriteByteArray(cookie.ToByteArray());
			stream.WriteUshort(channel);
			stream.WriteByte((byte)Encoding.ASCII.GetByteCount(screenName));
			stream.WriteString(screenName, Encoding.ASCII);
		}

		/// <summary>
		/// Converts an IP address string to a NBO uint
		/// </summary>
		private static uint ConvertIPAddress(string ipAddress)
		{
			string[] IPsplit = ipAddress.Split('.');
			byte shift = 24;
			uint complement = 0x00000000;
			// Convert the IP into an NBO uint
			for (int i = 0; i < 4; i++)
			{
				byte b = Byte.Parse(IPsplit[i]);
				complement |= (uint)(b << shift);
				shift -= 8;
			}
			return complement;
		}

		/// <summary>
		/// Infers the message encoding from the message type flag
		/// </summary>
		/// <param name="messageType">The message type flag received in the offline message notification</param>
		/// <returns>Either <see cref="Encoding.ASCII"/> or <see cref="Encoding.Unicode"/></returns>
		/// <remarks>According to Shutko, there are several message types that are "0xFE formatted."</remarks>
		private static Encoding GetOfflineMessageEncoding(byte messageType)
		{
			switch (messageType)
			{
				case 0x04:
				case 0x06:
				case 0x07:
				case 0x09:
				case 0x0C:
				case 0x0D:
				case 0x0E:
					return Encoding.Unicode;
				default:
					return Encoding.ASCII;
			}
		}
		#endregion
	}

	/// <summary>
	/// A set of flags that can be applied to outgoing messages
	/// </summary>
	[Flags]
	public enum OutgoingMessageFlags
	{
		/// <summary>
		/// No flags
		/// </summary>
		None,
		/// <summary>
		/// The message is an auto-generated response
		/// </summary>
		AutoResponse,
		/// <summary>
		/// If the recepient is offline, try to deliver using Offline Messaging
		/// </summary>
		DeliverOffline,
	}

	/// <summary>
	/// Describes reasons for messages being undeliverable
	/// </summary>
	public enum UndeliverableMessageReason
	{
		/// <summary>
		/// Message format was invalid
		/// </summary>
		InvalidMessage = 0x0000,
		/// <summary>
		/// The message was too large to be delivered
		/// </summary>
		MessageTooLarge = 0x0001,
		/// <summary>
		/// The sender's message sending rate was exceeded
		/// </summary>
		MessageRateExceeded = 0x0002,
		/// <summary>
		/// The sender's warning level is too high to be delivered
		/// </summary>
		SenderTooEvil = 0x0003,
		/// <summary>
		/// This client's warning level is too high to be delivered to
		/// </summary>
		SelfTooEvil = 0x0004
	}

	/// <summary>
	/// Describes the statuses of message delivery
	/// </summary>
	public enum MessageStatus
	{
		/// <summary>
		/// An unspecified error occured during message delivery
		/// </summary>
		UnknownError,
		/// <summary>
		/// The message was accepted by the server
		/// </summary>
		AcceptedForDelivery,
		/// <summary>
		/// The message was rejected because the destination buddy was offline
		/// </summary>
		BuddyOffline,
		/// <summary>
		/// A message sent with <see cref="OutgoingMessageFlags.DeliverOffline"/> was rejected because the recepient does not
		/// support offline messages
		/// </summary>
		OfflineMessagesNotSupported,
		/// <summary>
		/// A message sent with <see cref="OutgoingMessageFlags.DeliverOffline"/> was rejected because the recepient's
		/// offline storage is full
		/// </summary>
		OfflineStorageFull,
		/// <summary>
		/// The client has received no response from the server regarding the message
		/// </summary>
		MessageTimeout,
	}
}
