using System;
using System.Collections.Generic;
using System.Text;
using csammisrun.OscarLib;

namespace OscarLib.OscarLibConsole
{
    /// <summary>
    /// Contains the program's entry point
    /// </summary>
    public class MainClass
    {
        [STAThread]
        public static void Main()
        {
            Console.WriteLine("Screenname?");
            string name = Console.ReadLine();
            Console.WriteLine(String.Format("Password for {0}:", name));
            string password = Console.ReadLine();
            Console.WriteLine("Beginning login sequence");
            MainClass mc = new MainClass(name, password);
            mc.Go();
        }

        private Session sess;
        private DateTime start;
        FullUserInfo myui;
        Cookie filecookie;

        public MainClass(string name, string pw)
        {
            sess = new Session(name, pw);
            sess.InitializeLogger(Environment.CurrentDirectory);

            sess.ClientCapabilities = Capabilities.SendFiles | Capabilities.OscarLib | Capabilities.AIMtoICQ;
            sess.SetDefaultIdentification();

            sess.LoginCompleted += new LoginCompletedHandler(sess_LoginCompleted);
            sess.ErrorMessage += new ErrorMessageHandler(sess_ErrorMessage);
            sess.WarningMessage += new WarningMessageHandler(sess_WarningMessage);
            sess.StatusUpdate += new InformationMessageHandler(sess_StatusUpdate);
            sess.LoginFailed += new LoginFailedHandler(sess_LoginFailed);

            sess.Statuses.UserStatusReceived += new UserStatusReceivedHandler(sess_UserStatusReceived);
            sess.Statuses.UserInfoReceived += new UserInfoReceivedHandler(sess_UserInfoReceived);

            sess.MasterGroupItemReceived += new MasterGroupItemReceivedHandler(sess_MasterGroupItemReceived);
            sess.GroupItemReceived += new GroupItemReceivedHandler(sess_GroupItemReceived);
            sess.BuddyItemReceived += new BuddyItemReceivedHandler(sess_BuddyItemReceived);
            sess.ContactListFinished += new ContactListFinishedHandler(sess_ContactListFinished);

            sess.ChatInvitationReceived += new ChatInvitationReceivedHandler(sess_ChatInvitationReceived);
            sess.ChatRooms.ChatRoomJoined += new ChatRoomJoinedHandler(sess_ChatRoomCreated);

            sess.DirectIMRequestReceived += new DirectIMRequestReceivedHandler(sess_DirectIMRequestReceived);
            sess.DirectIMReceived += new DirectIMReceivedHandler(sess_DirectIMReceived);

            sess.Messages.TypingNotification += new TypingNotificationEventHandler(sess_TypingNotification);
            sess.Messages.MessageReceived += new MessageReceivedHandler(sess_MessageReceived);
            sess.Messages.OfflineMessagesReceived += new OfflineMessagesReceivedEventHandler(ICQ_OfflineMessagesReceived);
            sess.Messages.MessageDeliveryUpdate += new MessageDeliveryUpdateEventHandler(Messages_MessageDeliveryUpdate);

            sess.Graphics.AutoSaveLocation = Environment.CurrentDirectory;
            sess.Graphics.BuddyIconDownloaded += new BuddyIconDownloadedHandler(Graphics_BuddyIconDownloaded);
            sess.Graphics.BuddyIconUploadCompleted += new BuddyIconUploadCompletedHandler(Graphics_BuddyIconUploadCompleted);
            sess.Graphics.BuddyIconUploadFailed += new BuddyIconUploadFailedHandler(Graphics_BuddyIconUploadFailed);

            sess.FileTransferProgress += new FileTransferProgressHandler(sess_FileTransferProgress);
            sess.FileTransferCompleted += new FileTransferCompletedHandler(sess_FileTransferCompleted);
            sess.FileTransferRequestReceived += new FileTransferRequestReceivedHandler(sess_FileTransferRequestReceived);
            sess.FileTransferCancelled += new FileTransferCancelledHandler(sess_FileTransferCancelled);

            sess.ICQ.ShortUserInfoReceived += new ShortUserInfoReceivedHandler(Icq_ShortUserInfoReceived);
            sess.ICQ.FullUserInfoReceived += new FullUserInfoReceivedHandler(Icq_FullUserInfoReceived);

            Login(sess);
        }

        void Icq_FullUserInfoReceived(object sender, FullUserInfo ui)
        {
            myui = ui;
            TimeSpan end = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
            Console.WriteLine("FullUserInfo from Nickname: {0}, Firstname: {1}, Millisec: {2}", ui.Nickname, ui.Firstname, end.TotalMilliseconds);
        }

