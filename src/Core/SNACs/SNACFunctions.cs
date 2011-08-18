using System;
using System.Globalization;
using System.Text;

namespace csammisrun.OscarLib.Utility
{
    /// <summary>
    /// Defines a handler for an incoming SNAC
    /// </summary>
    /// <param name="dp">The <see cref="DataPacket"/> containing the SNAC</param>
    internal delegate void SNACHandler(DataPacket dp);

    /// <summary>
    /// Provides a set of static methods for dispatching incoming and outgoing SNAC data
    /// </summary>
    internal class SNACFunctions
    {
        /// <summary>
        /// Reads the error code and subcode from a SNAC(XX,01) packet
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(XX,01)</param>
        /// <param name="errorCode">The main error code contained in the SNAC</param>
        /// <param name="subCode">The subcode contained in the SNAC, or 0 if there is no subcode present</param>
        public static void GetSNACErrorCodes(DataPacket dp, out ushort errorCode, out ushort subCode)
        {
            errorCode = dp.Data.ReadUshort();
            subCode = 0;
            if (dp.Data.HasMoreData)
            {
                using (TlvBlock tlvs = dp.Data.ReadTlvBlockByLength(dp.Data.GetRemainingByteCount()))
                {
                    if (tlvs.HasTlv(0x0008))
                    {
                        subCode = tlvs.ReadUshort(0x0008);
                    }
                }
            }
        }

        /// <summary>
        /// Processes an error notification -- SNAC(XX,01)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object containing SNAC(XX,01)</param>
        public static void ProcessErrorNotification(DataPacket dp)
        {
            ushort errorcode = dp.Data.ReadUshort();

            /*
               * The Shutko documentation says that there should be an error subcode here
               * in TLV 0x0008, but the one time I've seen this (testing chat connections),
               * 0x00040003 came back to me
               */

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.CurrentCulture.NumberFormat,
                            "SNAC(0x{0:x4}, 0x0001) received: {1}", dp.SNAC.FamilyServiceID, (ServerErrorCode) errorcode);

            dp.ParentSession.OnWarning((ServerErrorCode) errorcode, dp);
        }

        #region SNAC enqueuing

        /// <summary>
        /// Enqueues a SNAC for transmission with the appropriate RateClass
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        /// <remarks>
        /// <para>
        /// This needs commenting
        /// </para>
        /// </remarks>
        public static void BuildFLAP(DataPacket dp)
        {
            // Build the FLAP header
            dp.FLAP.DatagramSequenceNumber = 0; // Gets bound in Connection.cs
            dp.FLAP.Channel = (byte) FLAPChannels.SNACData;

            // Build the size of the data to be sent, not counting the FLAP header
            dp.FLAP.DataSize = (ushort) (10 + dp.Data.GetByteCount());
            dp.Data.PrependOscarHeaders(dp.FLAP, dp.SNAC);
            SNACFunctions.DispatchToRateClass(dp);
        }

        /// <summary>
        /// Send a data packet to the rate class that will send it
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        public static void DispatchToRateClass(DataPacket dp)
        {
            if (dp.ParentConnection == null)
            {
                dp.ParentConnection = dp.ParentSession.Connections.GetByFamily(
                    dp.SNAC.FamilyServiceID);
                if (dp.ParentConnection == null || dp.ParentConnection.ReadyForData == false)
                {
                    dp.ParentSession.Connections.AddDelayedPacket(dp.SNAC.FamilyServiceID, dp);
                    return;
                }
            }

            int key = (int) (dp.SNAC.FamilyServiceID << 16) | dp.SNAC.FamilySubtypeID;
            RateClass rc = dp.ParentSession.RateClasses[key];
            if (rc != null)
            {
                rc.Enqueue(dp);
            }
            else
            {
                dp.ParentConnection.SendFLAP(dp.Data.GetBytes());
            }
        }

        #endregion

        #region SNAC processing

