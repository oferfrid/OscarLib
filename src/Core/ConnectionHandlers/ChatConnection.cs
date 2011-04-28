/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Collections.Generic;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// This is not a real connection
    /// </summary>
    public class ChatInvitationConnection : DirectConnection
    {
        #region Chat room invitation caching

        private static List<string> _chatroomcache = new List<string>();

        public static void CacheChatRoomCreation(string roomname, ushort exchange)
        {
            _chatroomcache.Add(roomname + exchange.ToString());
        }

        public static bool IsExplictChatCreation(string roomname, ushort exchange)
        {
            string cache = roomname + exchange.ToString();
            bool retval = _chatroomcache.Contains(cache);
            _chatroomcache.Remove(cache);
            return retval;
        }

        #endregion

        public ChatInvitation ChatInvite = null;
        public ChatRoom ChatRoom = null;

		public ChatInvitationConnection(ISession parent, int id, DirectConnectRole role)
            : base(parent, id, DirectConnectionMethod.Direct, role)
        {
        }
    }

    /// <summary>
    /// Encapsulates a connection to an OSCAR chat room
    /// </summary>
    public class ChatConnection : Connection
    {
        private ChatRoom _roominfo;

        /// <summary>
        /// Creates a new chat connection
        /// </summary>
        /// <param name="parent">The <see cref="ISession"/> that owns this connection</param>
        /// <param name="id">The connection's unique ID</param>
        /// <param name="roominfo">A <see cref="ChatRoom"/> object</param>
		public ChatConnection(ISession parent, int id, ChatRoom roominfo)
            : base(parent, id)
        {
            _roominfo = roominfo;
        }

        /// <summary>
        /// Gets the <see cref="ChatRoom"/> that is using this connection
        /// </summary>
        public ChatRoom ChatRoom
        {
            get { return _roominfo; }
        }
    }
}