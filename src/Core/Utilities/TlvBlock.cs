/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// A self-contained block of TLV data
    /// </summary>
    public class TlvBlock : IDisposable
    {
        private readonly List<Tlv> tlvList = new List<Tlv>();
        private readonly bool wasReceived;
        private int tlvBlockSize;

        #region Constructors / destructor

        /// <summary>
        /// Initializes a new <see cref="TlvBlock"/>
        /// </summary>
        public TlvBlock()
        {
            wasReceived = false;
        }

        /// <summary>
        /// Initializes a new <see cref="TlvBlock"/> from received data
        /// </summary>
        /// <param name="data">A received data buffer</param>
        public TlvBlock(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="TlvBlock"/> from received data
        /// </summary>
        /// <param name="data">A received data buffer</param>
        /// <param name="startIndex">The start of the TLV block in the data buffer</param>
        public TlvBlock(byte[] data, int startIndex)
            : this(data, startIndex, data.Length - startIndex)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="TlvBlock"/> from received data
        /// </summary>
        /// <param name="data">A received data buffer</param>
        /// <param name="startIndex">The start of the TLV block in the data buffer</param>
        /// <param name="readLength">The length of the TLV block in the data buffer</param>
        public TlvBlock(byte[] data, int startIndex, int readLength)
        {
            wasReceived = true;
            int endIndex = startIndex + readLength;
            while (startIndex < endIndex)
            {
                Tlv tlv = new Tlv();
                tlv.TypeNumber = (ushort)((data[startIndex] << 8) | data[startIndex + 1]);
                startIndex += 2;
                tlv.DataSize = (ushort)((data[startIndex] << 8) | data[startIndex + 1]);
                startIndex += 2;
                tlv.Data = new byte[tlv.DataSize];
                Array.Copy(data, startIndex, tlv.Data, 0, tlv.DataSize);
                startIndex += tlv.DataSize;
                tlvList.Add(tlv);
            }
        }

        /// <summary>
        /// Destructor:  Disposes the TLV block and its resources
        /// </summary>
        ~TlvBlock()
        {
            Dispose();
        }

        #endregion

        #region Read data methods

        /// <summary>
        /// Gets a value indicating whether or not a TLV with the specified type code exists in the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to search for</param>
        public bool HasTlv(int typeCode)
        {
            bool retval = false;
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = true;
                    break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a byte from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        public byte ReadByte(int typeCode)
        {
            byte retval = 0xFF;
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = tlv.Data[0];
                    break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        public ushort ReadUshort(int typeCode)
        {
            ushort retval = 0xFFFF;
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = (ushort) ((tlv.Data[0] << 8) | tlv.Data[1]);
                    break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        public uint ReadUint(int typeCode)
        {
            uint retval = 0xFFFFFFFF;
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = (uint)((tlv.Data[0] << 24) |
                                  (tlv.Data[1] << 16) |
                                  (tlv.Data[2] << 8) |
                                  tlv.Data[3]);
                    break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads an IP address from a 32-bit integer value in the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        public string ReadIPAddress(int typeCode)
        {
            String retval = "";
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = String.Format("{0}.{1}.{2}.{3}",
                                           tlv.Data[0], tlv.Data[1], tlv.Data[2], tlv.Data[3]);
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a string from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        /// <param name="stringEncoding">The <see cref="Encoding"/> of the string contained in the TLV</param>
        public String ReadString(int typeCode, Encoding stringEncoding)
        {
            String retval = "";
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = stringEncoding.GetString(tlv.Data, 0, tlv.Data.Length);
                }
            }
            return retval;
        }

        /// <summary>
        /// Converts a 4-byte unsigned integer DateTime object
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        /// <returns>A <see cref="DateTime"/> object representing the date in UTC</returns>
        /// <remarks>This method assumes the integer being read represents the UNIX time_t format:
        /// the number of seconds since the epoch (00:00:00 1 Jan 1970 GMT).</remarks>
        public DateTime ReadDateTime(int typeCode)
        {
            DateTime dateTime = new DateTime();
            dateTime = dateTime.AddYears(1969);
            dateTime = dateTime.AddSeconds(ReadUint(typeCode));

            return dateTime;
        }

        /// <summary>
        /// Reads a byte array from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV that contains the data</param>
        public byte[] ReadByteArray(int typeCode)
        {
            byte[] retval = null;
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval = tlv.Data;
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a list of TLVs with the given type code from the TLV block
        /// </summary>
        /// <param name="typeCode">The type code of the TLVs to read</param>
        public ReadOnlyCollection<Tlv> ReadAllTlvs(int typeCode)
        {
            List<Tlv> retval = new List<Tlv>();
            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retval.Add(tlv);
                }
            }
            return new ReadOnlyCollection<Tlv>(retval);
        }

        /// <summary>
        /// Reads one tlv in a tlv block
        /// </summary>
        /// <param name="typeCode">the typecode of the tlv</param>
        /// <returns>the tlv object</returns>
        public Tlv ReadTlv(int typeCode)
        {
            int counter = 0;
            Tlv retVal = null;

            foreach (Tlv tlv in tlvList)
            {
                if (tlv.TypeNumber == typeCode)
                {
                    retVal = tlv;
                    counter++;
                }
            }
            if (counter != 1)
            {
                throw new ApplicationException(
                      "Invalid method call 'TlvBlock.ReadTlv(int)'. There are '" + counter +
                      "' tlvs with the typecode '" + typeCode +
                      ". But only one tlv with this code was expected");
            }
            return retVal;
        }
        
        /// <summary>
        /// Gets the number of parsed TLVs
        /// </summary>
        public int GetTlvCount()
        {
            return tlvList.Count;
        }

        #endregion

        #region Write data methods

        /// <summary>
        /// Writes an empty TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to write</param>
        public void WriteEmpty(int typeCode)
        {
            CheckAccess();
            tlvList.Add(TLVMarshal.MakeTLV(typeCode, null));
        }

        /// <summary>
        /// Writes a single byte TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="data">The data to write</param>
        public void WriteByte(int typeCode, byte data)
        {
            CheckAccess();
            tlvList.Add(TLVMarshal.MakeTLV(typeCode, new byte[] {data}));
            tlvBlockSize += 1;
        }

        /// <summary>
        /// Writes a 16-bit unsigned integer TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="data">The data to write</param>
        public void WriteUshort(int typeCode, ushort data)
        {
            CheckAccess();

            byte[] byteArray = new byte[2];
            byteArray[0] = (byte) ((data & 0xFF00) >> 8);
            byteArray[1] = (byte) (data & 0x00FF);

            tlvList.Add(TLVMarshal.MakeTLV(typeCode, byteArray));
            tlvBlockSize += 2;
        }

        /// <summary>
        /// Writes a DateTime TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="date">The data to write</param>
        public void WriteDateTime(int typeCode, DateTime date)
        {
            TimeSpan span = date.ToUniversalTime() - new DateTime().AddYears(1969);
            WriteUint(typeCode, (uint) span.TotalSeconds);
        }

        /// <summary>
        /// Writes a 16-bit unsigned integer TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="data">The data to write</param>
        public void WriteUint(int typeCode, uint data)
        {
            CheckAccess();
            byte[] byteArray = new byte[4];
            byteArray[0] = (byte) ((data & 0xFF000000) >> 24);
            byteArray[1] = (byte) ((data & 0x00FF0000) >> 16);
            byteArray[2] = (byte) ((data & 0x0000FF00) >> 8);
            byteArray[3] = (byte) ((data & 0x000000FF));
            tlvList.Add(TLVMarshal.MakeTLV(typeCode, byteArray));
            tlvBlockSize += 4;
        }

        /// <summary>
        /// Writes a string TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="data">The data to write</param>
        /// <param name="encoding">The encoding to use to write the data</param>
        public void WriteString(int typeCode, string data, Encoding encoding)
        {
            CheckAccess();
            tlvList.Add(TLVMarshal.MakeTLV(typeCode, encoding.GetBytes(data)));
            tlvBlockSize += encoding.GetByteCount(data);
        }

        /// <summary>
        /// Writes a byte array TLV into the block
        /// </summary>
        /// <param name="typeCode">The type code of the TLV to contain the data</param>
        /// <param name="data">The data to write</param>
        public void WriteByteArray(int typeCode, byte[] data)
        {
            CheckAccess();
            tlvList.Add(TLVMarshal.MakeTLV(typeCode, data));
            tlvBlockSize += data.Length;
        }

        /// <summary>
        /// Checks to ensure that this <see cref="TlvBlock"/> can be written
        /// </summary>
        private void CheckAccess()
        {
            if (wasReceived)
            {
                throw new InvalidOperationException("Cannot modify a TlvBlock received by the server");
            }
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Called to release memory allocated by the TlvBlock
        /// </summary>
        /// <remarks>TODO:  This...does nothing</remarks>
        public void Dispose()
        {
        }

        #endregion

        /// <summary>
        /// Gets a transmittable byte array representing the <see cref="TlvBlock"/>
        /// </summary>
        public byte[] GetBytes()
        {
            byte[] retval = new byte[tlvList.Count*4 + tlvBlockSize];
            int index = 0;
            for (int i = 0, j = tlvList.Count; i < j; i++)
            {
                retval[index++] = (byte) ((tlvList[i].TypeNumber & 0xFF00) >> 8);
                retval[index++] = (byte) ((tlvList[i].TypeNumber & 0x00FF));
                retval[index++] = (byte) ((tlvList[i].DataSize & 0xFF00) >> 8);
                retval[index++] = (byte) ((tlvList[i].DataSize & 0x00FF));
                if (tlvList[i].Data != null)
                {
                    Array.Copy(tlvList[i].Data, 0, retval, index, tlvList[i].Data.Length);
                }
                index += tlvList[i].DataSize;
            }
            return retval;
        }

        /// <summary>
        /// Gets the number of bytes in this <see cref="TlvBlock"/>
        /// </summary>
        public int GetByteCount()
        {
            return tlvList.Count*4 + tlvBlockSize;
        }
    }
}