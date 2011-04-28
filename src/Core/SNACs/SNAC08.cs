using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0008 -- popups service
    /// </summary>
    internal static class SNAC08
    {
        /// <summary>
        /// Processes a popup message sent by the server -- SNAC(08,02)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(08,02)</param>
        public static void ProcessPopupMessage(DataPacket dp)
        {
            TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd());
            string message = tlvs.ReadString(0x0001, Encoding.ASCII);
            string url = tlvs.ReadString(0x0002, Encoding.ASCII);
            ushort width = tlvs.ReadUshort(0x0003);
            ushort height = tlvs.ReadUshort(0x0004);
            ushort delay = tlvs.ReadUshort(0x0005);

            dp.ParentSession.OnPopupMessage(width, height, delay, url, message);
        }
    }
}