using System;
using csammisrun.OscarLib;
using csammisrun.OscarLib.Utility;
using NUnit.Framework;

namespace OscarLib.UnitTests
{
    [TestFixture]
    public class ChatNavTests
    {
        /// <summary>
        /// Tests the chat room constructor that accepts an incoming data packet
        /// </summary>
        [Test]
        public void TestChatRoomCreation()
        {
            ByteStream dataStream = new ByteStream(Data.SNAC_0D_09);
            using (TlvBlock tlvs = new TlvBlock(dataStream.ReadByteArrayToEnd()))
            {
                Assert.IsTrue(tlvs.HasTlv(0x0004), "TLV 0x0004 missing from data stream");
                ChatRoom chatRoom = new ChatRoom(new ByteStream(tlvs.ReadByteArray(0x0004)));

                Assert.AreEqual(0x0004, chatRoom.Exchange);
                Assert.AreEqual("!aol://2719:10-4-chat9614646934270543373", chatRoom.FullName);
                Assert.AreEqual(0x0000, chatRoom.Instance);
                Assert.AreEqual("Chat 9614646934270543373", chatRoom.DisplayName);
                Assert.AreEqual(new DateTime(2007, 8, 5, 20, 9, 21, DateTimeKind.Utc), chatRoom.CreationTime);
            }
        }
    }
}