/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Contains information concerning a client adding the locally logged in UIN to their list
    /// </summary>
    public class AddedToRemoteListEventArgs : EventArgs
    {
        private string uin;

        /// <summary>
        /// Initializes a new AddedToRemoteListEventArgs
        /// </summary>
        public AddedToRemoteListEventArgs(String uin)
        {
            this.uin = uin;
        }

        /// <summary>
        /// Gets the UIN of the user that added the locally logged in UIN to their list
        /// </summary>
        public String Uin
        {
            get { return uin; }
        }
    }

    /// <summary>
    /// Describes an event received from the server indicating that
    /// a remote user could not send this client a message
    /// </summary>
    public class UndeliverableMessageEventArgs : EventArgs
    {
        private int channel;
        private int numberMissed;
        private UndeliverableMessageReason reason;
        private UserInfo userinfo;

        /// <summary>
        /// Initializes a new UndeliverableMessageEventArgs
        /// </summary>
        /// <param name="channel">The channel on which the message was missed</param>
        /// <param name="userInfo">A <see cref="UserInfo"/> block describing the sender</param>
        /// <param name="numberMissed">The number of messages missed from the sender</param>
        /// <param name="reason">An <see cref="UndeliverableMessageReason"/> value describing the failure</param>
        public UndeliverableMessageEventArgs(int channel, UserInfo userInfo,
                                             int numberMissed, UndeliverableMessageReason reason)
        {
            this.channel = channel;
            userinfo = userInfo;
            this.numberMissed = numberMissed;
            this.reason = reason;
        }

        /// <summary>
        /// Gets an <see cref="UndeliverableMessageReason"/> value describing the delivery failure
        /// </summary>
        public UndeliverableMessageReason Reason
        {
            get { return reason; }
        }

        /// <summary>
        /// Gets a <see cref="UserInfo"/> block describing the sender
        /// </summary>
        public UserInfo Sender
        {
            get { return userinfo; }
        }

        /// <summary>
        /// Gets the number of messages missed from the sender
        /// </summary>
        public int NumberMissed
        {
            get { return numberMissed; }
        }

        /// <summary>
        /// Gets the channel on which the messages were missed
        /// </summary>
        public int Channel
        {
            get { return channel; }
        }
    }

    /// <summary>
    /// Describes an event received from the server indicating that a message was
    /// accepted for delivery
    /// </summary>
    public class MessageAcceptedEventArgs : EventArgs
    {
        private Cookie cookie;
        private string screenname;

        /// <summary>
        /// Initializes a new MessageAcceptedEventArgs
        /// </summary>
        public MessageAcceptedEventArgs(Cookie cookie, string screenName)
        {
            this.cookie = cookie;
            screenname = screenName;
        }

        /// <summary>
        /// Gets the uniquely identifying <see cref="Cookie"/> for this message
        /// </summary>
        public Cookie Cookie
        {
            get { return cookie; }
        }

        /// <summary>
        /// Gets the screen name to which this message was delivered
        /// </summary>
        public string ScreenName
        {
            get { return screenname; }
        }
    }

    /// <summary>
    /// Describes a message received event
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        private IM message;

        /// <summary>
        /// Creates a new MessageReceivedEventArgs
        /// </summary>
        public MessageReceivedEventArgs(IM message)
        {
            this.message = message;
        }

        /// <summary>
        /// Gets the message that was received
        /// </summary>
        public IM Message
        {
            get { return message; }
        }
    }

    /// <summary>
    /// Describes a chat room change event
    /// </summary>
    public class ChatRoomChangedEventArgs : EventArgs
    {
        private UserInfo user;

        /// <summary>
        /// Initializes a new ChatRoomChangedEventArgs
        /// </summary>
        /// <param name="user">The user that has joined or left the chat room</param>
        public ChatRoomChangedEventArgs(UserInfo user)
        {
            this.user = user;
        }

        /// <summary>
        /// Gets the <see cref="UserInfo"/> describing the user that has joined
        /// or left the chat room
        /// </summary>
        public UserInfo User
        {
            get { return user; }
        }
    }

    /// <summary>
    /// Describes a received typing notification event
    /// </summary>
    public class TypingNotificationEventArgs : EventArgs
    {
        private TypingNotification notification;
        private string screenname;

        /// <summary>
        /// Initializes a new TypingNotificationEventArgs
        /// </summary>
        /// <param name="screenname">The screen name of the user who sent the typing notification</param>
        /// <param name="notification">The type of typing notification received</param>
        public TypingNotificationEventArgs(string screenname, TypingNotification notification)
        {
            this.screenname = screenname;
            this.notification = notification;
        }

        /// <summary>
        /// Gets the screen name of the user who sent the typing notification
        /// </summary>
        public string ScreenName
        {
            get { return screenname; }
        }

        /// <summary>
        /// Gets the type of typing notification received
        /// </summary>
        public TypingNotification Notification
        {
            get { return notification; }
        }
    }
}