/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using csammisrun.OscarLib.Utility;
using System.IO;

namespace csammisrun.OscarLib
{

    #region Logged in / out exceptions

    /// <summary>
    /// Thrown when an operation is requested that requires the <see cref="Session"/>
    /// to be logged in
    /// </summary>
    public class NotLoggedInException : Exception
    {
        /// <summary>
        /// Creates a new NotLoggedInException
        /// </summary>
        public NotLoggedInException()
        {
        }
    }

    /// <summary>
    /// Thrown when an operation is requested that requires the <see cref="Session"/>
    /// to be logged out
    /// </summary>
    public class LoggedInException : Exception
    {
        private string _message;

        /// <summary>
        /// Creates a new LoggedInException with a blank message
        /// </summary>
        public LoggedInException()
        {
            _message = "";
        }

        /// <summary>
        /// Creates a new LoggedInException
        /// </summary>
        /// <param name="message">The message to be passed with the exception</param>
        public LoggedInException(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Gets a message that describes the current exception
        /// </summary>
        public override string Message
        {
            get { return _message; }
        }
    }

    #endregion

    /// <summary>
    /// The representation of an AOL Instant Messenger or ICQ session
    /// </summary>
    public class Session
    {
        private readonly ConnectionManager connectionManager;
        private readonly ServiceManager serviceManager;
        private readonly SSIManager ssiManager;
        private readonly IcqManager icqManager;
        private readonly MessageManager messageManager;
        private readonly StatusManager statusManager;
        private readonly ChatRoomManager chatRoomManager;
        private readonly GraphicsManager graphicsManager;
        private readonly AuthorizationManager authManager;
        private readonly SearchManager searchManager;
        private readonly RateClassManager rateManager;
        private readonly UsageStatsManager statsManager;
        private readonly PacketDispatcher dispatcher = new PacketDispatcher();
        private readonly FamilyManager familyManager = new FamilyManager();
        private readonly LimitManager limitManager = new LimitManager();

        private string _screenname;
        private string _password;
        private readonly System.Collections.Hashtable _requestidstorage = new System.Collections.Hashtable();
        private bool _loggedin = false;
        private Capabilities _caps = Capabilities.OscarLib;
        private PrivacySetting _privacy;
        private readonly OSCARIdentification _clientid = new OSCARIdentification();
        private readonly ServerInfo _serverSettings = new ServerInfo();

        private string _scratchpath;

        private ushort _parametercount;
        private bool _publicidletime;

        private DateTime lastModificationDate;

        /// <summary>
        /// Gets or Sets the last modification date and time of the buddylist
        /// </summary>
        public DateTime LastModificationDate
        {
            get { return lastModificationDate; }
            set { lastModificationDate = value; }
        }

        #region Public events and protected event firing functions

        #region OscarLib-generated events
        /// <summary>
        /// Occurs when an unhandled exception is raised in the course of dispatching and processing a packet
        /// </summary>
        public event PacketDispatchExceptionHandler PacketDispatchException;
        /// <summary>
        /// Raises the <see cref="PacketDispatchException"/> event
        /// </summary>
        protected internal void OnPacketDispatchException(Exception ex, DataPacket packet)
        {
            if (PacketDispatchException != null)
            {
                PacketDispatchException(this, new PacketDispatchExceptionArgs(ex, packet));
            }
        }

        /// <summary>
        /// Occurs when the library generates a status update message
        /// </summary>
        public event InformationMessageHandler StatusUpdate;

        /// <summary>
        /// Raises the <see cref="StatusUpdate"/> event
        /// </summary>
        /// <param name="message">A status message</param>
        protected internal void OnStatusUpdate(string message)
        {
            if (this.StatusUpdate != null)
                this.StatusUpdate(this, message);
        }

        /// <summary>
        /// Occurs when the library generates a status update message during login
        /// </summary>
        public event LoginStatusUpdateHandler LoginStatusUpdate;

        /// <summary>
        /// Raises the <see cref="LoginStatusUpdate"/> event
        /// </summary>
        /// <param name="message">A status message</param>
        /// <param name="percentdone">The percentage of the login progress that has been completed</param>
        protected internal void OnLoginStatusUpdate(string message, double percentdone)
        {
            if (this.LoginStatusUpdate != null)
            {
                this.LoginStatusUpdate(this, message, percentdone);
            }
        }

        /// <summary>
        /// Occurs when the library generates a warning message
        /// </summary>
        public event WarningMessageHandler WarningMessage;

        /// <summary>
        /// Raises the <see cref="WarningMessage"/> event
        /// </summary>
        /// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the warning</param>
        /// <param name="dp">The Datapacket of the transfer with the error</param>
        protected internal void OnWarning(ServerErrorCode errorcode, DataPacket dp)
        {
            // csammis:  Losing a secondary connection (chat room, icon downloader)
            // isn't cause for logging off the session...and setting LoggedIn to false
            // doesn't log off the session anyway.  Call .Logoff() for that.

            //if (errorcode == ServerErrorCode.LostSecondaryConnection)
            //    this.LoggedIn = false;

            if(dp != null)
            {
                Logging.WriteString("OnWarning: {0}, RequestId: {1}", errorcode.ToString(), dp.SNAC.RequestID);
            }

            if (this.WarningMessage != null)
            {
                this.WarningMessage(this, errorcode);
            }
        }

        /// <summary>
        /// Occurs when the library generates an error message
        /// </summary>
        public event ErrorMessageHandler ErrorMessage;

        /// <summary>
        /// Raises the <see cref="ErrorMessage"/> event or the <see cref="LoginFailed"/> event
        /// </summary>
        /// <param name="errorcode">A <see cref="ServerErrorCode"/> describing the error</param>
        /// <param name="dp">The Datapacket of the transfer with the error</param>
        /// <remarks>If the login process has not completed, <see cref="LoginFailed"/> is raised.
        /// Otherwise, <see cref="ErrorMessage"/> is raised.</remarks>
        protected internal void OnError(ServerErrorCode errorcode, DataPacket dp)
        {
            if (dp != null) {
                Logging.WriteString("OnError: {0}, RequestId: {1}", errorcode.ToString(), dp.SNAC.RequestID);
            }

            if (!_loggedin)
            {
                if (this.LoginFailed != null)
                {
                    if (errorcode == ServerErrorCode.LostBOSConnection)
                    {
                        this.LoggedIn = false;
                        this.LoginFailed(this, LoginErrorCode.CantReachBOSServer);
                    }
                    else
                        this.LoginFailed(this, LoginErrorCode.UnknownError);
                }
            }
            else
            {
                if (this.ErrorMessage != null)
                    this.ErrorMessage(this, errorcode);
            }
        }

