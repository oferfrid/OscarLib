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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods to log events and dump data
    /// </summary>
    internal class Logging
    {
        public static StreamWriter sw = null;

        public static bool IsLoggingEnabled
        {
            get { return sw != null; }
        }

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(
            out long lpPerformanceFreq);

        public static void DumpFLAP(byte[] buffer, string message)
        {
            if (sw != null)
            {
                lock (sw)
                {
                    sw.WriteLine(DateTime.Now.ToString() + " - " + message);
                    int i, j = buffer.Length, k;

                    StringBuilder sb = new StringBuilder();

                    for (i = 0, k = 1; i < j; i++, k++)
                    {
                        sw.Write("{0:x2} ", buffer[i]);
                        if (buffer[i] >= 0x20 && buffer[i] <= 0x7E)
                            sb.Append((char) buffer[i]);
                        else
                            sb.Append(".");

                        if ((k == 16))
                        {
                            sw.WriteLine(String.Format("\t{0}", sb.ToString()));
                            sb.Remove(0, sb.Length);
                            k = 0;
                        }
                    }

                    sw.WriteLine(String.Format("\t{0}", sb.ToString()));
                    sw.WriteLine("");
                    sw.Flush();
                }
            }
        }

        public static void WriteString(string str, params object[] parameters)
        {
            if (sw != null)
            {
                lock (sw)
                {
                    sw.WriteLine(DateTime.Now.ToString() + " - " + String.Format(str, parameters));
                    sw.WriteLine("");
                    sw.Flush();
                }
            }
        }

        internal static void DumpDataPacket(DataPacket dp)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(CultureInfo.CurrentCulture.NumberFormat,
                                       "SNAC: ({0:x4},{1:x4}), flags = {2:x4}, requestID = {3:x8}"
                                       + Environ.NewLine +
                                       "FLAP: channel = {4:x1}, sequence = {5:x2}, datasize = {6:x2}",
                                       dp.SNAC.FamilyServiceID,
                                       dp.SNAC.FamilySubtypeID,
                                       dp.SNAC.Flags,
                                       dp.SNAC.RequestID,
                                       dp.FLAP.Channel,
                                       dp.FLAP.DatagramSequenceNumber,
                                       dp.FLAP.DataSize);
            DumpFLAP(dp.Data.GetBytes(), stringBuilder.ToString());
        }
    }
}