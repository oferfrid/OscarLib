/*
 * StreamSocket.cs
 * http://kTools.eu
 * Copyright ©2004-2011, Manfred Kremser
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * Version 0.1.0.0
 */


// Please uncomment the #define, if "Target Framework" is V4.0 or higher
// #define NET40

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Runtime;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 1591

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// The StreamSocket class can be used for network communication and is a mix of the following classes: Socket, NetworkStream, SslStream.
    /// It's accessable Methods and Properties are a selection of this three classes.
    /// The type of the class you can set with <see cref="StreamSocket.StreamType"/>, Ssl or Default.
    /// If Ssl is selected please don't forget to "Authenticate" after connecting.
    /// <remarks>
    /// It's very important, that you DON'T use any kind Exceptions from <see cref="System.Net.Sockets.Socket"/> like SocketException for "Read" and "Write".
    /// Maybe it's better to use the standard Exception for the StreamSocket class.
    /// </remarks>
    /// </summary>
    public class StreamSocket : ISocket, IStream, ISsl, IDisposable
    {
        Socket Socket_ = null;
        Stream Stream_ = null;
        StreamType StreamType_ = StreamType.NetworkStream;

        bool disposed = false;
        Dictionary<IAsyncResult, int> dict = new Dictionary<IAsyncResult, int>();


        #region StreamSocket Properties
        /// <summary>
        /// Accessing to the underlying Socket.
        /// </summary>
        public Socket Socket {
            get { return this.Socket_; }
            private set { this.Socket_ = value; }
        }
        /// <summary>
        /// Accessing to the underlying Stream.
        /// </summary>
        public Stream Stream {
            get { return this.Stream_; }
            private set { this.Stream_ = value; }
        }
        /// <summary>
        /// Read the current selected StreamType.
        /// </summary>
        public StreamType StreamType {
            get { return this.StreamType_; }
            private set { this.StreamType_ = value; }
        }
        #endregion

        #region StreamSocket Constructor / Destructor
        /// <summary>
        /// Initialize a new instance of the StreamSocket class.
        /// </summary>
        /// <param name="addressFamily">One of the <see cref="System.Net.Sockets.AddressFamily"/> values</param>
        /// <param name="socketType">One of the <see cref="System.Net.Sockets.SocketType"/> values</param>
        /// <param name="protocolType">One of the <see cref="System.Net.Sockets.ProtocolType"/> values</param>
        /// <param name="streamType">One of the <see cref="kTools.Net.StreamType"/> values</param>
        public StreamSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, StreamType streamType = StreamType.NetworkStream) {
            this.Socket = new Socket(addressFamily, socketType, protocolType);
            this.StreamType = streamType;
        }
        /// <summary>
        /// Initialize a new instance of the StreamSocket class.
        /// </summary>
        /// <param name="socket">Create the StreamSocket from this Socket</param>
        /// <param name="streamType">One of the <see cref="kTools.Net.StreamType"/> values</param>
        public StreamSocket(Socket socket, StreamType streamType = StreamType.NetworkStream) {
            this.Socket = socket;
            this.StreamType = streamType;
            this.StreamAttach();
        }
        /// <summary>
        /// Initialize a new instance of the StreamSocket class.
        /// </summary>
        /// <param name="streamType">One of the <see cref="kTools.Net.StreamType"/> values</param>
        public StreamSocket(StreamType streamType = StreamType.NetworkStream) {
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.StreamType = streamType;
        }
        /// <summary>
        /// Release all resources.
        /// </summary>
        ~StreamSocket() {
            this.Dispose();
        }
        #endregion

        #region StreamSocket Stream
        private void StreamAttach() {
            if(this.Socket == null) return;

            if(!this.Socket.Connected) return;

            if(this.Stream != null) return;

            switch(this.StreamType) {
                case StreamType.SslStream:
                    this.Stream = new SslStream(new NetworkStream(this.Socket), false);
                    break;
                case StreamType.NetworkStream:
                default:
                    this.Stream = new NetworkStream(this.Socket);
                    break;
            }
        }

        private void StreamDetach() {
            if(this.Stream == null) return;

            this.Stream.Close();
            this.Stream.Dispose();
            this.Stream = null;
        }

        private StreamType getStreamType(StreamType streamType) {
            if(!Enum.IsDefined(StreamType.GetType(), streamType))
                throw new ArgumentException("");

            return (streamType == StreamType.UseParent ? this.StreamType : streamType);
        }
        #endregion

        #region StreamSocket ....
        private int dictRemove(IAsyncResult asyncResult) {
            int result = 0;
            if(this.dict.TryGetValue(asyncResult, out result))
                this.dict.Remove(asyncResult);
            return result;
        }
        #endregion



        #region IStream Properties
        /// <summary>
        /// Gets a Boolean value that indicates whether the underlying stream is readable.
        /// </summary>
        public bool CanRead {
            get { return this.Stream.CanRead; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether the underlying stream is writable.
        /// </summary>
        public bool CanWrite {
            get { return this.Stream.CanWrite; }
        }
        /// <summary>
        /// Gets or sets the amount of time a read operation blocks waiting for data.
        /// </summary>
        public int ReadTimeout {
            get { return this.Socket.ReceiveTimeout; }
            set { this.Socket.ReceiveTimeout = value; }
        }
        /// <summary>
        /// Gets or sets the amount of time a write operation blocks waiting for data.
        /// </summary>
        public int WriteTimeout {
            get { return this.Socket.SendTimeout; }
            set { this.Socket.SendTimeout = value; }
        }
        #endregion

        #region IStream Read
        /// <summary>
        /// Begins an asynchronous read operation that reads data from the stream and stores it in the specified array.
        /// </summary>
        /// <param name="buffer">A Byte array that receives the bytes read from the stream.</param>
        /// <param name="offset">The zero-based location in buffer at which to begin storing the data read from this stream.</param>
        /// <param name="count">The maximum number of bytes to read from the stream.</param>
        /// <param name="callback">An AsyncCallback delegate that references the method to invoke when the read operation is complete.</param>
        /// <param name="state">A user-defined object that contains information about the read operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object that indicates the status of the asynchronous operation.</returns>
        public IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            return this.Stream.BeginRead(buffer, offset, count, callback, state);
        }
        /// <summary>
        /// Ends an asynchronous read operation started with a previous call to BeginRead.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult instance returned by a call to BeginRead.</param>
        /// <returns>A Int32 value that specifies the number of bytes read from the underlying stream.</returns>
        public int EndRead(IAsyncResult asyncResult) {
            return this.Stream.EndRead(asyncResult);
        }
        /// <summary>
        /// Reads data from this stream and stores it in the specified array.
        /// </summary>
        /// <param name="buffer">A Byte array that receives the bytes read from this stream.</param>
        /// <param name="offset">A Int32 that contains the zero-based location in buffer at which to begin storing the data read from this stream.</param>
        /// <param name="count">A Int32 that contains the maximum number of bytes to read from this stream.</param>
        /// <returns>A Int32 value that specifies the number of bytes read. When there is no more data to be read, returns 0.</returns>
        public int Read(byte[] buffer, int offset, int count) {
            return this.Stream.Read(buffer, offset, count);
        }
        /// <summary>
        /// Reads data from this stream and stores it in the specified array.
        /// </summary>
        /// <param name="buffer">A Byte array that receives the bytes read from this stream.</param>
        /// <returns>A Int32 value that specifies the number of bytes read. When there is no more data to be read, returns 0.</returns>
        public int Read(byte[] buffer) {
            return this.Read(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public int ReadByte() {
            return this.Stream.ReadByte();
        }
        #endregion

        #region IStream Write
        /// <summary>
        /// Begins an asynchronous write operation that writes Bytes from the specified buffer to the stream.
        /// </summary>
        /// <param name="buffer">A Byte array that supplies the bytes to be written to the stream.</param>
        /// <param name="offset">The zero-based location in buffer at which to begin reading bytes to be written to the stream.</param>
        /// <param name="count">An Int32 value that specifies the number of bytes to read from buffer.</param>
        /// <param name="callback">An AsyncCallback delegate that references the method to invoke when the write operation is complete. </param>
        /// <param name="state">A user-defined object that contains information about the write operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object indicating the status of the asynchronous operation. </returns>
        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            IAsyncResult result = this.Stream.BeginWrite(buffer, offset, count, callback, state);
            this.dict.Add(result, count);
            return result;
        }
        /// <summary>
        /// Ends an asynchronous write operation started with a previous call to BeginWrite.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult instance returned by a call to BeginWrite.</param>
        /// <returns>The number of bytes sent.</returns>
        public int EndWrite(IAsyncResult asyncResult) {
            this.Stream.EndWrite(asyncResult);
            return this.dictRemove(asyncResult);
        }
        /// <summary>
        /// Writes the specified data to this stream.
        /// </summary>
        /// <param name="buffer">A Byte array that supplies the bytes written to the stream.</param>
        /// <param name="offset">A Int32 that contains the zero-based location in buffer at which to begin reading bytes to be written to the stream.</param>
        /// <param name="count">A Int32 that contains the number of bytes to read from buffer.</param>
        /// <returns>The number of bytes sent.</returns>
        public int Write(byte[] buffer, int offset, int count) {
            this.Stream.Write(buffer, offset, count);
            return count;
        }
        /// <summary>
        /// Writes the specified data to this stream.
        /// </summary>
        /// <param name="buffer">A Byte array that supplies the bytes written to the stream.</param>
        /// <returns>The number of bytes sent.</returns>
        public int Write(byte[] buffer) {
            return this.Write(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <returns>The number of bytes sent.</returns>
        public int WriteByte(byte value) {
            this.Stream.WriteByte(value);
            return 1;
        }
        #endregion

        #region IStream ....
#if NET40
        /// <summary>
        /// Reads the bytes from the current stream and writes them to the destination stream.
        /// </summary>
        /// <param name="destination">The stream that will contain the contents of the current stream.</param>
        public void CopyTo(Stream destination) {
            this.Stream.CopyTo(destination);
        }
        /// <summary>
        /// Reads all the bytes from the current stream and writes them to a destination stream, using a specified buffer size.
        /// </summary>
        /// <param name="destination">The stream that will contain the contents of the current stream.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than zero. The default size is 4096.</param>
        public void CopyTo(Stream destination, int bufferSize) {
            this.Stream.CopyTo(destination, bufferSize);
        }
#endif
        /// <summary>
        /// Causes any buffered data to be written to the underlying device.
        /// </summary>
        public void Flush() {
            this.Stream.Flush();
        }
        #endregion



        #region ISocket Properties
        /// <summary>
        /// Gets the address family of the Socket.
        /// </summary>
        public AddressFamily AddressFamily {
            get { return this.Socket.AddressFamily; }
        }
        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        public int Available {
            get { return this.Socket.Available; }
        }
        /// <summary>
        /// Gets or sets a value that indicates whether the Socket is in blocking mode.
        /// </summary>
        public bool Blocking {
            get { return this.Socket.Blocking; }
            set { this.Socket.Blocking = value; }
        }
        /// <summary>
        /// Gets a value that indicates whether a Socket is connected to a remote host as of the last Send or Receive operation.
        /// </summary>
        public bool Connected {
            get { return this.Socket.Connected; }
        }
        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows Internet Protocol (IP) datagrams to be fragmented.
        /// </summary>
        public bool DontFragment {
            get { return this.Socket.DontFragment; }
            set { this.Socket.DontFragment = value; }
        }
        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket can send or receive broadcast packets.
        /// </summary>
        public bool EnableBroadcast {
            get { return this.Socket.EnableBroadcast; }
            set { this.Socket.EnableBroadcast = value; }
        }
        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the Socket allows only one process to bind to a port.
        /// </summary>
        public bool ExclusiveAddressUse {
            get { return this.Socket.ExclusiveAddressUse; }
            set { this.Socket.ExclusiveAddressUse = value; }
        }
        /// <summary>
        /// Gets the operating system handle for the Socket.
        /// </summary>
        public IntPtr Handle {
            get { return this.Socket.Handle; }
        }
        /// <summary>
        /// Gets a value that indicates whether the Socket is bound to a specific local port.
        /// </summary>
        public bool IsBound {
            get { return this.Socket.IsBound; }
        }
        /// <summary>
        /// Gets or sets a value that specifies whether the Socket will delay closing a socket in an attempt to send all pending data.
        /// </summary>
        public LingerOption LingerState {
            get { return this.Socket.LingerState; }
            set { this.Socket.LingerState = value; }
        }
        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public EndPoint LocalEndPoint {
            get { return this.Socket.LocalEndPoint; }
        }
        /// <summary>
        /// Gets or sets a value that specifies whether outgoing multicast packets are delivered to the sending application.
        /// </summary>
        public bool MulticastLoopback {
            get { return this.Socket.MulticastLoopback; }
            set { this.Socket.MulticastLoopback = value; }
        }
        /// <summary>
        /// Gets or sets a Boolean value that specifies whether the stream Socket is using the Nagle algorithm.
        /// </summary>
        public bool NoDelay {
            get { return this.Socket.NoDelay; }
            set { this.Socket.NoDelay = value; }
        }
        /// <summary>
        /// Gets the protocol type of the Socket.
        /// </summary>
        public ProtocolType ProtocolType {
            get { return this.Socket.ProtocolType; }
        }
        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint {
            get { return this.Socket.RemoteEndPoint; }
        }
        /// <summary>
        /// Gets the type of the Socket.
        /// </summary>
        public SocketType SocketType {
            get { return this.Socket.SocketType; }
        }
        /// <summary>
        /// Gets or sets a value that specifies the Time To Live (TTL) value of Internet Protocol (IP) packets sent by the Socket.
        /// </summary>
        public short Ttl {
            get { return this.Socket.Ttl; }
            set { this.Socket.Ttl = value; }
        }
        /// <summary>
        /// Specifies whether the socket should only use Overlapped I/O mode.
        /// </summary>
        public bool UseOnlyOverlappedIO {
            get { return this.Socket.UseOnlyOverlappedIO; }
            set { this.Socket.UseOnlyOverlappedIO = value; }
        }
        #endregion

        #region ISocket Accept
        /// <summary>
        /// Creates a new Socket for a newly created connection.
        /// </summary>
        /// <param name="streamType">The type of the new created socket.</param>
        /// <returns>A Socket for a newly created connection.</returns>
        public StreamSocket Accept(StreamType streamType = StreamType.UseParent) {
            Socket socket = this.Socket.Accept();
            return new StreamSocket(socket, this.getStreamType(streamType));
        }
        //public bool AcceptAsync(SocketAsyncEventArgs e) {
        //    return this.Socket.AcceptAsync(e);
        //}
        /// <summary>
        /// Overloaded. Begins an asynchronous operation to accept an incoming connection attempt.
        /// </summary>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult object that references the asynchronous Socket object creation.</returns>
        public IAsyncResult BeginAccept(AsyncCallback callback, object state) {
            return this.Socket.BeginAccept(callback, state);
        }
        /// <summary>
        /// Overloaded. Begins an asynchronous operation to accept an incoming connection attempt.
        /// </summary>
        /// <param name="receiveSize">The maximum number of bytes to receive.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult object that references the asynchronous Socket object creation.</returns>
        public IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state) {
            return this.Socket.BeginAccept(receiveSize, callback, state);
        }
        /// <summary>
        /// Overloaded. Begins an asynchronous operation to accept an incoming connection attempt.
        /// </summary>
        /// <param name="acceptSocket">The accepted Socket object. This value may be null.</param>
        /// <param name="receiveSize">The maximum number of bytes to receive.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult object that references the asynchronous Socket object creation.</returns>
        public IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state) {
            return this.Socket.BeginAccept(acceptSocket, receiveSize, callback, state);
        }
        /// <summary>
        /// Overloaded. Asynchronously accepts an incoming connection attempt.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult object that stores state information for this asynchronous operation as well as any user defined data.</param>
        /// <param name="streamType">The type of the new created socket.</param>
        /// <returns>A Socket object to handle communication with the remote host.</returns>
        public StreamSocket EndAccept(IAsyncResult asyncResult, StreamType streamType = StreamType.UseParent) {
            Socket socket = this.Socket.EndAccept(asyncResult);
            return new StreamSocket(socket, this.getStreamType(streamType));
        }
        /// <summary>
        /// Overloaded. Asynchronously accepts an incoming connection attempt.
        /// </summary>
        /// <param name="buffer">An array of type Byte that contains the bytes transferred.</param>
        /// <param name="asyncResult">An IAsyncResult object that stores state information for this asynchronous operation as well as any user defined data.</param>
        /// <param name="streamType">The type of the new created socket.</param>
        /// <returns>A Socket object to handle communication with the remote host.</returns>
        public StreamSocket EndAccept(out byte[] buffer, IAsyncResult asyncResult, StreamType streamType = StreamType.UseParent) {
            Socket socket = this.Socket.EndAccept(out buffer, asyncResult);
            return new StreamSocket(socket, this.getStreamType(streamType));
        }
        /// <summary>
        /// Overloaded. Asynchronously accepts an incoming connection attempt.
        /// </summary>
        /// <param name="buffer">An array of type Byte that contains the bytes transferred.</param>
        /// <param name="bytesTransferred">The number of bytes transferred.</param>
        /// <param name="asyncResult">An IAsyncResult object that stores state information for this asynchronous operation as well as any user defined data.</param>
        /// <param name="streamType">The type of the new created socket.</param>
        /// <returns>A Socket object to handle communication with the remote host.</returns>
        public StreamSocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult, StreamType streamType = StreamType.UseParent) {
            Socket socket = this.Socket.EndAccept(out buffer, out bytesTransferred, asyncResult);
            return new StreamSocket(socket, this.getStreamType(streamType));
        }
        #endregion

        #region ISocket Connect
        /***************** Begin Connect/Disconnect *****************/
        /// <summary>
        /// Overloaded. Begins an asynchronous request for a remote host connection.
        /// </summary>
        /// <param name="remoteEP">An EndPoint that represents the remote host.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state) {
            return this.Socket.BeginConnect(remoteEP, callback, state);
        }
        /// <summary>
        /// Overloaded. Begins an asynchronous request for a remote host connection.
        /// </summary>
        /// <param name="address">The IPAddress of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <param name="requestCallback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state) {
            return this.Socket.BeginConnect(address, port, requestCallback, state);
        }
        /// <summary>
        /// Overloaded. Begins an asynchronous request for a remote host connection.
        /// </summary>
        /// <param name="addresses">At least one IPAddress, designating the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <param name="requestCallback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state) {
            return this.Socket.BeginConnect(addresses, port, requestCallback, state);
        }
        /// <summary>
        /// Overloaded. Begins an asynchronous request for a remote host connection.
        /// </summary>
        /// <param name="host">The name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <param name="requestCallback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state) {
            return this.Socket.BeginConnect(host, port, requestCallback, state);
        }
        /// <summary>
        /// Begins an asynchronous request to disconnect from a remote endpoint.
        /// </summary>
        /// <param name="reuseSocket">true if this socket can be reused after the connection is closed; otherwise, false.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult object that references the asynchronous operation.</returns>
        public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state) {
            return this.Socket.BeginDisconnect(reuseSocket, callback, state);
        }
        /***************** Connect *****************/
        /// <summary>
        /// Overloaded. Establishes a connection to a remote host.
        /// </summary>
        /// <param name="remoteEP">An EndPoint that represents the remote host.</param>
        public void Connect(EndPoint remoteEP) {
            this.Socket.Connect(remoteEP);
            this.StreamAttach();
        }
        /// <summary>
        /// Overloaded. Establishes a connection to a remote host.
        /// </summary>
        /// <param name="address">The IPAddress of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        public void Connect(IPAddress address, int port) {
            this.Socket.Connect(address, port);
            this.StreamAttach();
        }
        /// <summary>
        /// Overloaded. Establishes a connection to a remote host.
        /// </summary>
        /// <param name="addresses">At least one IPAddress, designating the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        public void Connect(IPAddress[] addresses, int port) {
            this.Socket.Connect(addresses, port);
            this.StreamAttach();
        }
        /// <summary>
        /// Overloaded. Establishes a connection to a remote host.
        /// </summary>
        /// <param name="host">The name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        public void Connect(string host, int port) {
            this.Socket.Connect(host, port);
            this.StreamAttach();
        }
        //public bool ConnectAsync(SocketAsyncEventArgs e) {
        //    e.Completed += delegate(object sender, SocketAsyncEventArgs args) {
        //        if (args.SocketError == SocketError.Success)
        //            this.StreamAttach();
        //    };
        //    return this.Socket.ConnectAsync(e);
        //}
        /// <summary>
        /// Ends a pending asynchronous connection request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        public void EndConnect(IAsyncResult asyncResult) {
            this.Socket.EndConnect(asyncResult);
            if(asyncResult.IsCompleted)
                this.StreamAttach();
        }
        /***************** Disconnect *****************/
        /// <summary>
        /// Closes the socket connection and allows reuse of the socket.
        /// </summary>
        /// <param name="reuseSocket">true if this socket can be reused after the current connection is closed; otherwise, false.</param>
        public void Disconnect(bool reuseSocket) {
            this.Socket.Disconnect(reuseSocket);
            this.StreamDetach();
        }
        //public bool DisconnectAsync(SocketAsyncEventArgs e) {
        //    e.Completed += delegate(object sender, SocketAsyncEventArgs args) {
        //        if (args.SocketError == SocketError.Success)
        //            this.StreamDetach();
        //    };
        //    return this.Socket.DisconnectAsync(e);
        //}
        /// <summary>
        /// Ends a pending asynchronous disconnect request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult object that stores state information and any user-defined data for this asynchronous operation.</param>
        public void EndDisconnect(IAsyncResult asyncResult) {
            this.Socket.EndDisconnect(asyncResult);
            if(asyncResult.IsCompleted)
                this.StreamDetach();
        }
        #endregion

        #region ISocket Option
        /// <summary>
        /// Overloaded. Returns the value of a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <returns>An object that represents the value of the option. When the optionName parameter is set to Linger the return value is an instance of the LingerOption class. When optionName is set to AddMembership or DropMembership, the return value is an instance of the MulticastOption class. When optionName is any other value, the return value is an integer.</returns>
        public object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName) {
            return this.Socket.GetSocketOption(optionLevel, optionName);
        }
        /// <summary>
        /// Overloaded. Returns the value of a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionValue">An array of type Byte that is to receive the option setting.</param>
        public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) {
            this.Socket.GetSocketOption(optionLevel, optionName, optionValue);
        }
        /// <summary>
        /// Overloaded. Returns the value of a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionLength">The length, in bytes, of the expected return value.</param>
        /// <returns>An array of type Byte that contains the value of the socket option.</returns>
        public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength) {
            return this.Socket.GetSocketOption(optionLevel, optionName, optionLength);
        }
        /// <summary>
        /// Overloaded. Sets a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionValue">The value of the option, represented as a Boolean.</param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue) {
            this.Socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        /// <summary>
        /// Overloaded. Sets a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionValue">An array of type Byte that represents the value of the option.</param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue) {
            this.Socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        /// <summary>
        /// Overloaded. Sets a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionValue">A value of the option.</param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue) {
            this.Socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        /// <summary>
        /// Overloaded. Sets a Socket option.
        /// </summary>
        /// <param name="optionLevel">One of the SocketOptionLevel values.</param>
        /// <param name="optionName">One of the SocketOptionName values.</param>
        /// <param name="optionValue">A LingerOption or MulticastOption that contains the value of the option.</param>
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue) {
            this.Socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        #endregion

        #region ISocket IOControl
        /// <summary>
        /// Overloaded. Sets low-level operating modes for the Socket.
        /// </summary>
        /// <param name="ioControlCode">An Int32 value that specifies the control code of the operation to perform.</param>
        /// <param name="optionInValue">A Byte array that contains the input data required by the operation.</param>
        /// <param name="optionOutValue">A Byte array that contains the output data returned by the operation.</param>
        /// <returns>The number of bytes in the optionOutValue parameter.</returns>
        public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue) {
            return this.Socket.IOControl(ioControlCode, optionInValue, optionOutValue);
        }
        /// <summary>
        /// Overloaded. Sets low-level operating modes for the Socket.
        /// </summary>
        /// <param name="ioControlCode">A IOControlCode value that specifies the control code of the operation to perform.</param>
        /// <param name="optionInValue">A Byte array that contains the input data required by the operation.</param>
        /// <param name="optionOutValue">A Byte array that contains the output data returned by the operation.</param>
        /// <returns>The number of bytes in the optionOutValue parameter.</returns>
        public int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue) {
            return this.Socket.IOControl(ioControlCode, optionInValue, optionOutValue);
        }
        #endregion

        #region ISocket ....
        /// <summary>
        /// Duplicates the socket reference for the target process, and closes the socket for this process.
        /// </summary>
        /// <param name="targetProcessId">The ID of the target process where a duplicate of the socket reference is created.</param>
        /// <returns>The socket reference to be passed to the target process.</returns>
        public SocketInformation DuplicateAndClose(int targetProcessId) {
            return this.Socket.DuplicateAndClose(targetProcessId);
        }