        void Icq_ShortUserInfoReceived(object sender, ShortUserInfo ui)
        {
            TimeSpan end = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
            Console.WriteLine("ShortUserInfo from Nickname: {0}, Firstname: {1}, Millisec: {2}", ui.Nickname, ui.Firstname, end.TotalMilliseconds);
        }

        void Messages_MessageDeliveryUpdate(object sender, MessageStatusEventArgs e)
        {
            Console.WriteLine("Message to {0} has status {1}", e.Destination, e.Status);
        }

        void Graphics_BuddyIconDownloaded(object sender, BuddyIconDownloadedEventArgs e)
        {
            Console.WriteLine("Wrote icon for {0} to {1}", e.ScreenName, e.IconFile);
        }

        void Graphics_BuddyIconUploadFailed(object sender, BuddyIconUploadFailedArgs e)
        {
            Console.WriteLine("Icon upload failed: {0}", e.ErrorCode);
        }

        void Graphics_BuddyIconUploadCompleted(object sender, BuddyIconUploadCompletedArgs e)
        {
            Console.WriteLine("Icon upload succeeded: {0}", e.BartID.ToString());
        }

        private void ICQ_OfflineMessagesReceived(object sender, OfflineMessagesReceivedEventArgs e)
        {
            Console.WriteLine("Received {0} messages for {1}", e.ReceivedMessages.Count, e.ScreenName);
            foreach (OfflineIM oim in e.ReceivedMessages)
            {
                Console.WriteLine("{0}: {1}", oim.ReceivedOn, oim.Message);
            }
        }

        private void sess_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("ICBM message received from {0}: {1}", e.Message.ScreenName, e.Message.Message);
        }

        private void sess_TypingNotification(object sender, TypingNotificationEventArgs e)
        {
            Console.WriteLine("Typing notification received from {0}: {1}", e.ScreenName, e.Notification);
        }

        private void sess_DirectIMReceived(Session sess, DirectIM message)
        {
            Console.WriteLine("DirectIM Message from {1}: {0}", message.Message, message.ScreenName);
            sess.Messages.SendMessage(message.ScreenName, "Your momma!", OutgoingMessageFlags.None);
        }

        private void sess_DirectIMRequestReceived(Session sess, UserInfo sender, string message, Cookie key)
        {
            Console.WriteLine("Auto-accepting DIM invite from {0} ({1})", sender.ScreenName, message);
            sess.AcceptDirectIMSession(key);
        }

        private void sess_FileTransferRequestReceived(Session sess, UserInfo sender, string IP, string filename, uint filesize, string message, Cookie key)
        {
            filecookie = key;
            Console.WriteLine("Auto-accepting " + filename);
            sess.AcceptFileTransfer(key, "C:\\recv_" + filename);
        }

        private void sess_FileTransferCancelled(Session sess, UserInfo other, Cookie cookie, string reason)
        {
            filecookie = null;
            Console.WriteLine("FileTransferCancelled " + reason + " " + other.ScreenName);
        }

        private void sess_FileTransferCompleted(Session sess, Cookie cookie)
        {
            filecookie = null;
            Console.WriteLine("File transfer of {0} complete", cookie.ToString());
        }

        private void sess_FileTransferProgress(Session sess, Cookie cookie, uint BytesTransfered, uint BytesTotal)
        {
            Console.WriteLine("Transfered {0} of {1} of {2}", BytesTransfered, BytesTotal, cookie.ToString());
        }

        private void sess_UserInfoReceived(object sender, UserInfoResponse info)
        {
            Console.WriteLine("Got extended info for " + info.Info.ScreenName);
            if (info.Info.Icon != null)
            {
                //Console.WriteLine("Got valid icon info for " + info.Info.ScreenName);
            }
        }

        private void sess_UserStatusReceived(object sender, UserInfo userinfo)
        {
            Console.WriteLine("{0} has come online", userinfo.ScreenName);
            if (userinfo.Icon != null)
            {
                //Console.WriteLine("Got valid icon info for " + userinfo.ScreenName);
                //sess.Graphics.DownloadBuddyIcon(userinfo.ScreenName, userinfo.Icon);
            }
            else
            {
                sess.Statuses.RequestBasicUserInfo(userinfo.ScreenName, BasicUserInfoRequest.GeneralInfo);
            }
        }

