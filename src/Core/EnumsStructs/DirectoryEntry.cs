/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

//using System;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Encapsulates the result of a directory search
    /// </summary>
    public struct DirectoryEntry
    {
        /// <summary>
        /// Street address
        /// </summary>
        public string Address;

        /// <summary>
        /// City of residence
        /// </summary>
        public string City;

        /// <summary>
        /// Country of residence
        /// </summary>
        public string Country;

        /// <summary>
        /// Email address
        /// </summary>
        public string Email;

        /// <summary>
        /// First name
        /// </summary>
        public string FirstName;

        /// <summary>
        /// Interest
        /// </summary>
        /// <remarks>Format unknown</remarks>
        public string Interest;

        /// <summary>
        /// Last name
        /// </summary>
        public string LastName;

        /// <summary>
        /// Maiden name
        /// </summary>
        public string MaidenName;

        /// <summary>
        /// Middle name
        /// </summary>
        public string MiddleName;

        /// <summary>
        /// Nick name
        /// </summary>
        public string NickName;

        /// <summary>
        /// The region of the strings
        /// </summary>
        /// <remarks>This will most likely be "us-ascii"</remarks>
        public string Region;

        /// <summary>
        /// User's screenname
        /// </summary>
        public string ScreenName;

        /// <summary>
        /// State of residence
        /// </summary>
        public string State;

        /// <summary>
        /// ZIP code
        /// </summary>
        public string ZIPCode;

        /// <summary>
        /// Creates a new DirectoryEntry structure and initializes its members to <paramref name="init"/>
        /// </summary>
        /// <param name="init">The string with which to initialize the structure members</param>
        public DirectoryEntry(string init)
        {
            FirstName = LastName = MiddleName = MaidenName = init;
            Email = Country = State = City = init;
            ScreenName = Interest = NickName = ZIPCode = init;
            Region = Address = init;
        }
    }
}