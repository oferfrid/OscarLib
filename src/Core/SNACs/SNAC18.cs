using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0018 -- e-mail notification
    /// </summary>
    internal static class SNAC18
    {
        /// <summary>
        /// Processes a notification of e-mail information change -- SNAC(18,07)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(18,07)</param>
        public static void ProcessEmailInformation(DataPacket dp)
        {
            // There are 24 bytes of cookies prior to the TLV list, an 8-byte cookie
            // and a 16-byte cookie. The 16 byte cookie is identifying information
            // that may be useful
            byte[] cookie = dp.Data.ReadByteArray(8);
            byte[] idcookie = dp.Data.ReadByteArray(16);

            TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd());
            string alertTitle = tlvs.ReadString(0x0005, Encoding.ASCII);
            string url = tlvs.ReadString(0x0007, Encoding.ASCII);
            string alertUrl = tlvs.ReadString(0x000D, Encoding.ASCII);
            ushort numMessages = tlvs.ReadUshort(0x0080);
            byte unread = tlvs.ReadByte(0x0081);
            if (unread == 0)
            {
                numMessages = 0;
            }
            string domain = tlvs.ReadString(0x0082, Encoding.ASCII);
            ushort flags = tlvs.ReadUshort(0x0084);

            if (numMessages == 0xFFFF)
            {
                // If this message was received, at least one email message is available
                // even if TLV 0x0080 wasn't in the message
                numMessages = 1;
            }
        }
    }
}