        public void Go()
        {
            string line;
            while ((line = Console.ReadLine()) != "quit")
            {
                start = DateTime.Now;
                if (line.Equals("filesend"))
                {
                    filecookie = sess.SendFile("560280115", "C:\\DevTools.exe");
                }
                else if (line.Equals("filesendproxy")) 
                {
                    filecookie = sess.SendFileProxied("560280115", "C:\\DevTools.exe");
                }
                else if (line.Equals("filecancel"))
                {
                    if (filecookie != null)
                        sess.CancelFileTransfer(filecookie);
                }
                else if (line.Equals("upicon"))
                {
                    sess.Graphics.UploadBuddyIcon("C:\\testavatar.jpg");
                }
                else if (line.Equals("addbuddy"))
                {
                    Console.WriteLine("Name?");
                    string buddyname = Console.ReadLine();
                    Console.WriteLine("Group ID?");
                    ushort parentID = ushort.Parse(Console.ReadLine());
                    Console.WriteLine("Index?");
                    int index = int.Parse(Console.ReadLine());

                    sess.AddBuddy(buddyname, parentID, index, "", "", "", "", "", false, "");
                }
                else if (line.Equals("addgroup"))
                {
                    Console.WriteLine("Name?");
                    string groupname = Console.ReadLine();
                    Console.WriteLine("Index?");
                    int index = int.Parse(Console.ReadLine());

                    sess.AddGroup(groupname, index);
                }
                else if (line.Equals("movebuddy"))
                {
                    Console.WriteLine("ID?");
                    ushort buddyID = ushort.Parse(Console.ReadLine());
                    Console.WriteLine("Group ID?");
                    ushort parentID = ushort.Parse(Console.ReadLine());
                    Console.WriteLine("Index?");
                    int index = int.Parse(Console.ReadLine());

                    sess.MoveBuddy(buddyID, parentID, index);
                }
                else if (line.Equals("movegroup"))
                {
                    Console.WriteLine("ID?");
                    ushort groupID = ushort.Parse(Console.ReadLine());
                    Console.WriteLine("Index?");
                    int index = int.Parse(Console.ReadLine());

                    sess.MoveGroup(groupID, index);
                }
                else if (line.Equals("removebuddy"))
                {
                    Console.WriteLine("ScreenName?");
                    SSIBuddy buddy = sess.SSI.GetBuddyByName(Console.ReadLine());

                    if (buddy != null)
                    {
                        sess.SSI.RemoveBuddy(buddy);
                        sess.SSI.RemoveYourself(buddy.Name);
                    }
                }
                else if (line.Equals("removegroup"))
                {
                    Console.WriteLine("ID?");
                    ushort groupID = ushort.Parse(Console.ReadLine());

                    sess.RemoveGroup(groupID);
                }
                else if (line.Equals("offline"))
                {
                    Console.WriteLine("Retrieving offline messages");
                    sess.Messages.RetrieveOfflineMessages();
                }
                else if (line.Equals("sendoffline"))
                {
                    Console.WriteLine("Who?");
                    string destination = Console.ReadLine();
                    sess.Messages.SendMessage(destination, "An offline message", OutgoingMessageFlags.DeliverOffline);
                }
                else if (line.Equals("send"))
                {
                    Console.WriteLine("Where?");
                    string destination = Console.ReadLine();
                    sess.Messages.SendMessage(destination, "A test message");
                }
                else if (line.Equals("shortinfo"))
                {
                    Console.WriteLine("Who?");
                    sess.ICQ.RequestShortUserInfo(Console.ReadLine());
                }
                else if (line.Equals("fullinfo"))
                {
                    Console.WriteLine("Who?");
                    sess.ICQ.RequestFullUserInfo(Console.ReadLine());
                }
                else if (line.Equals("setpass"))
                {
                    Console.WriteLine("New password?");
                    sess.ICQ.SendNewPassword(Console.ReadLine());
                }
                else if (line.Equals("updatebuddy"))
                {
                    SSIBuddy buddy = sess.SSI.GetBuddyByName("560280115");
                    Console.WriteLine("New Nick?");
                    string nick = Console.ReadLine();
                    buddy.DisplayName = nick;
                    sess.SSI.ModifyBuddy(buddy);
                }
                else if (line.Equals("setinfo"))
                {
                    Console.WriteLine("Sets own userdetails");
                    sess.ICQ.SendUserInfo(myui);
                }
                else if (line.Equals("setemail"))
                {
                    Console.WriteLine("Email?");
                    string email = Console.ReadLine();
                    Console.WriteLine("error code: " + sess.ICQ.SendUserEmail(email));
                } 
                else if (line.Equals("checkout"))
                {
                    sess.SendContactListCheckout();
                }
            }
        }

        internal class ContactListGroup
        {
            public SSIGroup group = null;
            public SSIBuddy[] buddies = null;
        }

        private List<ContactListGroup> groups = new List<ContactListGroup>();

