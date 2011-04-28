using System;
using System.Collections.Generic;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Provides static methods for processing messages in SNAC family 0x0013 -- SSI service
    /// </summary>
    internal static class SNAC13
    {
        /// <summary>
        /// Sends a request for parameter information -- SNAC(13,02)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void RequestParametersList(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.ServiceParametersRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Processes the parameter information sent by the server -- SNAC(13,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(13,03)</param>
        public static void ProcessParametersList(DataPacket dp)
        {
            List<ushort> maximums = new List<ushort>();
            using (TlvBlock tlvs = new TlvBlock(dp.Data.ReadByteArrayToEnd()))
            {
                using (ByteStream stream = new ByteStream(tlvs.ReadByteArray(0x0004)))
                {
                    while (stream.HasMoreData)
                    {
                        maximums.Add(stream.ReadUshort());
                    }
                }
            }

            // Do something with these capabilities
            dp.ParentSession.Limits.MaxBuddies = maximums[0];
            dp.ParentSession.Limits.MaxGroups = maximums[1];
            dp.ParentSession.Limits.MaxPermits = maximums[2];
            dp.ParentSession.Limits.MaxDenys = maximums[3];

            dp.ParentSession.ParameterSetArrived();
        }

        /// <summary>
        /// Sends a request for the original SSI contact list -- SNAC(13,04)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void RequestInitialContactList(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.ContactListInitialRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        private static bool firstBuddyListPacket = true;

        /// <summary>
        /// Processes the server-stored buddy list -- SNAC(13,06)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(13,06)</param>
        public static void ProcessBuddyList(DataPacket dp)
        {
            byte ssiVersion = dp.Data.ReadByte();
            ushort numItems = dp.Data.ReadUshort();

            if (firstBuddyListPacket)
            {
                dp.ParentSession.SSI.ResetContactList();
                firstBuddyListPacket = false;
            }
            dp.ParentSession.SSI.PreallocateLists(numItems);
            for (int i = 0; i < numItems; i++)
            {
                SSIItem item = dp.Data.ReadSSIItem();
                dp.ParentSession.SSI.AddSSIItem(item);
            }

            DateTime lastModifiedTime = dp.Data.ReadDateTime();


            // Thanks to smartmlp for noticing this!
            // The buddy list can be sent in multiple SNACs, but only the last
            // in the series will have its LSB set to zero. Don't trigger other
            // events until that happens.
            if ((dp.SNAC.Flags & 0x0001) == 0x0000)
            {
                firstBuddyListPacket = true;
                if (!dp.ParentSession.LoggedIn)
                {
                    dp.ParentSession.OnLoginStatusUpdate("Contact list received", 1.0);
                    // Send a couple last SNACs, then tell the server that we're ready to go
                    dp.ParentSession.Services.SendReadyNotification(dp.ParentConnection);
                }
                dp.ParentSession.OnContactListFinished(lastModifiedTime);
            }
        }

        /// <summary>
        /// Requests that the server activate the client's SSI information -- SNAC(13,07)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <remarks>
        /// Sending this SNAC will cause the server to begin broadcasting online notifications
        /// to the contacts on the client's buddy list.
        /// </remarks>
		public static void ActivateSSI(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.LoadContactList;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Adds a series of SSI items to the server-side list -- SNAC(13,08)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="items">An array of <see cref="SSIItem"/> objects</param>
		public static void AddSSIItems(ISession sess, SSIItem[] items)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.SSIEditAddItems;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();

            stream.WriteSSIItems(items);
            sess.SSI.OutstandingRequests++;
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }


        /// <summary>
        /// Processes a notification of SSI items added remotely -- SNAC(13,08)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> containing SNAC(13,08)</param>
        public static void ProcessItemsAdded(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                SSIItem newitem = dp.Data.ReadSSIItem();
                dp.ParentSession.SSI.AddSSIItem(newitem);
            }
        }

        /// <summary>
        /// Modifies a series of SSI items on the server-side list -- SNAC(13,09)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
        /// <param name="items">An array of <see cref="SSIItem"/> objects</param>
		public static void ModifySSIItems(ISession sess, SSIItem[] items)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.SSIEditUpdateGroupHeader;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteSSIItems(items);
            sess.SSI.OutstandingRequests++;
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes a notification of SSI items modified remotely -- SNAC(13,09)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> containing SNAC(13,09)</param>
        public static void ProcessItemsModified(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                SSIItem moditem = dp.Data.ReadSSIItem();
                dp.ParentSession.SSI.UpdateSSIItem(moditem);
            }
        }

        /// <summary>
        /// Processes a notification of SSI items removed remotely -- SNAC(13,0A)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> containing SNAC(13,0A)</param>
        public static void ProcessItemsRemoved(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                SSIItem removeitem = dp.Data.ReadSSIItem();
                dp.ParentSession.SSI.RemoveSSIItem(removeitem);
            }
        }

        /// <summary>
        /// Removes a series of SSI items from the server-side list -- SNAC(13,0A)
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="items">An array of <see cref="SSIItem"/> objects</param>
		public static void RemoveSSIItems(ISession sess, SSIItem[] items)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.SSIEditRemoveItem;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteSSIItems(items);
            sess.SSI.OutstandingRequests++;
            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes the response codes from an SSI update -- SNAC(13,0E)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(13,0E)</param>
        public static void ProcessSSIUpdateResponse(DataPacket dp)
        {
            while (dp.Data.HasMoreData)
            {
                ushort errorCode = dp.Data.ReadUshort();
                if (errorCode != 0x0000 && errorCode != 0x0004)
                {
                    ServerErrorCode sec = ServerErrorCode.UnknownError;
                    switch (errorCode)
                    {
                        case 0x0002:
                            sec = ServerErrorCode.SSIItemNotFound;
                            break;
                        case 0x0003:
                            sec = ServerErrorCode.SSIItemAlreadyExists;
                            break;
                        case 0x000A:
                            sec = ServerErrorCode.SSICantAddItem;
                            break;
                        case 0x000C:
                            sec = ServerErrorCode.SSIItemLimitExceeded;
                            break;
                        case 0x000E:
                            sec = ServerErrorCode.SSIItemRequiresAuthorization;
                            break;
                    }

                    dp.ParentSession.OnWarning(sec);
                }
            }

            dp.ParentSession.SSI.OutstandingRequests--;
            Logging.DumpFLAP(new byte[] {},
                             "Outstanding SSI requests: " + dp.ParentSession.SSI.OutstandingRequests.ToString());
            if (dp.ParentSession.SSI.OutstandingRequests <= 0)
            {
                dp.ParentSession.SSI.OutstandingRequests = 0;
                dp.ParentSession.OnSSIEditComplete();
            }
        }

        /// <summary>
        /// Processes the "Local SSI is up to date" message from the server -- SNAC(13,0F)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(13,0F)</param>
        public static void ProcessSSIUpToDate(DataPacket dp)
        {
            dp.ParentSession.OnContactListFinished(dp.Data.ReadDateTime());
        }

        /// <summary>
        /// Starts an SSI modification transaction -- SNAC(13,11)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void SendEditStart(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.ContactsEditStart;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Finishes an SSI modification transaction -- SNAC(13,12)
        /// </summary>
		/// <param name="sess">A <see cref="ISession"/> object</param>
		public static void SendEditComplete(ISession sess)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.ContactsEditEnd;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, new ByteStream()));
        }

        /// <summary>
        /// Processes a "You Were Added" notification sent by the server -- SNAC(13,1C)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(13,1C)</param>
        /// <remarks>
        /// The server only sends this SNAC to ICQ clients, so the packet is passed to to the
        /// <see cref="IcqManager"/> for dealing-with
        /// </remarks>
        public static void ProcessAddedMessage(DataPacket dp)
        {
            dp.ParentSession.ICQ.ProcessIncomingPacket(dp);
        }

        /// <summary>
        /// Sends the authorization request to another client
        /// </summary>
        /// <param name="sess">the session object</param>
        /// <param name="screenname">the destination screenname</param>
        /// <param name="reason">the reason for the authorization request</param>
		public static void SendAuthorizationRequest(ISession sess, string screenname, string reason)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort)SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort)SSIService.SendAuthorizationRequest;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();

            stream.WriteByte((byte)Encoding.ASCII.GetByteCount(screenname));
            stream.WriteString(screenname, Encoding.ASCII);

            stream.WriteUshort((ushort)sess.Encoding.GetByteCount(reason));
            stream.WriteString(reason, sess.Encoding);

            stream.WriteUshort(0x0000);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes a authorization request snac
        /// </summary>
        /// <param name="dp">the data packet of the snac</param>
        public static void ProcessAuthorizationRequest(DataPacket dp)
        {
            string screenname = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            string reason = null;

            reason = dp.Data.ReadString(dp.Data.ReadUshort(), Session.CurrentSession.Encoding);

            dp.ParentSession.OnAuthorizationRequestReceived(screenname, reason);
        }

        /// <summary>
        /// Sends a response for a authorization request
        /// </summary>
        /// <param name="sess">the session object</param>
        /// <param name="screenname">the destination screenname</param>
        /// <param name="grantAuthorization">Determines if the authorization is granted</param>
        /// <param name="reason">The reason for this decision</param>
		public static void SendAuthorizationResponse(ISession sess, string screenname, bool grantAuthorization,
                                                     string reason)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.SendAuthorizationResponse;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();

            stream.WriteByte((byte) Encoding.ASCII.GetByteCount(screenname));
            stream.WriteString(screenname, Encoding.ASCII);

            stream.WriteByte(Convert.ToByte(grantAuthorization));

            stream.WriteUshort((ushort) sess.Encoding.GetByteCount(reason));
            stream.WriteString(reason, sess.Encoding);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes an authorization response snac
        /// </summary>
        /// <param name="dp">the data packet of the snac</param>
        public static void ProcessAuthorizationResponse(DataPacket dp)
        {
            string screenname = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            bool authorized = Convert.ToBoolean(dp.Data.ReadByte());
            string reason = dp.Data.ReadString(dp.Data.ReadUshort(), Session.CurrentSession.Encoding);

            dp.ParentSession.OnAuthorizationResponseReceived(screenname, authorized, reason);
        }

        /// <summary>
        /// Allows the authorization to another client in the future
        /// </summary>
        /// <param name="sess">the session object</param>
        /// <param name="screenname">the destination screenname</param>
        /// <param name="reason">the reason for the future authorization</param>
		public static void SendFutureAuthorizationGrant(ISession sess, string screenname, string reason)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.SendFutureAuthorizationGrant;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();

            stream.WriteByte((byte) Encoding.ASCII.GetByteCount(screenname));
            stream.WriteString(screenname, Encoding.ASCII);

            stream.WriteUshort((ushort) sess.Encoding.GetByteCount(reason));
            stream.WriteString(reason, sess.Encoding);

            stream.WriteUshort(0x0000);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }

        /// <summary>
        /// Processes a authorization request snac
        /// </summary>
        /// <param name="dp">the data packet of the snac</param>
        public static void ProcessFutureAuthorizationGrant(DataPacket dp)
        {
            string screenname = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            string reason = dp.Data.ReadString(dp.Data.ReadUshort(), Session.CurrentSession.Encoding);

            dp.ParentSession.OnAuthorizationRequestReceived(screenname, reason);
        }

        /// <summary>
        /// Requests for the server side contact list.
        /// </summary>
        /// <param name="sess">the session object</param>
        /// <param name="modificationDate">the last client side modification date</param>
        /// <param name="isLocalTime">Determines if the given modification time is in the local or universal time format</param>
        /// <param name="itemCount">the total item count at the client side</param>
		public static void SendContactListCheckout(ISession sess, DateTime modificationDate, bool isLocalTime,
                                                   ushort itemCount)
        {
            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = (ushort) SNACFamily.SSIService;
            sh.FamilySubtypeID = (ushort) SSIService.ContactListCheckout;
            sh.Flags = 0x0000;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            uint modDateValue = ByteStream.ConvertDateTimeToUint(modificationDate, isLocalTime);
            stream.WriteUint(modDateValue);

            stream.WriteUshort(itemCount);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(sess, sh, stream));
        }
    }
}