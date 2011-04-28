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
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides a handler for alerting a <see cref="DirectIMConnection"/> when a
    /// <see cref="DirectIMDataReader"/> has completed its work
    /// </summary>
    public delegate void DataReaderCompleteHandler();

    /// <summary>
    /// Provides a handler for alerting a <see cref="DirectIMConnection"/> when a
    /// <see cref="DirectIMDataWriter"/> has completed its work
    /// </summary>
    public delegate void DataWriterCompleteHandler();

    /// <summary>
    /// Describes the flags that can accompany a Direct IM
    /// </summary>
    internal enum DirectIMFlags
    {
        /// <summary>
        /// The DirectIM packet is an ordinary message
        /// </summary>
        None = 0x00,
        /// <summary>
        /// The message being sent is an automatic response (away message)
        /// </summary>
        AutoResponse = 0x01,
        /// <summary>
        /// The message being sent indicates a change in typing status
        /// </summary>
        /// <remarks>Sent alone, this flag indicates the typing has ceased. Sent with either <see cref="UserTyping"/> or <see cref="UserTyped"/>,
        /// the semantics of the packet are as described in those flags' respective comments.</remarks>
        TypingPacket = 0x02,
        /// <summary>
        /// The remote user is typing
        /// </summary>
        UserTyping = 0x08,
        /// <summary>
        /// The remote user has paused in their typing
        /// </summary>
        UserTyped = 0x04,
        /// <summary>
        /// This packet is to confirm the successful negotiation of a direct connection
        /// </summary>
        ConfirmationPacket = 0x20
    }

    /// <summary>
    /// Describes a stateful Rendezvous connection used to transmit text messages and images
    /// </summary>
    public class DirectIMConnection : DirectConnection
    {
        private Queue<DirectIM> _messagequeue = new Queue<DirectIM>();
        private Timer _messagetimer = null;

        /// <summary>
        /// Initializes a new DirectIMConnection
        /// </summary>
		public DirectIMConnection(ISession parent, int id, DirectConnectionMethod method, DirectConnectRole role)
            : base(parent, id, method, role)
        {
            _messagetimer = new Timer(new TimerCallback(MessageSendCallback), null, Timeout.Infinite, Timeout.Infinite);
            base.DirectConnectionReady += new DirectConnectionReady(DirectIMConnection_DirectConnectionReady);
            base.DirectConnectionFailed += new DirectConnectionFailed(DirectIMConnection_DirectConnectionFailed);
        }

        #region Base event handlers

        private void DirectIMConnection_DirectConnectionReady()
        {
            SendConfirmationPacket();
            parentSession.OnDirectConnectionComplete(Other, Cookie);
        }

        private void DirectIMConnection_DirectConnectionFailed(string reason)
        {
            _messagetimer.Change(Timeout.Infinite, Timeout.Infinite);
            parentSession.OnDirectIMSessionCancelled(this, reason);
        }

        #endregion

        /// <summary>
        /// Reads a ODC header from the base Rendezvous connection
        /// </summary>
        public override void ReadHeader()
        {
            try
            {
                byte[] odcheader = new byte[6];
                socket.BeginReceive(odcheader, 0, odcheader.Length, SocketFlags.None, new AsyncCallback(EndReadHeader),
                                   odcheader);
            }
            catch (Exception ex)
            {
                Logging.WriteString(String.Format("Exception in DirectIMConnection.ReadHeader: {0}", ex));
                DisconnectFromServer(true);
            }
        }

        /// <summary>
        /// Queues a <see cref="DirectIM"/> for sending
        /// </summary>
        public void SendMessage(DirectIM message)
        {
            lock (_messagequeue)
            {
                _messagequeue.Enqueue(message);
            }
        }

        /// <summary>
        /// Send a confirmation packet to the remote client, verifying that the connection is good
        /// </summary>
        private void SendConfirmationPacket()
        {
            int index = 0;
            byte[] packet = new byte[76];

            Marshal.InsertString(packet, "ODC2", Encoding.ASCII, ref index);
            Marshal.InsertUshort(packet, 0x004C, ref index);
            Marshal.InsertUint(packet, 0x00010006, ref index);
            index = 12;
            Marshal.CopyArray(Cookie.ToByteArray(), packet, 0, ref index);
            index = 36;
            Marshal.InsertUint(packet, 0x00000060, ref index);
            index = 44;
            Marshal.InsertString(packet, Other.ScreenName, Encoding.ASCII, ref index);

            Logging.DumpFLAP(packet, "SendConfirmationPacket");
            SendPacket(packet, new AsyncCallback(delegate
                                                     {
                                                         Logging.WriteString("SendConfirmationPacket complete");
                                                         _messagetimer.Change(500, 500);
                                                         ReadHeader();
                                                     }));
        }

        /// <summary>
        /// Dequeues a single message and sends it immediately
        /// </summary>
        private void MessageSendCallback(object state)
        {
            lock (_messagequeue)
            {
                if (_messagequeue.Count > 0)
                {
                    DirectIM message = _messagequeue.Dequeue();
                    // Turn off the timer while a message is being sent, it...could take a while
                    _messagetimer.Change(Timeout.Infinite, Timeout.Infinite);
                    SendPacket(message.ToByteArray(), new AsyncCallback(
                        delegate
                        {
                            // If the message has attachments, start a writer to spool out the data, reset the timer when it's done
                            if (message.Attachments.Count > 0)
                            {
                                DirectIMDataWriter writer =
                                    new DirectIMDataWriter(this,
                                                           message);
                                writer.DataWriterComplete +=
                                    new DataWriterCompleteHandler(
                                        delegate
                                        {
                                            _messagetimer.Change
                                                (500, 500);
                                        });
                                writer.Write();
                            }
                            else
                            {
                                // Reset the timer right away
                                _messagetimer.Change(500, 500);
                            }
                        }));
                }
            }
        }

        /// <summary>
        /// Processes the received message
        /// </summary>
        private void EndReadHeader(IAsyncResult res)
        {
            byte[] odcheader = null;
            try
            {
                odcheader = (byte[])res.AsyncState;
                socket.EndReceive(res);
            }
            catch (Exception ex)
            {
                Logging.WriteString(String.Format("Exception in DirectIMConnection.EndReadHeader: {0}", ex));
                DisconnectFromServer(true);
            }

            // Verify that this is an ODC header
            if (Encoding.ASCII.GetString(odcheader, 0, 4) != "ODC2")
            {
                // Huh...
                return;
            }

            ushort datalen = (ushort)((odcheader[4] << 8) | odcheader[5]);
            if (datalen < 6)
            {
                // Oh
                return;
            }

            ByteStream messageheader = ReadPacket(datalen - 6);
            if (messageheader == null)
            {
                // Tum ta tiddily tumpa turr
                return;
            }

            // Extract various members from the message header
            messageheader.AdvanceToPosition(6);
            byte[] cookie = messageheader.ReadByteArray(8);
            messageheader.AdvanceToPosition(22);
            uint datalength = messageheader.ReadUint();
            ushort charset = messageheader.ReadUshort();
            ushort subcharset = messageheader.ReadUshort();
            DirectIMFlags flags = (DirectIMFlags)messageheader.ReadUint();
            messageheader.AdvanceToPosition(38);
            string screenname = messageheader.ReadString(16, Encoding.ASCII);

            if ((flags & DirectIMFlags.TypingPacket) != 0)
            {
                // Determine the type of typing packet this is
                if ((flags & DirectIMFlags.UserTyping) != 0)
                {
                    //_parent.OnTypingNotification(screenname, TypingNotification.TypingStarted);
                }
                else if ((flags & DirectIMFlags.UserTyped) != 0)
                {
                    //_parent.OnTypingNotification(screenname, TypingNotification.TextTyped);
                }
                else
                {
                    // TODO:  restore these
                    // _parent.OnTypingNotification(screenname, TypingNotification.TypingFinished);
                }

                // Probably no data, but read it in anyway to make sure we're not missing anything
                ReadPacket((int)datalength);
            }
            else if ((flags & DirectIMFlags.ConfirmationPacket) != 0 && datalength == 0)
            {
                // Do we really do anything here?  I don't think so.
            }
            else
            {
                // Create a new instant message
                DirectIM dim = new DirectIM(Other.ScreenName, this);
                dim.Cookie = csammisrun.OscarLib.Cookie.GetReceivedCookie(cookie);
                dim.IsAutoResponse = ((flags & DirectIMFlags.AutoResponse) != 0);
                dim.Encoding = IM.GetEncodingFromCharset(charset, subcharset);

                // Create a spooler to incrementally read in a DirectIM packet,
                // then restart the read sequence when it's done
                DirectIMDataReader reader = new DirectIMDataReader(this, dim);
                reader.DataReaderComplete += new DataReaderCompleteHandler(delegate
                                                                               {
                                                                                   parentSession.OnDirectIMReceived(
                                                                                       reader.Message);
                                                                                   reader.Dispose();
                                                                                   ReadHeader();
                                                                               });
                reader.Read(datalength);
                return;
            }

            // Restart the read sequence
            ReadHeader();
        }

        #region Properties

        /// <summary>
        /// Returns the DirectIM capability CLSID
        /// </summary>
        public override Capabilities Capability
        {
            get { return Capabilities.DirectIM; }
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates the logic to read a message from a DirectIMConnection stream
    /// </summary>
    internal class DirectIMDataReader : IDisposable
    {
        private const int BUFFERSIZE = 1024;
        private const string DATATAG = @"<data id=[""]?(?<id>\w+)[""]? size=[""]?(?<size>\w+)[""]?>";
        private DirectIMConnection _conn = null;
        private Regex _datamatch = new Regex(DATATAG, RegexOptions.IgnoreCase);
        private byte[] _fullbuffer = null;
        private DirectIM _msg = null;
        private int _offset = 0;

        /// <summary>
        /// Initializes a new DirectIMDataReader
        /// </summary>
        public DirectIMDataReader(DirectIMConnection connection, DirectIM message)
        {
            _conn = connection;
            _conn.ReadPacketAsyncComplete += new ConnectionReadPacketHandler(_conn_ReadPacketAsyncComplete);
            _msg = message;
        }

        #region Methods

        /// <summary>
        /// Begins the data reading process
        /// </summary>
        public void Read(uint datasize)
        {
            _fullbuffer = new byte[datasize];

            // Read in little chunks until we find ourselves done
            _conn.ReadPacketAsync((int)Math.Min(BUFFERSIZE, datasize));
        }

        /// <summary>
        /// Append a chunk of data onto the full message buffer
        /// </summary>
        private void _conn_ReadPacketAsyncComplete(Connection conn, byte[] data)
        {
#if WindowsCE
	  Array.Copy(data, 0, _fullbuffer, _offset, data.Length);
#else
            Array.ConstrainedCopy(data, 0, _fullbuffer, _offset, data.Length);
#endif
            _offset += data.Length;
            data = null;

            // Alert the parent session of the message progress
            conn.ParentSession.OnDirectIMMessageProgress(false, conn.Cookie, (uint)_offset, (uint)_fullbuffer.Length);

            if (_offset < _fullbuffer.Length)
            {
                _conn.ReadPacketAsync(Math.Min(BUFFERSIZE, _fullbuffer.Length - _offset));
                return;
            }

            ProcessBuffer();
        }

        /// <summary>
        /// Process the contents of the raw message into the DirectIM object
        /// </summary>
        private void ProcessBuffer()
        {
            int index = 0;
            int length = _fullbuffer.Length;

            if ((index = IndexOfInBuffer("<BINARY>", _fullbuffer)) == -1)
            {
                index = length;
            }

            // Append the data up to the <binary> tag, if one was found, onto the IM text
            byte[] pretag = new byte[index];
#if WindowsCE
	  Array.Copy(_fullbuffer, 0, pretag, 0, pretag.Length);
#else
            Array.ConstrainedCopy(_fullbuffer, 0, pretag, 0, pretag.Length);
#endif
            _msg.Message += _msg.Encoding.GetString(pretag, 0, pretag.Length);
            pretag = null;

            // Convert the rest of the string to ASCII for regex matching
            string data = Encoding.ASCII.GetString(_fullbuffer, index, _fullbuffer.Length - index);
            //Logging.DumpFLAP(_fullbuffer, "Full buffer");

            foreach (Match m in _datamatch.Matches(data))
            {
                int id = Int32.Parse(m.Groups["id"].Value);
                int size = Int32.Parse(m.Groups["size"].Value);

                byte[] payload = new byte[size];
                // The payload starts at the index of the <BINARY> segment, at the offset of end of this <DATA> tag
                Array.Copy(_fullbuffer, index + m.Index + m.Length, payload, 0, size);
                //Logging.DumpFLAP(payload, "size = " + size);
                _msg.Attachments.Add(new Attachment(id, payload));
            }

            _fullbuffer = null;

            if (DataReaderComplete != null)
            {
                DataReaderComplete();
            }
        }

        /// <summary>
        /// Finds an ASCII string occurance in a byte buffer
        /// </summary>
        private int IndexOfInBuffer(string needle, byte[] haystack)
        {
            int i = 0, j = 0;
            for (i = 0; i < haystack.Length - needle.Length; i++)
            {
                for (j = 0; j < needle.Length; j++)
                {
                    if (needle[j] != (char)haystack[i + j])
                    {
                        break;
                    }
                }

                if (j == needle.Length)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="DirectIM"/> being populated by the data reader
        /// </summary>
        public DirectIM Message
        {
            get { return _msg; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _fullbuffer = null;
            _conn.ReadPacketAsyncComplete -= new ConnectionReadPacketHandler(_conn_ReadPacketAsyncComplete);
        }

        #endregion

        /// <summary>
        /// Signals that the DataReader has finished processing the message
        /// </summary>
        public event DataReaderCompleteHandler DataReaderComplete;
    }

    /// <summary>
    /// Encapsulates the logic to spool out a message from a DirectIMConnection stream
    /// </summary>
    internal class DirectIMDataWriter : IDisposable
    {
        private const int BUFFERSIZE = 256;
        private DirectIMConnection _conn = null;
        private byte[] _fullbuffer = null;
        private int _offset = 0;

        /// <summary>
        /// Initializes a new DirectIMDataReader
        /// </summary>
        public DirectIMDataWriter(DirectIMConnection connection, DirectIM message)
        {
            _conn = connection;
            _fullbuffer = message.AttachmentsToByteArray();
        }

        #region Methods

        /// <summary>
        /// Begins the data writing process
        /// </summary>
        public void Write()
        {
            _offset = 0;
            _conn.SendPacket(_fullbuffer, _offset, BUFFERSIZE, new AsyncCallback(EndWrite));
        }

        private void EndWrite(IAsyncResult res)
        {
            int sentsize = _conn.DataSocket.EndSend(res);
            _offset += sentsize;

            // Alert the parent session of progress
            _conn.ParentSession.OnDirectIMMessageProgress(true, _conn.Cookie, (uint)_offset, (uint)_fullbuffer.Length);

            if (_offset < _fullbuffer.Length)
            {
                _conn.SendPacket(_fullbuffer, _offset, BUFFERSIZE, new AsyncCallback(EndWrite));
            }
            else
            {
                if (DataWriterComplete != null)
                {
                    DataWriterComplete();
                }
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _fullbuffer = null;
        }

        #endregion

        /// <summary>
        /// Signals that the DataReader has finished processing the message
        /// </summary>
        public event DataWriterCompleteHandler DataWriterComplete;
    }
}