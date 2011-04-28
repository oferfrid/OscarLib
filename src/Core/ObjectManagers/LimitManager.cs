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
    /// Provides an access point for information about low-level protocol limitations
    /// </summary>
    public class LimitManager
    {
        #region SNAC02 limits

        private ushort _maxcapabilities = 0;
        private ushort _maxprofilelen = 0;

        /// <summary>
        /// Gets the maximum length of a user's profile
        /// </summary>
        public ushort MaxProfileLength
        {
            get { return _maxprofilelen; }
            internal set { _maxprofilelen = value; }
        }

        /// <summary>
        /// Gets the maximum number of capabilities a client can have
        /// </summary>
        public ushort MaxCapabilities
        {
            get { return _maxcapabilities; }
            internal set { _maxcapabilities = value; }
        }

        #endregion

        #region SNAC04 limits

        private ushort _maxmessagesize;
        private ushort _maxreceiverevil;

        private ushort _maxsenderevil;

        /// <summary>
        /// Gets the maximum message size, including all headers, that the OSCAR
        /// servers will accept
        /// </summary>
        public ushort MaxMessageSize
        {
            get { return _maxmessagesize; }
            internal set { _maxmessagesize = value; }
        }

        /// <summary>
        /// Gets the maximum warning level that this client will accept messages from
        /// </summary>
        public ushort MaxSenderWarningLevel
        {
            get { return _maxsenderevil; }
            internal set { _maxsenderevil = value; }
        }

        /// <summary>
        /// Gets the maximum warning level that this client will send messages to
        /// </summary>
        public ushort MaxReceiverWarningLevel
        {
            get { return _maxreceiverevil; }
            internal set { _maxreceiverevil = value; }
        }

        #endregion

        #region SNAC13 limits

        private ushort _maxbuddies = 0;
        private ushort _maxdenys = 0;
        private ushort _maxgroups = 0;
        private ushort _maxpermits = 0;

        /// <summary>
        /// Gets the maximum number of buddy list group items
        /// </summary>
        public ushort MaxGroups
        {
            get { return _maxgroups; }
            internal set { _maxgroups = value; }
        }

        /// <summary>
        /// Gets the maximum number of buddy list buddy items
        /// </summary>
        public ushort MaxBuddies
        {
            get { return _maxbuddies; }
            internal set { _maxbuddies = value; }
        }

        /// <summary>
        /// Gets the maximum number of buddy list "permit" items
        /// </summary>
        public ushort MaxPermits
        {
            get { return _maxpermits; }
            internal set { _maxpermits = value; }
        }

        /// <summary>
        /// Gets the maximum number of buddy list "deny" items
        /// </summary>
        public ushort MaxDenys
        {
            get { return _maxdenys; }
            internal set { _maxdenys = value; }
        }

        #endregion
    }
}