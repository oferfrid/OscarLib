/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Manages relationships between SNAC families and OSCAR connections
    /// </summary>
    internal class ConnectionManager
    {
        private Connection _bos = null;
        private ArrayList _chatconnections = new ArrayList();
        private Hashtable _connections = new Hashtable();
        private Hashtable _delayedpackets = new Hashtable();
        private List<DirectConnection> _directconnections = new List<DirectConnection>();
        private int _id = 0;
        private Session _parent = null;

        private IPAddress _localexternalip = null;
        private IPAddress _localinternalip = null;

        public IPAddress LocalExternalIP {
            get { return _localexternalip; }
            set { _localexternalip = value; }
        }

        public IPAddress LocalInternalIP {
            get { return _localinternalip; }
            set { _localinternalip = value; }
        }

        /// <summary>
        /// Initializes the ConnectionManager
        /// </summary>
        public ConnectionManager(Session parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Returns the BOS connection
        /// </summary>
        public Connection BOSConnection
        {
            get { return _bos; }
            set { _bos = value; }
        }

        /// <summary>
        /// Gets a value indicating whether or not there are pending connections
        /// </summary>
        public bool PendingConnections
        {
            get
            {
                if (BOSConnection != null)
                {
                    if (BOSConnection.Connecting)
                        return true;
                }
                lock (_connections)
                {
                    foreach (Connection conn in _connections.Values)
                    {
                        if (conn.Connecting)
                            return true;
                    }
                }
                return false;
            }
        }

        #region Delayed packet functionality

        /// <summary>
        /// Adds a packet to a delay queue for sending at a later time
        /// </summary>
        /// <param name="family">The family of the delayed packet</param>
        /// <param name="dp">The packet to delay</param>
        public void AddDelayedPacket(ushort family, DataPacket dp)
        {
            lock (_delayedpackets)
            {
                if (!_delayedpackets.ContainsKey(family))
                {
                    _delayedpackets.Add(family, new ArrayList());
                    dp.ParentSession.Services.RequestNewService(dp.SNAC.FamilyServiceID);
                }
                ((ArrayList) _delayedpackets[family]).Add(dp);
            }
        }

        public bool IsBeingDelayed(ushort family)
        {
            lock (_delayedpackets)
            {
                return _delayedpackets.ContainsKey(family);
            }
        }

        public void AddDelayedChatPacket(ChatRoom roominfo, DataPacket dp)
        {
        }

        /// <summary>
        /// Gets an array of delayed packets
        /// </summary>
        /// <param name="family">The family of the delayed packets</param>
        /// <returns>An array of packets that have been delayed, or <c>null</c> if none exist</returns>
        public DataPacket[] GetDelayedPackets(ushort family)
        {
            lock (_delayedpackets)
            {
                if (!_delayedpackets.ContainsKey(family))
                    return null;
                ArrayList packets = (ArrayList) _delayedpackets[family];
                _delayedpackets.Remove(family);

                DataPacket[] retval = new DataPacket[packets.Count];
                for (int i = 0; i < packets.Count; i++)
                {
                    retval[i] = (DataPacket) packets[i];
                }
                return retval;
            }
        }

        #endregion

        /// <summary>
        /// Creates a new connection and associates it with a family
        /// </summary>
        /// <param name="family">A family to associate it with</param>
        /// <returns>The newly created connection</returns>
        /// <remarks>If the family passed to CreateNewConnection is 0x0001, the
        /// newly created connection is associated as the BOS connection for the session.</remarks>
        public Connection CreateNewConnection(ushort family)
        {
            Connection retval = new Connection(_parent, _id++);
            if (family == 0x0001)
            {
                _bos = retval;
            }
            else
            {
                AssignFamily(family, retval);
            }
            return retval;
        }

        /// <summary>
        /// Removes a connection from the connection manager
        /// </summary>
        /// <param name="conn">The connection to remove</param>
        /// <param name="error"><c>true</c> if the deregistration is resulting from an error, <c>false</c> otherwise</param>
        public void DeregisterConnection(Connection conn, bool error)
        {
            List<ushort> unregister = new List<ushort>();
            bool endingauthconnection = false;

            lock (_connections)
            {
                foreach (DictionaryEntry de in _connections)
                {
                    if (de.Value == conn)
                    {
                        unregister.Add((ushort) de.Key);
                    }
                }

                foreach (ushort u in unregister)
                {
                    _connections.Remove(u);
                    if (u == 0x0017)
                    {
                        endingauthconnection = true;
                    }
                }
            }

            // If the connection is being deregistered because of error,
            // report it to the session
            if (error)
            {
                if (_parent.LoggedIn)
                {
                    if (conn == _bos)
                    {
                        _parent.OnError(ServerErrorCode.LostBOSConnection, null);
                    }
                    else
                    {
                        _parent.OnWarning(ServerErrorCode.LostSecondaryConnection, null);
                    }
                }
                else
                {
                    if (endingauthconnection)
                    {
                        _parent.OnLoginFailed(LoginErrorCode.CantReachAuthServer);
                    }
                    else
                    {
                        if (conn == _bos)
                        {
                            _parent.OnLoginFailed(LoginErrorCode.CantReachBOSServer);
                        }
                        else
                        {
                            _parent.OnWarning(ServerErrorCode.LostSecondaryConnection, null);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Creates a new chat room connection
        /// </summary>
        /// <param name="roominfo">The name of the chat room</param>
        /// <returns>The newly created chat connection</returns>
        public ChatConnection CreateNewChatConnection(ChatRoom roominfo)
        {
            ChatConnection retval = new ChatConnection(_parent, _id++, roominfo);
            roominfo.Connection = retval;
            _chatconnections.Add(roominfo);
            return retval;
        }

        /// <summary>
        /// Creates a new DirectIM connection
        /// </summary>
        /// <param name="method">The <see cref="DirectConnectionMethod"/> to use for the transfer</param>
        /// <param name="role">The <see cref="DirectConnectRole"/> of the local client in the transfer</param>
        public DirectIMConnection CreateNewDirectIMConnection(DirectConnectionMethod method, DirectConnectRole role)
        {
            DirectIMConnection retval = new DirectIMConnection(_parent, _id++, method, role);
            _directconnections.Add(retval);
            return retval;
        }

        /// <summary>
        /// Creates a new file transfer connection
        /// </summary>
        /// <param name="method">The <see cref="DirectConnectionMethod"/> to use for the transfer</param>
        /// <param name="role">The <see cref="DirectConnectRole"/> of the local client in the transfer</param>
        public FileTransferConnection CreateNewFileTransferConnection(DirectConnectionMethod method,
                                                                      DirectConnectRole role)
        {
            FileTransferConnection retval = new FileTransferConnection(_parent, _id++, method, role);
            _directconnections.Add(retval);
            return retval;
        }

        public ChatInvitationConnection CreateNewChatInvitationConnection(DirectConnectRole role)
        {
            ChatInvitationConnection retval = new ChatInvitationConnection(_parent, _id++, role);
            _directconnections.Add(retval);
            return retval;
        }

        /// <summary>
        /// Gets a <see cref="DirectConnection"/> by its Rendezvous cookie
        /// </summary>
        /// <returns>The <see cref="DirectConnection"/> that corresponds to the cookie, or null if no corresponding DC is found</returns>
        public DirectConnection GetDirectConnectionByCookie(Cookie key)
        {
            return GetDirectConnectionByCookie(key, false);
        }

        /// <summary>
        /// Gets a <see cref="DirectConnection"/> by its Rendezvous cookie
        /// </summary>
        /// <returns>The <see cref="DirectConnection"/> that corresponds to the cookie, or null if no corresponding DC is found</returns>
        public DirectConnection GetDirectConnectionByCookie(Cookie key, bool removefromcache)
        {
            DirectConnection retval = null;
            foreach (DirectConnection dc in _directconnections)
            {
                if(dc.Cookie.ToString() == key.ToString())
                {
                    retval = dc;
                    break;
                }
            }

            if (retval != null && removefromcache)
            {
                _directconnections.Remove(retval);
            }

            return retval;
        }

        /// <summary>
        /// Returns a <see cref="DirectIMConnection"/> by the screenname of the remote user
        /// </summary>
        public DirectIMConnection GetDirectIMByScreenname(string screenname)
        {
            lock (_directconnections)
            {
                foreach (DirectConnection dc in _directconnections)
                {
                    if (dc is DirectIMConnection && dc.Other.ScreenName == screenname)
                    {
                        return dc as DirectIMConnection;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Removes a direct connection from the connection manager cache
        /// </summary>
        public DirectConnection RemoveDirectConnection(Cookie key)
        {
            return GetDirectConnectionByCookie(key, true);
        }


        /// <summary>
        /// Gets a <see cref="Connection"/> object by the SNAC family ID it is associated with
        /// </summary>
        /// <param name="family">A SNAC family ID</param>
        /// <returns>A <see cref="Connection"/> object, or <c>null</c> if the ID is not
        /// registered with the ConnectionManager.</returns>
        public Connection GetByFamily(ushort family)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(family))
                {
                    return (Connection) _connections[family];
                }
                return null;
            }
        }

        public ChatRoom GetChatByConnection(ChatConnection conn)
        {
            foreach (ChatRoom cri in _chatconnections)
            {
                if (cri.Connection == conn)
                {
                    return cri;
                }
            }
            return null;
        }

        /// <summary>
        /// Disconnects from a chat room
        /// </summary>
        /// <param name="roomname">The roomname to disconnect</param>
        public void RemoveChatConnection(string roomname)
        {
            ChatRoom target = null;
            foreach (ChatRoom cri in _chatconnections)
            {
                if (cri.FullName == roomname)
                {
                    target = cri;
                    break;
                }
            }

            if (target != null)
            {
                _chatconnections.Remove(target);
                target.Connection.DisconnectFromServer(false);
                target = null;
            }
        }

        /// <summary>
        /// Assigns a <see cref="Connection"/> object to a SNAC family ID
        /// </summary>
        /// <param name="family">A SNAC family ID</param>
        /// <param name="conn">A <see cref="Connection"/> object</param>
        public void AssignFamily(ushort family, Connection conn)
        {
            lock (_connections)
            {
                if (_parent.Families.GetFamilyVersion(family) != 0xFFFF)
                    _connections[family] = conn;
            }
        }

        /// <summary>
        /// Returns a list of families assigned to a given connection
        /// </summary>
        /// <param name="conn">A <see cref="Connection"/> object</param>
        /// <returns>An array of SNAC family IDs registered with <paramref name="conn" /></returns>
        public ushort[] GetFamilies(Connection conn)
        {
            ArrayList list = new ArrayList();
            lock (_connections)
            {
                foreach (DictionaryEntry de in _connections)
                {
                    if (conn == (Connection) de.Value)
                    {
                        list.Add(de.Key);
                    }
                }
            }
            // Auto-add 0x0001
            ushort newkey = 0x0001;
            list.Add(newkey);

            ushort[] retval = new ushort[list.Count];
            for (int i = 0; i < retval.Length; i++)
            {
                retval[i] = (ushort) list[i];
            }
            return retval;
        }

        /// <summary>
        /// Gets a list of unique registered connections
        /// </summary>
        /// <returns>An ArrayList of <see cref="Connection"/> objects</returns>
        public ArrayList UniqueConnections()
        {
            ArrayList retval = new ArrayList();
            lock (_connections)
            {
                foreach (Connection conn in _connections.Values)
                {
                    if (!retval.Contains(conn))
                    {
                        retval.Add(conn);
                    }
                }
                return retval;
            }
        }
    }
}