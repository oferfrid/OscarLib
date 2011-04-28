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
    /// Describes the types of Typing Notification messages that can be sent or received
    /// </summary>
    public enum TypingNotification
    {
        /// <summary>
        /// Typing has finished
        /// </summary>
        TypingFinished = 0x0000,
        /// <summary>
        /// Text was typed
        /// </summary>
        /// <remarks>
        /// This notification is sent when a client has paused in typing, but not finished.
        /// Gaim indicates this state by changing the color of the remote client's screenname to orange.
        /// </remarks>
        TextTyped = 0x0001,
        /// <summary>
        /// Typing has started
        /// </summary>
        TypingStarted = 0x0002
    }
}