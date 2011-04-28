/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Handles a server connection completed event
    /// </summary>
    public delegate void ServerConnectionCompletedHandler(Connection conn);

    /// <summary>
    /// Handles a packet read complete event
    /// </summary>
    public delegate void ConnectionReadPacketHandler(Connection conn, byte[] data);

    /// <summary>
    /// Encapsulates a connection to an OSCAR server
    /// </summary>
    public class Connection
    {
        #region Static socket factory methods

        /// <summary>
        /// Begins the socket creation process
        /// </summary>
        public static void CreateDirectConnectSocket(string host, int port, Delegate callback)
        {
            // Make sure the callback is the right type
            // TODO: Better error prevention for CF?
#if !WindowsCE
            ParameterInfo[] param = callback.Method.GetParameters();
            if (param.Length != 2 || param[0].ParameterType != typeof (Socket) ||
                param[1].ParameterType != typeof (string))
            {
                throw new ArgumentException("Callback delegate must take a Socket and a string as its parameters");
            }
#endif

            SocketConnectionData scd = new SocketConnectionData();
            scd.Callback = callback;
            scd.Port = port;
            scd.Server = host;
            scd.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Dns.BeginGetHostEntry(scd.Server, new AsyncCallback(CreateDCSEndDnsLookup), scd);
            }
            catch (SocketException sockex)
            {
                string message = "Cannot connect to DNS service:"
                                 + Environ.NewLine + sockex.Message;

#if WindowsCE
		ProxiedSocketFactoryResultHandler handler = (callback as ProxiedSocketFactoryResultHandler);
		handler(scd.Socket, message);
#else
                callback.DynamicInvoke(scd.Socket, message);
#endif
            }
        }

        /// <summary>
        /// Ends the DNS lookup phase of connection and begins connecting the socket to the host
        /// </summary>
        protected static void CreateDCSEndDnsLookup(IAsyncResult res)
        {
            SocketConnectionData scd = null;

            IPHostEntry hosts = null;
            try
            {
                scd = res.AsyncState as SocketConnectionData;
                hosts = Dns.EndGetHostEntry(res);
            }
            catch (Exception sockex)
            {
                string message = "Cannot resolve server:"
                                 + Environ.NewLine + sockex.Message;

#if WindowsCE
		ProxiedSocketFactoryResultHandler handler = (scd.Callback as ProxiedSocketFactoryResultHandler);
		handler(scd.Socket, message);
#else
                scd.Callback.DynamicInvoke(scd.Socket, message);
#endif
                return;
            }

            IPAddress address = hosts.AddressList[0];
            IPEndPoint ipep = new IPEndPoint(address, scd.Port);

            try
            {
                scd.Socket.BeginConnect(ipep, new AsyncCallback(CreateDCSEndInitialConnection), scd);
            }
            catch (SocketException sockex)
            {
                string message = "Cannot connect to server:"
                                 + Environ.NewLine + sockex.Message;

#if WindowsCE
		ProxiedSocketFactoryResultHandler handler = (scd.Callback as ProxiedSocketFactoryResultHandler);
		handler(scd.Socket, message);
#else
                scd.Callback.DynamicInvoke(scd.Socket, message);
#endif
            }
        }

        /// <summary>
        /// Ends the connection phase and returns a connected socket
        /// </summary>
        protected static void CreateDCSEndInitialConnection(IAsyncResult res)
        {
            SocketConnectionData scd = null;
            try
            {
                scd = res.AsyncState as SocketConnectionData;
                scd.Socket.EndConnect(res);
#if WindowsCE
		ProxiedSocketFactoryResultHandler handler = (scd.Callback as ProxiedSocketFactoryResultHandler);
		handler(scd.Socket, "");
#else
                scd.Callback.DynamicInvoke(scd.Socket, "");
#endif
            }
            catch (Exception sockex)
            {
                string message = "Can't connect to server: "
                                 + Environ.NewLine + sockex.Message;

#if WindowsCE
		ProxiedSocketFactoryResultHandler handler = (scd.Callback as ProxiedSocketFactoryResultHandler);
		handler(scd.Socket, message);
#else
                scd.Callback.DynamicInvoke(scd.Socket, message);
#endif
            }
        }

        /// <summary>
        /// Encapsulates socket connection information
        /// </summary>
        private class SocketConnectionData
        {
            public Delegate Callback;
            public int Port;
            public string Server;
            public Socket Socket;
        }

        #endregion

        /// <summary>
        /// A value indicating whether or not this connection is in the process of connecting
        /// </summary>
        protected bool isConnecting;
        /// <summary>
        /// A timer that controls determining when a connection attempt has timed out
        /// </summary>
        private readonly Timer connectionTimeout;
        /// <summary>
        /// A value indicating whether or not this connection is in the process of disconnecting
        /// </summary>
        protected bool isDisconnecting;
        private ushort _flapsequencenum = 1;
        /// <summary>
        /// The unique ID of this connection
        /// </summary>
        protected int connectionId = -1;
        private TimerCallback _keepalivecallback = null;
        private Timer _keepalivetimer = null;
        /// <summary>
		/// The <see cref="ISession"/> that owns this connection
        /// </summary>
		protected ISession parentSession;
        /// <summary>
        /// The server to which to connect
        /// </summary>
        protected string server;
        /// <summary>
        /// The port to which to connect
        /// </summary>
        protected int port;
        private ProcessQueue _processor = null;
        /// <summary>
        /// A value indicating whether or not this connection is ready to accept data
        /// </summary>
        protected bool readyForData;
        private AsyncCallback _receivecallback = null;
        /// <summary>
        /// The <see cref="Socket"/> underlying this connection
        /// </summary>
        protected Socket socket;
        /// <summary>
        /// The cookie to send in the connection handshaking phase
        /// </summary>
        protected Cookie cookie;

        /// <summary>
        /// Creates a new connection
        /// </summary>
		/// <param name="parent">The <see cref="ISession"/> that owns this connection</param>
        /// <param name="id">The connection's unique ID</param>
		public Connection(ISession parent, int id)
        {
            _receivecallback = new AsyncCallback(ProcessFLAP);
            _processor = new ProcessQueue(parent);
            _keepalivecallback = new TimerCallback(SendKeepalive);
            _keepalivetimer = new Timer(_keepalivecallback, null, Timeout.Infinite, Timeout.Infinite);
            connectionTimeout = new Timer(new TimerCallback(ConnectionTimedOut), null, Timeout.Infinite, Timeout.Infinite);
            parentSession = parent;
            connectionId = id;
        }

        /// <summary>
        /// Occurs when the server connection is completed
        /// </summary>
        public event ServerConnectionCompletedHandler ServerConnectionCompleted;
        /// <summary>
        /// Occurs when a packet read via <see cref="ReadPacketAsync"/> has arrived
        /// </summary>
        public event ConnectionReadPacketHandler ReadPacketAsyncComplete;

        /// <summary>
        /// Connects to the server and performs the initial handshaking
        /// </summary>
        public virtual bool ConnectToServer()
        {
            StartTimeoutPeriod(30);
            parentSession.ProxiedSocketFactory(server, port,
                                         new ProxiedSocketFactoryResultHandler(ProxiedSocketFactoryResult));
            return true;
        }

        /// <summary>
        /// Starts counting down until a connection timeout event occurs
        /// </summary>
        /// <param name="seconds">The number of seconds to elapse until the connection times out</param>
        public void StartTimeoutPeriod(int seconds)
        {
            connectionTimeout.Change(seconds * 1000, Timeout.Infinite);
        }

        /// <summary>
        /// Stops the timeout timer
        /// </summary>
        public void StopTimeoutPeriod()
        {
            connectionTimeout.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Disconnects from the OSCAR server
        /// </summary>
        /// <param name="error"><c>true</c> if the disconnection is resulting from an error, <c>false</c> otherwise</param>
        /// <returns><c>true</c> if the disconnection succeeded without error,
        /// <c>false</c> otherwise</returns>
        public bool DisconnectFromServer(bool error)
        {
            isDisconnecting = true;

            try
            {
                socket.Blocking = false;
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                parentSession.Connections.DeregisterConnection(this, error);
                isConnecting = false;
                _keepalivetimer.Change(Timeout.Infinite, Timeout.Infinite);
                StopTimeoutPeriod();
            }
            return true;
        }

        /// <summary>
        /// Unmarshals a byte array into a FLAP header
        /// </summary>
        /// <param name="buffer">A byte array</param>
        /// <returns>A populated FLAP header</returns>
        /// <remarks>
        /// This method always assumes that the FLAP header is at the beginning of
        /// the buffer. The FLAP header is always the first six bytes of a communication
        /// frame coming from the server, and it is read into a six-byte buffer by
        /// itself at the beginning of a socket read operation.
        /// </remarks>
        private static FLAPHeader GetFLAPHeader(byte[] buffer)
        {
            FLAPHeader retval = new FLAPHeader();
            if (buffer != null)
            {
                retval.Channel = buffer[1];
                retval.DatagramSequenceNumber = (ushort) ((buffer[2] << 8) | buffer[3]);
                retval.DataSize = (ushort) ((buffer[4] << 8) | buffer[5]);
            }
            return retval;
        }

        private void SendKeepalive(object threadstate)
        {
            using (ByteStream stream = new ByteStream())
            {
                FLAPHeader fh;
                fh.Channel = 0x05;
                fh.DatagramSequenceNumber = 0;
                fh.DataSize = 0;

                stream.PrependOscarHeaders(fh, null);

                try
                {
                    byte[] keepalive = stream.GetBytes();
                    Marshal.InsertUshort(keepalive, (ushort) FLAPSequence, 2);
                    lock (socket)
                    {
                        socket.Send(keepalive);
                    }
                    Logging.WriteString(String.Format("Sent keepalive over connection {0}", connectionId));
                }
                catch (Exception ex)
                {
                    if (!isDisconnecting)
                    {
                        string message = "Send error: " + ex.Message;
                        Logging.WriteString(message + ", connection " + connectionId.ToString());
                        DisconnectFromServer(true);
                    }
                }

                _keepalivetimer.Change(60000, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Raises the <see cref="ServerConnectionCompleted"/> event
        /// </summary>
        protected internal void OnServerConnectionCompleted()
        {
            if (ServerConnectionCompleted != null)
            {
                ServerConnectionCompleted(this);
            }
        }

        #region Properties

        /// <summary>
		/// Gets the <see cref="ISession"/> that owns this connection object
        /// </summary>
		public ISession ParentSession
        {
            get { return parentSession; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this connection
        /// is in the initial "connecting" phase -- that is, the time between
        /// connection to the server and the client sending SNAC(01,02)
        /// </summary>
        public bool Connecting
        {
            get { return isConnecting; }
            set
            {
                if (isConnecting != value)
                {
                    isConnecting = value;
                    if (isConnecting == false)
                    {
                        _keepalivetimer.Change(60000, Timeout.Infinite);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the underlying socket is connected
        /// </summary>
        public bool Connected
        {
            get
            {
                if (socket == null)
                {
                    return false;
                }

                try
                {
                    // Ignore the return value, there might legitimately be zero bytes to be read
                    int value = socket.Available;
                    if (value == 0 && socket.Poll(0, SelectMode.SelectRead))
                    {
                        return false;
                    }
                }
                catch (SocketException sockex)
                {
                    Logging.WriteString("Socket.Available threw SocketException: " + sockex.Message);
                    return false;
                }
                catch (ObjectDisposedException odex)
                {
                    Logging.WriteString("Socket.Available threw ObjectDisposedException: " + odex.Message);
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a reference to the main socket used for data transfer
        /// </summary>
        internal Socket DataSocket
        {
            get { return socket; }
        }

        /// <summary>
        /// Gets or sets the server to which to connect
        /// </summary>
        public virtual string Server
        {
            get { return server; }
            set { server = value; }
        }

        /// <summary>
        /// Gets or sets the port to which to connect
        /// </summary>
        public virtual int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        /// Gets or sets the authentication cookie to send to the server
        /// </summary>
        public Cookie Cookie
        {
            get { return cookie; }
            set { cookie = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this connection
        /// is ready to send data
        /// </summary>
        public bool ReadyForData
        {
            get { return readyForData; }
            set { readyForData = true; }
        }

        /// <summary>
        /// Gets the Connection's ID, as assigned by the <see cref="ConnectionManager"/>
        /// </summary>
        public int ID
        {
            get { return connectionId; }
        }

        /// <summary>
        /// Gets or sets a Timer controlling the socket's initial connection timeout
        /// </summary>
        public Timer ConnectionTimeout
        {
            get { return connectionTimeout; }
        }

        /// <summary>
        /// Gets the next FLAP sequence ID in the sequence 0 through 2^15 - 1, inclusive
        /// </summary>
        /// <returns>The next FLAP sequence ID</returns>
        /// <remarks>The sequence ID series wraps around if it is about to overflow</remarks>
        private ushort FLAPSequence
        {
            get
            {
                lock (this)
                {
                    ushort retval = _flapsequencenum++;
                    if (_flapsequencenum == 0xFFFF)
                        _flapsequencenum = 0;
                    return retval;
                }
            }
        }

        private ProcessQueue Processor
        {
            get { return _processor; }
        }
        

        #endregion

        #region FLAP sending and receiving

        /// <summary>
        /// Gets the next FLAP header from the server
        /// </summary>
        public virtual void ReadHeader()
        {
            try
            {
                byte[] flapbuffer = new byte[6];
                lock (socket)
                {
                    socket.BeginReceive(flapbuffer, 0, flapbuffer.Length, SocketFlags.None,
                                       _receivecallback, flapbuffer);
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Receive error in Connection.ReadHeader: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                }
            }
        }

        /// <summary>
        /// Processes the asynchronous receipt of a FLAP
        /// </summary>
        /// <param name="res">The <see cref="IAsyncResult"/> of a BeginReceive call</param>
        private void ProcessFLAP(IAsyncResult res)
        {
            int bytesreceived = 0;
            int receiveindex = 0;

            byte[] flapbuffer = null;

            try
            {
                lock (socket)
                {
                    bytesreceived = socket.EndReceive(res);
                    if (bytesreceived == 0)
                    {
                        throw new Exception("Socket receive returned 0 bytes read");
                    }

                    flapbuffer = (byte[]) res.AsyncState;

                    receiveindex = bytesreceived;
                    while (receiveindex < flapbuffer.Length)
                    {
                        bytesreceived = socket.Receive(flapbuffer,
                                                      receiveindex,
                                                      flapbuffer.Length - receiveindex,
                                                      SocketFlags.None);
                        if (bytesreceived == 0)
                        {
                            throw new Exception("Socket receive returned 0 bytes read");
                        }
                        receiveindex += bytesreceived;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Receive error in ProcessFLAP: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                }
                return;
            }

            if (flapbuffer[0] == 0xFF)
            {
                int badcount = 0;
                for (badcount = 0; badcount < flapbuffer.Length && flapbuffer[badcount] == 0xFF; badcount++) ;

                // SOMEHOW there are two bytes of 0xFF occuring when requesting
                // SNAC family 0x10 and receiving SNAC(01,03).  So that has to stop
                for (int i = badcount; i < flapbuffer.Length; i++)
                {
                    flapbuffer[i - badcount] = flapbuffer[i];
                }

                socket.Receive(flapbuffer, flapbuffer.Length - badcount, badcount, SocketFlags.None);
            }

            // Get the FLAP header out of the async result
            FLAPHeader flap = GetFLAPHeader(flapbuffer);
            ByteStream stream = ReadPacket(flap.DataSize);
            if (stream == null)
            {
                return;
            }

            // The full packet is here, so we can chuck it out for processing
            switch (flap.Channel)
            {
                case 0x01: // New connection negotiation
                    // This will not occur, FLAP 0x01 is handled in ConnectToServer
                    break;
                case 0x02: //  SNAC data
                    if (stream.GetByteCount() < 10)
                    {
                        break; // Don't return, don't disconnect, just keep on keeping on
                    }

                    DataPacket dp = stream.CreateDataPacket();
                    dp.FLAP = flap;
                    dp.ParentConnection = this;
                    dp.ParentSession = parentSession;

                    Processor.Enqueue(dp);
                    break;
                case 0x03: // FLAP error
                    // Session error:  FLAP error, bailing out
                    Logging.WriteString("Received error FLAP");
                    break;
                case 0x04: // Close connection negotiation
                    Logging.WriteString("Received close connection FLAP");
                    DisconnectFromServer(false);
                    return;
                case 0x05: // Keepalive packet
                    Logging.WriteString("Received keepalive FLAP");
                    SendKeepalive(null);
                    break;
                default:
                    break;
            }

            // Shine on, you crazy connection
            _keepalivetimer.Change(60000, Timeout.Infinite);
            ReadHeader();
        }

        /// <summary>
        /// Synchronously read in a packet
        /// </summary>
        public ByteStream ReadPacket(int datalength)
        {
            byte[] packet = new byte[datalength];
            int bytesreceived = 0;
            int receiveindex = 0;

            try
            {
                lock (socket)
                {
                    while (receiveindex < packet.Length)
                    {
                        bytesreceived = socket.Receive(
                            packet,
                            receiveindex,
                            packet.Length - receiveindex,
                            SocketFlags.None);
                        if (bytesreceived == 0)
                        {
                            throw new Exception("Socket receive returned 0 bytes read");
                        }
                        receiveindex += bytesreceived;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Receive error in ReadPacket: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                    return null;
                }
            }

            return new ByteStream(packet);
        }

        /// <summary>
        /// Reads a packet of the specified length asynchronously.
        /// Data is returned in the <see cref="ReadPacketAsyncComplete"/> event.
        /// </summary>
        /// <param name="datalength">The number of bytes to read from the socket</param>
        public void ReadPacketAsync(int datalength)
        {
            byte[] buffer = new byte[datalength];
            try
            {
                lock (socket)
                {
                    Logging.WriteString(String.Format("Starting ReadPacketAsync datalength = {0}", datalength));
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(EndReadPacketAsync),
                                       buffer);
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Receive error in ReadPacketAsync: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                }
            }
        }

        private void EndReadPacketAsync(IAsyncResult res)
        {
            byte[] buffer = null;
            int bytesreceived = 0;
            int receiveindex = 0;
            try
            {
                lock (socket)
                {
                    bytesreceived = socket.EndReceive(res);
                    if (bytesreceived == 0)
                    {
                        throw new Exception("Socket receive returned 0 bytes read");
                    }

                    buffer = (byte[]) res.AsyncState;

                    receiveindex = bytesreceived;
                    while (receiveindex < buffer.Length)
                    {
                        bytesreceived =
                            socket.Receive(buffer, receiveindex, buffer.Length - receiveindex, SocketFlags.None);
                        if (bytesreceived == 0)
                        {
                            throw new Exception("Socket receive returned 0 bytes read");
                        }
                        receiveindex += bytesreceived;
                    }
                }

                if (ReadPacketAsyncComplete != null)
                {
                    ReadPacketAsyncComplete(this, buffer);
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Receive error in EndReadPacketAsync: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                }
            }
        }

        /// <summary>
        /// Sends a single FLAP
        /// </summary>
        /// <param name="buffer">The byte buffer to send</param>
        public virtual void SendFLAP(byte[] buffer)
        {
            int sentbytes = 0, sendindex = 0;

            try
            {
                lock (socket)
                {
                    // The FLAP ID gets assigned here so we can guaruntee per-connection sequence
                    Marshal.InsertUshort(buffer, FLAPSequence, 2);
                    while (sendindex < buffer.Length)
                    {
                        sentbytes = socket.Send(buffer, sendindex, buffer.Length - sendindex, SocketFlags.None);
                        if (sentbytes == 0)
                        {
                            throw new Exception("Socket send returned 0 bytes transmitted");
                        }
                        sendindex += sentbytes;
                    }
                    _keepalivetimer.Change(60000, Timeout.Infinite);

                    if (buffer[1] == 0x02 && Logging.IsLoggingEnabled)
                    {
                        ushort fam = (ushort)((buffer[6] << 8) | buffer[7]);
                        ushort sub = (ushort)((buffer[8] << 8) | buffer[9]);
                        Logging.DumpFLAP(buffer, String.Format("Connection {3} sent SNAC({0:x2},{1:x2}), {2} bytes",
                                                               fam, sub, sentbytes, ID));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    string message = "Send error: " + ex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                }
            }
            finally
            {
                buffer = null;
            }
        }

        #endregion

        #region Raw data sending and receiving

        /// <summary>
        /// Asynchronously sends a raw packet, calling the specified method when finished
        /// </summary>
        public void SendPacket(byte[] buffer, AsyncCallback callback)
        {
            SendPacket(buffer, 0, buffer.Length, callback);
        }

        /// <summary>
        /// Asynchronously sends a raw packet, calling the specified method when finished
        /// </summary>
        public void SendPacket(byte[] buffer, int offset, int length, AsyncCallback callback)
        {
            if (readyForData)
            {
                lock (socket)
                {
                    socket.BeginSend(buffer, offset, length, SocketFlags.None, callback, this);
                }
            }
        }

        /// <summary>
        /// Asynchronously receives a raw packet, calling the specified method when finished
        /// </summary>
        public void ReceivePacket(byte[] buffer, AsyncCallback callback)
        {
            if (readyForData)
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, buffer);
            }
        }

        #endregion

        #region Initial connection async callbacks

        /// <summary>
        /// 30 seconds have elapsed since the connection attempt started
        /// </summary>
        public virtual void ConnectionTimedOut(object data)
        {
            DisconnectFromServer(true);
        }

        private void ProxiedSocketFactoryResult(Socket sock, string errormsg)
        {
            if (!String.IsNullOrEmpty(errormsg))
            {
                Logging.WriteString(errormsg + ", connection " + connectionId.ToString());
                DisconnectFromServer(true);
                return;
            }

            socket = sock;
            Connecting = true;

            // Read in the first ten bytes (6 byte FLAP channel 0x01 + 0x00000001)
            // of the connection handshake
            byte[] serverhandshake = new byte[10];
            try
            {
                int bytesreceived = 0;
                int receiveindex = 0;
                lock (socket)
                {
                    while (bytesreceived < 10)
                    {
                        bytesreceived =
                            socket.Receive(serverhandshake, receiveindex, 10 - receiveindex, SocketFlags.None);
                        receiveindex += bytesreceived;
                    }
                }
            }
            catch (SocketException sockex)
            {
                string message = "Can't read handshake from server: "
                                 + Environ.NewLine + sockex.Message;
                Logging.WriteString(message + ", connection " + connectionId.ToString());
                DisconnectFromServer(true);
                return;
            }
            finally
            {
                serverhandshake = null;
            }

            // Construct our reply to the connection handshake
            using (ByteStream clientHandshake = new ByteStream())
            {
                FLAPHeader fh;
                fh.Channel = 0x01;
                fh.DatagramSequenceNumber = FLAPSequence;
                fh.DataSize = (ushort) (4 + ((cookie == null) ? 0 : 4 + cookie.GetByteCount()));

                clientHandshake.PrependOscarHeaders(fh, null);
                clientHandshake.WriteUint(Constants.PROTOCOL_VERSION);
                if (cookie != null)
                {
                    clientHandshake.WriteUshort(0x0006);
                    clientHandshake.WriteUshort((ushort)cookie.GetByteCount());
                    clientHandshake.WriteByteArray(cookie.ToByteArray());
                }

                try
                {
                    lock (socket)
                    {
                        socket.Send(clientHandshake.GetBytes());
                    }
                }
                catch (SocketException sockex)
                {
                    string message = "Couldn't send handshake to server:"
                                     + Environ.NewLine + sockex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                    return;
                }
                catch (ObjectDisposedException odex)
                {
                    string message = "Couldn't send handshake to server:"
                                     + Environ.NewLine + odex.Message;
                    Logging.WriteString(message + ", connection " + connectionId.ToString());
                    DisconnectFromServer(true);
                    return;
                }
            }

            StopTimeoutPeriod();
            Connecting = true;

            // And the handshaking is done. Auth connection will send
            // SNAC(17,06) and any other connection will receive SNAC(01,03)
            OnServerConnectionCompleted();
        }

        #endregion
    }
}