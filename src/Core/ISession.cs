using System;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
	public interface ISession
	{
		/// <summary>
		/// Gets or Sets the last modification date and time of the buddylist
		/// </summary>
		DateTime LastModificationDate { get; set; }

		/// <summary>
		/// Gets or sets the screen name associated with this session
		/// </summary>
		/// <remarks>
		/// The screen name cannot be set by this property while the client is offline.
		/// When the client is online, setting the screen name by this property changes the
		/// screen name's formatting on the server.
		/// </remarks>
		/// <exception cref="ArgumentException">Thrown when <paramref name="screenname"/> is not
		/// a valid AIM or ICQ screenname.</exception>
		string ScreenName { get; set; }

		/// <summary>
		/// Gets or sets the port number used for OSCAR logins
		/// </summary>
		/// <remarks>
		/// Traditionally, this is port 5190; however, AIM 6 has been caught using port 443 to negotiate
		/// connections with login.oscar.aol.com and ars.oscar.aol.com.  Future versions of OscarLib may use
		/// this property to support login via port 443.
		/// </remarks>
		ushort LoginPort { get; }

		/// <summary>
		/// Gets or sets this session's OSCAR identification information
		/// </summary>
		/// <exception cref="LoggedInException">Thrown when the <see cref="ISession"/> is already logged in</exception>
		OSCARIdentification ClientIdentification { get; set; }

		/// <summary>
		/// Gets or sets the OSCAR capabilities associated with the session
		/// </summary>
		/// <remarks>
		/// The client capabilities must be set before the session is logged in because the
		/// client's capabilities are communicated during the login process and are kept through
		/// the session.
		/// </remarks>
		/// <exception cref="LoggedInException">Thrown when the <see cref="ISession"/> is already logged in</exception>
		Capabilities ClientCapabilities { get; set; }

		/// <summary>
		/// Gets the recommented enocding format depending on the client capability settings
		/// </summary>
		Encoding Encoding { get; }

		/// <summary>
		/// Gets the session's Available message string
		/// </summary>
		/// <remarks>To set the the session's Available message, use the
		/// <see cref="ISession.SetAvailableMessage"/> method</remarks>
		string AvailableMessage { get; }

		/// <summary>
		/// Gets a value indicating whether this client's idle time is publicly available
		/// </summary>
		bool PublicIdleTime { get; }

		/// <summary>
		/// Gets a value indicating whether this client has completed the login process
		/// </summary>
		bool LoggedIn { get; }

		/// <summary>
		/// Gets the <see cref="SSIManager"/> associated with this session
		/// </summary>
		SSIManager SSI { get; }

		/// <summary>
		/// Gets the <see cref="LimitManager"/> associated with this session
		/// </summary>
		LimitManager Limits { get; }

		/// <summary>
		/// Gets the <see cref="IcqManager"/> associated with this session
		/// </summary>
		IcqManager ICQ { get; }

		/// <summary>
		/// Gets the <see cref="MessageManager"/> associated with this session
		/// </summary>
		MessageManager Messages { get; }

		/// <summary>
		/// Gets the <see cref="ChatRoomManager"/> associated with this session
		/// </summary>
		ChatRoomManager ChatRooms { get; }

		/// <summary>
		/// Gets the <see cref="GraphicsManager"/> associated with this session
		/// </summary>
		GraphicsManager Graphics { get; }

		/// <summary>
		/// Gets the <see cref="StatusManager"/> associated with this session
		/// </summary>
		StatusManager Statuses { get; }

		/// <summary>
		/// Gets or sets a filesystem path where OscarLib can place received data
		/// </summary>
		/// <remarks>During an OSCAR Direct Connect session, "transient" files may come over the wire.
		/// If ScratchPath is set to a valid path, OscarLib will save the files locally and return
		/// <see cref="System.IO.FileStream"/> references to the objects. Otherwise, the files will
		/// be returned as <see cref="System.IO.MemoryStream"/> objects, which will take more active memory.</remarks>
		string ScratchPath { get; set; }

		/// <summary>
		/// Gets the <see cref="ConnectionManager"/> associated with this session
		/// </summary>
		ConnectionManager Connections { get; }

		/// <summary>
		/// Gets the <see cref="ServiceManager"/> associated with this session
		/// </summary>
		ServiceManager Services { get; }

		/// <summary>
		/// Gets the <see cref="PacketDispatcher"/> associated with this session
		/// </summary>
		PacketDispatcher Dispatcher { get; }

		/// <summary>
		/// Gets the <see cref="FamilyManager"/> associated with this session
		/// </summary>
		FamilyManager Families { get; }

		/// <summary>
		/// Gets or sets the <see cref="RateClassManager"/> associated with this session
		/// </summary>
		RateClassManager RateClasses { get; }

		/// <summary>
		/// Gets or sets the <see cref="ProxiedSocketFactoryDelegate"/> to use to create new socket connections
		/// </summary>
		/// <remarks>By default, this property is set to <see cref="Session.DirectSocketConnectionFactory"/></remarks>
		ProxiedSocketFactoryDelegate ProxiedSocketFactory { get; set; }

		/// <summary>
		/// Occurs when an unhandled exception is raised in the course of dispatching and processing a packet
		/// </summary>
		event PacketDispatchExceptionHandler PacketDispatchException;

		/// <summary>
		/// Raises the <see cref="PacketDispatchException"/> event
		/// </summary>
		void OnPacketDispatchException(Exception ex, DataPacket packet);

		/// <summary>
		/// Occurs when the library generates a status update message
		/// </summary>
		event InformationMessageHandler StatusUpdate;

		/// <summary>
		/// Raises the <see cref="StatusUpdate"/> event
		/// </summary>
		/// <param name="message">A status message</param>
		void OnStatusUpdate(string message);

		/// <summary>
		/// Occurs when the library generates a status update message during login
		/// </summary>
		event LoginStatusUpdateHandler LoginStatusUpdate;

		/// <summary>
		/// Raises the <see cref="LoginStatusUpdate"/> event
		/// </summary>
		/// <param name="message">A status message</param>
		/// <param name="percentdone">The percentage of the login progress that has been completed</param>
		void OnLoginStatusUpdate(string message, double percentdone);

		/// <summary>
		/// Occurs when the library generates a warning message
		/// </summary>
		event WarningMessageHandler WarningMessage;

		/// <summary>
		/// Raises the <see cref="WarningMessage"/> event
		/// </summary>
		/// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the warning</param>
		void OnWarning(ServerErrorCode errorcode);

		/// <summary>
		/// Occurs when the library generates an error message
		/// </summary>
		event ErrorMessageHandler ErrorMessage;

		/// <summary>
		/// Raises the <see cref="ErrorMessage"/> event or the <see cref="LoginFailed"/> event
		/// </summary>
		/// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the error</param>
		/// <remarks>If the login process has not completed, <see cref="LoginFailed"/> is raised.
		/// Otherwise, <see cref="ErrorMessage"/> is raised.</remarks>
		void OnError(ServerErrorCode errorcode);

		/// <summary>
		/// Occurs when the login process is complete.
		/// </summary>
		event LoginCompletedHandler LoginCompleted;

		/// <summary>
		/// Raises the <see cref="LoginCompleted"/> event
		/// </summary>
		void OnLoginComplete();

		/// <summary>
		/// Occurs when a remote client has warned this client
		/// </summary>
		event WarningReceivedHandler WarningReceived;

		/// <summary>
		/// Raises the <see cref="WarningReceived"/> event.
		/// </summary>
		/// <param name="newlevel">The client's new warning level</param>
		/// <param name="anonymous"><c>true</c> if this warning was sent anonymously, <c>false</c> otherwise</param>
		/// <param name="ui">A <see cref="UserInfo"/> structure describing the warning user. If <paramref name="anonymous"/> is
		/// <c>true</c>, this structure is unpopulated</param>
		void OnWarningReceived(ushort newlevel, bool anonymous, UserInfo ui);

		/// <summary>
		/// Occurs when the server sends acknowledgement of a directory update request
		/// </summary>
		event DirectoryUpdateAcknowledgedHandler DirectoryUpdateAcknowledged;

		/// <summary>
		/// Raises the <see cref="DirectoryUpdateAcknowledged"/> event
		/// </summary>
		/// <param name="success"><c>true</c> if the directory update succeded, and <c>false</c> otherwise</param>
		void OnDirectoryUpdateAcknowledged(bool success);

		/// <summary>
		/// Occurs when a file transfer request is received
		/// </summary>
		event FileTransferRequestReceivedHandler FileTransferRequestReceived;

		/// <summary>
		/// Occurs when a Direct IM transfer request is received
		/// </summary>
		event DirectIMRequestReceivedHandler DirectIMRequestReceived;

		/// <summary>
		/// Raises the <see cref="FileTransferRequestReceived"/> event
		/// </summary>
		/// <param name="key">The unique key needed to respond to this request</param>
		void OnDirectConnectionRequestReceived(Cookie key);

		/// <summary>
		/// Occurs when a chat room invitation is received
		/// </summary>
		event ChatInvitationReceivedHandler ChatInvitationReceived;

		/// <summary>
		/// Raises the <see cref="ChatInvitationReceived"/> event
		/// </summary>
		/// <param name="sender">A <see cref="UserInfo"/> object represnting the inviter</param>
		/// <param name="roomname">The name of the chatroom</param>
		/// <param name="message">An invitation chatroom</param>
		/// <param name="encoding">The text encoding used in the chatroom</param>
		/// <param name="language">The language used in the chatroom</param>
		/// <param name="key">The unique key needed to respond to this request</param>
		void OnChatInvitationReceived(UserInfo sender,
													  string roomname,
													  string message,
													  Encoding encoding,
													  string language,
													  Cookie key);

		/// <summary>
		/// Occurs when the server sends a popup message
		/// </summary>
		event PopupMessageHandler PopupMessage;

		/// <summary>
		/// Raises the <see cref="PopupMessage"/> event
		/// </summary>
		/// <param name="width">The width of the popup box, in pixels</param>
		/// <param name="height">The height of the popup box, in pixels</param>
		/// <param name="delay">The autohide delay of the popup box, in seconds</param>
		/// <param name="url">The URL associated with the message</param>
		/// <param name="message">The message to display</param>
		void OnPopupMessage(int width, int height, int delay, string url, string message);

		/// <summary>
		/// Occurs when results from a user search are available
		/// </summary>
		event SearchByEmailResultsHandler SearchByEmailResults;

		/// <summary>
		/// Raises the <see cref="SearchByEmailResults"/> event
		/// </summary>
		/// <param name="email">The email address that was searched for</param>
		/// <param name="results">The screennames that are associated with the email address</param>
		void OnSearchByEmailResults(string email, string[] results);

		/// <summary>
		/// Occurs when the server sends the minimum status reporting interval at login
		/// </summary>
		event ReportingIntervalReceivedHandler ReportingIntervalReceived;

		/// <summary>
		/// Raises the <see cref="ReportingIntervalReceived"/> event
		/// </summary>
		/// <param name="hours">The minimum status reporting interval, in hours</param>
		void OnReportingIntervalReceived(int hours);

		/// <summary>
		/// Occurs when the server sends the results of a directory search
		/// </summary>
		event SearchResultsHandler SearchResults;

		/// <summary>
		/// Raises the <see cref="SearchResults"/> event
		/// </summary>
		/// <param name="results">The results of the directory search</param>
		void OnSearchResults(DirectoryEntry[] results);

		/// <summary>
		/// Occurs when the server sends a list of interests
		/// </summary>
		event InterestsReceivedHandler InterestsReceived;

		/// <summary>
		/// Raises the <see cref="InterestsReceived"/> event
		/// </summary>
		/// <param name="results">The results of the interests request</param>
		void OnInterestsReceived(InterestItem[] results);

		/// <summary>
		/// Occurs when the buddy list has been completely sent by the server
		/// </summary>
		event ContactListFinishedHandler ContactListFinished;

		/// <summary>
		/// Notifies the server to activate the SSI data for the client, and to begin
		/// alerting its contacts that it is now online and ready to receive messages
		/// 
		/// Implementing clients should call <see cref="ActivateBuddyList"/> in response to this event
		/// </summary>
		void OnContactListFinished(DateTime lastModificationDate);

		/// <summary>
		/// Occurs when the server sends a new buddy item to the client
		/// </summary>
		event BuddyItemReceivedHandler BuddyItemReceived;

		/// <summary>
		/// Raises the <see cref="BuddyItemReceived"/> event
		/// </summary>
		/// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
		void OnBuddyItemReceived(SSIBuddy buddy);

		/// <summary>
		/// Occurs when a buddy item has been removed from the server-side list
		/// </summary>
		event BuddyItemRemovedHandler BuddyItemRemoved;

		/// <summary>
		/// Raises the <see cref="BuddyItemRemoved"/> event
		/// </summary>
		/// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
		void OnBuddyItemRemoved(SSIBuddy buddy);

		/// <summary>
		/// Occurs when the server sends a new group item to the client
		/// </summary>
		event GroupItemReceivedHandler GroupItemReceived;

		/// <summary>
		/// Raises the <see cref="GroupItemReceived"/> event
		/// </summary>
		/// <param name="group">An <see cref="SSIGroup"/>"/> object</param>
		void OnGroupItemReceived(SSIGroup group);

		/// <summary>
		/// Occurs when a buddy item has been removed from the server-side list
		/// </summary>
		event GroupItemRemovedHandler GroupItemRemoved;

		/// <summary>
		/// Raises the <see cref="GroupItemRemoved"/> event
		/// </summary>
		/// <param name="group">An <see cref="SSIGroup"/> object</param>
		void OnGroupItemRemoved(SSIGroup group);

		/// <summary>
		/// Occurs when the server sends the master group item to the client
		/// </summary>
		event MasterGroupItemReceivedHandler MasterGroupItemReceived;

		/// <summary>
		/// Raises the <see cref="MasterGroupItemReceived"/> event
		/// </summary>
		/// <param name="numgroups">The number of groups we are going to receive</param>
		void OnMasterGroupItemReceived(int numgroups);

		/// <summary>
		/// Occurs when the an SSI edit is completed
		/// </summary>
		event SSIEditCompleteHandler SSIEditComplete;

		/// <summary>
		/// Raises the <see cref="SSIEditComplete"/> event
		/// </summary>
		void OnSSIEditComplete();

		/// <summary>
		/// Occurs when a client ask for authorization (ICQ)
		/// </summary>
		event AuthorizationRequestReceivedHandler AuthorizationRequestReceived;

		/// <summary>
		/// Raises the <see cref="AuthorizationRequestReceived"/> event
		/// </summary>
		/// <param name="screenname">the screenname that ask for authorization</param>
		/// <param name="reason">the reason message</param>
		void OnAuthorizationRequestReceived(string screenname, string reason);

		/// <summary>
		/// Occurs when a client granted or declined the authorization (ICQ)
		/// </summary>
		event AuthorizationResponseReceivedHandler AuthorizationResponseReceived;

		/// <summary>
		/// Raises the <see cref="AuthorizationResponseReceived"/> event
		/// </summary>
		/// <param name="screenname">the screenname that should get the response</param>
		/// <param name="authorizationGranted">Determines, if the authorization will be granted or not.</param>
		/// <param name="reason">The reason message</param>
		void OnAuthorizationResponseReceived(string screenname, bool authorizationGranted,
															 string reason);

		/// <summary>
		/// Occurs when a client granted the authorization for the future (ICQ)
		/// </summary>
		event FutureAuthorizationReceivedHandler FutureAuthorizationReceived;

		/// <summary>
		/// Raises the <see cref="FutureAuthorizationReceived"/> event
		/// </summary>
		/// <param name="screenname">the screenname that should get the future authorization</param>
		/// <param name="reason">The reason message</param>
		void OnAuthorizationResponseReceived(string screenname, string reason);

		/// <summary>
		/// Occurs when the login sequence fails
		/// </summary>
		event LoginFailedHandler LoginFailed;

		/// <summary>
		/// Raises the <see cref="LoginFailed"/> event
		/// </summary>
		/// <param name="errorcode">A <see cref="LoginErrorCode"/> describing the failure</param>
		void OnLoginFailed(LoginErrorCode errorcode);

		/// <summary>
		/// Occurs during a file transfer to indicate transfer progression
		/// </summary>
		event FileTransferProgressHandler FileTransferProgress;

		/// <summary>
		/// Raises the <see cref="FileTransferProgress"/> event
		/// </summary>
		/// <param name="cookie">The rendezvous cookie belonging to the file being transfered</param>
		/// <param name="bytestransfered">The number of bytes transfered so far</param>
		/// <param name="bytestotal">The total number of bytes to be transfered</param>
		void OnFileTransferProgress(Cookie cookie,
													uint bytestransfered, uint bytestotal);

		/// <summary>
		/// Occurs during a DirectIM session to indicate the progress of an incoming message
		/// </summary>
		/// <remarks>This event will only fire if the incoming message contains attachments</remarks>
		event DirectIMIncomingMessageProgressHandler DirectIMIncomingMessageProgress;

		/// <summary>
		/// Occurs during a DirectIM session to indicate the progress of an outgoing message
		/// </summary>
		/// <remarks>This event will only fire if the outgoing message contains attachments</remarks>
		event DirectIMOutgoingMessageProgressHandler DirectIMOutgoingMessageProgress;

		/// <summary>
		/// Raises the DirectIM message progress events
		/// </summary>
		/// <param name="incoming">A value indicating whether the message is incoming or outgoing</param>
		/// <param name="cookie">The rendezvous cookie belonging to the DirectIM session</param>
		/// <param name="bytestransfered">The number of bytes transfered so far</param>
		/// <param name="bytestotal">The total number of bytes to be transfered</param>
		void OnDirectIMMessageProgress(bool incoming, Cookie cookie, uint bytestransfered,
													   uint bytestotal);

		/// <summary>
		/// Occurs when a file transfer has been cancelled
		/// </summary>
		event FileTransferCancelledHandler FileTransferCancelled;

		/// <summary>
		/// Raises the <see cref="FileTransferCancelled"/> event
		/// </summary>
		/// <param name="other">The <see cref="UserInfo"/> of the user on the other side of the connection</param>
		/// <param name="cookie">The rendezvous cookie belonging to the cancelled file</param>
		/// <param name="reason">The reason for the cancellation</param>
		void OnFileTransferCancelled(UserInfo other, Cookie cookie, string reason);

		/// <summary>
		/// Raised when a DirectIM session has been cancelled
		/// </summary>
		event FileTransferCancelledHandler DirectIMSessionCancelled;

		/// <summary>
		/// Raises the <see cref="DirectIMSessionCancelled"/> event
		/// </summary>
		/// <param name="cookie">The rendezvous cookie belonging to the cancelled session</param>
		/// <param name="reason">The reason for the cancellation</param>
		void OnDirectIMSessionCancelled(DirectConnection conn, string reason);

		/// <summary>
		/// Raised when a DirectIM session has been closed
		/// </summary>
		event DirectIMSessionChangedHandler DirectIMSessionClosed;

		/// <summary>
		/// Raises the <see cref="DirectIMSessionClosed"/>
		/// </summary>
		/// <param name="other">A <see cref="UserInfo"/> object describing the other session participant</param>
		/// <param name="cookie">The rendezvous cookie belonging to the cancelled session</param>
		void OnDirectIMSessionClosed(UserInfo other, Cookie cookie);

		/// <summary>
		/// Raised when a DirectIM session is ready for data
		/// </summary>
		event DirectIMSessionChangedHandler DirectIMSessionReady;

		/// <summary>
		/// Raises the <see cref="DirectIMSessionReady"/> event
		/// </summary>
		/// <param name="other">A <see cref="UserInfo"/> object describing the other session participant</param>
		/// <param name="cookie">The rendezvous cookie belonging to the session</param>
		void OnDirectConnectionComplete(UserInfo other, Cookie cookie);

		/// <summary>
		/// Occurs when a file transfer has completed
		/// </summary>
		event FileTransferCompletedHandler FileTransferCompleted;

		/// <summary>
		/// Raises the <see cref="FileTransferCompleted"/> event
		/// </summary>
		/// <param name="cookie">The rendezvous cookie belonging to the completed file</param>
		void OnFileTransferCompleted(Cookie cookie);

		/// <summary>
		/// Occurs when a Direct IM has been received
		/// </summary>
		event DirectIMReceivedHandler DirectIMReceived;

		/// <summary>
		/// Raises the <see cref="OscarLib_DirectIMReceived"/> event
		/// </summary>
		/// <param name="message">The <see cref="DirectIM"/> received</param>
		void OnDirectIMReceived(DirectIM message);

		/// <summary>
		/// Sets the session's <see cref="ClientIdentification"/> to the AOL defaults
		/// </summary>
		/// <exception cref="LoggedInException">Thrown when the <see cref="ISession"/> is already logged in</exception>
		void SetDefaultIdentification();

		/// <summary>
		/// Initialize the logging system
		/// </summary>
		/// <param name="baseDir">The directory in which to save log files</param>
		/// <returns>The full logfile path</returns>
		string InitializeLogger(string baseDir);

		/// <summary>
		/// Begins the process of logging in to the OSCAR service
		/// </summary>
		/// <param name="loginserver">The OSCAR login server</param>
		/// <param name="port">The OSCAR service port</param>
		/// <remarks>
		/// <para>
		/// This function is non-blocking, because the login process does not happen
		/// instantly. The OSCAR library will raise the <see cref="ISession.LoginCompleted"/> event
		/// when the login process has finished successfully.
		/// </para>
		/// <para>
		/// The OSCAR library raises periodic status update events throughout the login process
		/// via the <see cref="ISession.StatusUpdate"/> event.
		/// </para>
		/// <para>
		/// Errors may occur during the login process;  if an error occurs, the OSCAR library raises
		/// the <see cref="ISession.ErrorMessage"/> event, and stops the remaining login sequence.
		/// </para>
		/// </remarks>
		/// <exception cref="LoggedInException">Thrown when the <see cref="ISession"/> is already logged in</exception>
		void Logon(string loginserver, int port);

		/// <summary>
		/// This is only connected to the initial login server connection
		/// </summary>
		/// <param name="conn"></param>
		void conn_ServerConnnectionCompleted(Connection conn);

		/// <summary>
		/// Disconnects all active OSCAR connections and resets the session
		/// </summary>
		void Logoff();

		/// <summary>
		/// Sets the client's available message
		/// </summary>
		/// <param name="message">The available message to set</param>
		/// <remarks>
		/// <para>
		/// If the session is logged in at the time SetAvailableMessage is called, the
		/// available message is reset immediately. Otherwise, it will be set once the
		/// session is logged in.
		/// </para>
		/// </remarks>
		void SetAvailableMessage(string message);

		/// <summary>
		/// Requests long information about a user
		/// </summary>
		/// <param name="screenname">The user to get information about</param>
		/// <param name="type">The type of information to request</param>
		/// <remarks>Results are returned by the <see cref="UserInfoReceived"/> event</remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void RequestUserInfo(string screenname, UserInfoRequest type);

		/// <summary>
		/// Requests basic user information
		/// </summary>
		/// <param name="screenname">The user to get information about</param>
		/// <param name="type">The type of information to request</param>
		/// <remarks>
		/// <para>Results are returned by the <see cref="UserInfoReceived"/> event</para>
		/// <para>Gaim uses this method of user info request exclusively</para>
		/// </remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void RequestBasicUserInfo(string screenname, BasicUserInfoRequest type);

		/// <summary>
		/// Requests a list of accounts associated with an email address
		/// </summary>
		/// <param name="email">The email address to use while searching</param>
		/// <throws cref="System.Exception">Thrown when the session is not logged in</throws>
		/// <remarks>Results are returned by the <see cref="SearchByEmailResults"/> event</remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void SearchUsersByEmail(string email);

		/// <summary>
		/// Sends an invitation to join AIM
		/// </summary>
		/// <param name="email">The email address of the person to invite</param>
		/// <param name="text">The text of the invitation</param>
		/// <remarks>
		/// The libfaim documentation contains this delightful vignette:
		/// <code>Once upon a time, there used to be a menu item in AIM clients that
		/// said something like "Invite a friend to use AIM..." and then it would
		/// ask for an email address and it would sent a mail to them saying
		/// how perfectly wonderful the AIM service is and why you should use it
		/// and click here if you hate the person who sent this to you and want to
		/// complain and yell at them in a small box with pretty fonts.</code>
		/// </remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void SendAIMInvitation(string email, string text);

		/// <summary>
		/// Adds a buddy to the client's server-side buddy list
		/// </summary>
		/// <param name="screenname">The screenname of the buddy to add</param>
		/// <param name="parentID">The ID of the parent group of the buddy</param>
		/// <param name="index">The index of the buddy in the group</param>
		/// <param name="alias">The alias of the buddy ("" for none)</param>
		/// <param name="email">The email address of the buddy ("" for none)</param>
		/// <param name="comment">The comment to be stored for the buddy ("" for none)</param>
		/// <param name="SMS">The SMS number for the buddy ("" for none)</param>
		/// <param name="soundfile">The soundfile for the buddy ("" for none)</param>
		/// <param name="authorziationRequired"><c>true</c> if we require authorization for this buddy, <c>false</c> otherwise</param>
		///<param name="authorizationReason">The authorization reason/message that will be send to the client</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		/// <remarks>This function will probably not remain here; the SSI Manager will be made public</remarks>
		[Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddBuddy method without the index parameter.")]
		void AddBuddy(string screenname, ushort parentID, int index, string alias, string email, string comment,
									  string SMS, string soundfile, bool authorziationRequired, string authorizationReason);

		/// <summary>
		/// Adds a buddy to the client's server-side buddy list
		/// </summary>
		/// <param name="screenname">The screenname of the buddy to add</param>
		/// <param name="parentID">The ID of the parent group of the buddy</param>
		/// <param name="alias">The alias of the buddy ("" for none)</param>
		/// <param name="email">The email address of the buddy ("" for none)</param>
		/// <param name="comment">The comment to be stored for the buddy ("" for none)</param>
		/// <param name="SMS">The SMS number for the buddy ("" for none)</param>
		/// <param name="soundfile">The soundfile for the buddy ("" for none)</param>
		/// <param name="authorziationRequired"><c>true</c> if we require authorization for this buddy, <c>false</c> otherwise</param>
		///<param name="authorizationReason">The authorization reason/message that will be send to the client</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		/// <remarks>This function will probably not remain here; the SSI Manager will be made public</remarks>
		void AddBuddy(string screenname, ushort parentID, string alias, string email, string comment,
									  string SMS, string soundfile, bool authorziationRequired, string authorizationReason);

		/// <summary>
		/// Moves a buddy
		/// </summary>
		/// <param name="buddyID">The ID of the buddy to move</param>
		/// <param name="parentID">The ID of the destination group</param>
		/// <param name="index">The index in the destination group to move to</param>
		void MoveBuddy(ushort buddyID, ushort parentID, int index);

		/// <summary>
		/// Remove a buddy
		/// </summary>
		/// <param name="buddyID">The ID of the buddy to remove</param>
		void RemoveBuddy(ushort buddyID, ushort parentID);

		/// <summary>
		/// Adds a group to the client's server-side buddy list
		/// </summary>
		/// <param name="groupname">The name of the new group</param>
		/// <param name="index">The index into the current list of groups</param>
		[Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddGroup methods without the index parameter.")]
		void AddGroup(string groupname, int index);

		/// <summary>
		/// Adds a group to the client's server-side buddy list
		/// </summary>
		/// <param name="groupname">The name of the new group</param>
		/// <param name="id">The group id</param>
		void AddGroup(string groupname, ushort id);

		/// <summary>
		/// Adds a group to the client's server-side buddy list
		/// </summary>
		/// <param name="groupname">The name of the new group</param>
		void AddGroup(string groupname);

		/// <summary>
		/// Adds the master group. This is necessary if a contact list is empty to add further groups after
		/// </summary>
		/// <param name="groupname">The master group name</param>
		void AddMasterGroup(string groupname);

		/// <summary>
		/// Move a group in the buddy list
		/// </summary>
		/// <param name="groupID">The ID of the group to move</param>
		/// <param name="index">The new index of the group</param>
		void MoveGroup(ushort groupID, int index);

		/// <summary>
		/// Remove a group from the server-side buddy list
		/// </summary>
		/// <param name="groupID">ID of the group to remove</param>
		void RemoveGroup(ushort groupID);

		/// <summary>
		/// Tells AIM to begin sending UserStatus objects to client (online or away)
		/// Client should call in response to <see cref="ContactListFinished"/> event
		/// </summary>
		void ActivateBuddyList();

		/// <summary>
		/// Requests a list of user interests from the server
		/// </summary>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void RequestInterestsList();

		/// <summary>
		/// Sends a client status report to the server
		/// </summary>
		/// <remarks>
		/// <para>
		/// This report contains generic information about the client's machine. Its contents are as follows:
		/// <list>
		/// <item>The time the report was sent</item>
		/// <item>The client's screenname</item>
		/// <item>The name and version of Windows on this machine</item>
		/// <item>The name of this machine's processor</item>
		/// <item>The name and version of this machine's Winsock library</item>
		/// </list>
		/// </para>
		/// <para>This report should be sent at regular intervals by the client application. The minimum required
		/// reporting interval is sent by the server during login and received by the
		/// <see cref="ReportingIntervalReceived"/> event</para>
		/// </remarks>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void SendStatusReport();

		/// <summary>
		/// Sends an icq authorization request
		/// </summary>
		/// <param name="screenname">the destination screenname</param>
		/// <param name="reason">the request reason</param>
		void SendAuthorizationRequest(string screenname, string reason);

		/// <summary>
		/// Sends an icq authorization response
		/// </summary>
		/// <param name="screenname">the destination screenname</param>
		/// <param name="grantAuthorization">true, if the authorization should be granted, otherwise false</param>
		/// <param name="reason">the reason for the decision</param>
		void SendAuthorizationResponse(string screenname, bool grantAuthorization, string reason);

		/// <summary>
		/// Grants the authorization to another screenname for the future
		/// </summary>
		/// <param name="screenname">The uin/screenname</param>
		/// <param name="reason">The reason message</param>
		/// <remarks>TODO ... seems to be obsolete in the current Oscar version</remarks>
		void SendFutureAuthorizationGrant(string screenname, string reason);

		/// <summary>
		/// Sends a requests for the server side buddylist. Server should reply with
		/// the buddylist, or with the info that the client side buddylist is up to date
		/// <remarks>TODO have to be tested</remarks>
		/// </summary>
		void SendContactListCheckout();

		/// <summary>
		/// Sends a requests for the server side buddylist. Server should reply with
		/// the buddylist, or with the info that the client side buddylist is up to date
		/// </summary>
		/// <param name="lastModificationDate">the date when the client side buddylist was updated the last time</param>
		/// <remarks>TODO have to be tested</remarks>
		void SendContactListCheckout(DateTime lastModificationDate);

		/// <summary>
		/// Requests a remote client's buddy icon
		/// </summary>
		/// <param name="screenname">The screenname of the client</param>
		/// <param name="icon">The <see cref="IconInfo"/> of the client's icon, received from the
		/// <see cref="StatusManager.UserStatusReceived"/> event</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		void GetBuddyIcon(string screenname) //, IconInfo icon)
			;

		/// <summary>
		/// Send a file to a remote client via a direct connection
		/// </summary>
		/// <param name="recipient">The screenname of the remote client</param>
		/// <param name="filename">The path of the file to send</param>
		/// <returns>A key with which to reference this file transfer, or "" if a warning was
		/// generated during the initialization process</returns>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		Cookie SendFile(string recipient, string filename);

		/// <summary>
		/// Start a DirectIM session with a remote client via a direct connection
		/// </summary>
		/// <param name="recipient">The screenname of the remote client</param>
		/// <param name="message">A message with which to invite the remote client</param>
		/// <returns>A key with which to reference this DirectIM session, or "" if a warning was
		/// generated during the initialization process</returns>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		Cookie StartDirectIM(string recipient, string message);

		/// <summary>
		/// Send a file to a remote client via an AOL proxy
		/// </summary>
		/// <param name="recipient">The screenname of the remote client</param>
		/// <param name="filename">The path of the file to send</param>
		/// <returns>A key with which to reference this file transfer, or "" if a warning was
		/// generated during the initialization process</returns>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		Cookie SendFileProxied(string recipient, string filename);

		/// <summary>
		/// Start a DirectIM session with a remote client via an AOL proxy
		/// </summary>
		/// <param name="recipient">The screenname of the remote client</param>
		/// <param name="message">A message with which to invite the remote client</param>
		/// <returns>A key with which to reference this DirectIM session, or "" if a warning was
		/// generated during the initialization process</returns>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		Cookie StartDirectIMProxied(string recipient, string message);

		/// <summary>
		/// Accept an invitation to a DirectIM session
		/// </summary>
		/// <param name="key">The key received in the <see cref="OscarLib_DirectIMRequestReceived"/> event</param>
		void AcceptDirectIMSession(Cookie key);

		/// <summary>
		/// Accept a file being sent to the client
		/// </summary>
		/// <param name="key">The key received in the <see cref="FileTransferRequestReceived"/> event</param>
		/// <param name="savelocation">The path to which to save the file</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		/// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
		void AcceptFileTransfer(Cookie key, string savelocation);

		/// <summary>
		/// Cancel a pending or in-progress file transfer
		/// </summary>
		/// <param name="key">The key received with the transfer request</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		/// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
		void CancelFileTransfer(Cookie key);

		/// <summary>
		/// Cancel a pending or in-progress Direct IM session
		/// </summary>
		/// <param name="key">The key received with the connection request</param>
		/// <exception cref="NotLoggedInException">Thrown when the <see cref="ISession"/> is not logged in</exception>
		/// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
		void CancelDirectIMSession(Cookie key);

		/// <summary>
		/// Update the user's ICQ settings on the server
		/// </summary>
		/// <param name="info"></param>
		void SetICQInfo(ICQInfo info);

		/// <summary>
		/// Returns an MD5 hash of the client's password, an authorization key, and a constant string
		/// </summary>
		/// <param name="authkey">The authorization key sent by the server</param>
		/// <returns>A 16-byte MD5 hash</returns>
		/// <remarks>
		/// <para>
		/// The hashing process is fairly simple:
		/// <list>
		/// <item>The authorization key is put into a buffer</item>
		/// <item>The password itself is hashed via MD5 and appended to the buffer</item>
		/// <item>The constant string, "AOL Instant Messenger (SM)", is appended to the buffer in plaintext</item>
		/// <item>The entire buffer is MD5 hashed and returned to the caller</item>
		/// </list>
		/// </para>
		/// <para>
		/// This method exists to prevent the password from having to be passed around in a data structure
		/// </para>
		/// </remarks>
		byte[] HashPassword(byte[] authkey);

		/// <summary>
		/// Stores data associated with a SNAC request/reply
		/// </summary>
		/// <param name="requestid">A SNAC request ID</param>
		/// <param name="data">The data to be stored</param>
		void StoreRequestID(uint requestid, object data);

		/// <summary>
		/// Retrieves data associated with a SNAC request/reply
		/// </summary>
		/// <param name="requestid">A SNAC request ID</param>
		/// <returns>The data previously stored by <see cref="StoreRequestID"/></returns>
		object RetrieveRequestID(uint requestid);

		/// <summary>
		/// Sets the session's privacy setting sent by the server in SNAC(13,06)
		/// </summary>
		/// <param name="ps">One of the <see cref="PrivacySetting"/> enumeration members</param>
		void SetPrivacyFromServer(PrivacySetting ps);

		/// <summary>
		/// Sets whether or not the client's idle time is public -- SNAC(13,06)
		/// </summary>
		/// <param name="publicidletime">true if others can see this client's idle time, false otherwise</param>
		void SetPresence(bool publicidletime);

		/// <summary>
		/// Keeps track of the SNAC parameter responses that have been received thus far
		/// </summary>
		void ParameterSetArrived();
	}

}
