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
    /// <summary>
    /// Represents a TLV. TLV stands for Type, Length, Value and is used to transmit data in an organized format
    /// </summary>
    public class Tlv
    {
        private byte[] data;
        private ushort dataSize;
        private ushort typeNumber;

        /// <summary>
        /// The type of this TLV
        /// </summary>
        public ushort TypeNumber
        {
            get { return typeNumber; }
            set { typeNumber = value; }
        }

        /// <summary>
        /// The size, in bytes, of the data that follows the header
        /// </summary>
        public ushort DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        /// <summary>
        /// A byte array of data. Complex data such as strings must be packed into the byte array by the user
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }
    }

    /// <summary>
    /// Summary description for TLV.
    /// </summary>
    internal class TLVMarshal
    {
        public static Tlv MakeTLV(int typeCode, byte[] data)
        {
            Tlv retval = new Tlv();
            retval.TypeNumber = (ushort) typeCode;
            retval.Data = data;
            retval.DataSize = (ushort) ((data == null) ? 0 : data.Length);
            return retval;
        }
    }
}