/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Describes flags that affect how ICBM messages are sent
    /// </summary>
    [Flags]
    public enum MessageFlags : ushort
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0x00,
        /// <summary>
        /// The client sending the message has a buddy icon
        /// </summary>
        HasIcon = 0x01,
        /// <summary>
        /// This message is an automated response
        /// </summary>
        AutoResponse = 0x02,
        /// <summary>
        /// An acknowledgement of delivery has been requested
        /// </summary>
        [Obsolete("MessageManager always requests acknowledgements")]
        RequestAcknowledgement = 0x04,
        /// <summary>
        /// A buddy icon has been requested
        /// </summary>
        RequestIcon = 0x08,
        /// <summary>
        /// This message was received while the user was offline
        /// </summary>
        ReceivedOffline = 0x10
    }
}