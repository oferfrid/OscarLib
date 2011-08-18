/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Manages a collection of Family objects
    /// </summary>
    /// <remarks>
    /// <para>
    /// The FamilyManager object exposes a variety of methods to abstract 
    /// interaction with Family objects. A client only needs to know the 
    /// SNAC ID of a family to call a FamilyManager Get* method and return
    /// information on that SNAC.
    /// </para>
    /// <para>
    /// In addition to managing the collection of Family object, FamilyManager
    /// also loads the family data from the XML #aim data file.
    /// </para>
    /// </remarks>
    internal class FamilyManager : IEnumerable
    {
        private Hashtable _delayqueues;
        private Hashtable _families;

        /// <summary>
        /// Constructs a new FamilyManager
        /// </summary>
        /// <remarks>After a FamilyManager is created, but before it is used, the </remarks>
        public FamilyManager()
        {
            _families = new Hashtable();
            _delayqueues = new Hashtable();
            LoadFamilies();
        }

        /// <summary>
        /// Gets the number of families registered with this manager
        /// </summary>
        public int Count
        {
            get { return _families.Count; }
        }

        #region IEnumerable Members

        /// <summary>
        /// Returns an IEnumerator that can iterate through the FamilyMember
        /// </summary>
        /// <returns>An IDictionaryEnumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return _families.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets the version of a family
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The version of the family, or 0xFFFF if the family ID does not exist</returns>
        public ushort GetFamilyVersion(ushort family)
        {
            ushort retval = 0xFFFF;
            if (_families.ContainsKey(family))
                retval = ((Family) _families[family]).Version;
            return retval;
        }

        /// <summary>
        /// Gets the tool ID of a family
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The tool ID of the family, or 0xFFFF if the family ID does not exist</returns>
        public ushort GetFamilyToolID(ushort family)
        {
            ushort retval = 0xFFFF;
            if (_families.ContainsKey(family))
                retval = ((Family) _families[family]).ToolID;
            return retval;
        }

        /// <summary>
        /// Gets the tool version of a family
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The tool version of the family, or 0xFFFF if the family ID does not exist</returns>
        public ushort GetFamilyToolVersion(ushort family)
        {
            ushort retval = 0xFFFF;
            if (_families.ContainsKey(family))
                retval = ((Family) _families[family]).ToolVersion;
            return retval;
        }

        /// <summary>
        /// Gets the name of a family
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The name of the family, or an empty string if the family ID does not exist</returns>
        public string GetFamilyName(ushort family)
        {
            string retval = "";
            if (_families.ContainsKey(family))
                retval = ((Family) _families[family]).Name;
            return retval;
        }

        /// <summary>
        /// Gets the description of a family
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The description of the family, or an empty string if the family ID does not exist</returns>
        public string GetFamilyDescription(ushort family)
        {
            string retval = "";
            if (_families.ContainsKey(family))
                retval = ((Family) _families[family]).Description;
            return retval;
        }

        /// <summary>
        /// Gets a raw Family object
        /// </summary>
        /// <param name="family">The ID of the family</param>
        /// <returns>The requested family, or null if the specified ID does not exist</returns>
        /// <remarks>Whenever possible, the other FamilyManager.Get* functions should be used instead of using GetFamily for raw access</remarks>
        public Family GetFamily(ushort family)
        {
            Family retval = null;
            if (_families.ContainsKey(family))
                retval = (Family) _families[family];
            return retval;
        }

        /// <summary>
        /// Loads the family information into a hashtable
        /// </summary>
        public void LoadFamilies()
        {
            ushort key = 0x0001;
            _families.Add(key++, new Family(0x0001, 0x0003, 0x0110, 0x0629,
                                            "Generic Service Controls",
                                            "This service is used for basic client-server communication and connection setup"));
            _families.Add(key++, new Family(0x0002, 0x0001, 0x0110, 0x0629,
                                            "Location Services",
                                            "This service is used to get/change user online data (like profile or capabilities or AIM away messages)"));
            _families.Add(key++, new Family(0x0003, 0x0001, 0x0110, 0x0629,
                                            "Buddy List Management Services",
                                            "This service is used to manage old-style buddy lists and for online/offline/status notifications"));
            _families.Add(key++, new Family(0x0004, 0x0001, 0x0110, 0x0629,
                                            "ICBM Service",
                                            "Used for Instant Messages and warnings"));
            _families.Add(key++, new Family(0x0005, 0x0001, 0x0001, 0x0001,
                                            "Advertisements Service",
                                            "This service is depreciated. AOL clients currently use web services to get advertisement data"));
            _families.Add(key++, new Family(0x0006, 0x0001, 0x0110, 0x0629,
                                            "Invitation Service",
                                            "This service is used to send AIM service invitations to a friend (or an enemy)"));
            _families.Add(key++, new Family(0x0007, 0x0001, 0x0010, 0x0629,
                                            "Administrative Service",
                                            "This service is used to manage AIM user account data (screenname formating, email) and for account confirmation"));
            _families.Add(key++, new Family(0x0008, 0x0001, 0x0104, 0x0001,
                                            "Popup Notices Service",
                                            "Servers use this small family to popup a special message window on the client side (AIM Only)"));
            _families.Add(key++, new Family(0x0009, 0x0001, 0x0110, 0x0629,
                                            "Privacy Management Service",
                                            "Clients use this message to manage their visible/invisible lists and user class permissions"));
            _families.Add(key++, new Family(0x000A, 0x0001, 0x0110, 0x0629,
                                            "User Lookup Service",
                                            "This service was used by old AIM clients to search users by email address"));
            _families.Add(key++, new Family(0x000B, 0x0001, 0x0104, 0x0001,
                                            "Usage Stats Service",
                                            "This service is used by AOL to gather statistical information about client usage"));
            _families.Add(key++, new Family(0x000C, 0x0001, 0x0104, 0x0001,
                                            "Translation Service",
                                            "This service is depreciated"));
            _families.Add(key++, new Family(0x000D, 0x0001, 0x0010, 0x0629,
                                            "Chat Navigation Service",
                                            "This service is used to manage AIM chat information (create/delete rooms, search for a room, get room members)"));
            _families.Add(key++, new Family(0x000E, 0x0001, 0x0010, 0x0629,
                                            "Chat Service",
                                            "AIM chat service (get/send messages, join/left notification, warnings, room update information, etc.)"));
            _families.Add(key++, new Family(0x000F, 0x0001, 0x0010, 0x0629,
                                            "Directory User Search",
                                            "This service is used by modern AOL clients for user search (supercedes 0x000A)"));
            _families.Add(key++, new Family(0x0010, 0x0001, 0x0010, 0x0629,
                                            "SSBI Service",
                                            "This service is used for storing and retrieving Server-Side Buddy Icons"));
            key = 0x0013;
            _families.Add(key, new Family(0x0013, 0x0004, 0x0110, 0x0629,
                                          "SSI Service",
                                          "This service allows clients to store their contact list data locally"));
            key = 0x0015;
            _families.Add(key, new Family(0x0015, 0x0001, 0x0110, 0x047C,
                                          "ICQ Extensions Service",
                                          "This service is used by ICQ clients for compatibility with the old Mirabilis ICQ server database engine"));
            key = 0x0017;
            _families.Add(key, new Family(0x0017, 0x0000, 0x0000, 0x0000,
                                          "Authorization and Registration Service",
                                          "This service is used for client registration / authentication"));
            key = 0x0018;
            _families.Add(key, new Family(0x0018, 0x0001, 0x0010, 0x0629,
                                          "Email Service",
                                          "Not well-known"));
            key = 0x0085;
            _families.Add(key, new Family(0x0085, 0x0001, 0x0000, 0x0000,
                                          "Broadcast Service",
                                          "This is an IServerd extension to allow a client to send broadcasts. If a client has permissions to send broadcasts, it will get this family in SNAC(01,03)"));
        }

        #region Delay queue functions

        /// <summary>
        /// Adds a frame to the delay queue
        /// </summary>
        /// <param name="sh">The SNAC header of the frame to add</param>
        /// <param name="data">A byte array consisting of the delayed frame</param>
        /// <returns><c>true</c> if a queue already existed for this family, or <c>false</c> if <see cref="ServiceManager.RequestNewService"/> should be called
        /// </returns>
        public bool AddFrameToDelayQueue(SNACHeader sh, byte[] data)
        {
            Queue q = null;
            bool retval = true;
            ushort key = sh.FamilyServiceID;

            DelayedFrame df = new DelayedFrame();
            df.SNAC = sh;
            df.Data = data;

            if (_delayqueues.ContainsKey(key))
            {
                q = (Queue) _delayqueues[key];
                q.Enqueue(df);
                retval = true;
            }
            else
            {
                q = new Queue();
                q.Enqueue(df);
                _delayqueues[key] = q;
                retval = false;
            }
            return retval;
        }

        /// <summary>
        /// Gets a queue of delayed frames
        /// </summary>
        /// <param name="family">The family of the delayed frames</param>
        /// <returns>A <see cref="System.Collections.Queue"/> containing any delayed frames, or
        /// <c>null</c> if there are no delayed frames for the family.</returns>
        public Queue GetDelayedFrames(ushort family)
        {
            Queue q = null;
            if (_delayqueues.ContainsKey(family))
            {
                q = (Queue) _delayqueues[family];
                _delayqueues.Remove(family);
            }
            return q;
        }

        #endregion
    }
}