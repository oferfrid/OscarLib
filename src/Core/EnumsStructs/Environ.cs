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
    /// Available to every class in OscarLib to handle the problem that the Compact Framework
    /// doesn't support Environment.NewLine
    /// </summary>
    public class Environ
    {
#if WindowsCE
    /// <summary>
    /// A string describing the newline character sequence on the local platform
    /// </summary>
		public const string NewLine = "\n";
#else
        /// <summary>
        /// A string describing the newline character sequence on the local platform
        /// </summary>
        public static string NewLine = Environment.NewLine;
#endif
    }
}