        #endregion

        #region SNAC01 events

        /// <summary>
        /// Occurs when the login process is complete.
        /// </summary>
        public event LoginCompletedHandler LoginCompleted;

        /// <summary>
        /// Raises the <see cref="LoginCompleted"/> event
        /// </summary>
        protected internal void OnLoginComplete()
        {
            LoggedIn = true;
            if (LoginCompleted != null)
            {
                LoginCompleted(this);
            }
        }

        /// <summary>
        /// Occurs when a remote client has warned this client
        /// </summary>
        public event WarningReceivedHandler WarningReceived;

        /// <summary>
        /// Raises the <see cref="WarningReceived"/> event.
        /// </summary>
        /// <param name="newlevel">The client's new warning level</param>
        /// <param name="anonymous"><c>true</c> if this warning was sent anonymously, <c>false</c> otherwise</param>
        /// <param name="ui">A <see cref="UserInfo"/> structure describing the warning user. If <paramref name="anonymous"/> is
        /// <c>true</c>, this structure is unpopulated</param>
        protected internal void OnWarningReceived(ushort newlevel, bool anonymous, UserInfo ui)
        {
            if (this.WarningReceived != null)
                this.WarningReceived(this, newlevel, anonymous, ui);
        }

        #endregion

        #region SNAC02 events
        /// <summary>
        /// Occurs when the server sends acknowledgement of a directory update request
        /// </summary>
        public event DirectoryUpdateAcknowledgedHandler DirectoryUpdateAcknowledged;

        /// <summary>
        /// Raises the <see cref="DirectoryUpdateAcknowledged"/> event
        /// </summary>
        /// <param name="success"><c>true</c> if the directory update succeded, and <c>false</c> otherwise</param>
        protected internal void OnDirectoryUpdateAcknowledged(bool success)
        {
            if (this.DirectoryUpdateAcknowledged != null)
                this.DirectoryUpdateAcknowledged(this, success);
        }

        #endregion

        #region SNAC04 events

        /// <summary>
        /// Occurs when a file transfer request is received
        /// </summary>
        public event FileTransferRequestReceivedHandler FileTransferRequestReceived;

        /// <summary>
        /// Occurs when a Direct IM transfer request is received
        /// </summary>
        public event DirectIMRequestReceivedHandler DirectIMRequestReceived;

        /// <summary>
        /// Raises the <see cref="FileTransferRequestReceived"/> event
        /// </summary>
        /// <param name="key">The unique key needed to respond to this request</param>
        protected internal void OnDirectConnectionRequestReceived(Cookie key)
        {
            DirectConnection conn = Connections.GetDirectConnectionByCookie(key);

            if (conn is FileTransferConnection && this.FileTransferRequestReceived != null)
            {
                FileTransferConnection ftc = conn as FileTransferConnection;
                this.FileTransferRequestReceived(this, ftc.Other, ftc.VerifiedIP, ftc.FileHeader.Name,
                                                 ftc.TotalFileSize, ftc.Message, key);
            }
            else if (conn is DirectIMConnection && this.DirectIMRequestReceived != null)
            {
                this.DirectIMRequestReceived(this, conn.Other, conn.Message, key);
            }
            //else if (rd.DirectConnection.ConnectionType == DirectConnectType.DirectIM &&
            //  this.OscarLib_DirectIMRequestReceived != null)
            //{
            //  this.OscarLib_DirectIMRequestReceived(this, rd.UserInfo);
            //}
        }

        /// <summary>
        /// Occurs when a chat room invitation is received
        /// </summary>
        public event ChatInvitationReceivedHandler ChatInvitationReceived;

        /// <summary>
        /// Raises the <see cref="ChatInvitationReceived"/> event
        /// </summary>
        /// <param name="sender">A <see cref="UserInfo"/> object represnting the inviter</param>
        /// <param name="roomname">The name of the chatroom</param>
        /// <param name="message">An invitation chatroom</param>
        /// <param name="encoding">The text encoding used in the chatroom</param>
        /// <param name="language">The language used in the chatroom</param>
        /// <param name="key">The unique key needed to respond to this request</param>
        protected internal void OnChatInvitationReceived(UserInfo sender,
                                                         string roomname,
                                                         string message,
                                                         Encoding encoding,
                                                         string language,
                                                         Cookie key)
        {
            if (this.ChatInvitationReceived != null)
                this.ChatInvitationReceived(this, sender, roomname, message, encoding, language, key);
        }

        #endregion

        #region SNAC0F events

        /// <summary>
        /// Occurs when the server sends the results of a directory search
        /// </summary>
        public event SearchResultsHandler SearchResults;

        /// <summary>
        /// Raises the <see cref="SearchResults"/> event
        /// </summary>
        /// <param name="results">The results of the directory search</param>
        protected internal void OnSearchResults(DirectoryEntry[] results)
        {
            if (this.SearchResults != null)
                this.SearchResults(this, results);
        }

        /// <summary>
        /// Occurs when the server sends a list of interests
        /// </summary>
        public event InterestsReceivedHandler InterestsReceived;

        /// <summary>
        /// Raises the <see cref="InterestsReceived"/> event
        /// </summary>
        /// <param name="results">The results of the interests request</param>
        protected internal void OnInterestsReceived(InterestItem[] results)
        {
            if (this.InterestsReceived != null)
                this.InterestsReceived(this, results);
        }

        #endregion

        #region SNAC13 events

        /// <summary>
        /// Occurs when the buddy list has been completely sent by the server
        /// </summary>
        public event ContactListFinishedHandler ContactListFinished;

