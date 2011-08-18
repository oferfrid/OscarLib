using System;
using System.Collections.Generic;
using System.Text;
using csammisrun.OscarLib.Utility;
using System.Timers;

namespace csammisrun.OscarLib
{
    public class UsageStatsManager : ISnacFamilyHandler
    {
        /// <summary>
        /// The SNAC family responsible for sending client details
        /// </summary>
        private const int SNAC_USAGESTATS_FAMILY = 0x000B;
        private enum UsageStatsService
        {
            /// <summary>
            /// Client / Server error. Can be sent by client or server.
            /// </summary>
            ClientServerError = 0x01,
            /// <summary>
            /// Set minimum reporting interval (server -> client)
            /// </summary>
            SetMinimumInterval = 0x02,
            /// <summary>
            /// Usage stats report (client -> server)
            /// </summary>
            UsageReport = 0x03,
            /// <summary>
            /// Acknowledgement of receipt of a usage stats report (server -> client)
            /// </summary>
            UsageReportAck = 0x04
        }

        private readonly Session parent;
        private ulong statsIntervalMins = 1200 * 60;   // 1200 Hours is default
        private ulong statsCounterMins = 0;
        private readonly Timer statsTimer;

        internal UsageStatsManager(Session parent) {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_USAGESTATS_FAMILY);

            //statsTimer = new Timer(60000);
            //statsTimer.Elapsed += new ElapsedEventHandler(statsTimerCallback);
            //statsTimer.Start();
        }

        public void ProcessIncomingPacket(Utility.DataPacket dp) {
            switch ((UsageStatsService)dp.SNAC.FamilySubtypeID) {
                case UsageStatsService.ClientServerError:
                    SNACFunctions.ProcessErrorNotification(dp);
                    break;
                case UsageStatsService.SetMinimumInterval:
                    ProcessReportInterval(dp);
                    break;
                case UsageStatsService.UsageReportAck:
                    ProcessReportAck(dp);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Set minimum report interval -- SNAC(0b,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0b,02)</param>
        private void ProcessReportInterval(DataPacket dp) {
            // read report interval in hours
            ushort n = dp.Data.ReadUshort();
            if (n > 0) statsIntervalMins = (ulong)(n * 60);
        }

        /// <summary>
        /// Process the report request response -- SNAC(0b,04)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(0b,04)</param>
        private void ProcessReportAck(DataPacket dp) {
            // no data available
        }

        //KSD-SYSTEMS - no full structure definitions found - enable timer at constructor
        private void RequestReport() {
            //ByteStream stream = new ByteStream();

            //stream.WriteUshort(0x0009); // ???

            ///* * * * * Header * * * * */
            //SNACHeader sh = new SNACHeader();
            //sh.FamilyServiceID = (ushort)SNAC_USAGESTATS_FAMILY;
            //sh.FamilySubtypeID = (ushort)UsageStatsService.UsageReport;

            ////parent.StoreRequestID(sh.RequestID, screenname);
            //SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        private void statsTimerCallback(object sender, ElapsedEventArgs e) {
            if (this.parent.LoggedIn)
                statsCounterMins++;
            else
                statsCounterMins = 0;

            if (statsCounterMins >= statsIntervalMins) {
                statsCounterMins = 0;
                RequestReport();
            }
        }
    }
}
