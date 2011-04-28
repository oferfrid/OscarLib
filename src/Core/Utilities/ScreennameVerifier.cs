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
    /// Contains utility methods to verify the correctness of a screenname
    /// </summary>
    internal static class ScreennameVerifier
    {
        /// <summary>
        /// Verifies if a screenname is a valid AIM network name
        /// </summary>
        /// <param name="screenname">The screenname to verify</param>
        /// <returns>A value indicating whether or not the screename is valid</returns>
        public static bool IsValidAIM(string screenname)
        {
            foreach (char c in screenname)
            {
                if (!(IsAlpha(c) || IsNumeric(c)) &&
                    c != ' ' && c != '.' && c != '@' && c != '-' && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verifies if a screenname is a valid ICQ network name
        /// </summary>
        /// <param name="screenname">The screenname to verify</param>
        /// <returns>A value indicating whether or not the screename is valid</returns>
        public static bool IsValidICQ(string screenname)
        {
            foreach (char c in screenname)
            {
                if (!IsNumeric(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies if a screenname is a valid SMS number
        /// </summary>
        /// <param name="screenname">The screenname to verify</param>
        /// <returns>A value indicating whether or not the screename is valid</returns>
        public static bool IsValidSMS(string screenname)
        {
            if (screenname[0] != '+')
                return false;

            for (int i = 1; i < screenname.Length; i++)
                if (!IsNumeric(screenname[i]))
                    return false;

            return true;
        }

        private static bool IsAlpha(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private static bool IsNumeric(char c)
        {
            return (c >= '0' && c <= '9');
        }
    }
}