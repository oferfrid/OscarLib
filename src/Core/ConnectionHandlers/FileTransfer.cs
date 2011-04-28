using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
  

  /// <summary>
  /// Provides static methods for managing peer-to-peer file transfers
  /// </summary>
  class FileTransferManager
  {
    private Session _parent = null;

    public FileTransferManager(Session parent)
    {
      _parent = parent;
    }

    #region Public methods
    /// <summary>
    /// Send a file to the specified recipient via the specified method
    /// </summary>
    /// <param name="recipient">The screenname to which to send the file</param>
    /// <param name="filename">The path of the file to send</param>
    /// <param name="method">The <see cref="DirectConnectionMethod"/> to use when negotiating the file transfer</param>
    /// <returns>A unique key used to reference this file transfer</returns>
    public string SendFile(string recipient, string filename, DirectConnectionMethod method)
    {
      RendezvousData rd = CreateSendFileData(recipient, filename);
      return SetupRendezvousConnection(rd, method);
    }

    /// <summary>
    /// Start a DirectImage session with the specified recipient
    /// </summary>
    /// <param name="receipient">The screenname to which to send the image</param>
    /// <param name="method">The <see cref="DirectConnectionMethod"/> to use when negotiating the file transfer</param>
    /// <returns>A unique key used to reference this direct image session</returns>
    public string StartDirectImage(string receipient, DirectConnectionMethod method)
    {
      RendezvousData rd = CreateDirectIMData(receipient);
      return SetupRendezvousConnection(rd, method);
    }

    /// <summary>
    /// Accepts an invitation to receive a file
    /// </summary>
    /// <param name="rd">A previously cached <see cref="RendezvousData"/> object describing the connection</param>
    public void AcceptSendFileTransfer(RendezvousData rd)
    {
      if (rd.UseProxy)
      {
        StartReceiveThroughProxy(rd);
      }
      else
      {
        StartReceiveThroughDirectConnection(rd);
      }
    }
    #endregion

    /// <summary>
    /// Sets up a direct connection's sockets
    /// </summary>
    private string SetupRendezvousConnection(RendezvousData rd, DirectConnectionMethod method)
    {
      string retval = _parent.Rendezvous.CacheRendezvousData(rd);

      // Get the client IP
      string localhost = Dns.GetHostName();
      rd.ClientIP = Dns.GetHostEntry(localhost).AddressList[0].ToString();

      if (method == DirectConnectionMethod.Direct)
      {
        rd.UseProxy = false;
        StartSendThroughDirectConnection(rd);
      }
      else if (method == DirectConnectionMethod.Proxied)
      {
        rd.UseProxy = true;
        StartSendThroughProxy(rd);
      }

      // Return the key to the caller
      return retval;
    }

    #region OSCAR Rendezvous Proxy utilities
    /// <summary>
    /// Synchronously sends a rendezvous proxy INITSEND message
    /// </summary>
    void ProxyInitializeSend(RendezvousData rd)
    {
      Socket sock = rd.DirectConnection.Transfer;
      Encoding enc = Marshal.ASCII;
      byte screennamelength = (byte)enc.GetByteCount(_parent.ScreenName);

      int index = 0, bytessent = 0;
      byte[] data = new byte[12 + 1 + screennamelength + 8 + 4 + 16];
      // Proxy header
      InsertProxyHeader(data, RendezvousProxyCommand.InitializeSend, ref index);
      // Screenname size + string
      data[index++] = screennamelength;
      Marshal.InsertString(data, _parent.ScreenName, enc, ref index);
      // The rendezvous cookie
      Marshal.CopyArray(rd.Cookie, data, 0, ref index);
      // TLV 0x0001, length 0x0010
      Marshal.InsertUint(data, 0x00010010, ref index);
      // The Send File capability
      Marshal.CopyArray(CapabilityProcessor.GetCapabilityArray(rd.Capability), data, 0, ref index);

      while (bytessent < data.Length)
      {
        bytessent += sock.Send(data, bytessent, data.Length - bytessent, SocketFlags.None);
      }
      Logging.DumpFLAP(data, "Rendezvous proxy initialize send");
    }

    /// <summary>
    /// Synchronously sends a rendezvous proxy INITRECV message
    /// </summary>
    void ProxyInitializeReceive(RendezvousData rd)
    {
      Socket sock = rd.DirectConnection.Transfer;
      Encoding enc = Marshal.ASCII;
      //byte screennamelength = (byte)enc.GetByteCount(rd.UserInfo.ScreenName);
      byte screennamelength = (byte)enc.GetByteCount(_parent.ScreenName);

      int index = 0, bytessent = 0;
      byte[] data = new byte[12 + 1 + screennamelength + 2 + 8 + 20];
      // Proxy header
      InsertProxyHeader(data, RendezvousProxyCommand.InitializeReceive, ref index);
      // Screenname size + string
      data[index++] = screennamelength;
      //Marshal.InsertString(data, rd.UserInfo.ScreenName, enc, ref index);
      Marshal.InsertString(data, _parent.ScreenName, enc, ref index);
      // The fake (heh) port
      Marshal.InsertUshort(data, rd.Port, ref index);
      // The rendezvous cookie
      Marshal.CopyArray(rd.Cookie, data, 0, ref index);
      // TLV 0x0001, length 0x0010
      Marshal.InsertUint(data, 0x00010010, ref index);
      // The Send File capability
      Marshal.CopyArray(CapabilityProcessor.GetCapabilityArray(rd.Capability), data, 0, ref index);

      while (bytessent < data.Length)
      {
        bytessent += sock.Send(data, bytessent, data.Length - bytessent, SocketFlags.None);
      }
      Logging.DumpFLAP(data, "Rendezvous proxy initialize receive");
    }

    /// <summary>
    /// Synchronously reads a packet from a Rendezvous proxy connection
    /// </summary>
    RendezvousProxyPacket ReadProxyPacket(RendezvousData rd)
    {
      Socket sock = rd.DirectConnection.Transfer;
      int bytesreceived = 0;

      byte[] header = new byte[12];
      while (bytesreceived < header.Length)
      {
        bytesreceived += sock.Receive(header, bytesreceived, header.Length - bytesreceived, SocketFlags.None);
      }
      Logging.DumpFLAP(header, "Rendezvous proxy read packet header");

      int index = 4;
      RendezvousProxyPacket retval = new RendezvousProxyPacket();
      retval.Command = (RendezvousProxyCommand)Marshal.ByteArrayToUshort(header, ref index);

      index = 0;
      retval.Data = new byte[Marshal.ByteArrayToUshort(header, ref index) - 10];
      bytesreceived = 0;
      while (bytesreceived < retval.Data.Length)
      {
        bytesreceived += sock.Receive(retval.Data, bytesreceived, retval.Data.Length - bytesreceived, SocketFlags.None);
      }
      Logging.DumpFLAP(retval.Data, "Rendezvous proxy read packet data");
      return retval;
    }

    /// <summary>
    /// Inserts a 12-byte header into a Rendezvous proxy negotiation packet
    /// </summary>
    /// <param name="data">The byte buffer containing the packet</param>
    /// <param name="command">The <see cref="RendezvousProxyCommand"/> to send</param>
    /// <param name="index">The offset at which to insert the header</param>
    void InsertProxyHeader(byte[] data, RendezvousProxyCommand command, ref int index)
    {
      Marshal.InsertUshort(data, (ushort)(data.Length - 2), ref index);
      Marshal.InsertUshort(data, 0x044A, ref index);
      Marshal.InsertUshort(data, (ushort)command, ref index);
      Marshal.InsertUint(data, 0x00000000, ref index);
      Marshal.InsertUshort(data, 0x0000, ref index);
    }

    /// <summary>
    /// The negotiated proxy connection has received a READY packet, 
    /// </summary>
    /// <param name="res"></param>
    void ProxyReceivedReady(IAsyncResult res)
    {
      RendezvousData rd = null;
      try
      {
        rd = res.AsyncState as RendezvousData;
        rd.DirectConnection.Transfer.EndReceive(res);

        if (rd.Sequence == RendezvousSequence.DirectOrStage1)
        {
          // We start sending the file as if it was a direct connection now
          BeginSendFile(rd);
        }
        else
        {
          // We start receiving the file as if it was a direct connection now
          BeginReceiveFile(rd);
        }
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
      }
    }
    #endregion

    #region Sender negotiation methods
    /// <summary>
    /// Start a listening socket for direct connection transfer
    /// </summary>
    /// <param name="rd">A <see cref="RendezvousData"/> object</param>
    private void StartSendThroughDirectConnection(RendezvousData rd)
    {
      // Start a listening socket and populate the DCI structure with
      // network information
      IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 0);
      rd.DirectConnection.Listener = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);
      rd.DirectConnection.Listener.Bind(ipep);

      rd.Port = (ushort)((IPEndPoint)rd.DirectConnection.Listener.LocalEndPoint).Port;
      rd.DirectConnection.Listener.Listen(1);
      rd.DirectConnection.Listener.BeginAccept(new AsyncCallback(AcceptConnection), rd);

      // Send the "send file" request on SNAC(04,06):02
      //SNAC04.SendDirectConnectionRequest(_parent, rd);
    }

    /// <summary>
    /// Accepts a connection from a file transfer listener socket
    /// </summary>
    /// <param name="res">An <see cref="IAsyncResult"/> object</param>
    /// <remarks>The <see cref="IAsyncResult.AsyncState"/> member of <paramref name="res"/>
    /// is set to the <see cref="FileTransferInfo"/> object representing the current
    /// transfer operation.</remarks>
    private void AcceptConnection(IAsyncResult res)
    {
      RendezvousData rd = (RendezvousData)res.AsyncState;
      try
      {
        rd.DirectConnection.Transfer = rd.DirectConnection.Listener.EndAccept(res);
      }
      catch (Exception)
      {
        //rd.DirectConnection.CancelTransfer("Client closed connection");
        return;
      }
      finally
      {
        try { rd.DirectConnection.Listener.Shutdown(SocketShutdown.Both); }
        catch (Exception) { }
        try { rd.DirectConnection.Listener.Close(); }
        catch (Exception) { }
      }

      BeginSendFile(rd);
    }

    /// <summary>
    /// Start a stage 1 proxy connection
    /// </summary>
    /// <remarks>
    /// A stage 1 proxy connection is used when the local client is the initiator of the Rendezvous session.
    /// The local client makes a connection to the AOL proxy server and sends a SNAC(04,07) Rendezvous invite
    /// to the target with the IP and port information provided by the proxy server.
    /// </remarks>
    /// <seealso cref="Stage1ProxyConnectFinished"/>
    /// <seealso cref="SNAC04.SendDirectConnectionRequest"/>
    private void StartSendThroughProxy(RendezvousData rd)
    {
      rd.UseProxy = true;
      rd.Sequence = RendezvousSequence.DirectOrStage1;

      rd.DirectConnection.ConnectTransferSocket("ars.oscar.aol.com", _parent.LoginPort,
        new AsyncCallback(InitProxyConnectFinished));
    }

    /// <summary>
    /// Completes the initial connection to the proxy server
    /// </summary>
    /// <remarks>This method is used to complete the proxy server transaction for both
    /// Stage 1 sending and Stage 2 receiver-redirect proxy scenarios</remarks>
    private void InitProxyConnectFinished(IAsyncResult res)
    {
      RendezvousData rd = res.AsyncState as RendezvousData;
      DirectConnectInfo fti = rd.DirectConnection;

      try
      {
        fti.Transfer.EndConnect(res);

        ProxyInitializeSend(rd);
        RendezvousProxyPacket rpp = ReadProxyPacket(rd);
        if (rpp.Command == RendezvousProxyCommand.Acknowledge)
        {
          rd.Port = Marshal.ByteArrayToUshort(rpp.Data, 0);
          byte[] ipaddr = new byte[4];
          Marshal.CopyArray(rpp.Data, ipaddr, 2);
          rd.ProxyIP = (new IPAddress(ipaddr)).ToString();

          // Send the "send file" request on SNAC(04,06):02
         // SNAC04.SendDirectConnectionRequest(_parent, rd);

          // Wait for the proxy to send its 12 byte READY sequence
          rd.DirectConnection.Transfer.BeginReceive(new byte[12], 0, 12,
            SocketFlags.None, new AsyncCallback(ProxyReceivedReady), rd);
        }
        else
        {
          int index = 0;
          ushort error = Marshal.ByteArrayToUshort(rpp.Data, ref index);
          if (error == 0x0004)
          {
            throw new Exception("Recipient not logged in");
          }
          else if (error == 0x000D)
          {
            throw new Exception("Client sent bad request");
          }
          throw new Exception("AOL proxy sent unknown error");
        }
      }
      catch (Exception ex)
      {
        fti.CancelTransfer(ex.Message);
      }
    }

    /// <summary>
    /// Sends a Rendezvous INITRECV and begins sending the file through the proxy connection
    /// </summary>
    internal void StartSendThroughStage2Proxy(RendezvousData rd)
    {
      try
      {
        ProxyInitializeReceive(rd);
        RendezvousProxyPacket rpp = ReadProxyPacket(rd);
        if (rpp.Command == RendezvousProxyCommand.Ready)
        {
          BeginSendFile(rd);
        }
      }
      catch (Exception)
      {
        rd.DirectConnection.CancelTransfer("Proxy refused connection");
      }
    }

    /// <summary>
    /// Begins to send a file through the RendezvousData's Transfer socket
    /// </summary>
    /// <remarks>This method cancels the direct connection itself rather than throw an exception</remarks>
    private void BeginSendFile(RendezvousData rd)
    {
      // Open the file for reading
      try
      {
        rd.DirectConnection.DataChunk = new byte[8192];
        rd.DirectConnection.DataStream = (new StreamReader(rd.DirectConnection.LocalFileName, false)).BaseStream;
        rd.DirectConnection.StreamPosition = 0;
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer("Can't open file for reading");
        return;
      }

      // Send a PROMPT message and receive an ACK message
      try
      {
        SendFileTransmitterHandshake(rd);
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
        return;
      }

      // Signal the parent session that we've started transfering a file
      rd.ParentSession.OnFileTransferProgress(
        RendezvousManager.GetKeyFromCookie(rd.Cookie),
        0, rd.DirectConnection.Header.Size);

      // Send the first chunk
      SendFileTransmitterSendChunk(rd);
    }

    /// <summary>
    /// Send an OFT PROMPT message and receive an ACK message
    /// </summary>
    /// <param name="rd">A <see cref="RendezvousData"/> object</param>
    private void SendFileTransmitterHandshake(RendezvousData rd)
    {
      byte[] buffer = null; // Marshal.CreateFileTransferMessage(0x0101, rd.DirectConnection);
      int index;

      // Send a PROMPT with a blank cookie
      for (index = 8; index < 16; index++)
        buffer[index] = 0x00;

      index = 0;
      try
      {
        while (index < buffer.Length)
        {
          index += rd.DirectConnection.Transfer.Send(
            buffer, index,
            buffer.Length - index,
            SocketFlags.None);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }

      // Receive the return ACK
      index = 0;
      try
      {
        while (index < buffer.Length)
        {
          index += rd.DirectConnection.Transfer.Receive(
            buffer, index,
            buffer.Length - index,
            SocketFlags.None);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }

      // Compare cookies to verify everything's okay
      for (int i = 0; i < 8; i++)
      {
        if (buffer[i + 8] != rd.Cookie[i])
        {
          throw new Exception("Recepient sent a bad cookie. Possible man-in-the-middle?");
        }
      }
    }
    #endregion

    #region Receiver negotiation methods
    /// <summary>
    /// Connect to the sender and begin receiving the file
    /// </summary>
    /// <remarks>If the direct connection fails at this stage, control is passed to <see cref="FallbackToStage2Connection"/></remarks>
    private void StartReceiveThroughDirectConnection(RendezvousData rd)
    {
      try
      {
        rd.DirectConnection.ConnectTransferSocket(rd.VerifiedIP, rd.Port,
          new AsyncCallback(InitReceiveFileConnectFinished));
      }
      catch (Exception)
      {
        FallbackToStage2Connection(rd);
      }
    }

    /// <summary>
    /// Connection to sender has completed
    /// </summary>
    /// <remarks>
    /// If the socket connection fails at this stage, control is passed to <see cref="FallbackToStage2Connection"/>.
    /// If an error occurs during OFT negotiation or while opening the target file for writing, the transfer is canceled.
    /// </remarks>
    private void InitReceiveFileConnectFinished(IAsyncResult res)
    {
      RendezvousData rd = res.AsyncState as RendezvousData;
      try
      {
        rd.DirectConnection.Transfer.EndConnect(res);
      }
      catch (Exception)
      {
        // Hmm, that didn't work, fall back
        FallbackToStage2Connection(rd);
        return;
      }

      try
      {
        BeginReceiveFile(rd);
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
      }
    }

    /// <summary>
    /// Set up the connection to receive data through a stage 1 or stage 3 proxy transfer
    /// </summary>
    private void StartReceiveThroughProxy(RendezvousData rd)
    {
      try
      {
        rd.DirectConnection.ConnectTransferSocket(rd.ProxyIP, _parent.LoginPort,
          new AsyncCallback(InitProxyReceiveConnectFinished));
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
      }
    }

    /// <summary>
    /// Completes the final connection to the proxy server
    /// </summary>
    private void InitProxyReceiveConnectFinished(IAsyncResult res)
    {
      RendezvousData rd = res.AsyncState as RendezvousData;
      try
      {
        rd.DirectConnection.Transfer.EndConnect(res);

        ProxyInitializeReceive(rd);
        RendezvousProxyPacket rpp = ReadProxyPacket(rd);
        if (rpp.Command == RendezvousProxyCommand.Error)
        {
          throw new Exception("Proxy server refused connection");
        }
        BeginReceiveFile(rd);
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
      }
    }

    /// <summary>
    /// Direct connection to the sender has failed, attempt a proxy redirect
    /// </summary>
    private void FallbackToStage2Connection(RendezvousData rd)
    {
      rd.UseProxy = true;
      rd.Sequence = RendezvousSequence.Stage2;
      rd.DirectConnection.ConnectTransferSocket("ars.oscar.aol.com", _parent.LoginPort,
        new AsyncCallback(InitProxyConnectFinished));
    }

    /// <summary>
    /// Receives the PROMPT message and responds with an ACK message, then prepares the transfer socket to receive data
    /// </summary>
    /// <param name="rd">The <see cref="RendezvousData"/> object receiving a file</param>
    private void BeginReceiveFile(RendezvousData rd)
    {
      byte[] filetransferheader = null;
      int index = 0;

      // Read in 256 bytes, PROMPT type and blank cookie

      filetransferheader = new byte[256];
      try
      {
        while (index < filetransferheader.Length)
        {
          index += rd.DirectConnection.Transfer.Receive(
            filetransferheader,
            index,
            filetransferheader.Length - index,
            SocketFlags.None);
        }
      }
      catch (Exception ex)
      {
        string message = "Error negotiating file transfer:" + Environ.NewLine + ex.Message;
        throw new Exception(message);
      }

      index = 8;
      DirectConnectInfo dci = rd.DirectConnection;
      //Marshal.ByteArrayToFTI(filetransferheader, ref index, ref dci);
      rd.DirectConnection = dci; // Just to be sure

      // Respond with the same header, but with the ACK type and the ICBM cookie set
      index = 6;
      Marshal.InsertUshort(filetransferheader, 0x0202, ref index);
      Marshal.CopyArray(rd.Cookie, filetransferheader, 0, ref index);

      index = 0;
      try
      {
        while (index < filetransferheader.Length)
        {
          index += rd.DirectConnection.Transfer.Send(
            filetransferheader,
            index,
            filetransferheader.Length - index,
            SocketFlags.None);
        }
      }
      catch (Exception ex)
      {
        string message = "Error negotiating file transfer:" + Environ.NewLine + ex.Message;
        throw new Exception(message);
      }

      // Open the file for writing
      try
      {
        rd.DirectConnection.DataChunk = new byte[8192];
        rd.DirectConnection.DataStream = (new StreamWriter(rd.DirectConnection.LocalFileName, false)).BaseStream;
        rd.DirectConnection.StreamPosition = 0;
      }
      catch (Exception)
      {
        throw new Exception("Can't open target file for writing");
      }

      // Signal the parent session that the file transfer has started
      rd.ParentSession.OnFileTransferProgress(RendezvousManager.GetKeyFromCookie(rd.Cookie),
        0, rd.DirectConnection.Header.Size);

      // Start receiving data
      rd.DirectConnection.Transfer.BeginReceive(rd.DirectConnection.DataChunk, 0,
        rd.DirectConnection.DataChunk.Length,
        SocketFlags.None, new AsyncCallback(SendFileTransferReceive), rd);
    }

    /// <summary>
    /// Sends a DONE message
    /// </summary>
    /// <param name="rd">A <see cref="RendezvousData"/> object</param>
    private void SendFileReceiverDone(RendezvousData rd)
    {
      Marshal.CopyArray(rd.Cookie, rd.DirectConnection.Header.Cookie, 0);
      byte[] buffer = null;// Marshal.CreateFileTransferMessage(0x0204, rd.DirectConnection);
      int index = 0;
      try
      {
        while (index < buffer.Length)
        {
          index += rd.DirectConnection.Transfer.Send(
            buffer,
            index,
            buffer.Length - index,
            SocketFlags.None);
        }
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
      }
    }
    #endregion

    /// <summary>
    /// Constructs a new RendezvousData object for a Direct IM session
    /// </summary>
    private RendezvousData CreateDirectIMData(string recipient)
    {
      // Set up a direct connection structure
      RendezvousData rd = new RendezvousData();
      rd.UserInfo.ScreenName = recipient;
      rd.ParentSession = _parent;
      rd.Capability = Capabilities.DirectIM;
      rd.Type = 0x0000;

      rd.DirectConnection = new DirectConnectInfo(rd, DirectConnectType.DirectIM);
      return rd;
    }

    /// <summary>
    /// Constructs a new RendezvousData object for a file transfer
    /// </summary>
    private RendezvousData CreateSendFileData(string recipient, string filename)
    {
      // Set up a direct connection information structure
      RendezvousData rd = new RendezvousData();
      rd.UserInfo = new UserInfo();
      rd.UserInfo.ScreenName = recipient;
      rd.ParentSession = _parent;
      rd.Capability = Capabilities.SendFiles;
      rd.Type = 0x0000;

      DirectConnectInfo fti = new DirectConnectInfo(rd, DirectConnectType.FileTransfer);

      // Put the basename of the file into the structure
      int slashindex = filename.LastIndexOf("\\");
      fti.Header.Name = filename.Substring(slashindex + 1, filename.Length - (slashindex + 1));
      fti.LocalFileName = filename;

      // Get the size of the file to put in the structure and checksum it
      fti.Header.Size = (uint)(new FileInfo(filename)).Length;
      fti.Header.Checksum = Checksum(_parent, filename);

      // Couldn't locate the passed-in file
      if (fti.Header.Checksum == 0xFFFFFFFF)
      {
        return null;
      }

      // Set the totals. Eventually OscarLib may support > 1 files at a time
      fti.TotalFiles = 1;
      fti.TotalParts = 1;
      fti.FilesLeft = 1;
      fti.TotalSize = fti.Header.Size;

      rd.DirectConnection = fti;
      return rd;
    }

    #region File data sending and receiving methods
    /// <summary>
    /// Sends a chunk of a file to the recepient
    /// </summary>
    /// <param name="rd">A <see cref="RendezvousData"/> object</param>
    private void SendFileTransmitterSendChunk(RendezvousData rd)
    {
      int bytesreadfromfile = 0;
      try
      {
        // Read a chunk from the file
        bytesreadfromfile = rd.DirectConnection.DataStream.Read(
          rd.DirectConnection.DataChunk, 0, rd.DirectConnection.DataChunk.Length);

        // Begin to send it over the wire
        rd.DirectConnection.Transfer.BeginSend(
        rd.DirectConnection.DataChunk,
        0, bytesreadfromfile,
        SocketFlags.None, new AsyncCallback(SendFileTransmitterSendEnd), rd);
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
        return;
      }
    }

    /// <summary>
    /// Called when a chunk of data has been sent to the remote client
    /// </summary>
    /// <param name="res">An <see cref="IAsyncResult"/> object</param>
    private void SendFileTransmitterSendEnd(IAsyncResult res)
    {
      RendezvousData rd = (RendezvousData)res.AsyncState;
      int bytessent = 0;
      try
      {
        bytessent = rd.DirectConnection.Transfer.EndSend(res);
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
        return;
      }

      rd.DirectConnection.StreamPosition += (uint)bytessent;

      // Signal parent session of the progress
      rd.ParentSession.OnFileTransferProgress(
        RendezvousManager.GetKeyFromCookie(rd.Cookie),
        rd.DirectConnection.StreamPosition,
        rd.DirectConnection.Header.Size);

      // If all the file has been sent, wait for the acknowledgement message
      // and close up shop. Otherwise, send another chunk
      if (rd.DirectConnection.StreamPosition == rd.DirectConnection.Header.Size)
      {
        byte[] receivedone = new byte[256];
        try
        {
          rd.DirectConnection.Transfer.Receive(receivedone);
        }
        catch (Exception ex)
        {
          rd.DirectConnection.CancelTransfer(ex.Message);
          return;
        }
        
        // Check checksums?

        rd.DirectConnection.CompleteTransfer();
      }
      else
      {
        SendFileTransmitterSendChunk(rd);
      }
    }

    /// <summary>
    /// The callback function for socket reads during a file transfer
    /// </summary>
    /// <param name="res">An <see cref="IAsyncResult"/> object</param>
    private void SendFileTransferReceive(IAsyncResult res)
    {
      int bytesread = 0;
      RendezvousData rd = (RendezvousData)res.AsyncState;
      DirectConnectInfo fti = rd.DirectConnection;

      // End the async receive operation
      try
      {
        bytesread = fti.Transfer.EndReceive(res);
        if (bytesread == 0)
          throw new Exception("Remote client cancelled transfer");
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
        return;
      }

      // Write the received data out to the file
      try
      {
        fti.DataStream.Write(fti.DataChunk, 0, bytesread);
        fti.StreamPosition += (uint)bytesread;
      }
      catch (Exception ex)
      {
        rd.DirectConnection.CancelTransfer(ex.Message);
        return;
      }

      // Checksum the received chunk
      fti.Header.ReceivedChecksum = ChecksumChunk(fti.DataChunk, (uint)bytesread, fti.Header.ReceivedChecksum);

      // Signal progress to the parent session
      rd.ParentSession.OnFileTransferProgress(
        RendezvousManager.GetKeyFromCookie(rd.Cookie),
        rd.DirectConnection.StreamPosition,
        rd.DirectConnection.Header.Size);

      // Check to see if the transfer has finished
      if (fti.StreamPosition >= fti.Header.Size)
      {
        fti.DataChunk = null;
        fti.DataStream.Close();

        // Send out the acknowledgement, compare the checksums, and finish up
        SendFileReceiverDone(rd);

        if (fti.Header.ReceivedChecksum != fti.Header.Checksum)
        {
          rd.DirectConnection.CancelTransfer("Received data does not match expected checksum");
          return;
        }

        rd.DirectConnection.CompleteTransfer();
      }
      else
      {
        // Keep receiving asynchronously
        fti.Transfer.BeginReceive(fti.DataChunk, 0, fti.DataChunk.Length,
        SocketFlags.None, new AsyncCallback(SendFileTransferReceive), rd);
      }
    }
    #endregion

    #region Utility methods
    /// <summary>
    /// Performs an OSCAR File Transfer checksum 
    /// </summary>
    /// <param name="sess">A <see cref="Session"/> object</param>
    /// <param name="filename">The file to checksum</param>
    /// <returns>The checksum of the file</returns>
    public uint Checksum(Session sess, string filename)
    {
      uint retval = 0xFFFF0000;
      FileStream infile = null;

      try
      {
        infile = File.OpenRead(filename);
        byte[] chunk = new byte[1024];
        int bytesread = 0;
        int streamposition = 0;
        do
        {
          bytesread = infile.Read(chunk, 0, chunk.Length);
          retval = ChecksumChunk(chunk, (uint)bytesread, retval);
          streamposition += bytesread;

        } while (bytesread == chunk.Length);
      }
      catch (Exception ex)
      {
        sess.OnFileTransferCancelled(filename, ex.Message);
        return 0xFFFFFFFF;
      }
      finally
      {
        infile.Close();
      }
      return retval;
    }

    private uint ChecksumChunk(byte[] filechunk, uint chunklength, uint start)
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

    /// <summary>
    /// Returns a value indicating whether the IP address is in a private address space according to RFC 1918
    /// </summary>
    private bool IsNATtedIP(string ipaddress)
    {
      byte[] bytes = IPAddress.Parse(ipaddress).GetAddressBytes();
      return (bytes[0] == 10) || (bytes[0] == 192 && bytes[1] == 168) ||
        (bytes[0] == 176 && (16 <= bytes[1] && bytes[1] <= 31));
    }
    #endregion
  }
}
