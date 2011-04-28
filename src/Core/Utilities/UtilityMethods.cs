/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Globalization;
using System.Text;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Contains global utility methods
    /// </summary>
    internal static class UtilityMethods
    {
        /// <summary>
        /// Translate %XX to ASCII values
        /// </summary>
        public static string DeHexUri(string uriPart)
        {
            StringBuilder retval = new StringBuilder();
            for (int i = 0; i < uriPart.Length; i++)
            {
                if (uriPart[i] != '%')
                {
                    retval.Append(uriPart[i]);
                }
                else
                {
                    string hex = uriPart.Substring(++i, 2);
                    i++;
                    retval.Append((char) ParseInt(hex, NumberStyles.HexNumber));
                }
            }
            return retval.ToString();
        }

        /// <summary>
        /// Wraps Int32.Parse methods for Windows CE compatibility
        /// </summary>
        public static int ParseInt(string number, NumberStyles styles)
        {
            int result = 0;
#if WindowsCE
          result = Int32.Parse(number, styles, null);
#else
            Int32.TryParse(number, styles, CultureInfo.InvariantCulture, out result);
#endif
            return result;
        }

        /// <summary>
        /// Returns a string describing an Encoding that is appropriate for OSCAR
        /// </summary>
        /// <param name="encoding">An <see cref="Encoding"/></param>
        /// <returns>A string representation of <paramref name="encoding"/> suitable for OSCAR</returns>
        public static string OscarEncodingToString(Encoding encoding)
        {
            if (encoding == Encoding.BigEndianUnicode)
            {
                return "unicode-2.0";
            }
            return encoding.WebName;
        }

        /// <summary>
        /// Scans an input string to determine its best-fit OSCAR-supported encoding
        /// </summary>
        /// <param name="input">An input string to be scanned</param>
        /// <returns>A text encoding with which to encode the string; see Remarks</returns>
        /// <remarks>
        /// <para>OSCAR supports three text encodings:  ASCII, ISO-8859-1, and UTF16-BE. In order
        /// to minimize outgoing message length, this method finds the best-fit encoding for the string.</para>
        /// </remarks>
        public static Encoding FindBestOscarEncoding(String input)
        {
            bool areAllLessThan80 = true;
            bool areAllLessThanFF = true;

            foreach (char c in input)
            {
                if (c > 0x80)
                {
                    areAllLessThan80 = false;
                    if (c > 0xFF)
                    {
                        areAllLessThanFF = false;
                        break;
                    }
                }
            }

            if (areAllLessThan80 && areAllLessThanFF)
            {
                return Encoding.ASCII;
            }
            else if (areAllLessThanFF)
            {
                return Encoding.GetEncoding(28591);
            }
            else
            {
                return Encoding.BigEndianUnicode;
            }
        }
    }
}