        /// <summary>
        /// Notifies the server to activate the SSI data for the client, and to begin
        /// alerting its contacts that it is now online and ready to receive messages
        /// 
        /// Implementing clients should call <see cref="ActivateBuddyList"/> in response to this event
        /// </summary>
        protected internal void OnContactListFinished(DateTime lastModificationDate)
        {
            if (this.ContactListFinished != null)
            {
                this.LastModificationDate = lastModificationDate;
                this.ContactListFinished(this, lastModificationDate);
            }
        }

        /// <summary>
        /// Raised when a remote ICQ user adds the locally logged in UIN to their list
        /// </summary>
        public event AddedToRemoteListEventHandler AddedToRemoteList;

        protected internal void OnAddedToRemoteList(string uin)
        {
            if (AddedToRemoteList != null)
                AddedToRemoteList(this, new AddedToRemoteListEventArgs(uin));
        }

        /// <summary>
        /// Occurs when the server sends a new buddy item to the client
        /// </summary>
        public event BuddyItemReceivedHandler BuddyItemReceived;

        /// <summary>
        /// Raises the <see cref="BuddyItemReceived"/> event
        /// </summary>
        /// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
        protected internal void OnBuddyItemReceived(SSIBuddy buddy)
        {
            if (this.BuddyItemReceived != null)
                this.BuddyItemReceived(this, buddy);
        }

        /// <summary>
        /// Occurs when a buddy item has been removed from the server-side list
        /// </summary>
        public event BuddyItemRemovedHandler BuddyItemRemoved;

        /// <summary>
        /// Raises the <see cref="BuddyItemRemoved"/> event
        /// </summary>
        /// <param name="buddy">An <see cref="SSIBuddy"/> object</param>
        protected internal void OnBuddyItemRemoved(SSIBuddy buddy)
        {
            if (this.BuddyItemRemoved != null)
            {
                this.BuddyItemRemoved(this, buddy);
            }
        }

        /// <summary>
        /// Occurs when the server sends a new group item to the client
        /// </summary>
        public event GroupItemReceivedHandler GroupItemReceived;

        /// <summary>
        /// Raises the <see cref="GroupItemReceived"/> event
        /// </summary>
        /// <param name="group">An <see cref="SSIGroup"/>"/> object</param>
        protected internal void OnGroupItemReceived(SSIGroup group)
        {
            if (this.GroupItemReceived != null)
                this.GroupItemReceived(this, group);
        }

        /// <summary>
        /// Occurs when a buddy item has been removed from the server-side list
        /// </summary>
        public event GroupItemRemovedHandler GroupItemRemoved;

        /// <summary>
        /// Raises the <see cref="GroupItemRemoved"/> event
        /// </summary>
        /// <param name="group">An <see cref="SSIGroup"/> object</param>
        protected internal void OnGroupItemRemoved(SSIGroup group)
        {
            if (this.GroupItemRemoved != null)
            {
                this.GroupItemRemoved(this, group);
            }
        }

        /// <summary>
        /// Occurs when the server sends the master group item to the client
        /// </summary>
        public event MasterGroupItemReceivedHandler MasterGroupItemReceived;

        /// <summary>
        /// Raises the <see cref="MasterGroupItemReceived"/> event
        /// </summary>
        /// <param name="numgroups">The number of groups we are going to receive</param>
        protected internal void OnMasterGroupItemReceived(int numgroups)
        {
            if (this.MasterGroupItemReceived != null)
                this.MasterGroupItemReceived(this, numgroups);
        }

        /// <summary>
        /// Occurs when the an SSI edit is completed
        /// </summary>
        public event SSIEditCompleteHandler SSIEditComplete;

        /// <summary>
        /// Raises the <see cref="SSIEditComplete"/> event
        /// </summary>
        protected internal void OnSSIEditComplete()
        {
            if (this.SSIEditComplete != null)
            {
                this.SSIEditComplete(this);
            }
        }

        /// <summary>
        /// Occurs when a client ask for authorization (ICQ)
        /// </summary>
        public event AuthorizationRequestReceivedHandler AuthorizationRequestReceived;

        /// <summary>
        /// Raises the <see cref="AuthorizationRequestReceived"/> event
        /// </summary>
        /// <param name="screenname">the screenname that ask for authorization</param>
        /// <param name="reason">the reason message</param>
        protected internal void OnAuthorizationRequestReceived(string screenname, string reason)
        {
            if (this.AuthorizationRequestReceived != null)
                this.AuthorizationRequestReceived(this, screenname, reason);
        }


        /// <summary>
        /// Occurs when a client granted or declined the authorization (ICQ)
        /// </summary>
        public event AuthorizationResponseReceivedHandler AuthorizationResponseReceived;

        /// <summary>
        /// Raises the <see cref="AuthorizationResponseReceived"/> event
        /// </summary>
        /// <param name="screenname">the screenname that should get the response</param>
        /// <param name="authorizationGranted">Determines, if the authorization will be granted or not.</param>
        /// <param name="reason">The reason message</param>
        protected internal void OnAuthorizationResponseReceived(string screenname, bool authorizationGranted,
                                                                string reason)
        {
            if (this.AuthorizationResponseReceived != null)
                this.AuthorizationResponseReceived(this, screenname, authorizationGranted, reason);
        }

        /// <summary>
        /// Occurs when a client granted the authorization for the future (ICQ)
        /// </summary>
        public event FutureAuthorizationReceivedHandler FutureAuthorizationReceived;

        /// <summary>
        /// Raises the <see cref="FutureAuthorizationReceived"/> event
        /// </summary>
        /// <param name="screenname">the screenname that should get the future authorization</param>
        /// <param name="reason">The reason message</param>
        protected internal void OnFutureAuthorizationReceived(string screenname, string reason)
        {
            //KSD-SYSTEMS - changed at 24.02.2010 -> from OnAuthorizationResponseReceived to current Methode-Name
            if (this.FutureAuthorizationReceived != null)
                this.FutureAuthorizationReceived(this, screenname, reason);
        }

        #endregion

        #region Authorization manager events

        /// <summary>
        /// Occurs when the login sequence fails
        /// </summary>
        public event LoginFailedHandler LoginFailed;

