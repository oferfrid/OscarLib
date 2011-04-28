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
    /// Describes reasons for messages being undeliverable
    /// </summary>
    public enum UndeliverableMessageReason
    {
        /// <summary>
        /// Message format was invalid
        /// </summary>
        InvalidMessage = 0x0000,
        /// <summary>
        /// The message was too large to be delivered
        /// </summary>
        MessageTooLarge = 0x0001,
        /// <summary>
        /// The sender's message sending rate was exceeded
        /// </summary>
        MessageRateExceeded = 0x0002,
        /// <summary>
        /// The sender's warning level is too high to be delivered
        /// </summary>
        SenderTooEvil = 0x0003,
        /// <summary>
        /// This client's warning level is too high to be delivered to
        /// </summary>
        SelfTooEvil = 0x0004
    }
}