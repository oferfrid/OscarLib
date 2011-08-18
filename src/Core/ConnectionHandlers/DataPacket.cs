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
    // It's a class, not a struct, for the inline initializers
    /// <summary>
    /// Encapsulates an incoming or outgoing data packet
    /// </summary>
    public class DataPacket
    {
        private ByteStream data;

        /// <summary>
        /// The FLAP header that was received with the packet
        /// </summary>
        public FLAPHeader FLAP;
        /// <summary>
        /// The connection that will send or has received the packet
        /// </summary>
        public Connection ParentConnection;
        /// <summary>
        /// The session that owns the packet
        /// </summary>
        public Session ParentSession;
        /// <summary>
        /// The SNAC header that was either received with the packet
        /// or will determine how the packet is sent
        /// </summary>
        public SNACHeader SNAC = new SNACHeader();

        /// <summary>
        /// Initializes a new DataPacket
        /// </summary>
        public DataPacket()
        {
            data = new ByteStream();
        }

        /// <summary>
        /// Initializes a new DataPacket from a byte array
        /// </summary>
        /// <param name="data">A byte array used to construct a <see cref="ByteStream"/>
        /// for the <see cref="Data"/> property</param>
        public DataPacket(byte[] data)
        {
            this.data = new ByteStream(data);
        }

        /// <summary>
        /// Initializes a new DataPacket from a ByteStream
        /// </summary>
        /// <param name="stream">A <see cref="ByteStream"/> that will be exposed
        /// as the <see cref="Data"/> property.</param>
        public DataPacket(ByteStream stream)
        {
            data = stream;
        }

        /// <summary>
        /// Gets the <see cref="ByteStream"/> containing the packet's data
        /// </summary>
        public ByteStream Data
        {
            get { return data; }
        }

        /// <summary>
        /// Sets the current <see cref="Data"/> parameter to a new <see cref="ByteStream"/>
        /// </summary>
        public void ResetByteStream(ByteStream fullStream)
        {
            if (data != null)
            {
                data.Dispose();
            }
            data = fullStream;
        }
    }
}