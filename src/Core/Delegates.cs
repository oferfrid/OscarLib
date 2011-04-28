/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using csammisrun.OscarLib.Utility;
using System.Collections.ObjectModel;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Provides a callback for notifying the user of message delivery updates
    /// </summary>
    /// <param name="sender">The current session's <see cref="MessageManager"/></param>
    /// <param name="e">A <see cref="MessageStatusEventArgs"/> describing the event</param>
    public delegate void MessageDeliveryUpdateEventHandler(object sender, MessageStatusEventArgs e);
    /// <summary>
    /// Details about when and to whom a message was sent
    /// </summary>
    public class MessageStatusEventArgs : EventArgs
    {
        private readonly string destination;
        private readonly DateTime timestamp;
        private readonly Cookie cookie;
        private MessageStatus status = MessageStatus.UnknownError;

        /// <summary>
        /// Initializes a new MessageStatusInfo stamped with the current time
        /// </summary>
        /// <param name="cookie">The cookie uniquely identifying the message</param>
        /// <param name="destination">The screenname to which the message was sent</param>
        public MessageStatusEventArgs(Cookie cookie, string destination)
        {
            this.cookie = cookie;
            this.destination = destination;
            this.timestamp = DateTime.Now;
        }

        /// <summary>
        /// Gets the screen name to which the message was sent
        /// </summary>
        public string Destination
        {
            get { return destination; }
        }

        /// <summary>
        /// Gets the cookie which uniquely identifies the message
        /// </summary>
        public Cookie Cookie
        {
            get { return cookie; }
        }

        /// <summary>
        /// Gets the status of the message
        /// </summary>
        public MessageStatus Status
        {
            get { return status; }
            internal set { status = value; }
        }

        /// <summary>
        /// Gets the time at which the message was sent
        /// </summary>
        public DateTime Timestamp
        {
            get { return timestamp; }
        }
    }

    /// <summary>
    /// Provides a callback function for informational messages sent by the OSCAR library
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="message">A status message</param>
	public delegate void InformationMessageHandler(ISession sess, string message);

    /// <summary>
    /// Provides a callback function for warning messages sent by the OSCAR library
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the warning</param>
    /// <remarks>
    /// Messages received on this event are non-fatal errors, the session will continue
    /// </remarks>
	public delegate void WarningMessageHandler(ISession sess, ServerErrorCode errorcode);

    /// <summary>
    /// Provides a callback function for error messages sent by the OSCAR library
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the error</param>
    /// <remarks>
    /// Messages received on this event are fatal, and the session will end
    /// </remarks>
	public delegate void ErrorMessageHandler(ISession sess, ServerErrorCode errorcode);

    /// <summary>
    /// Provides a callback function for typing notifications sent by the server
    /// </summary>
    public delegate void TypingNotificationEventHandler(object sender, TypingNotificationEventArgs e);

    /// <summary>
    /// Provides a callback function to be called when the login sequence is completed
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
	public delegate void LoginCompletedHandler(ISession sess);

    /// <summary>
    /// Provides a callback function to be called during the login sequence to alert the user
    /// of positive progress
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="message">A status message</param>
    /// <param name="percentdone">The percentage of the login progress that has been completed</param>
	public delegate void LoginStatusUpdateHandler(ISession sess, string message, double percentdone);

    #region SNAC17 delegates

    /// <summary>
    /// Provides a callback function to be called if the login sequence fails
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="errorcode">A description of the failure</param>
	public delegate void LoginFailedHandler(ISession sess, LoginErrorCode errorcode);

    #endregion

    /// <summary>
    /// Provides a callback function for receiving ICBM messages sent by the server
    /// </summary>
    public delegate void MessageReceivedHandler(object sender, MessageReceivedEventArgs e);

    /// <summary>
    /// Handles the receipt of offline messages from the server
    /// </summary>
    public delegate void OfflineMessagesReceivedEventHandler(object sender,
                                                             OfflineMessagesReceivedEventArgs e);
    /// <summary>
    /// Contains event information for messages received offline
    /// </summary>
    public class OfflineMessagesReceivedEventArgs : EventArgs
    {
        private readonly ReadOnlyCollection<OfflineIM> receivedMessages;
        private readonly String screenName;

        /// <summary>
        /// Initializes a new set of parameters for an offline messages received event
        /// </summary>
        public OfflineMessagesReceivedEventArgs(String screenName, Collection<OfflineIM> receivedMessages)
        {
            this.screenName = screenName;
            this.receivedMessages = new ReadOnlyCollection<OfflineIM>(receivedMessages);
        }

        /// <summary>
        /// The screen name that received the messages
        /// </summary>
        public String ScreenName
        {
            get { return screenName; }
        }

        /// <summary>
        /// Gets the messages that were received offline
        /// </summary>
        public ReadOnlyCollection<OfflineIM> ReceivedMessages
        {
            get { return receivedMessages; }
        }
    }

    /// <summary>
    /// Provides a callback function for receiving file transfer requests
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="sender">The <see cref="UserInfo"/> of the client requesting the transfer</param>
    /// <param name="IP">The verified IP address of the remote client</param>
    /// <param name="filename">The name of the file the remote client is attempting to send</param>
    /// <param name="filesize">The size of the file, in bytes</param>
    /// <param name="message">The message received with the file transfer request</param>
    /// <param name="key">The unique key needed to respond to this request</param>
    public delegate void FileTransferRequestReceivedHandler(
		ISession sess, UserInfo sender, string IP, string filename, uint filesize, string message, Cookie key);

    /// <summary>
    /// Provides a callback function for receiving Direct IM transfer requests
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="sender">The <see cref="UserInfo"/> of the client requesting the session</param>
    /// <param name="message">The message received with the DirectIM request</param>
    /// <param name="key">The unique key needed to respond to this request</param>
	public delegate void DirectIMRequestReceivedHandler(ISession sess, UserInfo sender, string message, Cookie key);

    /// <summary>
    /// Provides a callback function for querying a user's information
    /// </summary>
    /// <param name="sender">The <see cref="StatusManager"/> object raising the event</param>
    /// <param name="info">A <see cref="UserInfoResponse"/> structure describing the user</param>
    public delegate void UserInfoReceivedHandler(object sender, UserInfoResponse info);

    /// <summary>
    /// Provides a callback function for receiving a user status notification from the server
    /// </summary>
    /// <param name="sender">The <see cref="StatusManager"/> object raising the event</param>
    /// <param name="userinfo">A <see cref="UserInfo"/> block describing the user</param>
    public delegate void UserStatusReceivedHandler(object sender, UserInfo userinfo);

    /// <summary>
    /// Provides a callback function for receiving search by email results from the server
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="email">The email address that was searched for</param>
    /// <param name="results">The screennames that are associated with the email address</param>
	public delegate void SearchByEmailResultsHandler(ISession sess, string email, string[] results);

    /// <summary>
    /// Provides a callback function for receiving popup messages from the server
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="width">The width of the popup box, in pixels</param>
    /// <param name="height">The height of the popup box, in pixels</param>
    /// <param name="delay">The autohide delay of the popup box, in seconds</param>
    /// <param name="url">The URL associated with the message</param>
    /// <param name="message">The message to display</param>
	public delegate void PopupMessageHandler(ISession sess, int width, int height, int delay, string url, string message);

    /// <summary>
    /// Provides a callback function for undeliverable message notices from the server
    /// </summary>
    public delegate void UndeliverableMessageEventHandler(object sender, UndeliverableMessageEventArgs e);

    /// <summary>
    /// Provides a callback function for message accepted notifications from the server
    /// </summary>
    public delegate void MessageAcceptedEventHandler(object sender, MessageAcceptedEventArgs e);

    #region SNAC0F delegates

    /// <summary>
    /// Provides a callback function for directory search results sent by the server
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="results">The results of the directory search</param>
	public delegate void SearchResultsHandler(ISession sess, DirectoryEntry[] results);

    /// <summary>
    /// Provides a callback function for getting a list of interest items
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="results">The results of the interests request</param>
	public delegate void InterestsReceivedHandler(ISession sess, InterestItem[] results);

    #endregion

    /// <summary>
    /// Provides a callback function for receiving user offline notifications
    /// </summary>
    /// <param name="sender">The <see cref="StatusManager"/> raising the event</param>
    /// <param name="userinfo">A <see cref="UserInfo"/> block describing the user</param>
    public delegate void UserOfflineHandler(object sender, UserInfo userinfo);

    #region SNAC13 delegates

    /// <summary>
    /// Provides a callback function for notifiying the client once the SSI list has been sent
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="lastModificationDate">the date when the buddylist was modified at last</param>
	public delegate void ContactListFinishedHandler(ISession sess, DateTime lastModificationDate);

    /// <summary>
    /// Provides a callback function for receiving a new buddy item at signon
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
	public delegate void BuddyItemReceivedHandler(ISession sess, SSIBuddy buddy);

    /// <summary>
    /// Provides a callback function for removal of a buddy item from the server at runtime
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
	public delegate void BuddyItemRemovedHandler(ISession sess, SSIBuddy buddy);

    /// <summary>
    /// Provides a callback function for receiving a new group item at signon
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="group">An <see cref="SSIGroup"/>"/> object</param>
	public delegate void GroupItemReceivedHandler(ISession sess, SSIGroup group);

    /// <summary>
    /// Provides a callback function for removal of a group item from the server at runtime
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="group">An <see cref="SSIGroup"/> object</param>
	public delegate void GroupItemRemovedHandler(ISession sess, SSIGroup group);

    /// <summary>
    /// Provides a callback function for receiving a master group item at signon
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="numgroups">The number of groups</param>
	public delegate void MasterGroupItemReceivedHandler(ISession sess, int numgroups);

    /// <summary>
    /// Provides a callback function to alert the client when an SSI edit is complete
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    public delegate void SSIEditCompleteHandler(ISession sess);

    /// <summary>
    /// Provides a callback function to aleart the client when a authorization request has been received
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="screenname">the screenname that ask for authorization</param>
    /// <param name="reason">the reason message</param>
	public delegate void AuthorizationRequestReceivedHandler(ISession sess, string screenname, string reason);

    /// <summary>
    /// Provides a callback function to aleart the client when a authorization response has been received
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="screenname">the screenname that granted or declined the authorization</param>
    /// <param name="authorizationGranted">the reason message</param>
    /// <param name="reason"></param>
    public delegate void AuthorizationResponseReceivedHandler(
		ISession sess, string screenname, bool authorizationGranted, string reason);

    /// <summary>
    /// Provides a callback function to aleart the client when a future authorization granted response has been received
    /// </summary>
    /// <param name="sess"></param>
    /// <param name="screenname"></param>
    /// <param name="reason"></param>
	public delegate void FutureAuthorizationReceivedHandler(ISession sess, string screenname, string reason);

    #endregion

    /// <summary>
    /// Provides a callback function for receiving a warning notification
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="newlevel">The client's new warning level</param>
    /// <param name="anonymous"><c>true</c> if this warning was sent anonymously, <c>false</c> otherwise</param>
    /// <param name="ui">A <see cref="UserInfo"/> structure describing the warning user. If <paramref name="anonymous"/> is
    /// <c>true</c>, this structure is unpopulated</param>
	public delegate void WarningReceivedHandler(ISession sess, ushort newlevel, bool anonymous, UserInfo ui);

    /// <summary>
    /// Provides a callback function for receiving the MOTD from the server at signon
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="motdtype">The type of the MOTD</param>
    /// <param name="message">The message string</param>
	public delegate void MessageOfTheDayReceivedHandler(ISession sess, ushort motdtype, string message);

    /// <summary>
    /// Provides a callback function for receiving directory update acknowledgements
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="success"><c>true</c> if the directory update succeded, and <c>false</c> otherwise</param>
	public delegate void DirectoryUpdateAcknowledgedHandler(ISession sess, bool success);

    /// <summary>
    /// Provides a callback function for receiving status report intervals from the server
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="hours">The minimum status reporting interval, in hours</param>
	public delegate void ReportingIntervalReceivedHandler(ISession sess, int hours);

    /// <summary>
    /// Provides a callback function for tracking the progress of a file transfer
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="cookie">The rendezvous cookie belonging to the file being transfered</param>
    /// <param name="BytesTransfered">The number of bytes transfered so far</param>
    /// <param name="BytesTotal">The total number of bytes to be transfered</param>
	public delegate void FileTransferProgressHandler(ISession sess,
                                                     Cookie cookie,
                                                     uint BytesTransfered,
                                                     uint BytesTotal);

    /// <summary>
    /// Provides a callback function for tracking the progress of an incoming DirectIM message
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="cookie">The rendezvous cookie belonging to the DirectIM session</param>
    /// <param name="BytesTransfered">The number of bytes transfered so far</param>
    /// <param name="BytesTotal">The total number of bytes to be transfered</param>
	public delegate void DirectIMIncomingMessageProgressHandler(ISession sess,
                                                                Cookie cookie,
                                                                uint BytesTransfered,
                                                                uint BytesTotal);

    /// <summary>
    /// Provides a callback function for tracking the progress of an outgoing DirectIM message
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="cookie">The rendezvous cookie belonging to the DirectIM session</param>
    /// <param name="BytesTransfered">The number of bytes transfered so far</param>
    /// <param name="BytesTotal">The total number of bytes to be transfered</param>
	public delegate void DirectIMOutgoingMessageProgressHandler(ISession sess,
                                                                Cookie cookie,
                                                                uint BytesTransfered,
                                                                uint BytesTotal);

    /// <summary>
    /// Provides a callback function for receiving notification of a cancelled transfer
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="other">The <see cref="UserInfo"/> of the user on the other side of the connection</param>
    /// <param name="cookie">The rendezvous cookie belonging to the cancelled file</param>
    /// <param name="reason">The reason for the cancellation</param>
	public delegate void FileTransferCancelledHandler(ISession sess,
                                                      UserInfo other,
                                                      Cookie cookie,
                                                      string reason);

    /// <summary>
    /// Provides a callback function for receiving notification of a completed transfer
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="cookie">The rendezvous cookie belonging to the completed file</param>
	public delegate void FileTransferCompletedHandler(ISession sess, Cookie cookie);

    /// <summary>
    /// Provides a callback function for receiving notification of a newly connected or disconnected DirectIM session
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="other">The <see cref="UserInfo"/> of the user on the other side of the connection</param>
    /// <param name="cookie">The rendezvous cookie belonging to the session</param>
	public delegate void DirectIMSessionChangedHandler(ISession sess, UserInfo other, Cookie cookie);

    /// <summary>
    /// Provides a callback function for receiving DirectIMs sent by a remote user
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="message">A <see cref="DirectIM"/> message</param>
	public delegate void DirectIMReceivedHandler(ISession sess, DirectIM message);

    #region Chat room delegates

    /// <summary>
    /// Provides a callback function for notification of successfully joining a chat room
    /// </summary>
    /// <param name="sender">The <see cref="ChatRoomManager"/> raising the event</param>
    /// <param name="newroom">A <see cref="ChatRoom"/> object representing the new room</param>
    public delegate void ChatRoomJoinedHandler(object sender, ChatRoom newroom);

    /// <summary>
    /// Provides a callback function for notification of an error in a chat room
    /// </summary>
    /// <param name="sender">The <see cref="ChatRoomManager"/> raising the event</param>
    /// <param name="newroom">A <see cref="ChatRoom"/> object representing the room in error</param>
    public delegate void ChatRoomErrorHandler(object sender, ChatRoom newroom);

    /// <summary>
    /// Provides a callback function for receiving chat room invitations
    /// </summary>
	/// <param name="sess">The <see cref="ISession"/> object raising the event</param>
    /// <param name="sender">A <see cref="UserInfo"/> object describing the inviter</param>
    /// <param name="roomname">The name of the chatroom</param>
    /// <param name="message">An invitation chatroom</param>
    /// <param name="encoding">The text encoding used in the chatroom</param>
    /// <param name="language">The language used in the chatroom</param>
    /// <param name="key">The unique key needed to respond to this request</param>
	public delegate void ChatInvitationReceivedHandler(ISession sess, UserInfo sender, string roomname,
                                                       string message, Encoding encoding, string language, Cookie key);

    #endregion

    internal delegate void ProxiedSocketFactoryResultHandler(Socket socket, string errormsg);

    /// <summary>
    /// Provides a factory function for connecting sockets through a proxy
    /// </summary>
    /// <param name="host">The host to which to connect</param>
    /// <param name="port">The port on the host to which to connect</param>
    /// <param name="callback">A callback in which to receive the socket and, potentially, an error message.</param>
    /// <returns>A TCP stream socket ready to send and receive data</returns>
    /// <remarks>
    /// <para>This delegate is intended to be used if OscarLib is part of an application that provides a global
    /// socket connection factory through various proxies.</para>
    /// <para>The <paramref name="callback"/> parameter must be a delegate that accepts a Socket and a string as its parameters.</para>
    /// </remarks>
    public delegate void ProxiedSocketFactoryDelegate(string host, int port, Delegate callback);

    public delegate void PacketDispatchExceptionHandler(object sender, PacketDispatchExceptionArgs e);

    /// <summary>
    /// Encapuslates the arguments to a packet dispatch exception event
    /// </summary>
    public class PacketDispatchExceptionArgs : EventArgs
    {
        private readonly Exception ex;
        private readonly DataPacket packet;

        /// <summary>
        /// Initializes a new PacketDispatchExceptionArgs
        /// </summary>
        public PacketDispatchExceptionArgs(Exception exception, DataPacket packet)
        {
            this.ex = exception;
            this.packet = packet;
        }

        /// <summary>
        /// The exception that was raised during dispatch
        /// </summary>
        public Exception Exception
        {
            get { return ex; }
        }

        /// <summary>
        /// The data packet that caused the exception
        /// </summary>
        public DataPacket Packet
        {
            get { return packet; }
        }
    }
}