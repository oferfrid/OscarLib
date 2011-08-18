/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    public class FileTransferConnection : DirectConnection
    {
        private bool _cancelling = false;

        private byte[] _datachunk = null;
        private Stream _datastream = null;
        private int _filesremaining = 0;
        private FileHeader _header = new FileHeader();
        private string _localfilename = "";
        private Encoding _localfilenameencoding = Encoding.ASCII;
        private uint _streamposition = 0;
        private int _totalfiles = 1;
        private int _totalparts = 0;
        private uint _totalsize = 0;
        private ushort _subtype = 0;

        /// <summary>
        /// Initializes a new FileTransferConnection
        /// </summary>
        public FileTransferConnection(Session parent, int id, DirectConnectionMethod dcmethod, DirectConnectRole role)
            : base(parent, id, dcmethod, role)
        {
            Logging.WriteString("Creating new FTC (id " + id.ToString() + ")");
            base.DirectConnectionFailed += new DirectConnectionFailed(FileTransferConnection_DirectConnectionFailed);
            base.DirectConnectionReady += new DirectConnectionReady(FileTransferConnection_DirectConnectionReady);
        }

        #region Sending file methods

        /// <summary>
        /// Begins to send a file through the RendezvousData's Transfer socket
        /// </summary>
        /// <remarks>This method cancels the direct connection itself rather than throw an exception</remarks>
        private void BeginSendFile()
        {
            // Open the file for reading
            try
            {
                _datachunk = new byte[8192];
                _datastream = (new StreamReader(LocalFileName, false)).BaseStream;
                _streamposition = 0;
            }
            catch (Exception)
            {
                CancelFileTransfer("Can't open file for reading");
                return;
            }

            // Send a PROMPT message and receive an ACK message
            try
            {
                SendFileTransmitterHandshake();
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
                return;
            }

            // Signal the parent session that we've started transfering a file
            parent.OnFileTransferProgress(Cookie, 0, FileHeader.Size);

            // Send the first chunk
            SendFileTransmitterSendChunk();
        }

        /// <summary>
        /// Send an OFT PROMPT message and receive an ACK message
        /// </summary>
        private void SendFileTransmitterHandshake()
        {
            byte[] buffer = Marshal.CreateFileTransferMessage(0x0101, this);
            int index;

            // Send a PROMPT with a blank cookie
            for (index = 8; index < 16; index++)
                buffer[index] = 0x00;

            index = 0;
            try
            {
                while (index < buffer.Length)
                {
                    index += socket.Write(buffer, index, buffer.Length - index);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            Logging.DumpFLAP(buffer, "Sent PROMPT");

            // Receive the return ACK
            index = 0;
            try
            {
                while (index < buffer.Length)
                {
                    index += socket.Read(buffer, index, buffer.Length - index);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            Logging.DumpFLAP(buffer, "Received ACK");

            // Compare cookies to verify everything's okay
            byte[] compareCookie = Cookie.ToByteArray();
            for (int i = 0; i < 8; i++)
            {
                if (buffer[i + 8] != compareCookie[i])
                {
                    throw new Exception("Recepient sent a bad cookie. Possible man-in-the-middle?");
                }
            }
        }

        /// <summary>
        /// Sends a chunk of a file to the recepient
        /// </summary>
        private void SendFileTransmitterSendChunk()
        {
            int bytesreadfromfile = 0;
            try
            {
                // Read a chunk from the file
                bytesreadfromfile = _datastream.Read(_datachunk, 0, _datachunk.Length);

                // Begin to send it over the wire
                socket.BeginWrite(_datachunk, 0, bytesreadfromfile, new AsyncCallback(SendFileTransmitterSendEnd), null);
                //socket.BeginSend(_datachunk, 0, bytesreadfromfile, SocketFlags.None, new AsyncCallback(SendFileTransmitterSendEnd), null);
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
            }
        }

        /// <summary>
        /// Called when a chunk of data has been sent to the remote client
        /// </summary>
        /// <param name="res">An <see cref="IAsyncResult"/> object</param>
        private void SendFileTransmitterSendEnd(IAsyncResult res)
        {
            int bytessent = 0;
            try
            {
                bytessent = socket.EndWrite(res);
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
                return;
            }

            _streamposition += (uint) bytessent;

            // Signal parent session of the progress
            parent.OnFileTransferProgress(Cookie, _streamposition, FileHeader.Size);

            // If all the file has been sent, wait for the acknowledgement message
            // and close up shop. Otherwise, send another chunk
            if (_streamposition == FileHeader.Size)
            {
                byte[] receivedone = new byte[256];
                try
                {
                    socket.Read(receivedone);
                }
                catch (Exception ex)
                {
                    CancelFileTransfer(ex.Message);
                    return;
                }

                // Check checksums?

                CompleteFileTransfer();
            }
            else
            {
                SendFileTransmitterSendChunk();
            }
        }

        #endregion

        #region Receiving file methods

        /// <summary>
        /// Receives the PROMPT message and responds with an ACK message, then prepares the transfer socket to receive data
        /// </summary>
        private void BeginReceiveFile()
        {
            byte[] filetransferheader = null;
            int index = 0;

            // Read in 256 bytes, PROMPT type and blank cookie

            Logging.WriteString("In BeginReceiveFile()");

            filetransferheader = new byte[256];
            try
            {
                while (index < filetransferheader.Length)
                {
                    index += socket.Read(filetransferheader, index, filetransferheader.Length - index);
                }
            }
            catch (Exception ex)
            {
                string message = "Error negotiating file transfer:"
                                 + Environ.NewLine + ex.Message;
                CancelFileTransfer(message);
            }

            Logging.WriteString("Got file transfer header");

            using (ByteStream bstream = new ByteStream(filetransferheader))
            {
                bstream.AdvanceToPosition(8);
                bstream.ReadFileTransferInformation(this);
            }

            // Respond with the same header, but with the ACK type and the ICBM cookie set
            index = 6;
            Marshal.InsertUshort(filetransferheader, 0x0202, ref index);
            Marshal.CopyArray(Cookie.ToByteArray(), filetransferheader, 0, ref index);

            Logging.WriteString("Rewrote file transfer header");

            index = 0;
            try
            {
                while (index < filetransferheader.Length)
                {
                    index += socket.Write(filetransferheader, index, filetransferheader.Length - index);
                }
            }
            catch (Exception ex)
            {
                string message = "Error negotiating file transfer:"
                                 + Environ.NewLine + ex.Message;
                CancelFileTransfer(message);
            }

            // Open the file for writing
            try
            {
                _datachunk = new byte[8192];
                _datastream = (new StreamWriter(LocalFileName, false)).BaseStream;
                _streamposition = 0;
            }
            catch (Exception)
            {
                throw new Exception("Can't open target file for writing");
            }

            Logging.WriteString("File opened for writing");

            // Signal the parent session that the file transfer has started
            parent.OnFileTransferProgress(Cookie, 0, FileHeader.Size);

            // Start receiving data
            socket.BeginRead(_datachunk, 0, _datachunk.Length, new AsyncCallback(SendFileTransferReceive), null);
            //socket.BeginReceive(_datachunk, 0, _datachunk.Length, SocketFlags.None, new AsyncCallback(SendFileTransferReceive), null);
        }

        /// <summary>
        /// The callback function for socket reads during a file transfer
        /// </summary>
        /// <param name="res">An <see cref="IAsyncResult"/> object</param>
        private void SendFileTransferReceive(IAsyncResult res)
        {
            int bytesread = 0;

            Logging.WriteString("In SendFileTransferReceive");

            // End the async receive operation
            try
            {
                bytesread = socket.EndRead(res);
                if (bytesread == 0)
                    throw new Exception("Remote client cancelled transfer");
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
                return;
            }

            // Write the received data out to the file
            try
            {
                _datastream.Write(_datachunk, 0, bytesread);
                _streamposition += (uint) bytesread;
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
                return;
            }

            // Checksum the received chunk
            FileHeader.ReceivedChecksum = ChecksumChunk(_datachunk, (uint) bytesread, FileHeader.ReceivedChecksum);

            // Signal progress to the parent session
            parent.OnFileTransferProgress(Cookie, _streamposition, FileHeader.Size);

            // Check to see if the transfer has finished
            if (_streamposition >= FileHeader.Size)
            {
                _datachunk = null;
                _datastream.Close();

                // Send out the acknowledgement, compare the checksums, and finish up
                SendFileReceiverDone();

                if ( (FileHeader.ReceivedChecksum != FileHeader.Checksum && FileHeader.Checksum > 0) || (_streamposition != FileHeader.Size && FileHeader.Checksum == 0) )
                {
                    CancelFileTransfer("Received data does not match expected checksum");
                    return;
                }

                CompleteFileTransfer();
            }
            else
            {
                // Keep receiving asynchronously
                socket.BeginRead(_datachunk, 0, _datachunk.Length, new AsyncCallback(SendFileTransferReceive), null);
                //ocket.BeginReceive(_datachunk, 0, _datachunk.Length, SocketFlags.None, new AsyncCallback(SendFileTransferReceive), null);
            }
        }

        /// <summary>
        /// Sends a DONE message
        /// </summary>
        private void SendFileReceiverDone()
        {
            Marshal.CopyArray(Cookie.ToByteArray(), FileHeader.Cookie, 0);
            byte[] buffer = Marshal.CreateFileTransferMessage(0x0204, this);
            int index = 0;
            try
            {
                while (index < buffer.Length)
                {
                    index += socket.Write(buffer, index, buffer.Length - index);
                }
            }
            catch (Exception ex)
            {
                CancelFileTransfer(ex.Message);
            }
        }

        #endregion

        #region Transfer shutdown methods

        private void CompleteFileTransfer()
        {
            CloseTransfer("", false);
        }

        /// <summary>
        /// Cancel an in-progress file transfer
        /// </summary>
        public void CancelFileTransfer(string reason)
        {
            CloseTransfer(reason, true);
        }

        private void CloseTransfer(string message, bool error)
        {
            if (_cancelling)
                return;

            _cancelling = true;

            Logging.WriteString("CloseTransfer: " + message);

            try
            {
                if (_listener != null && _listener.Connected)
                {
                    _listener.Shutdown(SocketShutdown.Both);
                    _listener.Close();
                }
            }
            catch (Exception)
            {
            }

            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (Exception)
            {
            }

            try
            {
                if (_datastream != null)
                {
                    _datastream.Close();
                }
            }
            catch (Exception)
            {
            }

            _datachunk = null;

            if (error)
            {
                parent.OnFileTransferCancelled(Other, Cookie, message);
                parent.Messages.SendDirectConnectionCancellation(this, message);
            }
            else
                parent.OnFileTransferCompleted(Cookie);

            // Remove connection from ConnManager
        }

        #endregion

        #region Direct connection event handlers

        private void FileTransferConnection_DirectConnectionReady()
        {
            try
            {
                // The transfer socket's ready to go, we just have to figure out what we're doing and do it
                if (Role == DirectConnectRole.Initiator)
                {
                    BeginSendFile();
                }
                else
                {
                    BeginReceiveFile();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteString("Error scanner something happen");
                Logging.WriteString(ex.StackTrace);
                throw;
            }
        }

        private void FileTransferConnection_DirectConnectionFailed(string reason)
        {
            parent.OnFileTransferCancelled(Other, Cookie, reason);
        }

        #endregion

        #region Properties
        public ushort SubType
        {
            get { return _subtype; }
            set { _subtype = value; }
        }

        /// <summary>
        /// Gets or sets the path to the local file to send or receive
        /// </summary>
        public string LocalFileName
        {
            get { return _localfilename; }
            set
            {
                _localfilename = value;

                if(Role == DirectConnectRole.Initiator && File.Exists(_localfilename))
                {
                    // Set all the file-dependent data
                    int slashindex = _localfilename.LastIndexOf("\\");
                    _header.Name = _localfilename.Substring(slashindex + 1, _localfilename.Length - (slashindex + 1));
                    _header.Size = (uint) (new FileInfo(_localfilename)).Length;
                    _header.Checksum = Checksum();
                    _totalfiles = 1;
                    _totalparts = 1;
                    _filesremaining = 1;
                    _totalsize = _header.Size;
                }
            }
        }

        /// <summary>
        /// Gets or sets the encoding of the filename to send
        /// </summary>
        public Encoding LocalFileNameEncoding
        {
            get { return _localfilenameencoding; }
            set { _localfilenameencoding = value; }
        }

        /// <summary>
        /// Returns the SendFiles capability
        /// </summary>
        public override Capabilities Capability
        {
            get { return Capabilities.SendFiles; }
        }

        public FileHeader FileHeader
        {
            get { return _header; }
        }

        /// <summary>
        /// Gets the total number of files to send
        /// </summary>
        public int TotalFiles
        {
            get { return _totalfiles; }
            set { _totalfiles = value; }
        }

        public int FilesRemaining
        {
            get { return _filesremaining; }
            set { _filesremaining = value; }
        }

        public int TotalParts
        {
            get { return _totalparts; }
            set { _totalparts = value; }
        }

        /// <summary>
        /// Gets the total number of bytes to send
        /// </summary>
        public uint TotalFileSize
        {
            get { return _totalsize; }
            set { _totalsize = value; }
        }

        #endregion

        /// <summary>
        /// Performs an OSCAR File Transfer checksum 
        /// </summary>
        /// <returns>The checksum of the file</returns>
        private uint Checksum()
        {
            uint retval = 0xFFFF0000;
            using (FileStream infile = File.OpenRead(LocalFileName))
            {
                try
                {
                byte[] chunk = new byte[1024];
                int bytesread = 0;
                int streamposition = 0;
                do
                {
                    bytesread = infile.Read(chunk, 0, chunk.Length);
                    retval = ChecksumChunk(chunk, (uint) bytesread, retval);
                    streamposition += bytesread;
                } while (bytesread == chunk.Length);
                }
                catch (Exception ex)
                {
                    CancelFileTransfer(ex.Message);
                    retval = 0xFFFFFFFF;
                }
            }

            return retval;
        }

        public static uint ChecksumChunk(byte[] filechunk, uint chunklength, uint start)
        {
            uint retval = (start >> 16) & 0xFFFF;
            uint oldretval;
            ushort filebyte;

            for (int i = 0; i < filechunk.Length && i < chunklength; i++)
            {
                oldretval = retval;
                filebyte = filechunk[i];
                if ((i & 1) == 0)
                {
                    filebyte <<= 8;
                }
                retval -= filebyte;
                if (retval > oldretval)
                    retval--;
            }
            retval = ((retval & 0x0000ffff) + (retval >> 16));
            retval = ((retval & 0x0000ffff) + (retval >> 16));
            return retval << 16;
        }

        private void ReadFileTransferHeader(ByteStream stream)
        {
            //FileHeader fh = _header;
            //fh.Cookie = stream.ReadByteArray(8);
            //fh.Encryption = stream.ReadUshort();
            //fh.Compression = stream.ReadUshort();

            //_totalfiles = stream.ReadUshort();
            //_filesremaining = stream.ReadUshort();
            //_totalparts = stream.ReadUshort();

            //fh.PartsLeft = stream.ReadUshort();
            //_totalsize = stream.ReadUint();
            //fh.Size = stream.ReadUint();
            //fh.modtime = stream.ReadUint();
            //fh.Checksum = stream.ReadUint();
            //fh.ResourceForkReceivedChecksum = stream.ReadUint();
            //fh.ResourceForkSize = stream.ReadUint();
            //fh.cretime = stream.ReadUint();
            //fh.ResourceForkChecksum = stream.ReadUint();
            //fh.nrecvd = stream.ReadUint();
            //fh.ReceivedChecksum = stream.ReadUint();


            //fh.IdString = ByteArrayToNullTerminatedString(buffer, ref index, 32);

            //fh.flags = buffer[index++];
            //fh.lnameoffset = buffer[index++];
            //fh.lsizeoffset = buffer[index++];

            //CopyArray(buffer, fh.dummy, ref index);

            //CopyArray(buffer, fh.macfileinfo, ref index);

            //fh.nencode = ByteArrayToUshort(buffer, ref index);
            //fh.nlanguage = ByteArrayToUshort(buffer, ref index);

            //fh.Name = ByteArrayToNullTerminatedString(buffer, ref index, 64);
        }
    }
}