        /// <summary>
        /// Dispatch a newly received SNAC to a handler function
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object</param>
        /// <remarks>
        /// This function is HUGE! NDisplay gave it an atrocious complexity rating, but
        /// it's as straightforward as a giant select() statement can be.
        /// </remarks>
        [Obsolete("Do not add to this method.  Implement an ISnacFamilyHandler and use Session.Dispatcher.")]
        public static void ProcessSNAC(DataPacket dp)
        {
            switch ((SNACFamily) dp.SNAC.FamilyServiceID)
            {
                case SNACFamily.DirectoryUserSearch:
                    {
                        DirectorySearch sub = (DirectorySearch) dp.SNAC.FamilySubtypeID;
                        switch (sub)
                        {
                            case DirectorySearch.ClientServerError: // 0x0001
                                SNACFunctions.ProcessErrorNotification(dp);
                                break;
                            case DirectorySearch.SearchUserResponse: // 0x0003
                                SNAC0F.ProcessSearchResults(dp);
                                break;
                            case DirectorySearch.InterestsListResponse: // 0x0005
                                SNAC0F.ProcessInterestList(dp);
                                break;
                        }
                    }
                    break;
                case SNACFamily.LocationService:
                    {
                        LocationServices sub = (LocationServices) dp.SNAC.FamilySubtypeID;
                        switch (sub)
                        {
                            case LocationServices.ClientServerError: // 0x0001
                                SNACFunctions.ProcessErrorNotification(dp);
                                break;
                            case LocationServices.UpdateDirectoryInfoResponse: // 0x000A
                                SNAC02.ProcessUpdateResult(dp);
                                break;
                            case LocationServices.SNAC020BReply:
                                SNAC02.ProcessSelfLocationReply(dp); // 0x000C
                                break;
                            case LocationServices.UpdateInterestsResponse: // 0x0010
                                SNAC02.ProcessUpdateResult(dp);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case SNACFamily.PrivacyManagementService:
                    {
                        PrivacyManagementService sub = (PrivacyManagementService) dp.SNAC.FamilySubtypeID;
                        switch (sub)
                        {
                            case PrivacyManagementService.ClientServerError: // 0x0001
                                SNACFunctions.ProcessErrorNotification(dp);
                                break;
                            case PrivacyManagementService.ServiceParametersRequest: // 0x0002
                                SNAC09.ProcessParametersListRequest(dp);
                                break;
                            case PrivacyManagementService.ServiceParametersResponse: // 0x0003
                                SNAC09.ProcessParametersList(dp);
                                break;
                            case PrivacyManagementService.ServiceError:
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case SNACFamily.SSIService:
                    {
                        SSIService sub = (SSIService) dp.SNAC.FamilySubtypeID;
                        switch (sub)
                        {
                            case SSIService.ClientServerError: // 0x0001
                                SNACFunctions.ProcessErrorNotification(dp);
                                break;
                            case SSIService.ServiceParametersResponse: // 0x0003
                                SNAC13.ProcessParametersList(dp);
                                break;
                            case SSIService.ContactListResponse: // 0x0006
                                SNAC13.ProcessBuddyList(dp);
                                break;
                            case SSIService.SSIEditAddItems: // 0x0008
                                SNAC13.ProcessItemsAdded(dp);
                                break;
                            case SSIService.SSIEditUpdateGroupHeader: // 0x0009
                                SNAC13.ProcessItemsModified(dp);
                                break;
                            case SSIService.SSIEditRemoveItem: // 0x000A
                                SNAC13.ProcessItemsRemoved(dp);
                                break;
                            case SSIService.SSIEditAcknowledge: // 0x000E
                                SNAC13.ProcessSSIUpdateResponse(dp);
                                break;
                            case SSIService.LocalSSIUpToDate: // 0x000F
                                SNAC13.ProcessSSIUpToDate(dp);
                                break;
                            case SSIService.YouWereAddedMessage: // 0x001C
                                SNAC13.ProcessAddedMessage(dp);
                                break;
                            case SSIService.AuthorizationResponse: // 0x001B
                                SNAC13.ProcessAuthorizationResponse(dp);
                                break;
                            case SSIService.FutureAuthorizationGranted: // 0x0015
                                SNAC13.ProcessFutureAuthorizationGrant(dp);
                                break;
                            case SSIService.AuthorizationRequest: //0x0019
                                SNAC13.ProcessAuthorizationRequest(dp);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                 default:
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat(
                        System.Globalization.CultureInfo.CurrentCulture.NumberFormat,
                        "Unknown SNAC: ({0:x4},{1:x4}), flags = {2:x4}, requestID = {3:x8}",
                        dp.SNAC.FamilyServiceID,
                        dp.SNAC.FamilySubtypeID,
                        dp.SNAC.Flags,
                        dp.SNAC.RequestID);
                    Logging.DumpFLAP(dp.Data.GetBytes(), sb.ToString());
                    break;
            }
        }

        #endregion
    }
}