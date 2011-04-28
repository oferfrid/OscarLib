using System.Text;
using csammisrun.OscarLib.Utility;
using NUnit.Framework;

namespace OscarLib.UnitTests
{
    /// <summary>
    /// Runs tests on the <see cref="ByteStream"/> class
    /// </summary>
    [TestFixture]
    public class ByteStreamTests
    {
        private byte[] testData = new byte[] {0x08, 0x41, 0x64, 0x72, 0x69, 0x65, 0x6e, 0x6e, 0x65};

        /// <summary>
        /// Tests the numeric write functions of a byte stream
        /// </summary>
        [Test]
        public void TestByteStreamNumberWriting()
        {
            ByteStream stream = new ByteStream();
            stream.WriteByte(0x08);
            stream.WriteUshort(0x4164);
            stream.WriteUint(0x7269656e);
            stream.WriteByteArray(new byte[] {0x6e, 0x65});

            Assert.AreEqual(testData.Length, stream.GetByteCount());
            ByteStream testStream = new ByteStream(stream.GetBytes());
            Assert.AreEqual("Adrienne", testStream.ReadString(testStream.ReadByte(), Encoding.ASCII));
        }

        /// <summary>
        /// Tests the read functions of a byte stream
        /// </summary>
        [Test]
        public void TestByteStreamReading()
        {
            // Test bytes
            using (ByteStream stream = new ByteStream(testData))
            {
                for (int i = 0; i < testData.Length; i++)
                {
                    Assert.AreEqual(testData[i], stream.ReadByte(), "ReadByte returned incorrect value");
                }
            }
            // Test ushorts
            using (ByteStream stream = new ByteStream(testData))
            {
                Assert.AreEqual(0x0841, stream.ReadUshort());
                Assert.AreEqual(0x6472, stream.ReadUshort());
                Assert.AreEqual(0x6965, stream.ReadUshort());
                Assert.AreEqual(0x6e6e, stream.ReadUshort());
            }
            // Test uints
            using (ByteStream stream = new ByteStream(testData))
            {
                Assert.AreEqual(0x08416472, stream.ReadUint());
                Assert.AreEqual(0x69656e6e, stream.ReadUint());
            }
            // Test string
            using (ByteStream stream = new ByteStream(testData))
            {
                Assert.AreEqual("Adrienne", stream.ReadString(stream.ReadByte(), Encoding.ASCII));
            }
        }

        /// <summary>
        /// Tests the string writing function of a byte stream
        /// </summary>
        [Test]
        public void TestByteStreamStringWriting()
        {
            ByteStream stream = new ByteStream();
            stream.WriteByte((byte) Encoding.ASCII.GetByteCount("Adrienne"));
            stream.WriteString("Adrienne", Encoding.ASCII);

            Assert.AreEqual(testData.Length, stream.GetByteCount());
            byte[] data = stream.GetBytes();
            for (int i = 0; i < testData.Length; i++)
            {
                Assert.AreEqual(testData[i], data[i]);
            }
        }
    }
}