        /// <summary>
        /// Raises the <see cref="LoginFailed"/> event
        /// </summary>
        /// <param name="errorcode">A <see cref="LoginErrorCode"/> describing the failure</param>
        protected internal void OnLoginFailed(LoginErrorCode errorcode)
        {
            if (LoginFailed != null)
            {
                LoggedIn = false;
                LoginFailed(this, errorcode);
            }
        }

        #endregion

        #region Direct Connection events

        /// <summary>
        /// Occurs during a file transfer to indicate transfer progression
        /// </summary>
        public event FileTransferProgressHandler FileTransferProgress;

        /// <summary>
        /// Raises the <see cref="FileTransferProgress"/> event
        /// </summary>
        /// <param name="cookie">The rendezvous cookie belonging to the file being transfered</param>
        /// <param name="bytestransfered">The number of bytes transfered so far</param>
        /// <param name="bytestotal">The total number of bytes to be transfered</param>
        protected internal void OnFileTransferProgress(Cookie cookie,
                                                       uint bytestransfered, uint bytestotal)
        {
            if (this.FileTransferProgress != null)
            {
                this.FileTransferProgress(this, cookie, bytestransfered, bytestotal);
            }
        }

        /// <summary>
        /// Occurs during a DirectIM session to indicate the progress of an incoming message
        /// </summary>
        /// <remarks>This event will only fire if the incoming message contains attachments</remarks>
        public event DirectIMIncomingMessageProgressHandler DirectIMIncomingMessageProgress;

        /// <summary>
        /// Occurs during a DirectIM session to indicate the progress of an outgoing message
        /// </summary>
        /// <remarks>This event will only fire if the outgoing message contains attachments</remarks>
        public event DirectIMOutgoingMessageProgressHandler DirectIMOutgoingMessageProgress;

        /// <summary>
        /// Raises the DirectIM message progress events
        /// </summary>
        /// <param name="incoming">A value indicating whether the message is incoming or outgoing</param>
        /// <param name="cookie">The rendezvous cookie belonging to the DirectIM session</param>
        /// <param name="bytestransfered">The number of bytes transfered so far</param>
        /// <param name="bytestotal">The total number of bytes to be transfered</param>
        protected internal void OnDirectIMMessageProgress(bool incoming, Cookie cookie, uint bytestransfered,
                                                          uint bytestotal)
        {
            if (incoming)
            {
                if (DirectIMIncomingMessageProgress != null)
                {
                    DirectIMIncomingMessageProgress(this, cookie, bytestransfered, bytestotal);
                }
            }
            else
            {
                if (DirectIMOutgoingMessageProgress != null)
                {
                    DirectIMOutgoingMessageProgress(this, cookie, bytestransfered, bytestotal);
                }
            }
        }

        /// <summary>
        /// Occurs when a file transfer has been cancelled
        /// </summary>
        public event FileTransferCancelledHandler FileTransferCancelled;

        /// <summary>
        /// Raises the <see cref="FileTransferCancelled"/> event
        /// </summary>
        /// <param name="other">The <see cref="UserInfo"/> of the user on the other side of the connection</param>
        /// <param name="cookie">The rendezvous cookie belonging to the cancelled file</param>
        /// <param name="reason">The reason for the cancellation</param>
        protected internal void OnFileTransferCancelled(UserInfo other, Cookie cookie, string reason)
        {
            if (this.FileTransferCancelled != null)
            {
                this.FileTransferCancelled(this, other, cookie, reason);
            }
        }

        /// <summary>
        /// Raised when a DirectIM session has been cancelled
        /// </summary>
        public event FileTransferCancelledHandler DirectIMSessionCancelled;

        /// <summary>
        /// Raises the <see cref="DirectIMSessionCancelled"/> event
        /// </summary>
        /// <param name="cookie">The rendezvous cookie belonging to the cancelled session</param>
        /// <param name="reason">The reason for the cancellation</param>
        protected internal void OnDirectIMSessionCancelled(DirectConnection conn, string reason)
        {
            Connections.RemoveDirectConnection(conn.Cookie);
            Messages.SendDirectConnectionCancellation(conn, reason);

            if (this.DirectIMSessionCancelled != null)
            {
                this.DirectIMSessionCancelled(this, conn.Other, conn.Cookie, reason);
            }
        }

        /// <summary>
        /// Raised when a DirectIM session has been closed
        /// </summary>
        public event DirectIMSessionChangedHandler DirectIMSessionClosed;

        /// <summary>
        /// Raises the <see cref="DirectIMSessionClosed"/>
        /// </summary>
        /// <param name="other">A <see cref="UserInfo"/> object describing the other session participant</param>
        /// <param name="cookie">The rendezvous cookie belonging to the cancelled session</param>
        protected internal void OnDirectIMSessionClosed(UserInfo other, Cookie cookie)
        {
            Connections.RemoveDirectConnection(cookie);
            if (this.DirectIMSessionClosed != null)
            {
                this.DirectIMSessionClosed(this, other, cookie);
            }
        }

        /// <summary>
        /// Raised when a DirectIM session is ready for data
        /// </summary>
        public event DirectIMSessionChangedHandler DirectIMSessionReady;

        /// <summary>
        /// Raises the <see cref="DirectIMSessionReady"/> event
        /// </summary>
        /// <param name="other">A <see cref="UserInfo"/> object describing the other session participant</param>
        /// <param name="cookie">The rendezvous cookie belonging to the session</param>
        protected internal void OnDirectConnectionComplete(UserInfo other, Cookie cookie)
        {
            if (this.DirectIMSessionReady != null)
            {
                this.DirectIMSessionReady(this, other, cookie);
            }
        }

        /// <summary>
        /// Occurs when a file transfer has completed
        /// </summary>
        public event FileTransferCompletedHandler FileTransferCompleted;

        /// <summary>
        /// Raises the <see cref="FileTransferCompleted"/> event
        /// </summary>
        /// <param name="cookie">The rendezvous cookie belonging to the completed file</param>
        protected internal void OnFileTransferCompleted(Cookie cookie)
        {
            if (this.FileTransferCompleted != null)
            {
                this.FileTransferCompleted(this, cookie);
            }
        }

