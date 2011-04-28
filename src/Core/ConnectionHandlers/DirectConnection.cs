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
using System.Text;
using System.Threading;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Describes the AOL Rendezvous proxy commands
    /// </summary>
    internal enum RendezvousProxyCommand
    {
        /// <summary>
        /// An error occured in the proxy connection
        /// </summary>
        Error = 0x0001,
        /// <summary>
        /// Used to initialize the proxy connection for sending a file
        /// </summary>
        InitializeSend = 0x0002,
        /// <summary>
        /// The proxy is acknowledging a command
        /// </summary>
        Acknowledge = 0x0003,
        /// <summary>
        /// Used to initialize the proxy connection for receiving a file
        /// </summary>
        InitializeReceive = 0x0004,
        /// <summary>
        /// The proxy connection is set up and ready
        /// </summary>
        Ready = 0x0005
    }

    /// <summary>
    /// Encapsulates a proxy negotiation packet
    /// </summary>
    internal class RendezvousProxyPacket
    {
        public RendezvousProxyCommand Command = RendezvousProxyCommand.Error;
        public byte[] Data = null;
    }

    /// <summary>
    /// Defines methods of creating direct connections
    /// </summary>
    public enum DirectConnectionMethod
    {
        /// <summary>
        /// Directly connect to the peer
        /// </summary>
        Direct,
        /// <summary>
        /// Connect to the peer via a proxy
        /// </summary>
        Proxied
    }

    /// <summary>
    /// Specifies the type of the Direct Connection
    /// </summary>
    internal enum DirectConnectType
    {
        /// <summary>
        /// The Direct Connection is continuous and should not be closed after a single transfer
        /// </summary>
        DirectIM,
        /// <summary>
        /// The Direct Connection is to be used to transfer a file and then disconnect
        /// </summary>
        FileTransfer
    }

    /// <summary>
    /// A handler for alerting child classes of <see cref="DirectConnection"/> of a connection failure
    /// </summary>
    /// <param name="reason">A string describing the reason for the failure</param>
    public delegate void DirectConnectionFailed(string reason);

    /// <summary>
    /// A handler for alerting child classes of <see cref="DirectConnection"/> when the connection completes successfully
    /// </summary>
    public delegate void DirectConnectionReady();

    /// <summary>
    /// Specifies the role that created a Direct Connection
    /// </summary>
    public enum DirectConnectRole
    {
        /// <summary>
        /// This connection initiated the Rendezvous session
        /// </summary>
        Initiator,
        /// <summary>
        /// This connection was created as a result of a Rendezvous invitation
        /// </summary>
        Receiver
    }

    public class DirectConnection : Connection
    {
        private string _clientip = "";
        private bool _controlledlistenerstop = false;
        protected Socket _listener = null;
        private string _message = "";
        private DirectConnectionMethod _method = DirectConnectionMethod.Direct;
        private UserInfo _other = new UserInfo();
        private string _proxyip = "";
        private readonly DirectConnectRole _role;
        private RendezvousSequence _sequence = RendezvousSequence.DirectOrStage1;
        private ushort _subtype = 0;
        private RendezvousType _type = RendezvousType.Invite;
        private string _verifiedip = "";

        /// <summary>
        /// Creates a new Direct Connection
        /// </summary>
		/// <param name="parent">The <see cref="ISession"/> that owns this connection</param>
        /// <param name="id">The connection's unique ID</param>
        /// <param name="dcmethod">The method (direct or proxied) that the connection is to use</param>
        /// <param name="role">The role of this connection in a Rendezvous session</param>
		public DirectConnection(ISession parent, int id,
                                DirectConnectionMethod dcmethod, DirectConnectRole role)
            : base(parent, id)
        {
            _method = dcmethod;
            _role = role;

            // Generate a random ICBM cookie
            Cookie = Cookie.CreateNullTerminatedCookieForSending();

            // Get the local IP

            //ClientIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ClientIP = address.ToString();
                    break;
                }
            }

            if (String.IsNullOrEmpty(ClientIP))
            {
                throw new Exception("Local host does not have an IPv4 address bound to it");
            }
        }

        /// <summary>
        /// Gets the OSCAR capability used by this Direct Connection
        /// </summary>
        public virtual Capabilities Capability
        {
            get { return Capabilities.None; }
        }

        /// <summary>
        /// Gets the <see cref="RendezvousSequence"/> of the connection
        /// </summary>
        public RendezvousSequence Sequence
        {
            get { return _sequence; }
            set { _sequence = value; }
        }

        /// <summary>
        /// Gets the <see cref="DirectConnectRole"/> of the connection
        /// </summary>
        public DirectConnectRole Role
        {
            get { return _role; }
        }

        /// <summary>
        /// Gets or sets the <see cref="UserInfo"/> of the other user in this direct connection
        /// </summary>
        public UserInfo Other
        {
            get { return _other; }
            set { _other = value; }
        }

        public string ClientIP
        {
            get { return _clientip; }
            set { _clientip = value; }
        }

        public string VerifiedIP
        {
            get { return _verifiedip; }
            set { _verifiedip = value; }
        }

        public string ProxyIP
        {
            get { return _proxyip; }
            set { _proxyip = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// Gets the <see cref="DirectConnectionMethod"/> in use by this connection
        /// </summary>
        public DirectConnectionMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }

        /// <summary>
        /// Gets the <see cref="RendezvousType"/> of this connection
        /// </summary>
        public RendezvousType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public ushort SubType
        {
            get { return _subtype; }
            set { _subtype = value; }
        }

        public Socket Listener
        {
            get { return _listener; }
        }

        #region Non-implemented overrides

        /// <summary>
        /// Not implemented in DirectConnection
        /// </summary>
        public override void ReadHeader()
        {
            throw new NotImplementedException("DirectConnection does not support FLAP-level communication");
        }

        /// <summary>
        /// Not implemented in DirectConnection
        /// </summary>
        public override void SendFLAP(byte[] buffer)
        {
            throw new NotImplementedException("DirectConnection does not support FLAP-level communication");
        }

        #endregion

        protected event DirectConnectionFailed DirectConnectionFailed;
        protected event DirectConnectionReady DirectConnectionReady;

        /// <summary>
        /// Connect to a remote user using Rendezvous
        /// </summary>
        public override bool ConnectToServer()
        {
            switch (_role)
            {
                case DirectConnectRole.Initiator:
                    ConnectAsInitiator();
                    break;
                case DirectConnectRole.Receiver:
                    AcceptDirectConnectionInvitation();
                    break;
            }

            return true;
        }

        public override void ConnectionTimedOut(object data)
        {
            if (socket != null)
            {
                socket.Close();
            }
            if (_listener != null)
            {
                _listener.Close();
            }

            OnDirectConnectFailed("Connection timed out");
        }

        /// <summary>
        /// Accepts an invitation to direct connect
        /// </summary>
        private void AcceptDirectConnectionInvitation()
        {
            // Start the timeout timer
            StartTimeoutPeriod(30);

            if (_method == DirectConnectionMethod.Proxied)
            {
                StartReceiveThroughProxy();
            }
            else
            {
                StartReceiveThroughDirectConnection();
            }
        }

        /// <summary>
        /// Signals to implementing classes that the connection attempt failed
        /// </summary>
        protected internal void OnDirectConnectFailed(string reason)
        {
            // Stop the timeout timer
            StopTimeoutPeriod();
            if (DirectConnectionFailed != null)
            {
                DirectConnectionFailed(reason);
            }
        }

        /// <summary>
        /// Signals to implementing classes that the connection is ready to use
        /// </summary>
        protected internal void OnDirectConnectReady()
        {
            Logging.WriteString("Signalling direct connection ready");
            // Stop the timeout timer
            StopTimeoutPeriod();
            ReadyForData = true;
            if (DirectConnectionReady != null)
            {
                DirectConnectionReady();
            }
        }

        #region OSCAR Rendezvous Proxy utilities

        /// <summary>
        /// Synchronously sends a rendezvous proxy INITSEND message
        /// </summary>
        private void ProxyInitializeSend()
        {
            Encoding enc = Encoding.ASCII;
            byte screennamelength = (byte) enc.GetByteCount(parentSession.ScreenName);

            int index = 0, bytessent = 0;
            byte[] data = new byte[12 + 1 + screennamelength + 8 + 4 + 16];
            // Proxy header
            InsertProxyHeader(data, RendezvousProxyCommand.InitializeSend, ref index);
            // Screenname size + string
            data[index++] = screennamelength;
            Marshal.InsertString(data, parentSession.ScreenName, enc, ref index);
            // The rendezvous cookie
            Marshal.CopyArray(Cookie.ToByteArray(), data, 0, ref index);
            // TLV 0x0001, length 0x0010
            Marshal.InsertUint(data, 0x00010010, ref index);
            // The Send File capability
            Marshal.CopyArray(CapabilityProcessor.GetCapabilityArray(Capability), data, 0, ref index);

            while (bytessent < data.Length)
            {
                bytessent += socket.Send(data, bytessent, data.Length - bytessent, SocketFlags.None);
            }
            Logging.DumpFLAP(data, "Rendezvous proxy initialize send");
        }

        /// <summary>
        /// Synchronously sends a rendezvous proxy INITRECV message
        /// </summary>
        private void ProxyInitializeReceive()
        {
            Encoding enc = Encoding.ASCII;
            //byte screennamelength = (byte)enc.GetByteCount(rd.UserInfo.ScreenName);
            byte screennamelength = (byte) enc.GetByteCount(parentSession.ScreenName);

            int index = 0, bytessent = 0;
            byte[] data = new byte[12 + 1 + screennamelength + 2 + 8 + 20];
            // Proxy header
            InsertProxyHeader(data, RendezvousProxyCommand.InitializeReceive, ref index);
            // Screenname size + string
            data[index++] = screennamelength;
            //Marshal.InsertString(data, rd.UserInfo.ScreenName, enc, ref index);
            Marshal.InsertString(data, parentSession.ScreenName, enc, ref index);
            // The fake (heh) port
            Marshal.InsertUshort(data, (ushort) port, ref index);
            // The rendezvous cookie
            Marshal.CopyArray(Cookie.ToByteArray(), data, 0, ref index);
            // TLV 0x0001, length 0x0010
            Marshal.InsertUint(data, 0x00010010, ref index);
            // The Send File capability
            Marshal.CopyArray(CapabilityProcessor.GetCapabilityArray(Capability), data, 0, ref index);

            while (bytessent < data.Length)
            {
                bytessent += socket.Send(data, bytessent, data.Length - bytessent, SocketFlags.None);
            }
            Logging.DumpFLAP(data, "Rendezvous proxy initialize receive");
        }

        /// <summary>
        /// Synchronously reads a packet from a Rendezvous proxy connection
        /// </summary>
        private RendezvousProxyPacket ReadProxyPacket()
        {
            int bytesreceived = 0;

            byte[] header = new byte[12];
            while (bytesreceived < header.Length)
            {
                bytesreceived += socket.Receive(header, bytesreceived, header.Length - bytesreceived, SocketFlags.None);
            }
            Logging.DumpFLAP(header, "Rendezvous proxy read packet header");

            RendezvousProxyPacket retval = new RendezvousProxyPacket();
            using (ByteStream bstream = new ByteStream(header))
            {
                retval.Data = new byte[bstream.ReadUshort() - 10];
                bstream.AdvanceToPosition(4);
                retval.Command = (RendezvousProxyCommand)bstream.ReadUshort();
            }

            bytesreceived = 0;
            while (bytesreceived < retval.Data.Length)
            {
                bytesreceived +=
                    socket.Receive(retval.Data, bytesreceived, retval.Data.Length - bytesreceived, SocketFlags.None);
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
        private void InsertProxyHeader(byte[] data, RendezvousProxyCommand command, ref int index)
        {
            Marshal.InsertUshort(data, (ushort) (data.Length - 2), ref index);
            Marshal.InsertUshort(data, 0x044A, ref index);
            Marshal.InsertUshort(data, (ushort) command, ref index);
            Marshal.InsertUint(data, 0x00000000, ref index);
            Marshal.InsertUshort(data, 0x0000, ref index);
        }

        /// <summary>
        /// The negotiated proxy connection has received a READY packet, 
        /// </summary>
        /// <param name="res"></param>
        private void ProxyReceivedReady(IAsyncResult res)
        {
            try
            {
                Logging.WriteString("Proxy received READY");
                socket.EndReceive(res);
                OnDirectConnectReady();
            }
            catch (Exception ex)
            {
                OnDirectConnectFailed(ex.Message);
            }
        }

        #endregion

        #region Initiator connection methods

        private void ConnectAsInitiator()
        {
            switch (_method)
            {
                case DirectConnectionMethod.Direct:
                    ConnectAsInitiatorDirect();
                    break;
                case DirectConnectionMethod.Proxied:
                    ConnectAsInitiatorProxied();
                    break;
            }
        }

        private void ConnectAsInitiatorDirect()
        {
            // Start a listening socket and populate the DCI structure with
            // network information
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 0);
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(ipep);

            RendezvousData rd = new RendezvousData();
            Port = (ushort) ((IPEndPoint) _listener.LocalEndPoint).Port;
            _listener.Listen(1);
            _listener.BeginAccept(new AsyncCallback(AcceptConnection), rd);

            // Send the "send file" request on SNAC(04,06):02
            parentSession.Messages.RequestDirectConnection(this);
        }

        private void ConnectAsInitiatorProxied()
        {
            Server = "ars.oscar.aol.com";
            Port = parentSession.LoginPort;

            parentSession.ProxiedSocketFactory(Server, Port, new ProxiedSocketFactoryResultHandler(InitProxyConnectFinished));
        }

        /// <summary>
        /// Completes the initial connection to the proxy server
        /// </summary>
        /// <remarks>This method is used to complete the proxy server transaction for both
        /// Stage 1 sending and Stage 2 receiver-redirect proxy scenarios</remarks>
        private void InitProxyConnectFinished(Socket socket, string errormsg)
        {
            try
            {
                if (!String.IsNullOrEmpty(errormsg))
                {
                    throw new Exception(errormsg);
                }

                this.socket = socket;

                ProxyInitializeSend();
                RendezvousProxyPacket rpp = ReadProxyPacket();
                if (rpp.Command == RendezvousProxyCommand.Acknowledge)
                {
                    using (ByteStream bstream = new ByteStream(rpp.Data))
                    {
                        Port = bstream.ReadUshort();
                        _proxyip = (new IPAddress(bstream.ReadByteArray(4))).ToString();
                    }

                    // Send the "send file" request on SNAC(04,06):02
                    parentSession.Messages.RequestDirectConnection(this);

                    // Wait for the proxy to send its 12 byte READY sequence
                    lock (socket)
                    {
                        socket.BeginReceive(new byte[12], 0, 12, SocketFlags.None, new AsyncCallback(ProxyReceivedReady),
                                           null);
                    }
                }
                else
                {
                    ushort error = (ushort)((rpp.Data[0] << 8) | rpp.Data[1]);
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
                if (DirectConnectionFailed != null)
                {
                    DirectConnectionFailed(ex.Message);
                }
            }
        }

        /// <summary>
        /// Sends a Rendezvous INITRECV and begins sending the file through the proxy connection
        /// </summary>
        internal void StartSendThroughStage2Proxy()
        {
            parentSession.ProxiedSocketFactory(_proxyip, port,
                                         new ProxiedSocketFactoryResultHandler(SendThroughStage2ProxyConnectFinished));
        }

        private void SendThroughStage2ProxyConnectFinished(Socket socket, string errormsg)
        {
            try
            {
                if (!String.IsNullOrEmpty(errormsg))
                {
                    throw new Exception(errormsg);
                }

                this.socket = socket;
                ProxyInitializeReceive();
                RendezvousProxyPacket rpp = ReadProxyPacket();
                if (rpp.Command == RendezvousProxyCommand.Ready)
                {
                    OnDirectConnectReady();
                }
            }
            catch (Exception)
            {
                OnDirectConnectFailed("Proxy refused connection");
            }
        }

        public void StopListeningSocket()
        {
            _controlledlistenerstop = true;
            if (Listener != null)
            {
                Listener.Close();
            }
        }

        private void AcceptConnection(IAsyncResult res)
        {
            try
            {
                socket = _listener.EndAccept(res);
            }
            catch (Exception)
            {
                if (!_controlledlistenerstop)
                {
                    OnDirectConnectFailed("Client closed connection");
                }
                return;
            }
            finally
            {
                try
                {
                    _listener.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                }
                try
                {
                    _listener.Close();
                }
                catch (Exception)
                {
                }
            }

            // Stage one file sending starts
            OnDirectConnectReady();
        }

        #endregion

        #region Receiver connection methods

        /// <summary>
        /// Connect to the sender and begin receiving the file
        /// </summary>
        /// <remarks>If the direct connection fails at this stage, control is passed to <see cref="FallbackToStage2Connection"/></remarks>
        private void StartReceiveThroughDirectConnection()
        {
            parentSession.ProxiedSocketFactory(_verifiedip, port,
                                         new ProxiedSocketFactoryResultHandler(InitReceiveFileConnectFinished));
        }

        /// <summary>
        /// Connection to sender has completed
        /// </summary>
        /// <remarks>
        /// If the socket connection fails at this stage, control is passed to <see cref="FallbackToStage2Connection"/>.
        /// </remarks>
        private void InitReceiveFileConnectFinished(Socket socket, string errormsg)
        {
            if (!String.IsNullOrEmpty(errormsg))
            {
                FallbackToStage2Connection();
                return;
            }

            this.socket = socket;
            OnDirectConnectReady();
        }

        /// <summary>
        /// Set up the connection to receive data through a stage 1 or stage 3 proxy transfer
        /// </summary>
        private void StartReceiveThroughProxy()
        {
            parentSession.ProxiedSocketFactory(_proxyip, parentSession.LoginPort,
                                         new ProxiedSocketFactoryResultHandler(InitProxyReceiveConnectFinished));
        }

        /// <summary>
        /// Completes the final connection to the proxy server
        /// </summary>
        private void InitProxyReceiveConnectFinished(Socket socket, string errormsg)
        {
            if (!String.IsNullOrEmpty(errormsg))
            {
                OnDirectConnectFailed(errormsg);
                return;
            }

            try
            {
                this.socket = socket;
                ProxyInitializeReceive();
                RendezvousProxyPacket rpp = ReadProxyPacket();
                if (rpp.Command == RendezvousProxyCommand.Error)
                {
                    throw new Exception("Proxy server refused connection");
                }
                OnDirectConnectReady();
            }
            catch (Exception ex)
            {
                OnDirectConnectFailed(ex.Message);
            }
        }

        /// <summary>
        /// Direct connection to the sender has failed, attempt a proxy redirect
        /// </summary>
        private void FallbackToStage2Connection()
        {
            _method = DirectConnectionMethod.Proxied;
            _sequence = RendezvousSequence.Stage2;
            parentSession.ProxiedSocketFactory("ars.oscar.aol.com", parentSession.LoginPort,
                                         new ProxiedSocketFactoryResultHandler(InitProxyConnectFinished));
        }

        #endregion
    }
}