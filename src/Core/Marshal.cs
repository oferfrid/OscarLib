/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System.Text;
using System;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// The Marshal class contains utility functions
    /// to aid in marshalling and unmarshalling data into OSCAR-compatable
    /// format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BitConverter class was not used to aid in byte-array-to-data-type
    /// functions due to the endian-ness of the data.
    /// As an example, consider the following byte array:
    /// <code>byte[] buffer = new byte[] {0x00,0x01,0x02,0x03};</code>
    /// When asked to create a ushort from the first two bytes in the array,
    /// BitConverter will return 128 (0x0100) as opposed to 1 (0x0001). The OSCAR
    /// protocol expects byte buffers to be read left-to-right, in network order
    /// format.
    /// </para>
    /// </remarks>
    internal static class Marshal
    {
        /// <summary>
        /// Writes an unsigned short (two bytes) into an array
        /// </summary>
        /// <param name="buffer">A byte buffer</param>
        /// <param name="u">An unsigned short</param>
        /// <param name="index">The index at which to write the two bytes</param>
        public static void InsertUshort(byte[] buffer, ushort u, int index)
        {
            InsertUshort(buffer, u, ref index);
        }

        /// <summary>
        /// Writes an unsigned short (two bytes) into an array
        /// </summary>
        /// <param name="buffer">A byte buffer</param>
        /// <param name="u">An unsigned short</param>
        /// <param name="index">The index at which to write the two bytes</param>
        public static void InsertUshort(byte[] buffer, ushort u, ref int index)
        {
            if (index + 2 > buffer.Length)
            {
                // trouble
            }
            buffer[index++] = (byte)((u & 0xFF00) >> 8);
            buffer[index++] = (byte)(u & 0x00FF);
        }

        /// <summary>
        /// Writes an unsigned int (four bytes) into an array
        /// </summary>
        /// <param name="buffer">A byte buffer</param>
        /// <param name="u">An unsigned int</param>
        /// <param name="index">The index at which to write the four bytes</param>
        public static void InsertUint(byte[] buffer, uint u, ref int index)
        {
            if (index + 4 > buffer.Length)
            {
                // trouble
            }
            buffer[index++] = (byte)((u & 0xFF000000) >> 24);
            buffer[index++] = (byte)((u & 0x00FF0000) >> 16);
            buffer[index++] = (byte)((u & 0x0000FF00) >> 8);
            buffer[index++] = (byte)(u & 0x000000FF);
        }

        /// <summary>
        /// Writes an string into an array
        /// </summary>
        /// <param name="buffer">A byte buffer</param>
        /// <param name="s">A string</param>
        /// <param name="encoding">An <see cref="Encoding"/> describing the string</param>
        /// <param name="index">The index at which to write the string</param>
        public static void InsertString(byte[] buffer, string s, Encoding encoding, ref int index)
        {
            byte[] bytes = encoding.GetBytes(s);
            CopyArray(bytes, buffer, 0, ref index);
        }

		public static DataPacket BuildDataPacket(ISession sess, SNACHeader header, ByteStream data)
        {
            DataPacket dp = new DataPacket(data);
            dp.ParentSession = sess;
            dp.SNAC = header;
            return dp;
        }

        public static ushort EncodingToCharset(Encoding encoding)
        {
            if (encoding == Encoding.ASCII)
            {
                return 0x0000;
            }
            else if (encoding == Encoding.BigEndianUnicode)
            {
                return 0x0002;
            }
            return 0x0003;
        }

        public static Encoding AolMimeToEncoding(string aolmime)
        {
            Encoding retval = Encoding.ASCII;

            if (!String.IsNullOrEmpty(aolmime))
            {
                int charsetindex = aolmime.IndexOf("charset=");
                if (charsetindex != -1)
                {
                    aolmime = aolmime.Substring(charsetindex + 8).Trim('\"');
                }

                try
                {
                    retval = Encoding.GetEncoding(aolmime);
                }
                catch
                {
                    // Gaim (at least) sends unicode-2-0 when it means utf-FFFE on Windows
                    if (aolmime.ToLower() == "unicode-2-0")
                    {
                        retval = Encoding.BigEndianUnicode;
                    }
                }
            }

            return retval;
        }

        /// <summary>
        /// Gets a byte array containing the AOL representation of the specified Encoding
        /// </summary>
        /// <param name="e">The <see cref="System.Text.Encoding"/> to convert</param>
        /// <returns>A byte array containing the US-ASCII string of the encoding</returns>
        /// <remarks>
        /// This function supports:
        ///		<list type="bullet">
        ///   <item><see cref="System.Text.Encoding.ASCII"/> -- text/aolrtf; charset="us-ascii"</item>
        ///		<item><see cref="System.Text.Encoding.BigEndianUnicode"/> -- text/aolrtf; charset="unicode-2-0"</item>
        /// 	<item>ISO-8859-1 (Latin1) -- text/aolrtf; charset="iso-8859-1"</item>
        ///		<item>Other -- text/aolrtf; charset="[webname]", where [webname] is the <see cref="System.Text.Encoding.WebName"/>
        ///         of the argument</item>
        ///		</list>
        /// </remarks>
        public static string EncodingToAolMime(Encoding e)
        {
            if (e == Encoding.BigEndianUnicode)
            {
                return "text/aolrtf; charset=\"unicode-2-0\"";
            }


            // TODO: Compact Framework probably needs to handle this encoding somehow... . detect a different way?
#if !WindowsCE
            if (e == Encoding.GetEncoding("iso-8859-1"))
            {
                return "text/aolrtf; charset=\"iso-8859-1\"";
            }
#endif

            return "text/aolrtf; charset=\"" + e.WebName + "\"";
        }

        #region CopyArray and overloads

        public static void CopyArray(byte[] src, byte[] dest, int src_start)
        {
            int dest_start = 0;
            CopyArray(src, dest, ref src_start, ref dest_start);
        }

        public static void CopyArray(byte[] src, byte[] dest, int src_start, int dest_start)
        {
            CopyArray(src, dest, ref src_start, ref dest_start);
        }

        public static void CopyArray(byte[] src, byte[] dest, int src_start, ref int dest_start)
        {
            CopyArray(src, dest, ref src_start, ref dest_start);
        }

        public static void CopyArray(byte[] src, byte[] dest, ref int src_start, ref int dest_start)
        {
            int j = src.Length;
            int k = dest.Length;
            while (src_start < j && dest_start < k)
            {
                dest[dest_start++] = src[src_start++];
            }
        }

        #endregion

        #region File transfer inserts

        public static void InsertFileHeader(byte[] buffer, FileTransferConnection conn, ref int index)
        {
            FileHeader fh = conn.FileHeader;

            int i;
            byte[] idstring = new byte[32];
            byte[] dummy = new byte[69];
            byte[] macinfo = new byte[16];
            byte[] filename = new byte[64];

            for (i = 0; i < 32 && i < fh.IdString.Length; i++)
            {
                idstring[i] = (byte)fh.IdString[i];
            }
            idstring[31] = 0x00;
            while (i < 32)
            {
                idstring[i++] = 0x00;
            }

            for (i = 0; i < 69; i++)
                dummy[i] = 0x00;

            for (i = 0; i < 16; i++)
                macinfo[i] = 0x00;

            for (i = 0; i < 64 && i < fh.Name.Length; i++)
            {
                filename[i] = (byte)fh.Name[i];
            }
            filename[63] = 0x00;
            while (i < 64)
            {
                filename[i++] = 0x00;
            }

            // 8
            CopyArray(fh.Cookie, buffer, 0, ref index);
            // 16
            InsertUshort(buffer, fh.Encryption, ref index);
            InsertUshort(buffer, fh.Compression, ref index);
            InsertUshort(buffer, (ushort)conn.TotalFiles, ref index);
            InsertUshort(buffer, (ushort)conn.FilesRemaining, ref index);
            InsertUshort(buffer, (ushort)conn.TotalParts, ref index);
            InsertUshort(buffer, fh.PartsLeft, ref index);
            // 28
            InsertUint(buffer, conn.TotalFileSize, ref index);
            InsertUint(buffer, fh.Size, ref index);
            InsertUint(buffer, fh.modtime, ref index);
            InsertUint(buffer, fh.Checksum, ref index);
            InsertUint(buffer, fh.ResourceForkReceivedChecksum, ref index);
            InsertUint(buffer, fh.ResourceForkSize, ref index);
            InsertUint(buffer, fh.cretime, ref index);
            InsertUint(buffer, fh.ResourceForkChecksum, ref index);
            InsertUint(buffer, fh.nrecvd, ref index);
            InsertUint(buffer, fh.ReceivedChecksum, ref index);
            // 68
            CopyArray(idstring, buffer, 0, ref index);
            // 100
            buffer[index++] = fh.flags;
            buffer[index++] = fh.lnameoffset;
            buffer[index++] = fh.lsizeoffset;
            // 103
            CopyArray(dummy, buffer, 0, ref index);
            // 172
            CopyArray(macinfo, buffer, 0, ref index);
            // 188
            InsertUshort(buffer, fh.nencode, ref index);
            InsertUshort(buffer, fh.nlanguage, ref index);
            // 192
            CopyArray(filename, buffer, 0, ref index);
            // 256
        }

        public static byte[] CreateFileTransferMessage(ushort type, FileTransferConnection conn)
        {
            int index = 0;
            byte[] retval = new byte[256];
            InsertString(retval, "OFT2", Encoding.ASCII, ref index);
            InsertUshort(retval, 0x0100, ref index);
            InsertUshort(retval, type, ref index);
            InsertFileHeader(retval, conn, ref index);

            return retval;
        }

        #endregion
    }
}