        /// <summary>
        /// Occurs when a Direct IM has been received
        /// </summary>
        public event DirectIMReceivedHandler DirectIMReceived;

        /// <summary>
        /// Raises the <see cref="OscarLib_DirectIMReceived"/> event
        /// </summary>
        /// <param name="message">The <see cref="DirectIM"/> received</param>
        protected internal void OnDirectIMReceived(DirectIM message)
        {
            if (this.DirectIMReceived != null)
            {
                this.DirectIMReceived(this, message);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Create a new OSCAR session
        /// </summary>
        /// <param name="screenname">The screenname to log in</param>
        /// <param name="password">The password associated with the screenname</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="screenname"/> is not
        /// a valid AIM or ICQ screenname.</exception>
        public Session(string screenname, string password)
        {
            // Check to make sure the screenname is something valid
            if (!ScreennameVerifier.IsValidAIM(screenname) &&
                !ScreennameVerifier.IsValidICQ(screenname))
            {
                throw new ArgumentException(screenname + " is not a valid AIM or ICQ screenname", "screenname");
            }

            // Save parameter values
            _screenname = screenname;
            _password = password;

            connectionManager = new ConnectionManager(this);
            serviceManager = new ServiceManager(this);
            ssiManager = new SSIManager(this);
            icqManager = new IcqManager(this);
            messageManager = new MessageManager(this);
            statusManager = new StatusManager(this);
            chatRoomManager = new ChatRoomManager(this);
            graphicsManager = new GraphicsManager(this);
            authManager = new AuthorizationManager(this);
            searchManager = new SearchManager(this);
            rateManager = new RateClassManager(this);
            statsManager = new UsageStatsManager(this);

            connectionManager.CreateNewConnection(0x0017);

            // Create a default set of capabilities for this session
            SetDefaultIdentification();

            // Set up some default values for public properties
            _publicidletime = true;

            // Set initial values for internal properties
            _loggedin = false;
            _privacy = PrivacySetting.AllowAllUsers;
            _parametercount = 0;
        }

        #region Public methods

        /// <summary>
        /// Sets the session's <see cref="ClientIdentification"/> to the AOL defaults
        /// </summary>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public void SetDefaultIdentification()
        {
            if (LoggedIn)
            {
                throw new LoggedInException("Identification cannot be changed after the session is logged in");
            }
            _clientid.ClientName = Constants.CLIENT_NAME;
            _clientid.ClientId = Constants.CLIENT_ID;
            _clientid.ClientMajor = Constants.CLIENT_MAJOR;
            _clientid.ClientMinor = Constants.CLIENT_MINOR;
            _clientid.ClientLesser = Constants.CLIENT_LESSER;
            _clientid.ClientBuild = Constants.CLIENT_BUILD;
            _clientid.ClientDistribution = Constants.CLIENT_DISTRIBUTION;
        }

        /// <summary>
        /// Initialize the logging system
        /// </summary>
        /// <param name="baseDir">The directory in which to save log files</param>
        /// <returns>The full logfile path</returns>
        public string InitializeLogger(string baseDir)
        {
            // Initialize the logging system
            DateTime currentTime = DateTime.Now;
            string logfileName = String.Empty;

            logfileName += this.ScreenName + "_";
            logfileName += currentTime.Year.ToString("0000") + "-" + currentTime.Month.ToString("00") + "-" + currentTime.Day.ToString("00") + "_";
            logfileName += currentTime.Hour.ToString("00") + "." + currentTime.Minute.ToString("00") + "." + currentTime.Second.ToString("00") + ".";
            logfileName += currentTime.Millisecond.ToString("000") + "_";
            logfileName += "OscarLib.log";

            string logFilePath = Path.Combine(baseDir, logfileName);
            if(!Directory.Exists(Path.GetDirectoryName(logFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            }

            Logging.sw = new StreamWriter(logFilePath);

            return logFilePath;
        }

        /// <summary>
        /// Begins the process of logging in to the OSCAR service
        /// </summary>
        /// <param name="loginserver">The OSCAR login server</param>
        /// <param name="loginport">The OSCAR service port</param>
        /// <param name="loginssl">Use ssl protocol</param>
        /// <remarks>
        /// <para>
        /// This function is non-blocking, because the login process does not happen
        /// instantly. The OSCAR library will raise the <see cref="Session.LoginCompleted"/> event
        /// when the login process has finished successfully.
        /// </para>
        /// <para>
        /// The OSCAR library raises periodic status update events throughout the login process
        /// via the <see cref="Session.StatusUpdate"/> event.
        /// </para>
        /// <para>
        /// Errors may occur during the login process;  if an error occurs, the OSCAR library raises
        /// the <see cref="Session.ErrorMessage"/> event, and stops the remaining login sequence.
        /// </para>
        /// </remarks>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public void Logon(string loginserver, int loginport, bool loginssl)
        {
            ServerSettings.LoginServer = loginserver;
            ServerSettings.LoginPort = loginport;
            ServerSettings.LoginSsl = loginssl;
            Logon();
        }

        /// <summary>
        /// Begins the process of logging in to the OSCAR service with default server
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function is non-blocking, because the login process does not happen
        /// instantly. The OSCAR library will raise the <see cref="Session.LoginCompleted"/> event
        /// when the login process has finished successfully.
        /// </para>
        /// <para>
        /// The OSCAR library raises periodic status update events throughout the login process
        /// via the <see cref="Session.StatusUpdate"/> event.
        /// </para>
        /// <para>
        /// Errors may occur during the login process;  if an error occurs, the OSCAR library raises
        /// the <see cref="Session.ErrorMessage"/> event, and stops the remaining login sequence.
        /// </para>
        /// </remarks>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public void Logon()
        {
            if (LoggedIn) 
            {
                throw new LoggedInException();
            }

            LoggedIn = false;
            Authorization.LoginToService(ServerSettings.LoginServer, ServerSettings.LoginPort, ServerSettings.LoginSsl);
            OnLoginStatusUpdate("Connecting to server", 0.00);
        }

        /// <summary>
        /// Disconnects all active OSCAR connections and resets the session
        /// </summary>
        public void Logoff()
        {
            LoggedIn = false;

            foreach (Connection conn in connectionManager.UniqueConnections())
            {
                conn.DisconnectFromServer(false);
            }
        }

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
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        /// <remarks>This function will probably not remain here; the SSI Manager will be made public</remarks>
        [Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddBuddy method without the index parameter.")]
        public void AddBuddy(string screenname, ushort parentID, int index, string alias, string email, string comment,
                             string SMS, string soundfile, bool authorziationRequired, string authorizationReason)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.AddBuddy(screenname, parentID, index, alias, email, SMS, comment, soundfile, authorziationRequired,
                              authorizationReason);
        }
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
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        /// <remarks>This function will probably not remain here; the SSI Manager will be made public</remarks>
        public void AddBuddy(string screenname, ushort parentID, string alias, string email, string comment,
                             string SMS, string soundfile, bool authorziationRequired, string authorizationReason)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.AddBuddy(screenname, parentID, alias, email, SMS, comment, soundfile, authorziationRequired,
                              authorizationReason);
        }

        /// <summary>
        /// Moves a buddy
        /// </summary>
        /// <param name="buddyID">The ID of the buddy to move</param>
        /// <param name="parentID">The ID of the destination group</param>
        /// <param name="index">The index in the destination group to move to</param>
        public void MoveBuddy(ushort buddyID, ushort parentID, int index)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.MoveBuddy(this.SSI.GetBuddyByID(buddyID), this.SSI.GetGroupByID(parentID), index);
        }

        /// <summary>
        /// Remove a buddy
        /// </summary>
        /// <param name="buddyID">The ID of the buddy to remove</param>
        public void RemoveBuddy(ushort buddyID)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            SSIBuddy buddy = this.SSI.GetBuddyByID(buddyID);
            if(buddy != null)
                this.SSI.RemoveBuddy(buddy);
        }

        /// <summary>
        /// Adds a group to the client's server-side buddy list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        /// <param name="index">The index into the current list of groups</param>
        [Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddGroup methods without the index parameter.")]
        public void AddGroup(string groupname, int index)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.AddGroup(groupname, index);
        }

        /// <summary>
        /// Adds a group to the client's server-side buddy list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        /// <param name="id">The group id</param>
        public void AddGroup(string groupname, ushort id)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.AddGroup(groupname, id);
        }
        
