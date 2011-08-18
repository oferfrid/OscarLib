using System;
using System.Collections.Generic;
using System.Text;
using csammisrun.OscarLib.Utility;
using System.Collections.ObjectModel;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles user directory search requests - SNAC families 0x000A and 0x000F
    /// </summary>
    public sealed class SearchManager : ISnacFamilyHandler
    {
        #region SNAC constants
        /// <summary>
        /// The SNAC family responsible for user lookups by email address
        /// </summary>
        private const int SNAC_USERLOOKUP_FAMILY = 0x000A;
        /// <summary>
        /// The SNAC family responsible for searching the user directory
        /// </summary>
        private const int SNAC_DIRECTORY_FAMILY = 0x000F;

        /// <summary>
        /// The SNAC code for doing a user search by email address
        /// </summary>
        private const int USERLOOKUP_EMAIL_SEARCH = 0x0002;
        /// <summary>
        /// The SNAC code for receiving email search results
        /// </summary>
        private const int USERLOOKUP_EMAIL_RESULTS = 0x0003;
        #endregion SNAC constants

        private readonly Session parent;

        /// <summary>
        /// Initializes a new SearchManager
        /// </summary>
        internal SearchManager(Session parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_USERLOOKUP_FAMILY);
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_DIRECTORY_FAMILY);
        }

        #region Events
        /// <summary>
        /// Occurs when results from a user search by email are available
        /// </summary>
        public event FindByEmailResultsHandler FindByEmailResults;
        #endregion Events

        #region Public methods
        /// <summary>
        /// Requests a list of accounts associated with an email address
        /// </summary>
        /// <param name="email">The email address to search for</param>
        /// <remarks>Results are returned by the <see cref="FindByEmailResults"/> event</remarks>
        /// <exception cref="NotLoggedInException">Thrown when the <see cref="Session"/> is not logged in</exception>
        public void FindUsersByEmail(string email)
        {
            if (!parent.LoggedIn)
            {
                throw new NotLoggedInException();
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_USERLOOKUP_FAMILY;
            sh.FamilySubtypeID = USERLOOKUP_EMAIL_SEARCH;

            ByteStream stream = new ByteStream();
            stream.WriteString(email, Encoding.ASCII);

            parent.StoreRequestID(sh.RequestID, email);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }
        #endregion Public methods

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 0x000A or 0x000F
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(csammisrun.OscarLib.Utility.DataPacket dp)
        {
            if (dp.SNAC.FamilyServiceID == SNAC_USERLOOKUP_FAMILY)
            {
                switch (dp.SNAC.FamilySubtypeID)
                {
                    case USERLOOKUP_EMAIL_RESULTS:
                        ProcessEmailSearchResults(dp);
                        break;
                }
            }
            else if (dp.SNAC.FamilyServiceID == SNAC_DIRECTORY_FAMILY)
            {
                switch (dp.SNAC.FamilySubtypeID)
                {
                }
            }
        }

        #endregion

        #region Processing methods
        /// <summary>
        /// Processes the results of a search-by-email request -- SNAC(0A,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0A,03)</param>
        void ProcessEmailSearchResults(DataPacket dp)
        {
            string email = parent.RetrieveRequestID(dp.SNAC.RequestID) as string;
            List<string> accts = new List<string>();

            while (dp.Data.HasMoreData)
            {
                ushort key = dp.Data.ReadUshort();
                accts.Add(dp.Data.ReadString(dp.Data.ReadUshort(), Encoding.ASCII));
            }

            if (FindByEmailResults != null)
            {
                FindByEmailResults(this, new FindByEmailResultsArgs(email, accts));
            }
        }
        #endregion Processing methods
    }

    /// <summary>
    /// Encapsulates the results of a user directory search by email address
    /// </summary>
    public class FindByEmailResultsArgs : EventArgs
    {
        private readonly string email;
        private readonly ReadOnlyCollection<string> results;

        /// <summary>
        /// Initializes a new SearchByEmailResultsArgs
        /// </summary>
        internal FindByEmailResultsArgs(string email, List<string> results)
        {
            this.email = email;
            this.results = new ReadOnlyCollection<string>(results);
        }

        /// <summary>
        /// Gets the email address that was used for the search
        /// </summary>
        public string Email
        {
            get { return email; }
        }

        /// <summary>
        /// Gets the set of screen names that matched the email search
        /// </summary>
        public ReadOnlyCollection<string> Results
        {
            get { return results; }
        }
    }

    /// <summary>
    /// Provides a callback function for receiving find-by-email results
    /// </summary>
    public delegate void FindByEmailResultsHandler(object sender, FindByEmailResultsArgs e);
}
