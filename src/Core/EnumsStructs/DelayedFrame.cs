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
    /// Encapsulates a delayed frame that will be sent after a successful service request
    /// </summary>
    internal struct DelayedFrame
    {
        public byte[] Data;
        public SNACHeader SNAC;
    }
}