        /// <summary>
        /// Adds a group to the client's server-side buddy list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        public void AddGroup(string groupname)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.AddGroup(groupname);
        }

        /// <summary>
        /// Adds the master group. This is necessary if a contact list is empty to add further groups after
        /// </summary>
        /// <param name="groupname">The master group name</param>
        public void AddMasterGroup(string groupname)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            this.AddGroup(groupname, (ushort)0);
        }

        /// <summary>
        /// Move a group in the buddy list
        /// </summary>
        /// <param name="groupID">The ID of the group to move</param>
        /// <param name="index">The new index of the group</param>
        public void MoveGroup(ushort groupID, int index)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            this.SSI.MoveGroup(this.SSI.GetGroupByID(groupID), index);
        }

        /// <summary>
        /// Remove a group from the server-side buddy list
        /// </summary>
        /// <param name="groupID">ID of the group to remove</param>
        public void RemoveGroup(ushort groupID)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            SSIGroup group = this.SSI.GetGroupByID(groupID);
            if (group != null)
                this.SSI.RemoveGroup(group);
        }

        /// <summary>
        /// Tells AIM to begin sending UserStatus objects to client (online or away)
        /// Client should call in response to <see cref="ContactListFinished"/> event
        /// </summary>
        public void ActivateBuddyList()
        {
            SNAC13.ActivateSSI(this);
            OnLoginComplete();
        }

        /// <summary>
        /// Requests a list of user interests from the server
        /// </summary>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public void RequestInterestsList()
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            SNAC0F.RequestInterestList(this);
        }

        /// <summary>
        /// Sends an icq authorization request
        /// </summary>
        /// <param name="screenname">the destination screenname</param>
        /// <param name="reason">the request reason</param>
        public void SendAuthorizationRequest(string screenname, string reason)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            // TODO reason string works only with ASCII encoding until now
            SNAC13.SendAuthorizationRequest(this, screenname, reason);
        }

        /// <summary>
        /// Sends an icq authorization response
        /// </summary>
        /// <param name="screenname">the destination screenname</param>
        /// <param name="grantAuthorization">true, if the authorization should be granted, otherwise false</param>
        /// <param name="reason">the reason for the decision</param>
        public void SendAuthorizationResponse(string screenname, bool grantAuthorization, string reason)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            SNAC13.SendAuthorizationResponse(this, screenname, grantAuthorization, reason);
        }

        /// <summary>
        /// Grants the authorization to another screenname for the future
        /// </summary>
        /// <param name="screenname">The uin/screenname</param>
        /// <param name="reason">The reason message</param>
        /// <remarks>TODO ... seems to be obsolete in the current Oscar version</remarks>
        public void SendFutureAuthorizationGrant(string screenname, string reason)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            SNAC13.SendFutureAuthorizationGrant(this, screenname, reason);
        }

        /// <summary>
        /// Sends a requests for the server side buddylist. Server should reply with
        /// the buddylist, or with the info that the client side buddylist is up to date
        /// <remarks>TODO have to be tested</remarks>
        /// </summary>
        public void SendContactListCheckout()
        {
            this.SendContactListCheckout(this.LastModificationDate);
        }

        /// <summary>
        /// Sends a requests for the server side buddylist. Server should reply with
        /// the buddylist, or with the info that the client side buddylist is up to date
        /// </summary>
        /// <param name="lastModificationDate">the date when the client side buddylist was updated the last time</param>
        /// <remarks>TODO have to be tested</remarks>
        public void SendContactListCheckout(DateTime lastModificationDate)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }
            ushort localSSIItemCount = this.SSI.GetLocalSSIItemCount();
            SNAC13.SendContactListCheckout(this, this.LastModificationDate, true, localSSIItemCount);
        }

        #region File transfer methods

        /// <summary>
        /// Send a file to a remote client via a direct connection
        /// </summary>
        /// <param name="recipient">The screenname of the remote client</param>
        /// <param name="filename">The path of the file to send</param>
        /// <returns>A key with which to reference this file transfer, or "" if a warning was
        /// generated during the initialization process</returns>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public Cookie SendFile(string recipient, string filename)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            FileTransferConnection ftconn =
                Connections.CreateNewFileTransferConnection(DirectConnectionMethod.Direct, DirectConnectRole.Initiator);
            ftconn.Other.ScreenName = recipient;
            ftconn.LocalFileName = filename;
            ftconn.ConnectToServer();
            return ftconn.Cookie;
        }

        /// <summary>
        /// Start a DirectIM session with a remote client via a direct connection
        /// </summary>
        /// <param name="recipient">The screenname of the remote client</param>
        /// <param name="message">A message with which to invite the remote client</param>
        /// <returns>A key with which to reference this DirectIM session, or "" if a warning was
        /// generated during the initialization process</returns>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public Cookie StartDirectIM(string recipient, string message)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            DirectIMConnection dimconn =
                Connections.CreateNewDirectIMConnection(DirectConnectionMethod.Direct, DirectConnectRole.Initiator);
            dimconn.Other.ScreenName = recipient;
            dimconn.Message = message;
            dimconn.ConnectToServer();
            return dimconn.Cookie;
        }

        /// <summary>
        /// Send a file to a remote client via an AOL proxy
        /// </summary>
        /// <param name="recipient">The screenname of the remote client</param>
        /// <param name="filename">The path of the file to send</param>
        /// <returns>A key with which to reference this file transfer, or "" if a warning was
        /// generated during the initialization process</returns>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public Cookie SendFileProxied(string recipient, string filename)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            FileTransferConnection newconn =
                Connections.CreateNewFileTransferConnection(DirectConnectionMethod.Proxied, DirectConnectRole.Initiator);
            newconn.Other.ScreenName = recipient;
            newconn.LocalFileName = filename;
            newconn.ConnectToServer();
            return newconn.Cookie;
        }

        /// <summary>
        /// Start a DirectIM session with a remote client via an AOL proxy
        /// </summary>
        /// <param name="recipient">The screenname of the remote client</param>
        /// <param name="message">A message with which to invite the remote client</param>
        /// <returns>A key with which to reference this DirectIM session, or "" if a warning was
        /// generated during the initialization process</returns>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public Cookie StartDirectIMProxied(string recipient, string message)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            DirectIMConnection dimconn =
                Connections.CreateNewDirectIMConnection(DirectConnectionMethod.Proxied, DirectConnectRole.Initiator);
            dimconn.Other.ScreenName = recipient;
            dimconn.Message = message;
            dimconn.ConnectToServer();
            return dimconn.Cookie;
        }

        /// <summary>
        /// Accept an invitation to a DirectIM session
        /// </summary>
        /// <param name="key">The key received in the <see cref="OscarLib_DirectIMRequestReceived"/> event</param>
        public void AcceptDirectIMSession(Cookie key)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            DirectIMConnection conn = Connections.GetDirectConnectionByCookie(key) as DirectIMConnection;
            if (conn != null)
            {
                conn.ConnectToServer();
            }
            else
            {
                throw new Exception("Invalid DirectIM session key: \"" + key + "\"");
            }
        }

        /// <summary>
        /// Accept a file being sent to the client
        /// </summary>
        /// <param name="key">The key received in the <see cref="FileTransferRequestReceived"/> event</param>
        /// <param name="savelocation">The path to which to save the file</param>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        /// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
        public void AcceptFileTransfer(Cookie key, string savelocation)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            FileTransferConnection conn = Connections.GetDirectConnectionByCookie(key) as FileTransferConnection;
            if (conn != null)
            {
                conn.LocalFileName = savelocation;
                conn.ConnectToServer();
            }
            else
            {
                throw new Exception("Invalid file transfer key: \"" + key + "\"");
            }
        }

        /// <summary>
        /// Cancel a pending or in-progress file transfer
        /// </summary>
        /// <param name="key">The key received with the transfer request</param>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        /// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
        public void CancelFileTransfer(Cookie key)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            FileTransferConnection conn = Connections.GetDirectConnectionByCookie(key) as FileTransferConnection;
            if (conn != null)
            {
                conn.CancelFileTransfer("User cancelled transfer");
            }
        }

        /// <summary>
        /// Cancel a pending or in-progress Direct IM session
        /// </summary>
        /// <param name="key">The key received with the connection request</param>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        /// <exception cref="System.Exception">Thrown when <paramref name="key"/> is not a valid file transfer key</exception>
        public void CancelDirectIMSession(Cookie key)
        {
            if (!LoggedIn)
            {
                throw new NotLoggedInException();
            }

            DirectIMConnection conn = Connections.GetDirectConnectionByCookie(key) as DirectIMConnection;
            if (conn != null)
            {
                Messages.SendDirectConnectionCancellation(conn, "User cancelled Direct Connection");
                conn.DisconnectFromServer(false);
            }
        }

        #endregion

        #endregion

        #region Internal methods
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
        protected internal byte[] HashPassword(byte[] authkey)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            ByteStream stream = new ByteStream();
            stream.WriteByteArray(authkey);
            stream.WriteByteArray(md5.ComputeHash(Encoding.ASCII.GetBytes(_password)));
            stream.WriteString(Constants.AIM_MD5_STRING, Encoding.ASCII);

            return md5.ComputeHash(stream.GetBytes());
        }

        /// <summary>
        /// Stores data associated with a SNAC request/reply
        /// </summary>
        /// <param name="requestid">A SNAC request ID</param>
        /// <param name="data">The data to be stored</param>
        protected internal void StoreRequestID(uint requestid, object data)
        {
            _requestidstorage.Add(requestid, data);
        }

        /// <summary>
        /// Retrieves data associated with a SNAC request/reply
        /// </summary>
        /// <param name="requestid">A SNAC request ID</param>
        /// <returns>The data previously stored by <see cref="StoreRequestID"/></returns>
        protected internal object RetrieveRequestID(uint requestid)
        {
            return _requestidstorage[requestid];
        }


        /// <summary>
        /// Sets the session's privacy setting sent by the server in SNAC(13,06)
        /// </summary>
        /// <param name="ps">One of the <see cref="PrivacySetting"/> enumeration members</param>
        protected internal void SetPrivacyFromServer(PrivacySetting ps)
        {
            _privacy = ps;
        }

        /// <summary>
        /// Sets whether or not the client's idle time is public -- SNAC(13,06)
        /// </summary>
        /// <param name="publicidletime">true if others can see this client's idle time, false otherwise</param>
        protected internal void SetPresence(bool publicidletime)
        {
            _publicidletime = publicidletime;
        }

        /// <summary>
        /// Keeps track of the SNAC parameter responses that have been received thus far
        /// </summary>
        protected internal void ParameterSetArrived()
        {
            _parametercount++;

            if (_parametercount == 5)
            {
                // We can send more stuff now
            }
        }

        #endregion

        #region Properties
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
        public string ScreenName
        {
            get { return _screenname; }
            set
            {
                if (LoggedIn)
                {
                    if (!(ScreennameVerifier.IsValidAIM(value) || ScreennameVerifier.IsValidICQ(value)))
                    {
                        throw new ArgumentException(value + " is not a valid AIM or ICQ screenname");
                    }
                    _screenname = value;

                    // TODO:  Actually reset the formatting...
                }
            }
        }

        /// <summary>
        /// Gets or sets this session's OSCAR identification information
        /// </summary>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public OSCARIdentification ClientIdentification
        {
            get { return _clientid; }
        }

        /// <summary>
        /// Gets or sets this session's OSCAR identification information
        /// </summary>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public ServerInfo ServerSettings
        {
            get { return _serverSettings; }
        }

        /// <summary>
        /// Gets or sets the OSCAR capabilities associated with the session
        /// </summary>
        /// <remarks>
        /// The client capabilities must be set before the session is logged in because the
        /// client's capabilities are communicated during the login process and are kept through
        /// the session.
        /// </remarks>
        /// <exception cref="LoggedInException">Thrown when the <see cref="Session"/> is already logged in</exception>
        public Capabilities ClientCapabilities
        {
            get { return _caps; }
            set
            {
                if (LoggedIn)
                {
                    throw new LoggedInException("Client capabilities cannot be set after the session is logged in");
                }
                _caps = value;
                if ((value & Capabilities.UTF8) == Capabilities.UTF8)
                {
                    this.encoding = Encoding.UTF8;
                }
                else
                {
                    this.encoding = Encoding.ASCII;
                }
            }
        }

        private Encoding encoding;

        /// <summary>
        /// Gets the recommended enocding format depending on the client capability settings
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
        }

        /// <summary>
        /// Gets a value indicating whether this client has completed the login process
        /// </summary>
        public bool LoggedIn
        {
            get { return _loggedin; }
            protected set { _loggedin = value; }
        }

        /// <summary>
        /// Gets the <see cref="SSIManager"/> associated with this session
        /// </summary>
        public SSIManager SSI
        {
            get { return ssiManager; }
        }

        /// <summary>
        /// Gets the <see cref="LimitManager"/> associated with this session
        /// </summary>
        public LimitManager Limits
        {
            get { return limitManager; }
        }

        /// <summary>
        /// Gets the <see cref="IcqManager"/> associated with this session
        /// </summary>
        public IcqManager ICQ
        {
            get { return icqManager; }
        }

        /// <summary>
        /// Gets the <see cref="MessageManager"/> associated with this session
        /// </summary>
        public MessageManager Messages
        {
            get { return messageManager; }
        }

        /// <summary>
        /// Gets the <see cref="ChatRoomManager"/> associated with this session
        /// </summary>
        public ChatRoomManager ChatRooms
        {
            get { return chatRoomManager; }
        }

        /// <summary>
        /// Gets the <see cref="GraphicsManager"/> associated with this session
        /// </summary>
        public GraphicsManager Graphics
        {
            get { return graphicsManager; }
        }

        /// <summary>
        /// Gets the <see cref="StatusManager"/> associated with this session
        /// </summary>
        public StatusManager Statuses
        {
            get { return statusManager; }
        }

        /// <summary>
        /// Gets the <see cref="SearchManager"/> associated with this session
        /// </summary>
        public SearchManager Searches
        {
            get { return searchManager; }
        }

        /// <summary>
        /// Gets or sets a filesystem path where OscarLib can place received data
        /// </summary>
        /// <remarks>During an OSCAR Direct Connect session, "transient" files may come over the wire.
        /// If ScratchPath is set to a valid path, OscarLib will save the files locally and return
        /// <see cref="System.IO.FileStream"/> references to the objects. Otherwise, the files will
        /// be returned as <see cref="System.IO.MemoryStream"/> objects, which will take more active memory.</remarks>
        public string ScratchPath
        {
            get { return _scratchpath; }
            set { _scratchpath = value; }
        }

        /// <summary>
        /// Gets the <see cref="ConnectionManager"/> associated with this session
        /// </summary>
        internal ConnectionManager Connections
        {
            get { return connectionManager; }
        }

        /// <summary>
        /// Gets the <see cref="ServiceManager"/> associated with this session
        /// </summary>
        internal ServiceManager Services
        {
            get { return serviceManager; }
        }

        /// <summary>
        /// Gets the <see cref="PacketDispatcher"/> associated with this session
        /// </summary>
        internal PacketDispatcher Dispatcher
        {
            get { return dispatcher; }
        }

        /// <summary>
        /// Gets the <see cref="FamilyManager"/> associated with this session
        /// </summary>
        internal FamilyManager Families
        {
            get { return familyManager; }
        }

        /// <summary>
        /// Gets the <see cref="RateClassManager"/> associated with this session
        /// </summary>
        internal RateClassManager RateClasses
        {
            get { return rateManager; }
        }

        /// <summary>
        /// Gets the <see cref="RateClassManager"/> associated with this session
        /// </summary>
        internal UsageStatsManager Stats {
            get { return statsManager; }
        }

        /// <summary>
        /// Gets the <see cref="AuthorizationManager"/> associated with this session
        /// </summary>
        internal AuthorizationManager Authorization
        {
            get { return authManager; }
        }
        #endregion
    }
}