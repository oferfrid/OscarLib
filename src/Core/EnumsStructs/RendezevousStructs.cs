/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Describes one of the types of Rendezvous connections
    /// </summary>
    public enum RendezvousType : ushort
    {
        /// <summary>
        /// The message is an invitation
        /// </summary>
        Invite = 0x0000,
        /// <summary>
        /// The message is a cancellation
        /// </summary>
        Cancel = 0x0001,
        /// <summary>
        /// The message is accepting an invitation
        /// </summary>
        Accept = 0x0002,
        /// <summary>
        /// Unknown message type
        /// </summary>
        Unknown = 0xFFFF
    }

    /// <summary>
    /// Describes Rendezvous sequence data
    /// </summary>
    public enum RendezvousSequence : ushort
    {
        /// <summary>
        /// This Rendezvous message is setting up for a direct connection or stage 1 proxy connection
        /// </summary>
        DirectOrStage1 = 0x0001,
        /// <summary>
        /// This Rendezvous message is requesting a stage 2 proxy redirection
        /// </summary>
        Stage2 = 0x0002,
        /// <summary>
        /// This Rendezvous message is requesting a stage 3 proxy redirection
        /// </summary>
        Stage3 = 0x0003
    }

    internal class RendezvousData
    {
        #region Static methods

        /// <summary>
        /// Returns the <see cref="RendezvousType"/> representation of a ushort
        /// </summary>
        public static RendezvousType TypeFromUshort(ushort value)
        {
            if (Enum.IsDefined(typeof (RendezvousType), value))
            {
                return (RendezvousType) value;
            }
            return RendezvousType.Unknown;
        }

        /// <summary>
        /// Returns the <see cref="RendezvousSequence"/> representation of a ushort
        /// </summary>
        public static RendezvousSequence SequenceFromUshort(ushort value)
        {
            if (Enum.IsDefined(typeof (RendezvousSequence), value))
            {
                return (RendezvousSequence) value;
            }
            return RendezvousSequence.DirectOrStage1;
        }

        /// <summary>
        /// Returns the ushort representation of a <see cref="RendezvousType"/>
        /// </summary>
        public static ushort UshortFromType(RendezvousType value)
        {
            return (ushort) value;
        }

        /// <summary>
        /// Returns the ushort representation of a <see cref="RendezvousSequence"/>
        /// </summary>
        public static ushort UshortFromSequence(RendezvousSequence value)
        {
            return (ushort) value;
        }

        #endregion

        private Capabilities _capability = Capabilities.None;
        private byte[] _cookie = new byte[8];
        private Session _parent = null;
        private RendezvousSequence _sequence = RendezvousSequence.DirectOrStage1;
        private bool _useproxy = false;
        private UserInfo _userinfo = null;
        public string ClientIP = "";


        public ushort ErrorCode = 0xFFFF;
        public ushort Port = 0;

        public string ProxyIP = "";
        public string VerifiedIP = "";

        /// <summary>
        /// Initializes a new RendezvousData object with a random cookie
        /// </summary>
        public RendezvousData()
        {
            Random r = new Random();

            for (int i = 0; i < 7; i++)
            {
                Cookie[i] = (byte) (r.Next(0, 9) + '0');
            }
            Cookie[7] = 0x00;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="Session"/> that owns this connection
        /// </summary>
        public Session ParentSession
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        /// Gets or sets the eight-byte cookie that identifies this connection
        /// </summary>
        public byte[] Cookie
        {
            get { return _cookie; }
            set { _cookie = value; }
        }


        /// <summary>
        /// Gets the <see cref="RendezvousSequence"/> of this connection
        /// </summary>
        public RendezvousSequence Sequence
        {
            get { return _sequence; }
            set { _sequence = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the AOL proxy should be used to route data
        /// </summary>
        public bool UseProxy
        {
            get { return _useproxy; }
            set { _useproxy = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="UserInfo"/> of the remote client
        /// </summary>
        public UserInfo UserInfo
        {
            get { return _userinfo; }
            set { _userinfo = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Capabilities"/> that this connection is handling
        /// </summary>
        public Capabilities Capability
        {
            get { return _capability; }
            set { _capability = value; }
        }

        #endregion
    }

    public class FileHeader
    {
        public uint Checksum = 0xffff0000;
        public ushort Compression = 0;
        public byte[] Cookie = new byte[8];
        public uint cretime = 0;
        public byte[] dummy = new byte[69];
        public ushort Encryption = 0;
        public byte flags = 0;
        public string IdString = "Cool FileXfer";
        public byte lnameoffset = 0;
        public byte lsizeoffset = 0;
        public byte[] macfileinfo = new byte[16];
        public uint modtime = 0;
        public string Name = "";
        public ushort nencode = 0;
        public ushort nlanguage = 0;
        public uint nrecvd = 0;
        public ushort PartsLeft = 0;
        public uint ReceivedChecksum = 0xffff0000;
        public uint ResourceForkChecksum = 0xffff0000;
        public uint ResourceForkReceivedChecksum = 0xffff0000;
        public uint ResourceForkSize = 0;
        public uint Size = 0;
    }

    //class DirectConnectInfo
    //{
    //  private DirectConnectType _connectiontype = DirectConnectType.FileTransfer;
    //  private string _localfilename = "";
    //  private Socket _listener = null;
    //  private Socket _transfer = null;

    //  /// <summary>
    //  /// Gets the type of this direct connection
    //  /// </summary>
    //  public DirectConnectType ConnectionType
    //  {
    //    get { return _connectiontype; }
    //  }

    //  /// <summary>
    //  /// The path to the file on the local computer
    //  /// </summary>
    //  /// <remarks>For a sender, this is the file to read from. For a receiver, this
    //  /// is the file to save to.</remarks>
    //  public string LocalFileName
    //  {
    //    get { return _localfilename; }
    //    set { _localfilename = value; }
    //  }

    //  public Encoding LocalFileNameEncoding = Marshal.ASCII;
    //  public string Message = "";

    //  /// <summary>
    //  /// Gets or sets the <see cref="Socket"/> that is listening for incoming connections
    //  /// </summary>
    //  /// <remarks>During a direct (non-proxied) Direct Connection, this socket is used to listen
    //  /// for connections when the client is the initiator.</remarks>
    //  public Socket Listener
    //  {
    //    get { return _listener; }
    //    set { _listener = value; }
    //  }

    //  /// <summary>
    //  /// Gets or sets the <see cref="Socket"/> that is used to transfer data on the direct connection
    //  /// </summary>
    //  public Socket Transfer
    //  {
    //    get { return _transfer; }
    //    set { _transfer = value; }
    //  }

    //  /// <summary>
    //  /// Asynchronously connects the transfer socket to the specified address and port
    //  /// </summary>
    //  /// <remarks>The IAsyncResult.AsyncState of the callback method is set to this
    //  /// DirectConnectInfo's <see cref="Parent"/> RendezvousData</remarks>
    //  public void ConnectTransferSocket(string address, int port, AsyncCallback callback)
    //  {
    //    IPHostEntry hosts = null;
    //    try
    //    {
    //      hosts = Dns.GetHostEntry(address);
    //    }
    //    catch (Exception ex)
    //    {
    //      throw ex;
    //    }

    //    IPAddress addr = hosts.AddressList[0];
    //    IPEndPoint ipep = new IPEndPoint(addr, port);
    //    this.Transfer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //    try
    //    {
    //      this.Transfer.BeginConnect(ipep, callback, _parent);
    //    }
    //    catch (Exception ex)
    //    {
    //      throw ex;
    //    }
    //  }

    //  public bool Successful = false;

    //  public ushort Subtype = 0xFFFF;
    //  public uint TotalSize = 0;
    //  public ushort TotalFiles = 0;
    //  public ushort TotalParts = 0;
    //  public ushort FilesLeft = 0;

    //  public byte[] DataChunk = null;
    //  public System.IO.Stream DataStream = null;
    //  public uint StreamPosition = 0;

    //  public FileHeader Header = new FileHeader();

    //  private bool _cancelling = false;
    //  private RendezvousData _parent = null;

    //  public DirectConnectInfo(RendezvousData parent, DirectConnectType type)
    //  {
    //    _connectiontype = type;
    //    _parent = parent;
    //  }

    //  /// <summary>
    //  /// Performs the operations to cancel a file transfer
    //  /// </summary>
    //  /// <param name="message">The cancellation message</param>
    //  public void CancelTransfer(string message)
    //  {
    //    CloseTransfer(message, true);
    //  }

    //  /// <summary>
    //  /// Performs the operations to complete a file transfer
    //  /// </summary>
    //  public void CompleteTransfer()
    //  {
    //    CloseTransfer("", false);
    //  }

    //  private void CloseTransfer(string message, bool error)
    //  {
    //    if (_cancelling)
    //      return;

    //    _cancelling = true;

    //    try
    //    {
    //      if (Listener != null && Listener.Connected)
    //      {
    //        Listener.Shutdown(System.Net.Sockets.SocketShutdown.Both);
    //        Listener.Close();
    //      }
    //    }
    //    catch (Exception) { }

    //    try
    //    {
    //      if (Transfer != null && Transfer.Connected)
    //      {
    //        Transfer.Shutdown(System.Net.Sockets.SocketShutdown.Both);
    //        Transfer.Close();
    //      }
    //    }
    //    catch (Exception) { }

    //    try
    //    {
    //      if (DataStream != null)
    //      {
    //        DataStream.Close();
    //      }
    //    }
    //    catch (Exception) { }

    //    DataChunk = null;

    //    if (_parent != null)
    //    {
    //      Session sess = _parent.ParentSession;
    //      if (error)
    //      {
    //        sess.OnFileTransferCancelled(RendezvousManager.GetKeyFromCookie(_parent.Cookie), message);
    //        //SNAC04.SendDirectConnectionCancellation(sess, _parent, message);
    //      }
    //      else
    //        sess.OnFileTransferCompleted(RendezvousManager.GetKeyFromCookie(_parent.Cookie));

    //      sess.Rendezvous.RemoveCachedData(_parent);
    //    }
    //  }


    //}

    internal class ChatInvitation
    {
        public Encoding Encoding = Encoding.ASCII;
        public string Language = "";
        public string Message = "";
    }
}