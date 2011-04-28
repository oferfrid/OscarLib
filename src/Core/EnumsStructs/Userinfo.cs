/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Globalization;
using System.Text;
using System;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Encapsulates an AIM/ICQ user information block
    /// </summary>
    public class UserInfo
    {
        private string availableMsg;
        private Capabilities _caps = Capabilities.None;
        private UserClass _class = UserClass.Unconfirmed;
        private DateTime _createtime;
        private UserDCInfo _dc;
        private BartID iconId;
        private uint _icqexternalip;
        private uint _icquserstatus;
        private ushort _idletime;
        private uint _onlinetime;
        private DateTime _registertime;
        private string _screenname;
        private DateTime _signontime;
        private ushort _warninglevel;

        /// <summary>
        /// Gets the user's screenname
        /// </summary>
        public string ScreenName
        {
            get { return _screenname; }
            set { _screenname = value; }
        }

        /// <summary>
        /// Gets the user's current warning level
        /// </summary>
        public ushort WarningLevel
        {
            get { return _warninglevel; }
            internal set { _warninglevel = value; }
        }

        /// <summary>
        /// Gets the user's <see cref="UserClass"/>
        /// </summary>
        public UserClass Class
        {
            get { return _class; }
            internal set { _class = value; }
        }

        /// <summary>
        /// Gets date and time of the user's account creation
        /// </summary>
        public DateTime CreateTime
        {
            get { return _createtime; }
            internal set { _createtime = value; }
        }

        /// <summary>
        /// Gets the user's signon time (UTC)
        /// </summary>
        public DateTime SignonTime
        {
            get { return _signontime; }
            internal set { _signontime = value; }
        }

        /// <summary>
        /// Gets the user's idle time, in minutes
        /// </summary>
        public ushort IdleTime
        {
            get { return _idletime; }
            internal set { _idletime = value; }
        }

        /// <summary>
        /// Gets the date and time when this account was registered ("Member Since")
        /// </summary>
        public DateTime RegisterTime
        {
            get { return _registertime; }
            internal set { _registertime = value; }
        }

        /// <summary>
        /// Gets an ICQ user's status
        /// </summary>
        /// <remarks>This property will be empty (0) for an AIM user</remarks>
        public uint ICQUserStatus
        {
            get { return _icquserstatus; }
            internal set { _icquserstatus = value; }
        }

        /// <summary>
        /// Gets an ICQ user's external IP address
        /// </summary>
        /// <remarks>This property will be empty for an AIM user</remarks>
        public uint ExternalIPAddress
        {
            get { return _icqexternalip; }
            internal set { _icqexternalip = value; }
        }

        /// <summary>
        /// Gets an ICQ user's DC (DirectConnect) info
        /// </summary>
        /// <remarks>This property will be <c>null</c> for an AIM user</remarks>
        public UserDCInfo DC
        {
            get { return _dc; }
            internal set { _dc = value; }
        }

        /// <summary>
        /// Gets the remote client's capability list
        /// </summary>
        public Capabilities ClientCapabilities
        {
            get { return _caps; }
            internal set { _caps = value; }
        }

        /// <summary>
        /// Gets the user's online time, in seconds
        /// </summary>
        public uint OnlineTime
        {
            get { return _onlinetime; }
            internal set { _onlinetime = value; }
        }

        /// <summary>
        /// Gets the user's buddy icon information
        /// </summary>
        /// <remarks>This property is <c>null</c> if no icon information has been received</remarks>
        public BartID Icon
        {
            get { return iconId; }
            internal set { iconId = value; }
        }

        /// <summary>
        /// Gets the user's "available" message
        /// </summary>
        /// <remarks>This string is encoded in UTF-16</remarks>
        public string AvailableMessage
        {
            get { return availableMsg; }
            internal set { availableMsg = value; }
        }
    }

    /// <summary>
    /// DC information for a user
    /// </summary>
    /// <remarks>TODO:  Needs an overhaul, I have no idea what this is for</remarks>
    public class UserDCInfo
    {
        /// <summary>
        /// Authentication cookie
        /// </summary>
        public uint AuthCookie;
        /// <summary>
        /// Client futures
        /// </summary>
        public uint ClientFutures = 0x00000003; // Always 3?
        /// <summary>
        /// DC type
        /// </summary>
        public byte DCType;
        /// <summary>
        /// DC version
        /// </summary>
        public ushort DCVersion;
        /// <summary>
        /// Internal IP
        /// </summary>
        public string InternalIP;
        /// <summary>
        /// Internal port number
        /// </summary>
        public uint InternalPort;
        /// <summary>
        /// Last extended information update time
        /// </summary>
        public uint LastExtInfoUpdateTime;
        /// <summary>
        /// Last status update time
        /// </summary>
        public uint LastExtStatusUpdateTime;
        /// <summary>
        /// Last information update time
        /// </summary>
        public uint LastInfoUpdateTime;
        /// <summary>
        /// Unknown
        /// </summary>
        public ushort Unknown;
        /// <summary>
        /// Web front port
        /// </summary>
        public uint WebFrontPort;
    }

    /// <summary>
    /// Describes an AOL user's class
    /// </summary>
    public enum UserClass
    {
        /// <summary>
        /// Unconfirmed user
        /// </summary>
        Unconfirmed = 0x0001,
        /// <summary>
        /// AOL adminstrator
        /// </summary>
        Administrator = 0x0002,
        /// <summary>
        /// AOL staff
        /// </summary>
        AOLStaff = 0x0004,
        /// <summary>
        /// Commercial account
        /// </summary>
        Commercial = 0x0008,
        /// <summary>
        /// Non-commercial account
        /// </summary>
        Free = 0x0010,
        /// <summary>
        /// User is away
        /// </summary>
        Away = 0x0020,
        /// <summary>
        /// ICQ user
        /// </summary>
        ICQ = 0x0040,
        /// <summary>
        /// AOL Wireless user
        /// </summary>
        Wireless = 0x0080
    }

    /// <summary>
    /// Describes a request for user information, one block at a time
    /// </summary>
    /// <remarks>The BasicUserInfoRequest members cannot be ORed together. To request more
    /// than one type of information at a time, use the <see cref="UserInfoRequest"/> enumeration
	/// with the <see cref="ISession.RequestUserInfo"/> method</remarks>
    public enum BasicUserInfoRequest
    {
        /// <summary>
        /// General information -- the user's profile
        /// </summary>
        GeneralInfo = 0x0001,
        /// <summary>
        /// Online information -- a <see cref="UserInfo"/> block
        /// </summary>
        OnlineInfo = 0x0002,
        /// <summary>
        /// Away message
        /// </summary>
        AwayMessage = 0x0003,
        /// <summary>
        /// Client capabilities
        /// </summary>
        Capabilities = 0x0004
    }

    /// <summary>
    /// Describes a multi-part request for user information
    /// </summary>
    /// <remarks>The UserInfoRequest members can be ORed together</remarks>
    public enum UserInfoRequest
    {
        /// <summary>
        /// User's profile
        /// </summary>
        UserProfile = 0x00000001,
        /// <summary>
        /// User's away message
        /// </summary>
        AwayMessage = 0x00000002,
        /// <summary>
        /// Remote client's capabilities
        /// </summary>
        Capabilities = 0x00000004
    }

    /// <summary>
    /// Encapsulates the results of a request for user information
    /// </summary>
    public struct UserInfoResponse
    {
        /// <summary>
        /// The user's away message
        /// </summary>
        public string AwayMessage;

        /// <summary>
        /// The <see cref="System.Text.Encoding"/> of the user's away message string
        /// </summary>
        public Encoding AwayMessageEncoding;

        /// <summary>
        /// The remote client's capabilities
        /// </summary>
        public Capabilities ClientCapabilities;

        /// <summary>
        /// The user's information structure
        /// </summary>
        public UserInfo Info;

        /// <summary>
        /// The user's profile
        /// </summary>
        public string Profile;

        /// <summary>
        /// The <see cref="System.Text.Encoding"/> of the user's profile string
        /// </summary>
        public Encoding ProfileEncoding;
    }
}