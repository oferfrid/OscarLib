/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{

    #region Common SSI interface

    /// <summary>
    /// Defines an interface for all SSI items that exposes their unique ID
    /// </summary>
    public interface IServerSideItem : IComparable
    {
        /// <summary>
        /// The unique ID of the SSI item
        /// </summary>
        ushort ItemID { get; }
    }

    #endregion

    #region SSI classes

    /// <summary>
    /// Encapsulates an SSI item representing a group of contacts
    /// </summary>
    public class SSIGroup : IServerSideItem
    {
        /// <summary>
        /// The name of the group
        /// </summary>
        public string Name = "";

        /// <summary>
        /// The group's ID number
        /// </summary>
        public ushort ID = 0;

        /// <summary>
        /// The IDs of the <see cref="SSIBuddy"/> objects contained in this group
        /// </summary>
        public List<ushort> Children = null;

        #region IServerSideItem Members

        /// <summary>
        /// The group ID of the group -- always zero
        /// </summary>
        public ushort ItemID
        {
            get { return 0; }
        }

        #endregion

        #region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object obj)
        {
            if (obj is SSIGroup)
            {
                return this.ID.CompareTo((obj as SSIGroup).ID);
            }
            return -1;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates an SSI item representing a single contact
    /// </summary>
    public class SSIBuddy : IServerSideItem
    {
        private ushort _groupid = 0;
        private string _name = "";
        private string _displayname = "";
        private string _email = "";

        private List<Tlv> _unprocessed = new List<Tlv>();

        /// <summary>
        /// The unique ID of the contact
        /// </summary>
        public readonly ushort ID;

        /// <summary>
        /// The ID of the contact's parent group
        /// </summary>
        public ushort GroupID
        {
            get { return _groupid; }
            set { _groupid = value; }
        }

        /// <summary>
        /// The actual name of the contact
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The display name of the contact
        /// </summary>
        public string DisplayName
        {
            get { return _displayname; }
            set { _displayname = value; }
        }

        /// <summary>
        /// The locally assigned email address of this contact
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        /// <summary>
        /// Gets a list of TLVs sent by the server that went unprocessed by OscarLib
        /// </summary>
        /// <remarks>As of at least 11-2006, there are two TLVs that seem to be necessary in SSI item modifications, 0x006A and 0x006D.
        /// This list provides a handy place to keep track of them.</remarks>
        internal List<Tlv> UnprocessedTLVs
        {
            get { return _unprocessed; }
        }

        /// <summary>
        /// The contact's SMS information
        /// </summary>
        public string SMS = "";

        /// <summary>
        /// A locally assigned comment about this contact
        /// </summary>
        public string Comment = "";

        /// <summary>
        /// Two bytes representing the different possible alert styles
        /// </summary>
        public ushort Alerts = 0;

        /// <summary>
        /// A sound file to play when this contact signs on
        /// </summary>
        public string SoundFile = "";

        /// <summary>
        /// Indicates whether the local client is awaiting authorization by the remote contact
        /// </summary>
        public bool AwaitingAuthorization = false;

        /// <summary>
        /// The reason message that can be transmitted, if authorization is required
        /// </summary>
        public string AuthorizationReason = "";

        /// <summary>
        /// This is set when a contact ads you to his contactlist, needed for extended contact info ???
        /// </summary>
        public byte[] MetaInfoToken = null;
        /// <summary>
        /// This is set when a contact ads you to his contactlist, ???
        /// </summary>
        public double MetaInfoTime = 0;

        /// <summary>
        /// Creates a new SSIBuddy object
        /// </summary>
        /// <param name="id">The ItemID for the object</param>
        public SSIBuddy(ushort id)
        {
            this.ID = id;
        }

        #region IServerSideItem Members

        /// <summary>
        /// The unique ID of the contact
        /// </summary>
        public ushort ItemID
        {
            get { return ID; }
        }

        #endregion

        #region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object obj)
        {
            if (obj is SSIBuddy)
            {
                return this.ID.CompareTo((obj as SSIBuddy).ID);
            }
            return -1;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates an SSI item representing a single Permit item
    /// </summary>
    public class SSIPermit : IServerSideItem
    {
        /// <summary>
        /// The contact name associated with item
        /// </summary>
        public string Name;

        /// <summary>
        /// The unique ID of the item
        /// </summary>
        public readonly ushort ID;

        /// <summary>
        /// Creates a new SSIPermitDeny object
        /// </summary>
        /// <param name="id">The ItemID for the object</param>
        public SSIPermit(ushort id)
        {
            this.ID = id;
        }

        #region IServerSideItem Members

        /// <summary>
        /// The unique ID of the item
        /// </summary>
        public ushort ItemID
        {
            get { return ID; }
        }

        #endregion

        #region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object obj)
        {
            if (obj is SSIPermit)
            {
                return this.ID.CompareTo((obj as SSIPermit).ID);
            }
            return -1;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates an SSI item representing a single Deny item
    /// </summary>
    public class SSIDeny : IServerSideItem
    {
        /// <summary>
        /// The contact name associated with item
        /// </summary>
        public string Name;

        /// <summary>
        /// The unique ID of the item
        /// </summary>
        public readonly ushort ID;

        /// <summary>
        /// Creates a new SSIDeny object
        /// </summary>
        /// <param name="id">The ItemID for the object</param>
        public SSIDeny(ushort id)
        {
            this.ID = id;
        }

        #region IServerSideItem Members

        /// <summary>
        /// The unique ID of the item
        /// </summary>
        public ushort ItemID
        {
            get { return ID; }
        }

        #endregion

        #region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object obj)
        {
            if (obj is SSIDeny)
            {
                return this.ID.CompareTo((obj as SSIDeny).ID);
            }
            return -1;
        }

        #endregion
    }

    internal class SSIIcon : IServerSideItem
    {
        public byte[] Hash;
        public ushort ID = 0;

        public uint IconSize = 0;
        public ushort Checksum = 0;
        public uint Stamp = 0;

        /// <summary>
        /// Computes checksums and hashes for a buddy icon
        /// </summary>
        /// <param name="filename">The filename of the icon</param>
        /// <returns><c>true</c> if the server-side icon should be updated, <c>false</c> otherwise</returns>
        public bool ComputeItems(string filename)
        {
            bool retval = false;
            if (String.IsNullOrEmpty(filename))
                return retval;

            try
            {
                byte[] hash = null;
                FileStream fs = File.OpenRead(filename);
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

                this.Stamp = (uint) (File.GetLastWriteTime(filename) - epoch).TotalSeconds;
                this.IconSize = (uint) fs.Length;
                this.Checksum = ChecksumIcon(filename);

                hash = System.Security.Cryptography.MD5.Create().ComputeHash(fs);
                if (this.Hash == null)
                {
                    this.Hash = hash;
                }

                for (int i = 0; i < this.Hash.Length; i++)
                {
                    if (this.Hash[i] != hash[i])
                        retval = true;
                }
            }
            catch
            {
                retval = false;
            }

            return retval;
        }

        private ushort ChecksumIcon(string filename)
        {
            uint retval = 0;
            int i = 0;

            using (StreamReader sr = new StreamReader(filename))
            {
                byte[] buffer = new byte[sr.BaseStream.Length];
                sr.BaseStream.Read(buffer, 0, buffer.Length);

                for (i = 0; i + 1 < buffer.Length; i += 2)
                {
                    retval += (uint) ((buffer[i + 1] << 8) + buffer[i]);
                }

                if (i < buffer.Length)
                    retval += buffer[i];

                retval = ((retval & 0xffff0000) >> 16) + (retval & 0x0000ffff);
            }
            return (ushort) retval;
        }

        #region IServerSideItem Members

        public ushort ItemID
        {
            get { return ID; }
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is SSIIcon)
            {
                return this.ID.CompareTo((obj as SSIIcon).ID);
            }
            return -1;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates the permit/deny settings stored on the server
    /// </summary>
    internal class SSIPermitDenySetting : IServerSideItem
    {
        public PrivacySetting Privacy = PrivacySetting.AllowAllUsers;
        public uint AllowedUserClasses = 0xFFFFFFFF;

        public readonly ushort ID;

        public SSIPermitDenySetting(ushort id)
        {
            this.ID = id;
        }

        #region IServerSideItem Members

        public ushort ItemID
        {
            get { return this.ID; }
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is SSIPermitDenySetting)
            {
                return this.ID.CompareTo((obj as SSIPermitDenySetting).ID);
            }
            return -1;
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Provides an interface for managing Server-Stored Information
    /// </summary>
    public class SSIManager
    {
        /// <summary>
        /// The number of objects a list of child objects is padded by to avoid resizing
        /// </summary>
        private const int CHILDPADDING = 3;

        private const int SSI_TYPE_BUDDY = 0x0000;
        private const int SSI_BUDDY_ALIAS = 0x0131;
        private const int SSI_BUDDY_EMAIL = 0x0137;
        private const int SSI_BUDDY_SMS = 0x013A;
        private const int SSI_BUDDY_COMMENTS = 0x013C;
        private const int SSI_BUDDY_ALERTS = 0x013D;
        private const int SSI_BUDDY_SOUNDFILE = 0x013E;
        private const int SSI_BUDDY_AUTHORIZE = 0x0066;
        private const int SSI_BUDDY_AUTHREASON = 0x015b;
        private const int SSI_BUDDY_DATA = 0x006D;
        private const int SSI_BUDDY_METATOKEN = 0x015c;
        private const int SSI_BUDDY_METATIME = 0x015d;

        private const int SSI_TYPE_GROUP = 0x0001;
        private const int SSI_GROUP_CHILDREN = 0x00C8;

        private const int SSI_TYPE_PERMIT = 0x0002;
        private const int SSI_TYPE_DENY = 0x0003;

        private const int SSI_TYPE_VISIBILITY = 0x0004;
        private const int SSI_VISIBILITY_PRIVACY = 0x00CA;
        private const int SSI_VISIBILITY_CLASSES = 0x00CB;

        private const int SSI_TYPE_IDLETIME = 0x0005;

        private const int SSI_TYPE_ICON = 0x0014;
        private const int SSI_ICON_HASH = 0x00D5;
        private const int SSI_ICON_PERSIST = 0x0131;

        private readonly Session parentSession;
        private int _groupcount = 0;
        private int _buddycount = 0;
        private SSIIcon iconitem = null;

        private readonly List<IServerSideItem> allItems = new List<IServerSideItem>();
        private readonly List<SSIPermit> _permits = new List<SSIPermit>();
        private readonly List<SSIDeny> _denies = new List<SSIDeny>();
        private SSIGroup masterGroup = new SSIGroup();

        private bool _publicidletime = true;

        private int _requests = 0;


        /// <summary>
        /// Creates a new SSIManager
        /// </summary>
        public SSIManager(Session parent)
        {
            parentSession = parent;
        }

        /// <summary>
        /// Adds an SSI item to the local list
        /// </summary>
        /// <param name="si">An <see cref="SSIItem"/> item</param>
        internal void AddSSIItem(SSIItem si)
        {
            switch (si.ItemType)
            {
                case SSIManager.SSI_TYPE_BUDDY:
                    CreateBuddy(si);
                    break;
                case SSIManager.SSI_TYPE_GROUP:
                    if (si.GroupID != 0)
                        CreateGroup(si, false);
                    else
                        CreateMasterGroup(si, false);
                    break;
                case SSIManager.SSI_TYPE_PERMIT:
                    CreatePermit(si);
                    break;
                case SSIManager.SSI_TYPE_DENY:
                    CreateDeny(si);
                    break;
                case SSIManager.SSI_TYPE_VISIBILITY:
                    ProcessVisibility(si);
                    break;
                case SSIManager.SSI_TYPE_IDLETIME:
                    ProcessIdleTime(si);
                    break;
                case SSIManager.SSI_TYPE_ICON:
                    ProcessIcon(si);
                    break;
                default:
                    Logging.WriteString("Got unrecognized SSI item of type " + si.ItemType);
                    break;
            }
        }

        /// <summary>
        /// Updates an SSI item on the local list
        /// </summary>
        /// <param name="si">An <see cref="SSIItem"/> item</param>
        internal void UpdateSSIItem(SSIItem si)
        {
            switch (si.ItemType)
            {
                case SSIManager.SSI_TYPE_GROUP:
                    if (si.GroupID != 0)
                        CreateGroup(si, true);
                    else
                        CreateMasterGroup(si, true);
                    break;
            }
        }

        /// <summary>
        /// Removes an SSI item from the local list
        /// </summary>
        /// <param name="si">An <see cref="SSIItem"/> item</param>
        internal void RemoveSSIItem(SSIItem si)
        {
            switch (si.ItemType)
            {
                case SSIManager.SSI_TYPE_GROUP:
                    RemoveGroupFromServer(si);
                    break;
                case SSIManager.SSI_TYPE_BUDDY:
                    RemoveBuddyFromServer(si);
                    break;
            }
        }

        #region Group items

        internal void CreateMasterGroup(SSIItem item, bool modify)
        {
            SSIGroup group = null;
            if (!modify) 
            {
                group = new SSIGroup();
                group.ID = item.GroupID;
            } 
            else 
            {
                group = GetGroupByID(item.GroupID);
            }

            group.Name = item.Name;

            using (ByteStream stream = new ByteStream(item.Tlvs.ReadByteArray(SSI_GROUP_CHILDREN)))
            {
                int numgroups = stream.GetByteCount()/2;
                group.Children = new List<ushort>(numgroups + CHILDPADDING);
                for (int k = 0; k < numgroups; k++)
                {
                    ushort childId = stream.ReadUshort();
                    group.Children.Add(childId);
                }
            }

            if (!modify)
            {
                lock (this)
                {
                    allItems.Add(group);
                    masterGroup = group;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="SSIGroup"/> from an SSI item sent by the server
        /// </summary>
        /// <param name="item">The received <see cref="SSIItem"/> object</param>
        /// <param name="modify">A value indicating whether the group is being modified by the server or created for the first time</param>
        internal void CreateGroup(SSIItem item, bool modify)
        {
            SSIGroup group = null;
            if (!modify)
            {
                group = new SSIGroup();
                group.ID = item.GroupID;
                AddSSIGroupToManager(group);
            }
            else
            {
                group = GetGroupByID(item.GroupID);
            }

            group.Name = item.Name;

            using (ByteStream stream = new ByteStream(item.Tlvs.ReadByteArray(SSI_GROUP_CHILDREN)))
            {
                int numgroups = stream.GetByteCount()/2;
                group.Children = new List<ushort>(numgroups + CHILDPADDING);
                for (int k = 0; k < numgroups; k++)
                {
                    ushort childId = stream.ReadUshort();
                    group.Children.Add(childId);
                }
            }

            //TODO:  For modify?  REALLY?
            parentSession.OnGroupItemReceived(group);
        }

        /// <summary>
        /// Gets a single <see cref="SSIGroup"/> object by its name
        /// </summary>
        /// <param name="name">The name of the group to return</param>
        /// <returns>
        /// A <see cref="SSIGroup"/> object, or <c>null</c> if there is no SSIGroup
        /// object with the given name
        /// </returns>
        public SSIGroup GetGroupByName(string name)
        {
            lock (this)
            {
                for (int i = 0, j = allItems.Count; i < j; i++)
                {
                    if (allItems[i] is SSIGroup && (allItems[i] as SSIGroup).Name == name)
                        return allItems[i] as SSIGroup;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a single <see cref="SSIGroup"/> object by its ID number
        /// </summary>
        /// <param name="id">The ID number of the group to return</param>
        /// <returns>
        /// A <see cref="SSIGroup"/> object, or <c>null</c> if there is no SSIGroup
        /// object with the given ID number
        /// </returns>
        public SSIGroup GetGroupByID(ushort id)
        {
            lock (this)
            {
                for (int i = 0, j = allItems.Count; i < j; i++)
                {
                    if (allItems[i] is SSIGroup && (allItems[i] as SSIGroup).ID == id)
                        return allItems[i] as SSIGroup;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a new group to the client's list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        /// <param name="index">The index of the new group</param>
        /// <remarks>The group is initially empty</remarks>
        [Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddGroup methods without the index parameter.")]
        public SSIGroup AddGroup(string groupname, int index)
        {

            SSIGroup parent = GetGroupByID(0);
            int length = (parent.Children != null) ? parent.Children.Count : 0;
            if (index > length)
                throw new ArgumentOutOfRangeException();

            ushort newid = this.GetNextGroupID();

            SSIItem newitem = new SSIItem();
            newitem.GroupID = newid;
            newitem.ItemID = 0;
            newitem.ItemType = SSIManager.SSI_TYPE_GROUP;
            newitem.Name = groupname;
            newitem.Tlvs = null;

            SSIGroup newgroup = new SSIGroup();
            newgroup.ID = newid;
            newgroup.Name = groupname;


            SNAC13.SendEditStart(parentSession);
            SNAC13.AddSSIItems(parentSession, new SSIItem[] {newitem});
            AddSSIGroupToManager(newgroup);
            AddChildToGroup(0, index, newid);
            SNAC13.SendEditComplete(parentSession);

            return newgroup;
        }

        /// <summary>
        /// Adds a new group to the client's list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        /// <param name="id">The id of the new group</param>
        /// <remarks>The group is initially empty</remarks>
        public SSIGroup AddGroup(string groupname, ushort id)
        {
            SSIItem newitem = new SSIItem();
            newitem.GroupID = id;
            newitem.ItemID = 0;
            newitem.ItemType = SSIManager.SSI_TYPE_GROUP;
            newitem.Name = groupname;
            newitem.Tlvs = null;

            SSIGroup newgroup = new SSIGroup();
            newgroup.ID = id;
            newgroup.Name = groupname;


            SNAC13.SendEditStart(parentSession);
            SNAC13.AddSSIItems(parentSession, new SSIItem[] { newitem });
            AddSSIGroupToManager(newgroup);
            
            if(id != 0)
                AddChildToGroup(0, id);

            SNAC13.SendEditComplete(parentSession);

            return newgroup;

        }

        /// <summary>
        /// Adds a new group to the client's list
        /// </summary>
        /// <param name="groupname">The name of the new group</param>
        /// <remarks>The group is initially empty</remarks>
        public SSIGroup AddGroup(string groupname)
        {
            return this.AddGroup(groupname, this.GetNextGroupID());
        }

        /// <summary>
        /// Renames an existing group
        /// </summary>
        /// <param name="groupID">The ID of the group to be renamed</param>
        /// <param name="newname">The new name of the group</param>
        public void RenameGroup(ushort groupID, string newname)
        {
            SSIGroup parent = GetGroupByID(groupID);
            if (parent == null)
            {
                // No such groupID
                return;
            }

            parent.Name = newname;

            SSIItem modification = new SSIItem();
            modification.GroupID = groupID;
            modification.ItemID = 0;
            modification.ItemType = SSIManager.SSI_TYPE_GROUP;
            modification.Name = newname;

            int i = 0, index = 0;
            int length = (parent.Children != null) ? parent.Children.Count : 0;

            byte[] newchildren = new byte[length*2];
            for (; i < length; i++)
            {
                Marshal.InsertUshort(newchildren, parent.Children[i], ref index);
            }

            TlvBlock tlvs = new TlvBlock();
            tlvs.WriteByteArray(SSI_GROUP_CHILDREN, newchildren);
            modification.Tlvs = tlvs;

            SNAC13.SendEditStart(parentSession);
            SNAC13.ModifySSIItems(parentSession, new SSIItem[] {modification});
            SNAC13.SendEditComplete(parentSession);
        }

        /// <summary>
        /// Adds a child item from a group
        /// </summary>
        /// <param name="groupID">The ID of the group to which to add a child</param>
        /// <param name="index">The index of the child in the group</param>
        /// <param name="childID">The ID of the child to add</param>
        protected void AddChildToGroup(ushort groupID, int index, ushort childID)
        {
            SSIGroup parent = GetGroupByID(groupID);
            int length = (parent.Children != null) ? parent.Children.Count : 0;
            if (index > length)
                throw new ArgumentOutOfRangeException("index", "The index value is higher then existing contacts at this group");

            if (parent == null)
            {
                // No such groupID
                return;
            }
            SSIItem modification = new SSIItem();
            modification.GroupID = parent.ID;
            modification.ItemID = 0;
            modification.ItemType = SSIManager.SSI_TYPE_GROUP;
            modification.Name = parent.Name;

            // Add the new child to the SSIGroup's child list
            //ushort[] copychildren = new ushort[length + 1];
            //if (parent.Children != null)
            //{
            //  Array.Copy(parent.Children, 0, copychildren, 0, index);
            //  if(index < parent.Children.Length)
            //    Array.Copy(parent.Children, index, copychildren, index+1, parent.Children.Length - index);
            //}
            //copychildren[index] = childID;
            if (parent.Children == null)
                parent.Children = new List<ushort>();

            parent.Children.Insert(index, childID);

            // Copy the children into a byte array
            int currindex = 0;
            byte[] newchildren = new byte[(length + 1)*2];
            for (int i = 0; i < length + 1; ++i)
            {
                Marshal.InsertUshort(newchildren, parent.Children[i], ref currindex);
            }

            TlvBlock tlvs = new TlvBlock();
            tlvs.WriteByteArray(SSI_GROUP_CHILDREN, newchildren);
            modification.Tlvs = tlvs;

            SNAC13.ModifySSIItems(parentSession, new SSIItem[] {modification});
        }

        /// <summary>
        /// Adds a child item to a group
        /// </summary>
        /// <param name="groupID">The ID of the group to which to add a child</param>
        /// <param name="childID">The ID of the child to add</param>
        protected void AddChildToGroup(ushort groupID, ushort childID)
        {
            SSIGroup parent = GetGroupByID(groupID);

            if (parent == null)
            {
                throw new ApplicationException(
                    "A group with the group id '" + groupID.ToString() + "' does not exists.");
            }

            if (parent.Children == null)
                parent.Children = new List<ushort>();

            parent.Children.Add(childID);

            SSIItem modification = new SSIItem();
            modification.GroupID = parent.ID;
            modification.ItemID = 0;
            modification.ItemType = SSIManager.SSI_TYPE_GROUP;
            modification.Name = parent.Name;
               
            byte[] newchildren = new byte[parent.Children.Count * 2];
            foreach (ushort child in parent.Children)
            {
                Marshal.InsertUshort(newchildren, child, parent.Children.IndexOf(child));
            }

            modification.Tlvs = new TlvBlock();
            modification.Tlvs.WriteByteArray(SSI_GROUP_CHILDREN, newchildren);

            SNAC13.ModifySSIItems(parentSession, new SSIItem[] { modification });

        }
        /// <summary>
        /// Shuffles children within a group
        /// </summary>
        /// <param name="groupID">The group to shuffle</param>
        /// <param name="index">The new index of the childID</param>
        /// <param name="childID">The ID to be moved</param>
        protected void ShuffleChildren(ushort groupID, int index, ushort childID)
        {
            SSIGroup parent = GetGroupByID(groupID);
            int length = (parent.Children != null) ? parent.Children.Count : 0;
            if (index >= length)
                throw new ArgumentOutOfRangeException();

            if (parent == null)
            {
                // No such groupID
                return;
            }

            int shiftindex = parent.Children.IndexOf(childID);
            if (shiftindex == index || shiftindex == -1)
            {
                // Nothing will be moved or no child found
                return;
            }

            parent.Children.RemoveAt(shiftindex);
            parent.Children.Insert(index < shiftindex ? index : index - 1, childID);

            int currindex = 0;
            byte[] newchildren = new byte[length*2];
            for (int i = 0; i < length; ++i)
            {
                Marshal.InsertUshort(newchildren, parent.Children[i], ref currindex);
            }

            TlvBlock tlvs = new TlvBlock();
            tlvs.WriteByteArray(SSI_GROUP_CHILDREN, newchildren);

            SSIItem modification = new SSIItem();
            modification.GroupID = parent.ID;
            modification.ItemID = 0;
            modification.ItemType = SSIManager.SSI_TYPE_GROUP;
            modification.Name = parent.Name;
            modification.Tlvs = tlvs;

            SNAC13.ModifySSIItems(parentSession, new SSIItem[] { modification });
        }

        /// <summary>
        /// Removes a child item from a group
        /// </summary>
        /// <param name="groupID">The ID of the group from which to remove a child</param>
        /// <param name="childID">The ID of the child to remove</param>
        protected void RemoveChildFromGroup(ushort groupID, ushort childID)
        {
            SSIGroup parent = GetGroupByID(groupID);
            SSIItem modification = new SSIItem();
            modification.GroupID = parent.ID;
            modification.ItemID = 0;
            modification.ItemType = SSIManager.SSI_TYPE_GROUP;
            modification.Name = parent.Name;

            int index = 0;
            int length = (parent.Children != null) ? parent.Children.Count : 0;
            if (length == 0)
                return;

            if (!parent.Children.Remove(childID))
            {
                // Child ID didn't exist in the group
                return;
            }

            byte[] newchildren = new byte[parent.Children.Count*2];
            for (int i = 0; i < parent.Children.Count; i++)
            {
                Marshal.InsertUshort(newchildren, parent.Children[i], ref index);
            }

            TlvBlock tlvs = new TlvBlock();
            tlvs.WriteByteArray(SSI_GROUP_CHILDREN, newchildren);
            modification.Tlvs = tlvs;

            SNAC13.ModifySSIItems(parentSession, new SSIItem[] {modification});
        }

        /// <summary>
        /// Moves a buddy
        /// </summary>
        /// <param name="buddy">The buddy to be moved</param>
        /// <param name="destination">The new parent group for the buddy</param>
        /// <param name="index">The new index in the destination group</param>
        public void MoveBuddy(SSIBuddy buddy, SSIGroup destination, int index)
        {
            if (buddy.GroupID != destination.ID)
            {
                SNAC13.SendEditStart(parentSession);
                this.RemoveBuddy(buddy);
                this.AddModifyBuddy(buddy.Name, destination.ID, index,
                              buddy.DisplayName, buddy.Email, buddy.SMS, buddy.Comment, buddy.SoundFile,
                              buddy.AwaitingAuthorization, buddy.AuthorizationReason, 
                              buddy.MetaInfoToken, buddy.MetaInfoTime, null, false);
                SNAC13.SendEditComplete(parentSession);
            }
            else
            {
                this.ShuffleChildren(destination.ID, index, buddy.ID);
            }
        }

        /// <summary>
        /// Moves a group within the contact list
        /// </summary>
        /// <param name="group">The group to move</param>
        /// <param name="index">The new index</param>
        public void MoveGroup(SSIGroup group, int index)
        {
            this.ShuffleChildren(0, index, group.ID);
        }

        /// <summary>
        /// Removes a group from the client's list
        /// </summary>
        /// <param name="group">The local <see cref="SSIGroup"/> object</param>
        /// <remarks>This method will remove all buddy items that are children of the group</remarks>
        public void RemoveGroup(SSIGroup group)
        {
            if (group.ID == 0)
                return; // Oscarlib doesnt support deleting the master group

            SNAC13.SendEditStart(parentSession);

            if (group.Children != null)
            {
                ushort[] children = group.Children.ToArray();
                foreach (ushort child in children)
                {
                    RemoveBuddy(GetBuddyByID(child));
                }
            }

            RemoveSSIGroupFromManager(group);
            RemoveSSIItem(SSIManager.SSI_TYPE_GROUP, 0, group.ID, group.Name);

            SNAC13.SendEditComplete(parentSession);
        }

        internal void RemoveGroupFromServer(SSIItem group)
        {
            lock (this)
            {
                SSIGroup target = null;
                foreach (SSIGroup ssi in GetSSIItems<SSIGroup>())
                {
                    if (ssi.ID == group.ItemID)
                    {
                        target = ssi;
                        break;
                    }
                }

                if (target == null)
                {
                    return;
                }

                RemoveSSIGroupFromManager(target);
                parentSession.OnGroupItemRemoved(target);
            }
        }

        /// <summary>
        /// Encapsulates the operation to add an SSI group to the internal lists and counters
        /// </summary>
        internal void AddSSIGroupToManager(SSIGroup group)
        {
            lock (this)
            {
                allItems.Add(group);
                _groupcount++;
            }
        }

        /// <summary>
        /// Encapsulates the operation to remove an SSI group from the internal lists and counters
        /// </summary>
        internal void RemoveSSIGroupFromManager(SSIGroup group)
        {
            lock (this)
            {
                allItems.Remove(group);
                _groupcount--;
            }
        }

        #endregion

        #region Buddy items

        /// <summary>
        /// Creates a new <see cref="SSIBuddy"/> from an SSI item sent by the server
        /// </summary>
        /// <param name="item">The received <see cref="SSIItem"/> object</param>
        internal void CreateBuddy(SSIItem item)
        {
            SSIBuddy buddy = new SSIBuddy(item.ItemID);
            buddy.GroupID = item.GroupID;
            buddy.Name = item.Name;
            buddy.AwaitingAuthorization = false;
            buddy.DisplayName = item.Tlvs.ReadString(SSI_BUDDY_ALIAS, Encoding.UTF8);
            buddy.Email = item.Tlvs.ReadString(SSI_BUDDY_EMAIL, Encoding.ASCII);
            buddy.SMS = item.Tlvs.ReadString(SSI_BUDDY_SMS, Encoding.ASCII);
            buddy.Comment = item.Tlvs.ReadString(SSI_BUDDY_COMMENTS, Encoding.UTF8);
            buddy.Alerts = item.Tlvs.ReadUshort(SSI_BUDDY_ALERTS);
            buddy.SoundFile = item.Tlvs.ReadString(SSI_BUDDY_SOUNDFILE, Encoding.ASCII);
            buddy.AwaitingAuthorization = item.Tlvs.HasTlv(SSI_BUDDY_AUTHORIZE);
            buddy.MetaInfoToken = item.Tlvs.ReadByteArray(SSI_BUDDY_METATOKEN);
            buddy.MetaInfoTime = item.Tlvs.ReadDouble(SSI_BUDDY_METATIME);
            

            AddSSIBuddyToManager(buddy);
            parentSession.OnBuddyItemReceived(buddy);
        }

        /// <summary>
        /// Gets a single buddy by its ID number
        /// </summary>
        /// <param name="buddyid">The ID number of the buddy to return</param>
        /// <param name="groupid">The ID number of the group to which the buddy belongs</param>
        /// <returns>An <see cref="SSIBuddy"/> object, or <c>null</c> if the buddy ID does not exist</returns>
        public SSIBuddy GetBuddyByID(ushort buddyid)
        {
            lock (this)
            {
                for (int i = 0, j = allItems.Count; i < j; i++)
                {
                    SSIBuddy item = allItems[i] as SSIBuddy;
                    if (item != null && item.ID == buddyid)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a single buddy by its Screenname
        /// </summary>
        /// <param name="screenname">The Screenname of the buddy to return</param>
        /// <returns>An <see cref="SSIBuddy"/> object, or <c>null</c> if the buddy ID does not exist</returns>
        public SSIBuddy GetBuddyByName(string screenname)
        {
            lock (this)
            {
                for (int i = 0, j = allItems.Count; i < j; i++)
                {
                    SSIBuddy item = allItems[i] as SSIBuddy;
                    if (item != null && item.Name == screenname)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a new buddy to the client's list
        /// </summary>
        /// <param name="name">The buddy's screen name</param>
        /// <param name="parentID">The ID of the group to which the buddy should be added</param>
        /// <param name="index">The index of the buddy in the group</param>
        /// <param name="alias">The alias of the buddy</param>
        /// <param name="email">The buddy's email address</param>
        /// <param name="SMS">The buddy's SMS contact number</param>
        /// <param name="comment">A comment about this buddy</param>
        /// <param name="soundfile">The path of a local soundfile associated with this buddy</param>
        /// <param name="authorizationRequired">Determines if the buddy needs authorization</param>
        /// <param name="authorizationReason">the authorization reason/message which will be send to the buddy</param>
        /// <returns>The ID number of the newly created buddy</returns>
        /// <remarks>
        /// SSI modification requires acknowledgement from the server. The newly created SSIBuddy
        /// item will be returned to the client upon successful addition
        /// </remarks>
        [Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddBuddy method without the index parameter.")]
        public SSIBuddy AddBuddy(string name, ushort parentID, int index, string alias,
                                 string email, string SMS, string comment, string soundfile, 
                                 bool authorizationRequired, string authorizationReason)
        {
            return AddModifyBuddy(name, parentID, index, alias, email, SMS, comment, soundfile, authorizationRequired, authorizationReason, null, double.MaxValue, null);
        }

        /// <summary>
        /// Adds a new buddy to the client's list
        /// </summary>
        /// <param name="name">The buddy's screen name</param>
        /// <param name="parentID">The ID of the group to which the buddy should be added</param>
        /// <param name="alias">The alias of the buddy</param>
        /// <param name="email">The buddy's email address</param>
        /// <param name="SMS">The buddy's SMS contact number</param>
        /// <param name="comment">A comment about this buddy</param>
        /// <param name="soundfile">The path of a local soundfile associated with this buddy</param>
        /// <param name="authorizationRequired">Determines if the buddy needs authorization</param>
        /// <param name="authorizationReason">the authorization reason/message which will be send to the buddy</param>
        /// <returns>The ID number of the newly created buddy</returns>
        /// <remarks>
        /// SSI modification requires acknowledgement from the server. The newly created SSIBuddy
        /// item will be returned to the client upon successful addition
        /// </remarks>
        public SSIBuddy AddBuddy(string name, ushort parentID, string alias,
                         string email, string SMS, string comment, string soundfile, bool authorizationRequired,
                         string authorizationReason)
        {
            return
                AddModifyBuddy(name, parentID, alias, email, SMS, comment, soundfile, authorizationRequired,
                               authorizationReason, null, double.MaxValue, null);
        }

        /// <summary>
        /// Adds a new buddy to the client's list
        /// </summary>
        /// <param name="name">The buddy's screen name</param>
        /// <param name="parentID">The ID of the group to which the buddy should be added</param>
        /// <param name="index">The index of the buddy in the group</param>
        /// <param name="alias">The alias of the buddy</param>
        /// <param name="email">The buddy's email address</param>
        /// <param name="SMS">The buddy's SMS contact number</param>
        /// <param name="comment">A comment about this buddy</param>
        /// <param name="soundfile">The path of a local soundfile associated with this buddy</param>
        /// <returns>The ID number of the newly created buddy</returns>
        /// <remarks>
        /// SSI modification requires acknowledgement from the server. The newly created SSIBuddy
        /// item will be returned to the client upon successful addition
        /// </remarks>
        public SSIBuddy AddBuddy(string name, ushort parentID, int index, string alias,
                                 string email, string SMS, string comment, string soundfile)
        {
            return AddModifyBuddy(name, parentID, index, alias, email, SMS, comment, soundfile, false, "", null, double.MaxValue, null);
        }

        /// <summary>
        /// Modifies a buddy on the client's list
        /// </summary>
        /// <param name="changebuddy">
        /// The local <see cref="SSIBuddy"/> object, with the required
        /// fields changed
        /// </param>
        /// <remarks>
        /// SSI modification requires acknowledgement from the server. The newly modified SSIBuddy
        /// item will be returned to the client upon successful addition
        /// </remarks>
        public void ModifyBuddy(SSIBuddy changebuddy)
        {
            AddModifyBuddy("", 0xFFFF, -1, "", "", "", "", "", false, "", null, double.MaxValue, changebuddy);
        }

        /// <summary>
        /// Removes a buddy from the client's list
        /// </summary>
        /// <param name="buddy">The local <see cref="SSIBuddy"/> object</param>
        public void RemoveBuddy(SSIBuddy buddy)
        {
            RemoveSSIBuddyFromManager(buddy);
            this.RemoveSSIItem(SSIManager.SSI_TYPE_BUDDY, buddy.ID, buddy.GroupID, buddy.Name);
            this.RemoveChildFromGroup(buddy.GroupID, buddy.ID);
        }

        /// <summary>
        /// Delete yourself from another client server-side contact list.
        /// </summary>
        /// <param name="screenname">The others ScreenName</param>
        public void RemoveYourself(string screenname)
        {
            SNAC13.RemoveMySSIItem(parentSession, screenname);
        }

        internal void RemoveBuddyFromServer(SSIItem buddy)
        {
            lock (this)
            {
                SSIBuddy target = null;
                foreach (SSIBuddy ssi in GetSSIItems<SSIBuddy>())
                {
                    if (ssi.ItemID == buddy.ItemID && ssi.GroupID == buddy.GroupID)
                    {
                        target = ssi;
                        break;
                    }
                }

                if (target == null)
                {
                    return;
                }

                RemoveSSIBuddyFromManager(target);
                parentSession.OnBuddyItemRemoved(target);
            }
        }

        /// <summary>
        /// Contains SSI item creation and sending routines for adding or modifying SSI buddy items
        /// </summary>
        /// <param name="name">The buddy's screen name</param>
        /// <param name="parentID">The ID of the group to which the buddy should be added</param>
        /// <param name="index">The new index of the buddy in the group</param>
        /// <param name="alias">The alias of the buddy</param>
        /// <param name="email">The buddy's email address</param>
        /// <param name="SMS">The buddy's SMS contact number</param>
        /// <param name="comment">A comment about this buddy</param>
        /// <param name="soundfile">The path of a local soundfile associated with this buddy</param>
        /// <param name="authorizationRequired">Determines if u need to be authorized by the buddy</param>
        /// <param name="authorizationReason">The authorization reason/message that will be send to the buddy</param>
        /// <param name="metatoken">The metatoken sended at login, it's needed for <see cref="IcqManager.RequestFullUserInfo"/></param>
        /// <param name="metatime">The metatime sended at login</param>
        /// <param name="original">The original <see cref="SSIBuddy"/> object if this is a modification
        /// <param name="sendedit">If you want to send begin and end edit for the process</param>
        /// operation, or <c>null</c> if this is a creation operation</param>
        /// <returns></returns>
        [Obsolete("This method is obsolete and will be removed soon. Use the overloaded AddModifyBuddy method without the index parameter.")]
        protected internal SSIBuddy AddModifyBuddy(string name, ushort parentID, int index, string alias,
                                                   string email, string SMS, string comment, string soundfile,
                                                   bool authorizationRequired, string authorizationReason,
                                                   byte[] metatoken, double metatime,
                                                   SSIBuddy original, bool sendedit = true)
        {
            ushort newid = 0;

            if (original != null)
            {
                newid = original.ID;
                name = original.Name;
                parentID = original.GroupID;
                alias = original.DisplayName;
                email = original.Email;
                soundfile = original.SoundFile;
                authorizationRequired = original.AwaitingAuthorization;
                metatoken = original.MetaInfoToken;
                metatime = original.MetaInfoTime;
            }
            else
            {
                newid = this.GetNextItemID();
            }

            SSIItem newitem = new SSIItem();
            newitem.ItemID = newid;
            newitem.GroupID = parentID;
            newitem.ItemType = SSIManager.SSI_TYPE_BUDDY;
            newitem.Name = name;
            newitem.Tlvs = new TlvBlock();

            if (!String.IsNullOrEmpty(alias))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_ALIAS, alias, Encoding.UTF8);
            if (!String.IsNullOrEmpty(email))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_EMAIL, email, Encoding.ASCII);
            if (!String.IsNullOrEmpty(SMS))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_SMS, SMS, Encoding.ASCII);
            if (!String.IsNullOrEmpty(comment))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_COMMENTS, comment, Encoding.UTF8);
            if (!String.IsNullOrEmpty(soundfile))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_SOUNDFILE, soundfile, Encoding.ASCII);
            if (!String.IsNullOrEmpty(authorizationReason))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_AUTHREASON, authorizationReason, Encoding.ASCII);
            if (authorizationRequired)
                newitem.Tlvs.WriteEmpty(SSIManager.SSI_BUDDY_AUTHORIZE);
            if (metatoken != null)
                newitem.Tlvs.WriteByteArray(SSIManager.SSI_BUDDY_METATOKEN, metatoken);
            if (metatime != double.MaxValue)
                newitem.Tlvs.WriteDouble(SSIManager.SSI_BUDDY_METATIME, metatime);

            SSIBuddy store = new SSIBuddy(newid);
            store.Name = name;
            store.GroupID = parentID;
            store.DisplayName = alias;
            store.Email = email;
            store.SMS = SMS;
            store.Comment = comment;
            store.SoundFile = soundfile;
            store.MetaInfoToken = metatoken;
            store.MetaInfoTime = metatime;

            if (sendedit) SNAC13.SendEditStart(parentSession);

            if (original == null)
            {
                AddSSIBuddyToManager(store);
                SNAC13.AddSSIItems(parentSession, new SSIItem[] {newitem});
                this.AddChildToGroup(parentID, index, newid);
            }
            else
                SNAC13.ModifySSIItems(parentSession, new SSIItem[] {newitem});

            if (sendedit) SNAC13.SendEditComplete(parentSession);

            return store;
        }

        /// <summary>
        /// Contains SSI item creation and sending routines for adding or modifying SSI buddy items
        /// </summary>
        /// <param name="name">The buddy's screen name</param>
        /// <param name="parentID">The ID of the group to which the buddy should be added</param>
        /// <param name="alias">The alias of the buddy</param>
        /// <param name="email">The buddy's email address</param>
        /// <param name="SMS">The buddy's SMS contact number</param>
        /// <param name="comment">A comment about this buddy</param>
        /// <param name="soundfile">The path of a local soundfile associated with this buddy</param>
        /// <param name="authorizationRequired">Determines if u need to be authorized by the buddy</param>
        /// <param name="authorizationReason">The authorization reason/message that will be send to the buddy</param>
        /// <param name="metatoken">The metatoken sended at login, it's needed for <see cref="IcqManager.RequestFullUserInfo"/></param>
        /// <param name="metatime">The metatime sended at login</param>
        /// <param name="original">The original <see cref="SSIBuddy"/> object if this is a modification
        /// <param name="sendedit">If you want to send begin and end edit for the process</param>
        /// operation, or <c>null</c> if this is a creation operation</param>
        /// <returns>The buddy object</returns>
        protected internal SSIBuddy AddModifyBuddy(string name, ushort parentID, string alias,
                                                   string email, string SMS, string comment, string soundfile,
                                                   bool authorizationRequired, string authorizationReason,
                                                   byte[] metatoken, double metatime,
                                                   SSIBuddy original, bool sendedit = true) {
            ushort newid = 0;
 
            if (original != null)
            {
                newid = original.ID;
                name = original.Name;
                parentID = original.GroupID;
                alias = original.DisplayName;
                email = original.Email;
                soundfile = original.SoundFile;
                authorizationRequired = original.AwaitingAuthorization;
                metatoken = original.MetaInfoToken;
                metatime = original.MetaInfoTime;
            }
            else
            {
                newid = this.GetNextItemID();
            }

            SSIItem newitem = new SSIItem();
            newitem.ItemID = newid;
            newitem.GroupID = parentID;
            newitem.ItemType = SSIManager.SSI_TYPE_BUDDY;
            newitem.Name = name;
            newitem.Tlvs = new TlvBlock();


            if (!String.IsNullOrEmpty(alias))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_ALIAS, alias, Encoding.UTF8);
            if (!String.IsNullOrEmpty(email))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_EMAIL, email, Encoding.ASCII);
            if (!String.IsNullOrEmpty(SMS))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_SMS, SMS, Encoding.ASCII);
            if (!String.IsNullOrEmpty(comment))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_COMMENTS, comment, Encoding.UTF8);
            if (!String.IsNullOrEmpty(soundfile))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_SOUNDFILE, soundfile, Encoding.ASCII);
            if (!String.IsNullOrEmpty(authorizationReason))
                newitem.Tlvs.WriteString(SSIManager.SSI_BUDDY_AUTHREASON, authorizationReason, Encoding.ASCII);
            if (authorizationRequired)
                newitem.Tlvs.WriteEmpty(SSIManager.SSI_BUDDY_AUTHORIZE);
            if (metatoken != null)
                newitem.Tlvs.WriteByteArray(SSIManager.SSI_BUDDY_METATOKEN, metatoken);
            if (metatime != double.MaxValue)
                newitem.Tlvs.WriteDouble(SSIManager.SSI_BUDDY_METATIME, metatime);

            SSIBuddy store = new SSIBuddy(newid);
            store.Name = name;
            store.GroupID = parentID;
            store.DisplayName = alias;
            store.Email = email;
            store.SMS = SMS;
            store.Comment = comment;
            store.SoundFile = soundfile;
            store.MetaInfoToken = metatoken;
            store.MetaInfoTime = metatime;

            if (sendedit) SNAC13.SendEditStart(parentSession);

            if (original == null)
            {
                AddSSIBuddyToManager(store);
                SNAC13.AddSSIItems(parentSession, new SSIItem[] { newitem });
                this.AddChildToGroup(parentID, newid);
            }
            else
                SNAC13.ModifySSIItems(parentSession, new SSIItem[] { newitem });

            if (sendedit) SNAC13.SendEditComplete(parentSession);

            return store;
        }

        /// <summary>
        /// Encapsulates the operation to add an SSI buddy to the internal lists and counters
        /// </summary>
        internal void AddSSIBuddyToManager(SSIBuddy buddy)
        {
            lock (this)
            {
                allItems.Add(buddy);
                _buddycount++;
            }
        }

        /// <summary>
        /// Encapsulates the operation to remove an SSI buddy from the internal lists and counters
        /// </summary>
        internal void RemoveSSIBuddyFromManager(SSIBuddy buddy)
        {
            lock (this)
            {
                allItems.Remove(buddy);
                _buddycount--;
            }
        }

        #endregion

        #region Permit and deny items

        /// <summary>
        /// Creates a new <see cref="SSIPermit"/> object from an SSI permit item sent by the server
        /// </summary>
        /// <param name="item">The received <see cref="SSIItem"/> object</param>
        internal void CreatePermit(SSIItem item)
        {
            SSIPermit permit = new SSIPermit(item.ItemID);
            permit.Name = item.Name;
            lock (this)
            {
                allItems.Add(permit);
                _permits.Add(permit);
            }
        }

        /// <summary>
        /// Adds a new permit item to the client's list
        /// </summary>
        /// <param name="screenname">The screenname to add to the permit list</param>
        public void AddPermitItem(string screenname)
        {
            ushort newid = GetNextItemID();

            SSIItem newitem = new SSIItem();
            newitem.GroupID = 0;
            newitem.ItemID = newid;
            newitem.ItemType = SSIManager.SSI_TYPE_PERMIT;
            newitem.Name = screenname;
            newitem.Tlvs = null;

            SSIPermit newpermit = new SSIPermit(newid);
            newpermit.Name = screenname;
            lock (this)
            {
                allItems.Add(newpermit);
                _permits.Add(newpermit);
            }

            SNAC13.AddSSIItems(parentSession, new SSIItem[] {newitem});
        }

        /// <summary>
        /// Removes an item from the client's permit list
        /// </summary>
        /// <param name="removeitem">The local <see cref="SSIPermit"/> object to remove</param>
        public void RemovePermitItem(SSIPermit removeitem)
        {
            lock (this)
            {
                allItems.Remove(removeitem);
                _permits.Remove(removeitem);
            }
            this.RemoveSSIItem(SSIManager.SSI_TYPE_PERMIT, removeitem.ID, 0, removeitem.Name);
        }

        /// <summary>
        /// Creates a new <see cref="SSIDeny"/> from an SSI deny item sent by the server
        /// </summary>
        /// <param name="item">The received <see cref="SSIItem"/> object</param>
        internal void CreateDeny(SSIItem item)
        {
            SSIDeny deny = new SSIDeny(item.ItemID);
            deny.Name = item.Name;
            lock (this)
            {
                allItems.Add(deny);
                _denies.Add(deny);
            }
        }

        /// <summary>
        /// Adds a new deny item to the client's list
        /// </summary>
        /// <param name="screenname">The screenname to add to the deny list</param>
        public void AddDenyItem(string screenname)
        {
            ushort newid = this.GetNextItemID();

            SSIItem newitem = new SSIItem();
            newitem.GroupID = 0;
            newitem.ItemID = newid;
            newitem.ItemType = SSIManager.SSI_TYPE_DENY;
            newitem.Name = screenname;
            newitem.Tlvs = null;

            SSIDeny newdeny = new SSIDeny(newid);
            newdeny.Name = screenname;
            lock (this)
            {
                allItems.Add(newdeny);
                _denies.Add(newdeny);
            }

            SNAC13.AddSSIItems(parentSession, new SSIItem[] {newitem});
        }

        /// <summary>
        /// Removes an item from the client's deny list
        /// </summary>
        /// <param name="removeitem">The local <see cref="SSIDeny"/> object to remove</param>
        public void RemoveDenyItem(SSIDeny removeitem)
        {
            lock (this)
            {
                allItems.Remove(removeitem);
                _denies.Remove(removeitem);
            }
            this.RemoveSSIItem(SSIManager.SSI_TYPE_DENY, removeitem.ID, 0, removeitem.Name);
        }

        #endregion

        #region Icon settings

        /// <summary>
        /// Processes a server-stored icon hash
        /// </summary>
        /// <param name="item">An <see cref="SSIItem"/> sent by the server</param>
        internal void ProcessIcon(SSIItem item)
        {
            if (iconitem == null)
            {
                iconitem = new SSIIcon();
            }
            iconitem.ID = item.ItemID;
            iconitem.Hash = item.Tlvs.ReadByteArray(SSI_ICON_HASH);

            lock (this)
            {
                allItems.Add(iconitem);
            }
        }
        #endregion

        #region Visibility settings

        /// <summary>
        /// Processes server-stored visibility settings
        /// </summary>
        /// <param name="item"></param>
        internal void ProcessVisibility(SSIItem item)
        {
            SSIPermitDenySetting pds = new SSIPermitDenySetting(item.ItemID);
            pds.Privacy = (PrivacySetting) item.Tlvs.ReadByte(SSI_VISIBILITY_PRIVACY);
            pds.AllowedUserClasses = item.Tlvs.ReadUint(SSI_VISIBILITY_CLASSES);

            lock (this)
            {
                allItems.Add(pds);
            }
        }

        internal SSIPermitDenySetting GetPDSetting()
        {
            lock (this)
            {
                foreach (object obj in allItems)
                {
                    if (obj is SSIPermitDenySetting)
                        return (SSIPermitDenySetting) obj;
                }
            }
            return null;
        }

        internal void SetPDSetting(SSIPermitDenySetting pds, bool newitem)
        {
            SSIItem item = new SSIItem();
            item.GroupID = 0;
            item.ItemID = pds.ItemID;
            item.Name = "";
            item.ItemType = SSIManager.SSI_TYPE_VISIBILITY;

            item.Tlvs = new TlvBlock();
            item.Tlvs.WriteByte(SSI_VISIBILITY_PRIVACY, (byte) pds.Privacy);
            item.Tlvs.WriteUint(SSI_VISIBILITY_CLASSES, pds.AllowedUserClasses);

            if (newitem)
                SNAC13.AddSSIItems(parentSession, new SSIItem[] {item});
            else
                SNAC13.ModifySSIItems(parentSession, new SSIItem[] {item});
        }

        /// <summary>
        /// Gets or sets the client's <see cref="PrivacySetting"/>
        /// </summary>
        public PrivacySetting Privacy
        {
            get
            {
                SSIPermitDenySetting retval = GetPDSetting();

                if (retval != null)
                    return retval.Privacy;
                else
                    return PrivacySetting.AllowAllUsers;
            }
            set
            {
                SSIPermitDenySetting pd = GetPDSetting();
                bool upload = false;
                bool newitem = false;

                if (pd == null)
                {
                    // Would this ever happen? I think all accounts have a PDS by
                    // default. I might be wrong though, it's happened before.
                    pd = new SSIPermitDenySetting(this.GetNextItemID());
                    lock (this)
                    {
                        allItems.Add(pd);
                    }
                    upload = true;
                    newitem = true;
                }

                if (pd.Privacy != value)
                {
                    pd.Privacy = value;
                    upload = true;
                }

                if (upload)
                {
                    SetPDSetting(pd, newitem);
                }
            }
        }

        /// <summary>
        /// Gets or sets the user classes that are allowed to contact the client
        /// </summary>
        public uint AllowedUserClasses
        {
            get
            {
                SSIPermitDenySetting retval = GetPDSetting();
                if (retval != null)
                    return retval.AllowedUserClasses;
                else
                    return 0xFFFFFFFF;
            }
            set
            {
                SSIPermitDenySetting pd = GetPDSetting();
                bool upload = false;
                bool newitem = false;

                if (pd == null)
                {
                    pd = new SSIPermitDenySetting(this.GetNextItemID());
                    lock (this)
                    {
                        allItems.Add(pd);
                    }
                    upload = true;
                    newitem = true;
                }

                if (pd.AllowedUserClasses != value)
                {
                    pd.AllowedUserClasses = value;
                    upload = true;
                }

                if (upload)
                    SetPDSetting(pd, newitem);
            }
        }

        #endregion

        #region Idle time settings

        /// <summary>
        /// Processes server-stored idle time settings
        /// </summary>
        /// <param name="item"></param>
        internal void ProcessIdleTime(SSIItem item)
        {
            uint value = item.Tlvs.ReadUint(0x00C9);
            _publicidletime = value == 0x0000046F;
        }

        #endregion

        #region Utilities and properties

        /// <summary>
        /// Removes an SSI item from the server-stored list
        /// </summary>
        /// <param name="type">The type of the item to remove</param>
        /// <param name="ID">The item's ID number</param>
        /// <param name="groupID">The item's group ID number</param>
        /// <param name="name">The name of the item</param>
        protected internal void RemoveSSIItem(ushort type, ushort ID, ushort groupID, string name)
        {
            SSIItem removal = new SSIItem();
            removal.ItemType = type;
            removal.ItemID = ID;
            removal.GroupID = groupID;
            removal.Name = name;
            removal.Tlvs = null;

            SNAC13.RemoveSSIItems(parentSession, new SSIItem[] {removal});
        }

        /// <summary>
        /// Returns the next unused item ID in the sequence
        /// </summary>
        /// <remarks>This method locks on the SSIManager object, caller locking is not necessary</remarks>
        private ushort GetNextItemID()
        {
            ushort retval = 0;
            bool foundid = false;
            lock (this)
            {
                while (!foundid)
                {
                    foundid = true;
                    for (int i = 0, j = allItems.Count; i < j; i++)
                    {
                        if (allItems[i].ItemID == retval)
                        {
                            retval++;
                            foundid = false;
                            break;
                        }
                    }
                }
            }
            return retval;
        }

        private ushort GetNextGroupID()
        {
            ushort retval = 1; // 0 is reserved for the non-stored master group
            bool foundid = false;
            lock (this)
            {
                while (!foundid)
                {
                    foundid = true;
                    foreach (SSIGroup group in GetSSIItems<SSIGroup>())
                    {
                        if (group.ID == retval)
                        {
                            retval++;
                            foundid = false;
                            break;
                        }
                    }
                }
            }
            return retval;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the SSI manager is waiting for the
        /// server to acknowledge a modification
        /// </summary>
        protected internal int OutstandingRequests
        {
            get
            {
                lock (this)
                {
                    return _requests;
                }
            }
            set
            {
                lock (this)
                {
                    _requests = value;
                }
            }
        }

        internal SSIIcon IconItem
        {
            get { return iconitem; }
            set { iconitem = value; }
        }

        /// <summary>
        /// Gets an enumerator for a specified type of SSI item
        /// </summary>
        /// <typeparam name="T">An <see cref="IServerSideItem"/>, typically an <see cref="SSIBuddy"/> or <see cref="SSIGroup"/></typeparam>
        /// <returns>An enumerator consisting of the SSI items of type <typeparamref name="T"/> contained by the SSI manager</returns>
        public IEnumerable<T> GetSSIItems<T>() where T : class
        {
            lock (this)
            {
                for (int i = 0, j = allItems.Count; i < j; i++)
                {
                    if (allItems[i] is T)
                    {
                        yield return allItems[i] as T;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the server side buddy list
        /// </summary>
        /// <returns>The buddylist</returns>
        public List<SSIBuddy> GetSSIBuddyList()
        {
            List<SSIBuddy> ssiBuddyList = new List<SSIBuddy>();
            lock (this)
            {
                foreach (IServerSideItem item in allItems)
                {
                    if(item as SSIBuddy != null)
                        ssiBuddyList.Add((SSIBuddy)item);
                }
            }
            return ssiBuddyList;
        }

        /// <summary>
        /// Gets the server side group list
        /// </summary>
        /// <returns>The grouplist</returns>
        public List<SSIGroup> GetSSIGroupList()
        {
            List<SSIGroup> ssiGroupList = new List<SSIGroup>();
            lock (this)
            {
                foreach (IServerSideItem item in allItems)
                {
                    if (item as SSIGroup != null)
                        ssiGroupList.Add((SSIGroup)item);
                }
            }
            return ssiGroupList;
        }
        internal void ResetContactList()
        {
            allItems.Clear();
        }

        internal ushort GetLocalSSIItemCount()
        {
            return (ushort) allItems.Count;
        }

        ///// <summary>
        ///// Gets a list of all current SSIBuddy items
        ///// </summary>
        //public IEnumerable<SSIBuddy> Buddies
        //{
        //  get { return _buddies; }
        //}

        ///// <summary>
        ///// Gets a list of all current SSIGroup items
        ///// </summary>
        //public List<SSIGroup> Groups
        //{
        //  get { return _groups; }
        //}

        ///// <summary>
        ///// Gets a list of all current SSIPermit items
        ///// </summary>
        //public List<SSIPermit> Permits
        //{
        //  get { return _permits; }
        //}

        ///// <summary>
        ///// Gets a list of all current SSIDeny items
        ///// </summary>
        //public List<SSIDeny> Denies
        //{
        //  get { return _denies; }
        //}

        /// <summary>
        /// Gets the master SSIGroup for this client
        /// </summary>
        public SSIGroup MasterGroup
        {
            get { return masterGroup; }
        }

        #endregion

        /// <summary>
        /// Resize the internal lists prior to SSI item processing
        /// </summary>
        /// <param name="num_items">The number of items that will be added</param>
        /// <remarks>This call is to avoid runtime- and over-allocation in the Items list</remarks>
        internal void PreallocateLists(ushort num_items)
        {
            allItems.Capacity += num_items + CHILDPADDING;
        }
    }
}