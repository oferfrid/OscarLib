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
    /// Encapsulates a description of a single SNAC family
    /// </summary>
    public class Family
    {
        private string _description;
        private ushort _ID;
        private string _name;
        private ushort _toolID, _toolversion;
        private ushort _version;

        /// <summary>
        /// Constructs a new SNAC family
        /// </summary>
        /// <param name="ID">The ID of the SNAC family</param>
        /// <param name="version">The version of this family implemented by the client</param>
        /// <param name="toolID">The ToolID of this family</param>
        /// <param name="toolversion">The ToolVersion of this family</param>
        /// <param name="name">The human-readable name of the family</param>
        /// <param name="description">A human-readable description of the family</param>
        public Family(ushort ID, ushort version, ushort toolID, ushort toolversion, string name, string description)
        {
            _ID = ID;
            _version = version;
            _toolID = toolID;
            _toolversion = toolversion;
            _name = name;
            _description = description;
        }

        #region Properties

        /// <summary>
        /// Gets the SNAC family ID
        /// </summary>
        public ushort ID
        {
            get { return _ID; }
        }

        /// <summary>
        /// Gets the version of this family implemented by the client
        /// </summary>
        public ushort Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the ToolID of this family
        /// </summary>
        public ushort ToolID
        {
            get { return _toolID; }
        }

        /// <summary>
        /// Gets the ToolVersion of this family
        /// </summary>
        public ushort ToolVersion
        {
            get { return _toolversion; }
        }

        /// <summary>
        /// Gets the human-readable name of this family
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the human-readable description of this family
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        #endregion
    }
}