#if NET40
        /// <summary>
        /// Set the IP protection level on a socket.
        /// </summary>
        /// <param name="level">The IP protection level to set on this socket.</param>
        public void SetIPProtectionLevel(IPProtectionLevel level) {
            this.Socket.SetIPProtectionLevel(level);
        }
#endif
        /// <summary>
        /// Disables sends and receives on a Socket.
        /// </summary>
        /// <param name="how">One of the SocketShutdown values that specifies the operation that will no longer be allowed. </param>
        public void Shutdown(SocketShutdown how) {
            this.Socket.Shutdown(how);
        }

        /// <summary>
        /// Associates a Socket with a local endpoint.
        /// </summary>
        /// <param name="localEP">The local EndPoint to associate with the Socket.</param>
        public void Bind(EndPoint localEP) {
            this.Socket.Bind(localEP);
        }
        /// <summary>
        /// Overloaded. Closes the Socket connection and releases all associated resources.
        /// </summary>
        public void Close() {
            this.Socket.Close();
        }
        /// <summary>
        /// Overloaded. Closes the Socket connection and releases all associated resources.
        /// </summary>
        /// <param name="timeout">Wait up to timeout seconds to send any remaining data, then close the socket.</param>
        public void Close(int timeout) {
            this.Socket.Close(timeout);
        }
        /// <summary>
        /// Places a Socket in a listening state.
        /// </summary>
        /// <param name="backlog">The maximum length of the pending connections queue.</param>
        public void Listen(int backlog) {
            this.Socket.Listen(backlog);
        }
        /// <summary>
        /// Determines the status of the Socket.
        /// </summary>
        /// <param name="microSeconds">The time to wait for a response, in microseconds.</param>
        /// <param name="mode">One of the SelectMode values.</param>
        /// <returns>The status of the Socket based on the polling mode value passed in the mode parameter.</returns>
        public bool Poll(int microSeconds, SelectMode mode) {
            return this.Socket.Poll(microSeconds, mode);
        }
        #endregion



        #region ISsl Properties
        /// <summary>
        /// Gets a Boolean value that indicates whether the certificate revocation list is checked during the certificate validation process.
        /// </summary>
        public bool CheckCertRevocationStatus {
            get { return (this.Stream as SslStream).CheckCertRevocationStatus; }
        }
        /// <summary>
        /// Gets a value that identifies the bulk encryption algorithm used by this SslStream.
        /// </summary>
        public CipherAlgorithmType CipherAlgorithm {
            get { return (this.Stream as SslStream).CipherAlgorithm; }
        }
        /// <summary>
        /// Gets a value that identifies the strength of the cipher algorithm used by this SslStream.
        /// </summary>
        public int CipherStrength {
            get { return (this.Stream as SslStream).CipherStrength; }
        }
        /// <summary>
        /// Gets the algorithm used for generating message authentication codes (MACs).
        /// </summary>
        public HashAlgorithmType HashAlgorithm {
            get { return (this.Stream as SslStream).HashAlgorithm; }
        }
        /// <summary>
        /// Gets a value that identifies the strength of the hash algorithm used by this instance.
        /// </summary>
        public int HashStrength {
            get { return (this.Stream as SslStream).HashStrength; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether authentication was successful. (Overrides AuthenticatedStream.IsAuthenticated.)
        /// </summary>
        public bool IsAuthenticated {
            get { return (this.Stream as SslStream).IsAuthenticated; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether this SslStream uses data encryption. (Overrides AuthenticatedStream.IsEncrypted.)
        /// </summary>
        public bool IsEncrypted {
            get { return (this.Stream as SslStream).IsEncrypted; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether both server and client have been authenticated. (Overrides AuthenticatedStream.IsMutuallyAuthenticated.)
        /// </summary>
        public bool IsMutuallyAuthenticated {
            get { return (this.Stream as SslStream).IsMutuallyAuthenticated; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether the local side of the connection used by this SslStream was authenticated as the server. (Overrides AuthenticatedStream.IsServer.)
        /// </summary>
        public bool IsServer {
            get { return (this.Stream as SslStream).IsServer; }
        }
        /// <summary>
        /// Gets a Boolean value that indicates whether the data sent using this stream is signed. (Overrides AuthenticatedStream.IsSigned.)
        /// </summary>
        public bool IsSigned {
            get { return (this.Stream as SslStream).IsSigned; }
        }
        /// <summary>
        /// Gets the key exchange algorithm used by this SslStream.
        /// </summary>
        public ExchangeAlgorithmType KeyExchangeAlgorithm {
            get { return (this.Stream as SslStream).KeyExchangeAlgorithm; }
        }
        /// <summary>
        /// Gets a value that identifies the strength of the key exchange algorithm used by this instance.
        /// </summary>
        public int KeyExchangeStrength {
            get { return (this.Stream as SslStream).KeyExchangeStrength; }
        }
        /// <summary>
        /// Gets the certificate used to authenticate the local endpoint.
        /// </summary>
        public X509Certificate LocalCertificate {
            get { return (this.Stream as SslStream).LocalCertificate; }
        }
        /// <summary>
        /// Gets the certificate used to authenticate the remote endpoint.
        /// </summary>
        public X509Certificate RemoteCertificate {
            get { return (this.Stream as SslStream).RemoteCertificate; }
        }
        /// <summary>
        /// Gets a value that indicates the security protocol used to authenticate this connection.
        /// </summary>
        public SslProtocols SslProtocol {
            get { return (this.Stream as SslStream).SslProtocol; }
        }
        /// <summary>
        /// Gets the TransportContext used for authentication using extended protection.
        /// </summary>
        public TransportContext TransportContext {
            get { return (this.Stream as SslStream).TransportContext; }
        }
        #endregion

        #region ISsl Authenticate
        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection.
        /// </summary>
        /// <param name="targetHost">The name of the server that will share this SslStream.</param>
        public void AuthenticateAsClient(string targetHost) {
            (this.Stream as SslStream).AuthenticateAsClient(targetHost);
        }
        /// <summary>
        /// Called by clients to authenticate the server and optionally the client in a client-server connection. The authentication process uses the specified certificate collection and SSL protocol.
        /// </summary>
        /// <param name="targetHost">The name of the server that will share this SslStream.</param>
        /// <param name="clientCertificates">The X509CertificateCollection that contains client certificates.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        public void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation) {
            (this.Stream as SslStream).AuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
        }
        /// <summary>
        /// Called by servers to authenticate the server and optionally the client in a client-server connection using the specified certificate.
        /// </summary>
        /// <param name="serverCertificate">The X509Certificate used to authenticate the server.</param>
        public void AuthenticateAsServer(X509Certificate serverCertificate) {
            (this.Stream as SslStream).AuthenticateAsServer(serverCertificate);
        }
        /// <summary>
        /// Called by servers to begin an asynchronous operation to authenticate the server and optionally the client using the specified certificates, requirements and security protocol.
        /// </summary>
        /// <param name="serverCertificate">The X509Certificate used to authenticate the server.</param>
        /// <param name="clientCertificateRequired">A Boolean value that specifies whether the client must supply a certificate for authentication.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        public void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation) {
            (this.Stream as SslStream).AuthenticateAsServer(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation);
        }
        /// <summary>
        /// Called by clients to begin an asynchronous operation to authenticate the server and optionally the client.
        /// </summary>
        /// <param name="targetHost">The name of the server that shares this SslStream.</param>
        /// <param name="asyncCallback">An AsyncCallback delegate that references the method to invoke when the authentication is complete.</param>
        /// <param name="asyncState">A user-defined object that contains information about the operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object that indicates the status of the asynchronous operation.</returns>
        public IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, object asyncState) {
            return (this.Stream as SslStream).BeginAuthenticateAsClient(targetHost, asyncCallback, asyncState);
        }
        /// <summary>
        /// Called by clients to begin an asynchronous operation to authenticate the server and optionally the client using the specified certificates and security protocol.
        /// </summary>
        /// <param name="targetHost">The name of the server that shares this SslStream.</param>
        /// <param name="clientCertificates">The X509CertificateCollection containing client certificates.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        /// <param name="asyncCallback">An AsyncCallback delegate that references the method to invoke when the authentication is complete.</param>
        /// <param name="asyncState">A user-defined object that contains information about the operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object that indicates the status of the asynchronous operation.</returns>
        public IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState) {
            return (this.Stream as SslStream).BeginAuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }
        /// <summary>
        /// Called by servers to begin an asynchronous operation to authenticate the client and optionally the server in a client-server connection.
        /// </summary>
        /// <param name="serverCertificate">The X509Certificate used to authenticate the server.</param>
        /// <param name="asyncCallback">An AsyncCallback delegate that references the method to invoke when the authentication is complete. </param>
        /// <param name="asyncState">A user-defined object that contains information about the operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object that indicates the status of the asynchronous operation. </returns>
        public IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback asyncCallback, object asyncState) {
            return (this.Stream as SslStream).BeginAuthenticateAsServer(serverCertificate, asyncCallback, asyncState);
        }
        /// <summary>
        /// Called by servers to begin an asynchronous operation to authenticate the server and optionally the client using the specified certificates, requirements and security protocol.
        /// </summary>
        /// <param name="serverCertificate">The X509Certificate used to authenticate the server.</param>
        /// <param name="clientCertificateRequired">A Boolean value that specifies whether the client must supply a certificate for authentication.</param>
        /// <param name="enabledSslProtocols">The SslProtocols value that represents the protocol used for authentication.</param>
        /// <param name="checkCertificateRevocation">A Boolean value that specifies whether the certificate revocation list is checked during authentication.</param>
        /// <param name="asyncCallback">An AsyncCallback delegate that references the method to invoke when the authentication is complete. </param>
        /// <param name="asyncState">A user-defined object that contains information about the operation. This object is passed to the asyncCallback delegate when the operation completes.</param>
        /// <returns>An IAsyncResult object that indicates the status of the asynchronous operation. </returns>
        public IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState) {
            return (this.Stream as SslStream).BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }
        /// <summary>
        /// Ends a pending asynchronous server authentication operation started with a previous call to BeginAuthenticateAsServer.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult instance returned by a call to BeginAuthenticateAsServer.</param>
        public void EndAuthenticateAsClient(IAsyncResult asyncResult) {
            (this.Stream as SslStream).EndAuthenticateAsClient(asyncResult);
        }
        /// <summary>
        /// Ends a pending asynchronous client authentication operation started with a previous call to BeginAuthenticateAsClient.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult instance returned by a call to BeginAuthenticateAsClient.</param>
        public void EndAuthenticateAsServer(IAsyncResult asyncResult) {
            (this.Stream as SslStream).EndAuthenticateAsServer(asyncResult);
        }
        #endregion



        #region IDisposable ....
        /// <summary>
        /// Releases all resources.
        /// </summary>
        public void Dispose() {
            if(!this.disposed) {
                if(this.Stream != null) {
                    this.Stream.Close();
                    this.Stream.Dispose();
                    this.Stream = null;
                }

                if(this.Socket != null) {
                    this.Socket.Close();
#if NET40
                    this.Socket.Dispose();
#endif
                    this.Socket = null;
                }

                GC.SuppressFinalize(this);

                this.disposed = true;
            }
        }
        #endregion
    }


    #region Enums
    /// <summary>
    /// Use to define the type of the StreamSocket
    /// </summary>
    public enum StreamType
    {
        /// <summary>
        /// The StreamSocket uses Ssl encryption
        /// </summary>
        SslStream = 0,
        /// <summary>
        /// The StreamSocket uses the standart Stream
        /// </summary>
        NetworkStream = 1,
        /// <summary>
        /// Please don't use this anywhere else except the "Accept"-Methods
        /// </summary>
        UseParent = 100
    }
    #endregion


    #region Interfaces
    public interface ISsl
    {
        //SslStream(Stream innerStream);
        //SslStream(Stream innerStream, bool leaveInnerStreamOpen);
        //SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback);
        //SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback);
        //SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback, EncryptionPolicy encryptionPolicy);

        bool CheckCertRevocationStatus { get; }
        CipherAlgorithmType CipherAlgorithm { get; }
        int CipherStrength { get; }
        HashAlgorithmType HashAlgorithm { get; }
        int HashStrength { get; }
        bool IsAuthenticated { get; }
        bool IsEncrypted { get; }
        bool IsMutuallyAuthenticated { get; }
        bool IsServer { get; }
        bool IsSigned { get; }
        ExchangeAlgorithmType KeyExchangeAlgorithm { get; }
        int KeyExchangeStrength { get; }
        X509Certificate LocalCertificate { get; }
        X509Certificate RemoteCertificate { get; }
        SslProtocols SslProtocol { get; }
        TransportContext TransportContext { get; }

        void AuthenticateAsClient(string targetHost);
        void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation);
        void AuthenticateAsServer(X509Certificate serverCertificate);
        void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation);
        IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, object asyncState);
        IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState);
        IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback asyncCallback, object asyncState);
        IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState);

        void EndAuthenticateAsClient(IAsyncResult asyncResult);
        void EndAuthenticateAsServer(IAsyncResult asyncResult);
    }


    public interface IStream
    {
        Stream Stream { get; }

        //static Stream Synchronized(Stream stream);

        //static readonly Stream Null;
        //bool CanTimeout { get; }
        //protected Stream();

        //virtual void Close();

        bool CanRead { get; }
        bool CanWrite { get; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }


        IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndRead(IAsyncResult asyncResult);
        int Read(byte[] buffer, int offset, int count);
        int Read(byte[] buffer);
        int ReadByte();


        IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndWrite(IAsyncResult asyncResult);
        int Write(byte[] buffer, int offset, int count);
        int Write(byte[] buffer);
        int WriteByte(byte value);

#if NET40
        void CopyTo(Stream destination);
        void CopyTo(Stream destination, int bufferSize);
#endif
        void Flush();
    }


    public interface ISocket
    {
        Socket Socket { get; }

        //static bool OSSupportsIPv4 { get; }
        //static bool OSSupportsIPv6 { get; }
        //static bool SupportsIPv4 { get; }
        //static bool SupportsIPv6 { get; }
        //static void CancelConnectAsync(SocketAsyncEventArgs e);
        //static bool ConnectAsync(SocketType socketType, ProtocolType protocolType, SocketAsyncEventArgs e);
        //static void Select(IList checkRead, IList checkWrite, IList checkError, int microSeconds);

        //void Socket(SocketInformation socketInformation);
        //void Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);

        //int ReceiveBufferSize { get; set; }
        //int ReceiveTimeout { get; set; }
        //int SendBufferSize { get; set; }
        //int SendTimeout { get; set; }

        AddressFamily AddressFamily { get; }
        int Available { get; }
        bool Blocking { get; set; }
        bool Connected { get; }
        bool DontFragment { get; set; }
        bool EnableBroadcast { get; set; }
        bool ExclusiveAddressUse { get; set; }
        IntPtr Handle { get; }
        bool IsBound { get; }
        LingerOption LingerState { get; set; }
        EndPoint LocalEndPoint { get; }
        bool MulticastLoopback { get; set; }
        bool NoDelay { get; set; }
        ProtocolType ProtocolType { get; }
        EndPoint RemoteEndPoint { get; }
        SocketType SocketType { get; }
        short Ttl { get; set; }
        bool UseOnlyOverlappedIO { get; set; }


        StreamSocket Accept(StreamType streamType);
        //bool AcceptAsync(SocketAsyncEventArgs e);
        IAsyncResult BeginAccept(AsyncCallback callback, object state);
        IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state);
        IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state);
        StreamSocket EndAccept(IAsyncResult asyncResult, StreamType streamType);
        StreamSocket EndAccept(out byte[] buffer, IAsyncResult asyncResult, StreamType streamType);
        StreamSocket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult, StreamType streamType);


        IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);
        IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state);
        IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state);
        IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state);
        IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state);
        void Connect(EndPoint remoteEP);
        void Connect(IPAddress address, int port);
        void Connect(IPAddress[] addresses, int port);
        void Connect(string host, int port);
        //bool ConnectAsync(SocketAsyncEventArgs e);
        void Disconnect(bool reuseSocket);
        //bool DisconnectAsync(SocketAsyncEventArgs e);
        void EndConnect(IAsyncResult asyncResult);
        void EndDisconnect(IAsyncResult asyncResult);


        object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName);
        void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
        byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);
        void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);


        int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue);
        int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue);


        SocketInformation DuplicateAndClose(int targetProcessId);
#if NET40
        void SetIPProtectionLevel(IPProtectionLevel level);
#endif
        void Shutdown(SocketShutdown how);


        void Bind(EndPoint localEP);
        void Close();
        void Close(int timeout);
        void Listen(int backlog);
        bool Poll(int microSeconds, SelectMode mode);
    }
    #endregion
}