        private void sess_MasterGroupItemReceived(Session sess, int k)
        {
            for (int i = 0; i < k; ++i)
                groups.Add(new ContactListGroup());

            Console.WriteLine(String.Format("Expecting {0} groups", k));
        }

        private void sess_GroupItemReceived(Session sess, SSIGroup group)
        {
            //groups[index].group = group;

            //Console.WriteLine(String.Format("Received Group({0}): {1}", index, group.Name));
            //if (group.Children != null)
            //{
            //  groups[index].buddies = new SSIBuddy[group.Children.GetLength(0)];

            //  Console.WriteLine("Children:");
            //  foreach (ushort childid in group.Children)
            //  {
            //    Console.WriteLine("  {0}", childid);
            //  }
            //}
            Console.WriteLine(String.Format("Received Group({0}): {1}", group.ID, group.Name));
        }

        private void sess_BuddyItemReceived(Session sess, SSIBuddy buddy)
        {
            //foreach (ContactListGroup clg in groups)
            //{
            //  if ((clg.group != null) && (clg.group.ID == buddy.GroupID))
            //  {
            //    for (int i = 0; i < clg.group.Children.GetLength(0); ++i)
            //    {
            //      if (clg.group.Children[i] == buddy.ID)
            //      {
            //        clg.buddies[i] = buddy;
            //      }
            //    }
            //  }
            //}
            Console.WriteLine(String.Format("Received Buddy({0}): {1}({2}) GroupId:{3}", buddy.ID, buddy.Name, buddy.DisplayName,buddy.GroupID));
        }

        private void BuddyListDump()
        {
            foreach (ContactListGroup clg in groups)
            {
                Console.WriteLine(String.Format("{0}({1})", clg.group.Name, clg.group.ID));
                if (clg.buddies != null)
                {
                    foreach (SSIBuddy buddy in clg.buddies)
                    {
                        Console.WriteLine(String.Format("  {0}({1})", buddy.Name, buddy.ID));
                    }
                }
            }
        }

        private void sess_ContactListFinished(Session sess, DateTime dt)
        {
            Console.WriteLine();
            Console.WriteLine("Contact List Finished");
            //BuddyListDump();
            sess.ActivateBuddyList();
        }

        private void sess_ChatRoomCreated(object sender, ChatRoom newroom)
        {
            Console.WriteLine("Chat room created OK");
            newroom.MessageReceived += new MessageReceivedHandler(newroom_MessageReceived);
            newroom.UserJoined += new ChatRoomChangedHandler(newroom_UserJoined);
            newroom.UserLeft += new ChatRoomChangedHandler(newroom_UserLeft);
        }

        private void newroom_UserLeft(object sender, ChatRoomChangedEventArgs e)
        {
            ChatRoom room = sender as ChatRoom;
            Console.WriteLine(e.User.ScreenName + " left " + room.DisplayName);
        }

        private void newroom_UserJoined(object sender, ChatRoomChangedEventArgs e)
        {
            ChatRoom room = sender as ChatRoom;
            Console.WriteLine(e.User.ScreenName + " joined " + room.DisplayName);
        }

        private void newroom_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ChatRoom room = sender as ChatRoom;
            Console.WriteLine(e.Message.ScreenName + "@" + room.DisplayName + ": " + e.Message.Message);
            room.SendMessage("OUCH");
        }

        private void sess_ChatInvitationReceived(Session sess, UserInfo sender, string roomname, string message,
                                                 Encoding encoding, string language, Cookie key)
        {
            Console.WriteLine("Received chat invite from " + sender.ScreenName + " to " + roomname + ": " + message);
            Console.WriteLine("Auto-accepting");
            sess.ChatRooms.JoinChatRoom(key);
        }

        public void Login(Session sess)
        {
            //sess.Logon("login.oscar.aol.com", 5190);

            //sess.Logon();
            sess.Logon("login.icq.com", 5190, false);
            //sess.Logon("slogin.icq.com", 443, true);
        }

private void sess_LoginCompleted(Session sess)
{
    Console.WriteLine("Login complete");
}

        private void sess_LoginFailed(Session sess, LoginErrorCode reason)
        {
            Console.WriteLine(reason);
        }

        private void sess_ErrorMessage(Session sess, ServerErrorCode error)
        {
            Console.WriteLine("Error: " + error);
        }

        private void sess_WarningMessage(Session sess, ServerErrorCode error)
        {
            Console.WriteLine("Warning: " + error);
        }

        private void sess_StatusUpdate(Session sess, string message)
        {
            Console.WriteLine("Status: " + message);
        }
    }
}