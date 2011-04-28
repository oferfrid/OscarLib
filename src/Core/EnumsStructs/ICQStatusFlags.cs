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
    /// Describes settable ICQ flags
    /// </summary>
    public enum ICQFlags
    {
        /// <summary>
        /// No special flags are set
        /// </summary>
        Normal = 0x0000,
        /// <summary>
        /// Client is Web Aware
        /// </summary>
        WebAware = 0x0001,
        /// <summary>
        /// Server can reveal client's IP address
        /// </summary>
        ShowIP = 0x0002,
        /// <summary>
        /// Today is the user's birthday
        /// </summary>
        Birthday = 0x0008,
        /// <summary>
        /// User has an ICQ homepage
        /// </summary>
        ActiveWebfront = 0x0020,
        /// <summary>
        /// Direct Connect is disabled
        /// </summary>
        DCDisabled = 0x0010,
        /// <summary>
        /// Direct Connect requires authorization
        /// </summary>
        DCAuthorizationOnly = 0x1000,
        /// <summary>
        /// Direct Connect with users on this client's contact list only
        /// </summary>
        DCContactsOnly = 0x2000
    }

    /// <summary>
    /// Describes the client's online state
    /// </summary>
    public enum ICQStatus
    {
        /// <summary>
        /// Client is online
        /// </summary>
        Online = 0x0000,
        /// <summary>
        /// Client is away
        /// </summary>
        Away = 0x0001,
        /// <summary>
        /// Client has set the "Do Not Disturb" flag
        /// </summary>
        DoNotDisturb = 0x0002,
        /// <summary>
        /// Client is not available
        /// </summary>
        NotAvailable = 0x0004,
        /// <summary>
        /// Client is occupied
        /// </summary>
        Occupied = 0x0010,
        /// <summary>
        /// Client is free for chat
        /// </summary>
        FreeForChat = 0x0020,
        /// <summary>
        /// Client is invisible
        /// </summary>
        Invisible = 0x0100
    }

    /// <summary>
    /// ICQ information
    /// </summary>
    /// <remarks>TODO:  Needs an overhaul</remarks>
    public class ICQInfo
    {
        /// <summary>
        /// Age
        /// </summary>
        public byte Age;
        /// <summary>
        /// Birthday
        /// </summary>
        public byte BirthDay;
        /// <summary>
        /// Birth month
        /// </summary>
        public byte BirthMonth;
        /// <summary>
        /// Birth year
        /// </summary>
        public ushort BirthYear;
        /// <summary>
        /// Email
        /// </summary>
        public string Email;
        /// <summary>
        /// Email addresses
        /// </summary>
        public string[] EmailAddresses;
        /// <summary>
        /// First name
        /// </summary>
        public string Firstname;
        /// <summary>
        /// Gender
        /// </summary>
        public byte Gender;
        /// <summary>
        /// Home street address
        /// </summary>
        public string HomeAddress;
        /// <summary>
        /// Home city
        /// </summary>
        public string HomeCity;
        /// <summary>
        /// Home country
        /// </summary>
        public ushort HomeCountry;
        /// <summary>
        /// Home fax number
        /// </summary>
        public string HomeFax;
        /// <summary>
        /// Home phone number
        /// </summary>
        public string HomePhone;
        /// <summary>
        /// Home state
        /// </summary>
        public string HomeState;
        /// <summary>
        /// Home postal code
        /// </summary>
        public string HomeZip;
        /// <summary>
        /// Information
        /// </summary>
        public string Information;
        /// <summary>
        /// Language 1
        /// </summary>
        public byte Language1;
        /// <summary>
        /// Language 2
        /// </summary>
        public byte Language2;
        /// <summary>
        /// Language 3
        /// </summary>
        public byte Language3;
        /// <summary>
        /// Last name
        /// </summary>
        public string Lastname;
        /// <summary>
        /// Mobile phone
        /// </summary>
        public string MobilePhone;
        /// <summary>
        /// Nickname
        /// </summary>
        public string Nickname;
        /// <summary>
        /// Homepage
        /// </summary>
        public string PersonalURL;
        /// <summary>
        /// Screenname
        /// </summary>
        public string Screenname;

        // Work information
        /// <summary>
        /// Work street address
        /// </summary>
        public string WorkAddress;
        /// <summary>
        /// Work city
        /// </summary>
        public string WorkCity;
        /// <summary>
        /// Work company
        /// </summary>
        public string WorkCompany;
        /// <summary>
        /// Work country
        /// </summary>
        public ushort WorkCountry;
        /// <summary>
        /// Work division
        /// </summary>
        public string WorkDivision;
        /// <summary>
        /// Work fax
        /// </summary>
        public string WorkFax;
        /// <summary>
        /// Work phone
        /// </summary>
        public string WorkPhone;
        /// <summary>
        /// Work position
        /// </summary>
        public string WorkPosition;
        /// <summary>
        /// Work state
        /// </summary>
        public string WorkState;
        /// <summary>
        /// Work website
        /// </summary>
        public string WorkWebsite;
        /// <summary>
        /// Work postal code
        /// </summary>
        public string WorkZip;

        // Additional information
    }
}