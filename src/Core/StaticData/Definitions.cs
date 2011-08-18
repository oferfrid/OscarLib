/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Describes the potential errors the AOL servers can report during login
    /// </summary>
    public enum LoginErrorCode
    {
        /// <summary>
        /// The screenname is unrecognized or not in a format recognized by OSCAR, or the password does not match the screenname
        /// </summary>
        /// <remarks>The OSCAR server could send either this or <see cref="IncorrectScreennamePassword"/> to indicate a bad username and/or password</remarks>
        InvalidScreennamePassword = 0x0001,
        /// <summary>
        /// A necessary OSCAR service is unavailable
        /// </summary>
        ServiceUnavailable = 0x0002,
        /// <summary>
        /// The server has experienced an unknown error
        /// </summary>
        UnknownError = 0x0003,
        /// <summary>
        /// The screenname is unrecognized or not in a format recognized by OSCAR, or the password does not match the screenname
        /// </summary>
        /// <remarks>The OSCAR server could send either this or <see cref="InvalidScreennamePassword"/> to indicate a bad username and/or password</remarks>
        IncorrectScreennamePassword = 0x0004,
        /// <summary>
        /// The password is incorrect
        /// </summary>
        /// <remarks>Logging in with ICQ accounts has returned 0x0005 for login failed when the password is wrong</remarks>
        WrongPassword = 0x0005,
        /// <summary>
        /// The client sent incorrect authorization sequences
        /// </summary>
        BadAuthorizationInfo = 0x0006,
        /// <summary>
        /// The account has become invalidated
        /// </summary>
        AccountInvalid = 0x0007,
        /// <summary>
        /// The AOL admins have deleted the account
        /// </summary>
        AccountDeleted = 0x0008,
        /// <summary>
        /// The account has expired
        /// </summary>
        AccountExpired = 0x0009,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        DatabaseNoAccess = 0x000A,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ResolverNoAccess = 0x000B,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        DatabaseInvalidFields = 0x000C,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        DatabaseBadStatus = 0x000D,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ResolverBadStatus = 0x000E,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        InternalError = 0x000F,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ServiceOffline = 0x0010,
        /// <summary>
        /// The account has been suspended for activity against the Terms of Service
        /// </summary>
        AccountSuspended = 0x0011,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        DatabaseSendError = 0x0012,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        DatabaseLinkError = 0x0013,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ReservationMapError = 0x0014,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ReservationLinkError = 0x0015,
        /// <summary>
        /// The server has experienced an internal error
        /// </summary>
        ReservationTimeout = 0x001A,
        /// <summary>
        /// Too many accounts are logged in from the same external IP address
        /// </summary>
        TooManyUsersFromIP = 0x0016,
        /// <summary>
        /// The account has attempted to log in too frequently in a span of time
        /// </summary>
        RateLimitExceeded = 0x0018,
        /// <summary>
        /// The account's warning level is too high to allow login
        /// </summary>
        WarningLevelTooHigh = 0x0019,
        /// <summary>
        /// The client is using an outdated version of the ICQ protocol
        /// </summary>
        OldICQVersion = 0x001B,
        /// <summary>
        /// The ICQ number cannot be registered on the network
        /// </summary>
        CantRegisterICQ = 0x001E,
        /// <summary>
        /// An AOL admin account has provided an invalid SecurID for authentication
        /// </summary>
        InvalidSecurID = 0x0020,
        /// <summary>
        /// The AOL admins have started reading your mail and realized you're &lt; 13 years old in real life
        /// </summary>
        AccountSuspendedBecauseOfAge = 0x0022,

        /*  The following error codes are defined by OscarLib, not AOL  */

        /// <summary>
        /// The authorization server cannot be reached
        /// </summary>
        /// <remarks>This is the first server to which OSCAR clients connect.  The first thing to check if this
        /// error code is received is to ensure that your network is functioning correctly. After that, it's AOL's fault.</remarks>
        CantReachAuthServer = 0x1000,
        /// <summary>
        /// The Basic OSCAR Service server cannot be reached
        /// </summary>
        /// <remarks>This is the server to which a client is redirected after successful authentication. Failure to connect
        /// to the BOS server is due to an OSCAR service outage.  Note that there are many BOS servers; some screennames may
        /// be able to connect when others cannot.</remarks>
        CantReachBOSServer = 0x1001,
    }

    /// <summary>
    /// Describes the potential errors the AOL servers can report during an active session
    /// </summary>
    public enum ServerErrorCode : ushort
    {
        /// <summary>
        /// Malformed SNAC header
        /// </summary>
        InvalidSNACHeader = 0x0001,
        /// <summary>
        /// The server cannot send messages as fast as the client is requesting them
        /// </summary>
        ServerRateLimited = 0x0002,
        /// <summary>
        /// The local client has been rate limited
        /// </summary>
        ClientRateLimited = 0x0003,
        /// <summary>
        /// The recepient of a message or request is not logged into the network
        /// </summary>
        ReceiverNotLoggedIn = 0x0004,
        /// <summary>
        /// A secondary service is not available
        /// </summary>
        RequestedServiceUnavailable = 0x0005,
        /// <summary>
        /// The requested secondary service does not exist
        /// </summary>
        RequestedServiceUnknown = 0x0006,
        /// <summary>
        /// The client sent a SNAC that is no longer supported
        /// </summary>
        ObsoleteSNAC = 0x0007,
        /// <summary>
        /// The server does not support the requested feature
        /// </summary>
        NotSupportedByServer = 0x0008,
        /// <summary>
        /// The client has reported during authentication that it does not support a given feature
        /// </summary>
        NotSupportedByClient = 0x0009,
        /// <summary>
        /// The remote client has refused the message
        /// </summary>
        RefusedByClient = 0x000A,
        /// <summary>
        /// The server's reply was too large to transmit
        /// </summary>
        ReplyTooBig = 0x000B,
        /// <summary>
        /// The response to a query was lost by the system
        /// </summary>
        ResponsesLost = 0x000C,
        /// <summary>
        /// The server has denied the client's request
        /// </summary>
        RequestDenied = 0x000D,
        /// <summary>
        /// Malformed SNAC packet contents
        /// </summary>
        InvalidSNACFormat = 0x000E,
        /// <summary>
        /// The client does not have sufficient rights to send a message
        /// </summary>
        /// <remarks>This most likely refers to administrator functions such as SecureID</remarks>
        InsufficientRights = 0x000F,
        /// <summary>
        /// The server will not relay the message because the receiver has been blocked by the sender
        /// </summary>
        ReceiverBlocked = 0x0010,
        /// <summary>
        /// The server will not relay the message because the sender's warning level is too high
        /// </summary>
        SenderWarningTooHigh = 0x0011,
        /// <summary>
        /// The server will not relay the message because the receiver's warning level is too high
        /// </summary>
        ReceiverWarningTooHigh = 0x0012,
        /// <summary>
        /// The remote user was unavailable
        /// </summary>
        /// <remarks>This error has been encountered when attempting to send an IM to a user via SNAC04 during an existing Direct
        /// Connection session with the same user</remarks>
        UserUnavailable = 0x0013,
        /// <summary>
        /// No match to request
        /// </summary>
        NoMatch = 0x0014,
        /// <summary>
        /// Internal server error:  A list has overflowed
        /// </summary>
        ListOverflow = 0x0015,
        /// <summary>
        /// The client's request specified an ambiguous target
        /// </summary>
        AmbiguousRequest = 0x0016,
        /// <summary>
        /// The server is not currently accepting new messages
        /// </summary>
        ServerQueueFull = 0x0017,
        /// <summary>
        /// Operation can't be performed while connected via AOL
        /// </summary>
        NotOnAOL = 0x0018,

        /*  The following error codes are defined by OscarLib, not AOL  */

        /// <summary>
        /// A secondary service connection (chat, buddy icons, etc.) has been lost
        /// </summary>
        LostSecondaryConnection = 0x1001,
        /// <summary>
        /// The primary service connection has been lost
        /// </summary>
        LostBOSConnection = 0x1002,
        /// <summary>
        /// Received unsolicited chat room information
        /// </summary>
        UnrequestedChatRoomInformation = 0x1003,
        /// <summary>
        /// The server requested a function which OscarLib does not support
        /// </summary>
        OscarLibUnsupportedFunction = 0x1004,
        /// <summary>
        /// Client received an unknown SNAC family
        /// </summary>
        UnknownSNACFamily = 0x1005,
        /// <summary>
        /// Client received an unknown ICBM channel
        /// </summary>
        UnknownMessageChannel = 0x1006,
        /// <summary>
        /// Client received an unknown Rendezvous channel
        /// </summary>
        UnknownRendezvousChannel = 0x1007,
        /// <summary>
        /// Unknown error
        /// </summary>
        UnknownError = 0x1008,
        /// <summary>
        /// The client referred to an unknown SSI item
        /// </summary>
        SSIItemNotFound = 0x1009,
        /// <summary>
        /// The client has attempted to add an SSI item with a duplicate ID
        /// </summary>
        SSIItemAlreadyExists = 0x100A,
        /// <summary>
        /// The server cannot add an item due to conflicting information
        /// </summary>
        SSICantAddItem = 0x100B,
        /// <summary>
        /// Too many SSI items of the specified type exist on the server
        /// </summary>
        SSIItemLimitExceeded = 0x100C,
        /// <summary>
        /// Modifying the specified SSI item requires SecureID authorization
        /// </summary>
        SSIItemRequiresAuthorization = 0x100D,
        /// <summary>
        /// Other Client with your ID makes a Request
        /// </summary>
        ExternalClientRequest = 0x100E
   }
}