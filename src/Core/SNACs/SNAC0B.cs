using System;
using System.Text;

// string encoding complete

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x000B -- statistic reporting service
    /// </summary>
    internal static class SNAC0B
    {
        /// <summary>
        /// Processes the reporting interval alert -- SNAC(0B,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0B,02)</param>
        public static void ProcessReportingInterval(DataPacket dp)
        {
            dp.ParentSession.OnReportingIntervalReceived(dp.Data.ReadUshort());
        }

        /// <summary>
        /// Sends a status report to the OSCAR server -- SNAC(0B,03)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <remarks>libfaim does not send this report</remarks>
		public static void SendStatusReport(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.UsageStatsService;
            sh.FamilySubtypeID = (ushort) UsageStatsService.UsageReport;
            sh.Flags = 0;
            sh.RequestID = Session.GetNextRequestID();

            // Get the necessary strings together
            string sn = sess.ScreenName;
            // Here we start lying until I can figure out how to get *all* the values in .NET
            string osversion = Environment.OSVersion.ToString();
            string osname = "Windows 98"; //osversion.Substring(0, osversion.LastIndexOf(" ") - 1);
            string osver = "4.10.67766446"; //osversion.Substring(osversion.LastIndexOf(" ") + 1);
            string procname = "Intel Pentium";
            string winsockname = "Microsoft Corporation BSD Socket API for Windows";
            string winsockver = "4.1.1998";

            Encoding enc = Encoding.ASCII;
            byte snlength = (byte) enc.GetByteCount(sn);
            ushort osversionlength = (ushort) enc.GetByteCount(osversion);
            ushort osnamelength = (ushort) enc.GetByteCount(osname);
            ushort osverlength = (ushort) enc.GetByteCount(osver);
            ushort procnamelength = (ushort) enc.GetByteCount(procname);
            ushort winsocknamelength = (ushort) enc.GetByteCount(winsockname);
            ushort winsockverlength = (ushort) enc.GetByteCount(winsockver);

            ByteStream mainStream = new ByteStream();
            using (ByteStream stream = new ByteStream())
            {
                TimeSpan t = (DateTime.Now - new DateTime(1970, 1, 1));
                stream.WriteUint((uint) t.Seconds);
                t = (DateTime.UtcNow - new DateTime(1970, 1, 1).ToUniversalTime());
                stream.WriteUint((uint) t.Seconds);
                stream.WriteUint(0x00000000);
                stream.WriteUint(0x00000000);
                stream.WriteUint(0x00000000);
                stream.WriteUint(0x00000000);
                stream.WriteByte(snlength);
                stream.WriteString(sn, Encoding.ASCII);
                stream.WriteUshort(osnamelength);
                stream.WriteString(osname, Encoding.ASCII);
                stream.WriteUshort(osverlength);
                stream.WriteString(osver, Encoding.ASCII);
                stream.WriteUshort(procnamelength);
                stream.WriteString(procname, Encoding.ASCII);
                stream.WriteUshort(winsocknamelength);
                stream.WriteString(winsockname, Encoding.ASCII);
                stream.WriteUshort(winsockverlength);
                stream.WriteString(winsockver, Encoding.ASCII);
                stream.WriteUint(0x00000002);
                stream.WriteUint(0x00010001);
                stream.WriteUint(0x00020002);

                using (TlvBlock tlv = new TlvBlock())
                {
                    tlv.WriteByteArray(0x0009, stream.GetBytes());
                    mainStream.WriteTlvBlock(tlv);
                }
            }

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, mainStream));
        }

        /// <summary>
        /// Processes the statistic report acknowledgement -- SNAC(0B,04)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(0B,04)</param>
        public static void ProcessReportAcknowledgement(DataPacket dp)
        {
            // Well...that's it. This SNAC is empty.
        }
    }
}