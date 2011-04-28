/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// The subtypes for family 0x01
    /// </summary>
    internal enum GenericServiceControls
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Client is now online and ready for normal function (client -> server)
        /// </summary>
        ClientOnline = 0x02,
        /// <summary>
        /// List of server-supported SNAC familes (server -> client)
        /// </summary>
        SupportedSNACFamiles = 0x03,
        /// <summary>
        /// Request for new service (client -> server)
        /// </summary>
        NewServiceRequest = 0x04,
        /// <summary>
        /// Redirection for 0x04 subtype (server -> client)
        /// </summary>
        NewServiceResponse = 0x05,
        /// <summary>
        /// Request rate limits information (client -> server)
        /// </summary>
        RateLimitRequest = 0x06,
        /// <summary>
        /// Rate limits information response (server -> client)
        /// </summary>
        RateLimitResponse = 0x07,
        /// <summary>
        /// Acknowledge rate limitations (client -> server)
        /// </summary>
        AcknowledgeRateLimits = 0x08,
        /// <summary>
        /// Delete rate limits group (client -> server)
        /// </summary>
        DeleteRateLimitRequest = 0x09,
        /// <summary>
        /// Rate information changed / rate limit warning (server -> client)
        /// </summary>
        RateLimitChangedResponse = 0x0A,
        /// <summary>
        /// Server pause command (server -> client)
        /// </summary>
        PauseRequest = 0x0B,
        /// <summary>
        /// Client pause acknowledgement (client -> server)
        /// </summary>
        PauseResponse = 0x0C,
        /// <summary>
        /// Server resume command  (server -> client)
        /// </summary>
        ResumeRequest = 0x0D,
        /// <summary>
        /// Request own online information (client -> server)
        /// </summary>
        OwnInformationRequest = 0x0E,
        /// <summary>
        /// Requested online info response (server -> client)
        /// </summary>
        OwnInformationResponse = 0x0F,
        /// <summary>
        /// Evil notification (server -> client)
        /// </summary>
        EvilNotification = 0x10,
        /// <summary>
        /// Set idle time (client -> server)
        /// </summary>
        SetIdleTime = 0x11,
        /// <summary>
        /// Migration notice and information (server -> client)
        /// </summary>
        MigrationNotice = 0x12,
        /// <summary>
        /// Message of the day (MOTD) (server -> client)
        /// </summary>
        MessageOfTheDay = 0x13,
        /// <summary>
        ///  Set privacy flags (client -> server)
        /// </summary>
        SetPrivacyFlags = 0x14,
        /// <summary>
        /// Well-known URLs (server -> client)
        /// </summary>
        WellKnownURLs = 0x15,
        /// <summary>
        /// No operation (NOP) (server -> client)
        /// </summary>
        NoOp = 0x16,
        /// <summary>
        /// Request server services versions (client -> server)
        /// </summary>
        ServerServicesVersionRequest = 0x17,
        /// <summary>
        /// Server services versions (server -> client)
        /// </summary>
        ServerServicesVersionResponse = 0x18,
        /// <summary>
        /// Set extended status (client -> server)
        /// </summary>
        SetExtendedStatus = 0x1E,
        /// <summary>
        /// Client verification request (server -> client)
        /// </summary>
        ClientVerificationRequest = 0x1F,
        /// <summary>
        /// Client verification response (client -> server)
        /// </summary>
        ClientVertificationResponse = 0x20,
        /// <summary>
        ///  Client's extended status from server (server -> client)
        /// </summary>
        ClientExtendedStatus = 0x21
    }

    /// <summary>
    /// The subtypes for family 0x02
    /// </summary>
    internal enum LocationServices
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Limitations / parameters request (client -> server)
        /// </summary>
        LimitationsParamsRequest = 0x02,
        /// <summary>
        /// Limitations / parameters response (server -> client)
        /// </summary>
        LimitationsParamsResponse = 0x03,
        /// <summary>
        /// Set own user information (client -> server)
        /// </summary>
        SetUserInformation = 0x04,
        /// <summary>
        /// Request a client's user information (client -> server)
        /// </summary>
        BasicInformationRequest = 0x05,
        /// <summary>
        /// User information response (server -> client)
        /// </summary>
        UserInformationResponse = 0x06,
        /// <summary>
        /// Watcher subrequest (client -> server)
        /// </summary>
        WatcherSubRequest = 0x07,
        /// <summary>
        /// Watcher notification (server -> client)
        /// </summary>
        WatcherReply = 0x08,
        /// <summary>
        /// Update directory information (client -> server)
        /// </summary>
        UpdateDirectoryInfoRequest = 0x09,
        /// <summary>
        /// Update directory information response (server -> client)
        /// </summary>
        UpdateDirectoryInfoResponse = 0x0A,
        /// <summary>
        /// Query for SNAC(0x02,0x0C) (client -> server)
        /// </summary>
        SNAC020CRequest = 0x0B,
        /// <summary>
        /// Reply to SNAC(0x02,0x0B) (server -> client)
        /// </summary>
        SNAC020BReply = 0x0C,
        /// <summary>
        /// Update user directory interests (client -> server)
        /// </summary>
        UpdateInterestsRequest = 0x0F,
        /// <summary>
        /// Update user directory interests response (server -> client)
        /// </summary>
        UpdateInterestsResponse = 0x10,
        /// <summary>
        /// User information query (client -> server)
        /// </summary>
        ExtendedInformationRequest = 0x15
    }

    internal enum BuddyListManagementService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
    }

    internal enum AdvertisementsService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Request for ads (client -> server)
        /// </summary>
        AdRequest = 0x02,
        /// <summary>
        /// Server sends requested ads in GIF format (server -> client)
        /// </summary>
        AdResponse = 0x03
    }

    internal enum InvitationService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Invite a friend to join AIM (client -> server)
        /// </summary>
        InvitationRequest = 0x02,
        /// <summary>
        /// Invitiation acknowledgement (server -> client)
        /// </summary>
        InvitationResponse = 0x03
    }

    internal enum AdministrativeService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Request account information (client -> server)
        /// </summary>
        AccountInfoRequest = 0x02,
        /// <summary>
        /// Requested account information (server -> client)
        /// </summary>
        AccountInfoResponse = 0x03,
        /// <summary>
        /// Request to change account information (client -> server)
        /// </summary>
        AccountInfoChangeRequest = 0x04,
        /// <summary>
        /// Account information change acknowledgement (server -> client)
        /// </summary>
        AccountInfoChangeResponse = 0x05,
        /// <summary>
        /// Request to confirm an account (client -> server)
        /// </summary>
        AccountConfirmRequest = 0x06,
        /// <summary>
        /// Account confirmation acknowledgement (server -> client)
        /// </summary>
        AccountConfirmResponse = 0x07,
        /// <summary>
        /// Request to delete an account (client -> server)
        /// </summary>
        AccountDeleteRequest = 0x08,
        /// <summary>
        /// Account deletion acknowledgement (server -> client)
        /// </summary>
        AccountDeleteResponse = 0x09
    }

    internal enum PopupNoticesService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// A command to display a popup message (server -> client)
        /// </summary>
        DisplayPopupMessage = 0x02
    }

    internal enum PrivacyManagementService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Request service parameters (client -> server)
        /// </summary>
        ServiceParametersRequest = 0x02,
        /// <summary>
        /// Requested service parameters (server -> client)
        /// </summary>
        ServiceParametersResponse = 0x03,
        /// <summary>
        /// Set group permissions mask (client -> server)
        /// </summary>
        SetGroupPermissionsMask = 0x04,
        /// <summary>
        /// Add to visible list (client -> server)
        /// </summary>
        AddToVisibleList = 0x05,
        /// <summary>
        /// Delete from visible list (client -> server)
        /// </summary>
        DeleteFromVisibleList = 0x06,
        /// <summary>
        /// Add to invisible list (client -> server)
        /// </summary>
        AddToInvisibleList = 0x07,
        /// <summary>
        /// Delete from invisible list (client -> server)
        /// </summary>
        DeleteFromInvisibleList = 0x08,
        /// <summary>
        /// Error in service (server -> client)
        /// </summary>
        ServiceError = 0x09,
        /// <summary>
        /// Add to visible list (?) (client -> server)
        /// </summary>
        AddToVisibleList2 = 0x0A,
        /// <summary>
        /// Delete from visible list (?) (client -> server)
        /// </summary>
        DeleteFromVisibleList2 = 0x0B
    }

    internal enum UserLookupService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Search for a user by e-mail (client -> server)
        /// </summary>
        FindUserByEmailRequest = 0x02,
        /// <summary>
        /// Search results (server -> client)
        /// </summary>
        FindUserByEmailResponse = 0x03
    }

    internal enum UsageStatsService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Set minimum reporting interval (server -> client)
        /// </summary>
        SetMinimumInterval = 0x02,
        /// <summary>
        /// Usage stats report (client -> server)
        /// </summary>
        UsageReport = 0x03,
        /// <summary>
        /// Acknowledgement of receipt of a usage stats report (server -> client)
        /// </summary>
        UsageReportAck = 0x04
    }

    internal enum TranslationService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Client translation request (client -> server)
        /// </summary>
        TranslateRequest = 0x02,
        /// <summary>
        /// Server translation response
        /// </summary>
        TranslateResponse = 0x03
    }

    internal enum ChatNavigationService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Request limitations (client -> server)
        /// </summary>
        LimitsRequest = 0x02,
        /// <summary>
        /// Request exchange information (client -> server)
        /// </summary>
        ExchangeInformationRequest = 0x03,
        /// <summary>
        /// Request room information (client -> server)
        /// </summary>
        RoomInformationRequest = 0x04,
        /// <summary>
        /// Request extended room information (client -> server)
        /// </summary>
        ExtendedRoomInformationRequest = 0x05,
        /// <summary>
        /// Request a room's member list (client -> server)
        /// </summary>
        MemberListRequest = 0x06,
        /// <summary>
        /// Search for a room (client -> server)
        /// </summary>
        RoomSearchRequest = 0x07,
        /// <summary>
        /// Create a new room (client -> server)
        /// </summary>
        CreateRoomRequest = 0x08,
        /// <summary>
        /// Server's response to requested information (server -> client)
        /// </summary>
        InformationResponse = 0x09
    }

    internal enum ChatService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Room information update (server -> client)
        /// </summary>
        RoomInformationUpdate = 0x02,
        /// <summary>
        /// Notification of users joining the room (server -> client)
        /// </summary>
        UsersJoinedNotification = 0x03,
        /// <summary>
        /// Notification of users leaving the room (server -> client)
        /// </summary>
        UsersLeftNotification = 0x04,
        /// <summary>
        /// Channel message from client (client -> server)
        /// </summary>
        MessageFromClient = 0x05,
        /// <summary>
        /// Channel message to client (server -> client)
        /// </summary>
        MessageToClient = 0x06,
        /// <summary>
        /// Evil request (client -> server)
        /// </summary>
        EvilRequest = 0x07,
        /// <summary>
        /// Evil response (server -> client)
        /// </summary>
        EvilResponse = 0x08,
        /// <summary>
        /// Chat message, data or error. Can be sent by client or server.
        /// </summary>
        ChatMessage = 0x09
    }

    internal enum DirectorySearch
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Client "search for user" request (client -> server)
        /// </summary>
        SearchUserRequest = 0x02,
        /// <summary>
        /// Server response to search request (server -> client)
        /// </summary>
        SearchUserResponse = 0x03,
        /// <summary>
        /// Request a client's interests list from the server (client -> server)
        /// </summary>
        InterestsListRequest = 0x04,
        /// <summary>
        /// Server response to interests list request (server -> client)
        /// </summary>
        InterestsListResponse = 0x05
    }

    internal enum SSIService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Request service parameters (client -> server)
        /// </summary>
        ServiceParametersRequest = 0x02,
        /// <summary>
        /// Service parameters response (server -> client)
        /// </summary>
        ServiceParametersResponse = 0x03,
        /// <summary>
        /// Request the contact list (first time) (client -> server)
        /// </summary>
        ContactListInitialRequest = 0x04,
        /// <summary>
        /// Contact list checkout (client -> server)
        /// </summary>
        ContactListCheckout = 0x05,
        /// <summary>
        /// Server contact list response (server -> client)
        /// </summary>
        ContactListResponse = 0x06,
        /// <summary>
        /// Load server-side copy of contact list after successful login (client -> server)
        /// </summary>
        LoadContactList = 0x07,
        /// <summary>
        /// SSI Edit:  Add items. Can be sent by client or server.
        /// </summary>
        SSIEditAddItems = 0x08,
        /// <summary>
        /// SSI Edit:  Update group header. Can be sent by client or server.
        /// </summary>
        SSIEditUpdateGroupHeader = 0x09,
        /// <summary>
        /// SSI Edit:  Remove item. Can be sent by client or server.
        /// </summary>
        SSIEditRemoveItem = 0x0A,
        /// <summary>
        /// SSI Edit acknowledgement (server -> client)
        /// </summary>
        SSIEditAcknowledge = 0x0E,
        /// <summary>
        /// Client's local SSI is up-to-date (server -> client)
        /// </summary>
        LocalSSIUpToDate = 0x0F,
        /// <summary>
        /// Edit contacts - begin transaction. Can be sent by client or server.
        /// </summary>
        ContactsEditStart = 0x11,
        /// <summary>
        /// Edit contacts - end transation. Can be sent by client or server.
        /// </summary>
        ContactsEditEnd = 0x12,
        /// <summary>
        /// Grant future authorization to client (client -> server)
        /// </summary>
        SendFutureAuthorizationGrant = 0x14,
        /// <summary>
        /// Future authorization granted (server -> client)
        /// </summary>
        FutureAuthorizationGranted = 0x15,
        /// <summary>
        /// Delete yourself from another client's contact list (client -> server)
        /// </summary>
        RemoveSelfFromContact = 0x16,
        /// <summary>
        /// Send authorization request (client -> server)
        /// </summary>
        SendAuthorizationRequest = 0x18,
        /// <summary>
        /// Authorization request (server -> client)
        /// </summary>
        AuthorizationRequest = 0x19,
        /// <summary>
        /// Send authorization reply (client -> server)
        /// </summary>
        SendAuthorizationResponse = 0x1A,
        /// <summary>
        /// Authorization reply (server -> client)
        /// </summary>
        AuthorizationResponse = 0x1B,
        /// <summary>
        /// "You were added" (to another client's contact list) message (server -> client)
        /// </summary>
        YouWereAddedMessage = 0x1C
    }

    internal enum ICQExtensionsService
    {
        /// <summary>
        /// Client / Server error. Can be sent by client or server.
        /// </summary>
        ClientServerError = 0x01,
        /// <summary>
        /// Meta information request (client -> server)
        /// </summary>
        MetaInformationRequest = 0x02,
        /// <summary>
        /// Meta information response (server -> client)
        /// </summary>
        MetaInformationResponse = 0x03
    }

    internal enum AuthorizationRegistrationService
    {
        /// <summary>
        /// Server error, registration refused. Can be sent by client or server.
        /// </summary>
        ServerError = 0x01,
        /// <summary>
        /// Client login request, MD5 login sequence (client -> server)
        /// </summary>
        LoginRequest = 0x02,
        /// <summary>
        /// Server login (or error) reply (server -> client)
        /// </summary>
        LoginResponse = 0x03,
        /// <summary>
        /// Request a new UIN (client -> server)
        /// </summary>
        NewUINRequest = 0x04,
        /// <summary>
        /// New UIN response (server -> client)
        /// </summary>
        NewUINResponse = 0x05,
        /// <summary>
        /// Request an MD5 authkey (client -> server)
        /// </summary>
        MD5AuthkeyRequest = 0x06,
        /// <summary>
        /// Server sends an MD5 authkey (server -> client)
        /// </summary>
        MD5AuthkeyResponse = 0x07,
        /// <summary>
        /// Server requests a SecureID (server -> client)
        /// </summary>
        SecureIDRequest = 0x0A,
        /// <summary>
        /// Client sends a SecureID (client -> server)
        /// </summary>
        SecureIDResponse = 0x0B
    }

    internal enum BroadcastService
    {
        /// <summary>
        /// Server error (server -> client)
        /// </summary>
        ServerError = 0x01,
        /// <summary>
        /// Send broadcast message to server (client -> server)
        /// </summary>
        BroadcastMessageRequest = 0x02,
        /// <summary>
        /// Server broadcast reply (server -> client)
        /// </summary>
        BroadcastMessageResponse = 0x03
    }
}