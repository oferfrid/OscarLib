/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */

using System;
using System.IO;
using System.Text;
using csammisrun.OscarLib.Utility;
using System.Collections.Generic;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Handles all requests for server-stored image data
    /// </summary>
    /// <remarks>This manager is responsible for processing SNACs from family 0x0010 (BART)</remarks>
    public class GraphicsManager : ISnacFamilyHandler
    {
        /// <summary>
        /// The SNAC family responsible for buddy art
        /// </summary>
        private const int SNAC_BART_FAMILY = 0x0010;

        #region Static members
        private static byte[] fakeIcon = new byte[] { 0x02, 0x01, 0xd2, 0x04, 0x72 };

        /// <summary>
        /// Tests to see if the specified icon data is an AOL-set blank icon
        /// </summary>
        public static bool IsBlankIcon(byte[] iconData)
        {
            if (iconData.Length == fakeIcon.Length)
            {
                for (int i = 0; i < iconData.Length; i++)
                {
                    if (iconData[i] != fakeIcon[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }
        #endregion

        #region SNAC subtype constants
        /// <summary>
        /// ICBM code for an error packet sent by the server
        /// </summary>
        private const int BART_ERROR = 0x0001;
        /// <summary>
        /// ICBM code for a request to upload buddy art
        /// </summary>
        private const int BART_UPLOAD = 0x0002;
        /// <summary>
        /// ICBM code for a reply to a buddy art upload
        /// </summary>
        private const int BART_UPLOAD_REPLY = 0x0003;
        /// <summary>
        /// ICBM code for a request to download buddy art
        /// </summary>
        private const int BART_DOWNLOAD = 0x0006;
        /// <summary>
        /// ICBM code for a reply to a buddy art download
        /// </summary>
        private const int BART_DOWNLOAD_REPLY = 0x0007;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a buddy icon upload completed without error
        /// </summary>
        public event BuddyIconUploadCompletedHandler BuddyIconUploadCompleted;
        /// <summary>
        /// Occurs when a buddy icon upload failed
        /// </summary>
        public event BuddyIconUploadFailedHandler BuddyIconUploadFailed;
        /// <summary>
        /// Occurs when a buddy icon is received from the server
        /// </summary>
        public event BuddyIconReceivedHandler BuddyIconReceived;
        /// <summary>
        /// Occurs when a buddy icon is downloaded from the server
        /// </summary>
        public event BuddyIconDownloadedHandler BuddyIconDownloaded;
        #endregion

		private readonly ISession parent;
        private string autoSaveLocation;
        private BartID ownBuddyIcon;
        private readonly List<BartID> currentlyDownloading = new List<BartID>();

        /// <summary>
        /// Initializes a new GraphicsManager
        /// </summary>
		internal GraphicsManager(ISession parent)
        {
            this.parent = parent;
            parent.Dispatcher.RegisterSnacFamilyHandler(this, SNAC_BART_FAMILY);
        }

        /// <summary>
        /// Gets or sets the file path to which downloaded icons are automatically saved
        /// </summary>
        /// <remarks>If AutoSaveLocation is set to a valid path, icons requested using
        /// <see cref="DownloadBuddyIcon"/> are automatically saved to disk.  Requests for
        /// icons that have been saved locally return the locally saved icon instead of sending
        /// a new download request.</remarks>
        public string AutoSaveLocation
        {
            get { return autoSaveLocation; }
            set { autoSaveLocation = value; }
        }

        /// <summary>
        /// Gets the ID of the local user's buddy icon
        /// </summary>
        public BartID OwnBuddyIcon
        {
            get { return ownBuddyIcon; }
            internal set { ownBuddyIcon = value; }
        }

        #region Methods
        /// <summary>
        /// Uploads a buddy icon to the AIM servers
        /// </summary>
        /// <param name="filename">The filename of the icon to upload</param>
        public void UploadBuddyIcon(string filename)
        {
            // Check to make sure the local file exists
            if (!File.Exists(filename))
            {
                OnBuddyIconUploadFailed(BartReplyCode.NotFound);
                return;
            }

            byte[] data = null;
            using (StreamReader reader = new StreamReader(filename))
            {
                if (reader.BaseStream.Length > 7168) // 7KB
                {
                    OnBuddyIconUploadFailed(BartReplyCode.TooBig);
                    return;
                }

                if (!VerifyIcon(filename))
                {
                    OnBuddyIconUploadFailed(BartReplyCode.DimensionsTooBig);
                    return;
                }

                data = new byte[reader.BaseStream.Length];
                int index = 0;
                while (index < data.Length)
                {
                    index += reader.BaseStream.Read(data, index, data.Length - index);
                }
            }

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BART_FAMILY;
            sh.FamilySubtypeID = BART_UPLOAD;
            sh.Flags = 0;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteUshort((ushort)BartTypeId.BuddyIcon);
            stream.WriteUshort((ushort)data.Length);
            stream.WriteByteArray(data);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }

        /// <summary>
        /// Downloads a buddy icon from the AIM servers
        /// </summary>
        /// <param name="screenName">The screenname to which the icon belongs</param>
        /// <param name="iconId">The ID of the icon stored on the server</param>
        public void DownloadBuddyIcon(string screenName, BartID iconId)
        {
            // Check to make sure the icon ID is valid and actually a buddy icon
            if (iconId == null || iconId.Data == null ||
                !(iconId.Type == BartTypeId.BuddyIcon || iconId.Type == BartTypeId.BigBuddyIcon))
            {
                return;
            }

            // Check to see if the icon's cached, if the cache location is set
            if (!String.IsNullOrEmpty(AutoSaveLocation))
            {
                string saveLocation = Path.Combine(AutoSaveLocation, iconId.ToString());
                if (File.Exists(saveLocation))
                {
                    OnBuddyIconDownloaded(screenName, iconId, saveLocation);
                    return;
                }
            }

            // Check to see if there's already a request in progress for this icon ID
            if (currentlyDownloading.Contains(iconId))
            {
                return;
            }
            currentlyDownloading.Add(iconId);

            SNACHeader sh = new SNACHeader();
            sh.FamilyServiceID = SNAC_BART_FAMILY;
            sh.FamilySubtypeID = BART_DOWNLOAD;
            sh.Flags = 0;
            sh.RequestID = Session.GetNextRequestID();

            ByteStream stream = new ByteStream();
            stream.WriteByte((byte)Encoding.ASCII.GetByteCount(screenName));
            stream.WriteString(screenName, Encoding.ASCII);
            stream.WriteByte(0x01);
            stream.WriteBartID(iconId);

            SNACFunctions.BuildFLAP(Marshal.BuildDataPacket(parent, sh, stream));
        }
        #endregion

        #region ISnacFamilyHandler Members
        /// <summary>
        /// Process an incoming <see cref="DataPacket"/> from SNAC family 0x10
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> received by the server</param>
        public void ProcessIncomingPacket(DataPacket dp)
        {
            switch (dp.SNAC.FamilySubtypeID)
            {
                case BART_ERROR:
                    break;
                case BART_UPLOAD_REPLY:
                    ProcessUploadReply(dp);
                    break;
                case BART_DOWNLOAD_REPLY:
                    ProcessDownloadReply(dp);
                    break;
            }
        }
        #endregion

        #region Handlers
        /// <summary>
        /// Processes a download reply - SNAC(10,07)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(10,07)</param>
        private void ProcessDownloadReply(DataPacket dp)
        {
            string screenName = dp.Data.ReadString(dp.Data.ReadByte(), Encoding.ASCII);
            BartID queriedId = new BartID(dp.Data);
            currentlyDownloading.Remove(queriedId);

            BartReplyCode responseCode = (BartReplyCode)dp.Data.ReadByte();
            if (responseCode == BartReplyCode.Success)
            {
                BartID replyId = new BartID(dp.Data);
                ushort dataLength = dp.Data.ReadUshort();
                byte[] data = dp.Data.ReadByteArray(dataLength);

                if (!Directory.Exists(AutoSaveLocation))
                {
                    MemoryStream iconStream = new MemoryStream(data);
                    if(BuddyIconReceived != null)
                    {
                        BuddyIconReceived(this, new BuddyIconReceivedEventArgs(screenName, replyId, iconStream));
                    }
                }
                else
                {
                    string saveLocation = Path.Combine(AutoSaveLocation, replyId.ToString());
                    using (FileStream writer = new FileStream(saveLocation, FileMode.Create, FileAccess.Write))
                    {
                        writer.Write(data, 0, data.Length);
                    }
                    OnBuddyIconDownloaded(screenName, replyId, saveLocation);
                }
            }
        }

        /// <summary>
        /// Processes an upload reply - SNAC(10,03)
        /// </summary>
        /// <param name="dp">A <see cref="DataPacket"/> object with a buffer containing SNAC(10,03)</param>
        private void ProcessUploadReply(DataPacket dp)
        {
            BartReplyCode responseCode = (BartReplyCode)dp.Data.ReadByte();
            if (responseCode == BartReplyCode.Success)
            {
                BartID uploadedItem = new BartID(dp.Data);
                if (BuddyIconUploadCompleted != null)
                {
                    BuddyIconUploadCompleted(this, new BuddyIconUploadCompletedArgs(uploadedItem));
                }
            }
            else
            {
                OnBuddyIconUploadFailed(responseCode);
            }
        }

        /// <summary>
        /// Raises the <see cref="BuddyIconDownloaded"/> event
        /// </summary>
        protected internal void OnBuddyIconDownloaded(string screenName, BartID iconId, string iconPath)
        {
            if (BuddyIconDownloaded != null)
            {
                BuddyIconDownloaded(this, new BuddyIconDownloadedEventArgs(screenName, iconId, iconPath));
            }
        }

        /// <summary>
        /// Raises the <see cref="BuddyIconUploadFailed"/> event
        /// </summary>
        protected internal void OnBuddyIconUploadFailed(BartReplyCode errorCode)
        {
            if (BuddyIconUploadFailed != null)
            {
                BuddyIconUploadFailed(this, new BuddyIconUploadFailedArgs(errorCode));
            }
        }
        #endregion

        #region Image verification
        /// <summary>
        /// Verifies that a user-chosen icon fits within the image dimension parameters
        /// </summary>
        /// <param name="filename">The location of the image file</param>
        /// <returns><c>true</c> if the icon fits within parameters, <c>false</c> otherwise</returns>
        public static bool VerifyIcon(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] fourbytes = new byte[4];
                fs.Read(fourbytes, 0, 4);

                if (Path.GetExtension(filename).ToLower() == "ico")
                {
                    // We have to let AOL figure out icons, since there
                    // is most likely a 32x32 or a 48x48 image in the
                    // icon somewhere, but it's more than a job to figure
                    // that much out, and to potentially have to extract it...
                    return true;
                }
                else if (fourbytes[0] == 'B' && fourbytes[1] == 'M')
                {
                    return VerifyBitmap(fs);
                }
                else if (fourbytes[0] == 'G' && fourbytes[1] == 'I' &&
                         fourbytes[2] == 'F' && fourbytes[3] == '8')
                {
                    return VerifyGIF(fs);
                }
                else if (fourbytes[0] == 0xFF && fourbytes[1] == 0xD8 &&
                         fourbytes[2] == 0xFF && fourbytes[3] == 0xE0)
                {
                    return VerifyJFIF(fs);
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Verifies that a JPEG File Interchange Format image falls inside the
        /// AOL-required dimensions:  between 48x48 and 50x50
        /// </summary>
        /// <param name="fs">
        /// An opened <see cref="FileStream"/> two bytes into the JFIF.
        /// </param>
        /// <returns>
        /// <c>true</c> if the JFIF fits in the required dimensions,
        /// <c>false</c> otherwise.
        /// </returns>
        private static bool VerifyJFIF(FileStream fs)
        {
            // Scroll to the Start Frame 0 position in the image
            bool foundframe0 = false;
            while (!foundframe0 && fs.Position < fs.Length)
            {
                if (fs.ReadByte() == 0xFF && fs.ReadByte() == 0xC0)
                    break;
            }
            if (fs.Position == fs.Length)
                return false;

            // Three skip bytes, then the next four are the width and height (in NBO format)
            byte[] size = new byte[7];
            if (fs.Read(size, 0, size.Length) != size.Length)
                return false;

            ushort height = 0, width = 0;
            using (ByteStream bstream = new ByteStream(size))
            {
                bstream.AdvanceToPosition(3);
                height = bstream.ReadUshort();
                width = bstream.ReadUshort();
            }
            return (width >= 48 && width <= 50) && (height >= 48 && height <= 50);
        }

        /// <summary>
        /// Verifies that a Graphics Interchange Format image falls inside the
        /// AOL-required dimensions:  between 48x48 and 50x50
        /// </summary>
        /// <param name="fs">
        /// An opened <see cref="FileStream"/> two bytes into the GIF.
        /// </param>
        /// <returns>
        /// <c>true</c> if the GIF fits in the required dimensions,
        /// <c>false</c> otherwise.
        /// </returns>
        private static bool VerifyGIF(FileStream fs)
        {
            // We don't care about the next 2 bytes...
            for (int i = 0; i < 2; i++)
                fs.ReadByte();

            // Then the next four are the width and height of the gif
            byte[] size = new byte[4];
            fs.Read(size, 0, size.Length);

            ushort width = BitConverter.ToUInt16(size, 0);
            ushort height = BitConverter.ToUInt16(size, size.Length / 2);
            return (width >= 48 && width <= 50) && (height >= 48 && height <= 50);
        }

        /// <summary>
        /// Verifies that a Device-Independent Bitmap falls inside the AOL-required dimensions:  between 48x48 and 50x50
        /// </summary>
        /// <param name="fs">
        /// An opened <see cref="FileStream"/> two bytes into the bitmap.
        /// </param>
        /// <returns>
        /// <c>true</c> if the bitmap fits in the required dimensions,
        /// <c>false</c> otherwise.
        /// </returns>
        private static bool VerifyBitmap(FileStream fs)
        {
            // We don't care about the next 16 bytes...
            for (int i = 0; i < 16; i++)
                fs.ReadByte();

            // Then the next eight are the width and height of the bmp
            byte[] size = new byte[8];
            fs.Read(size, 0, size.Length);

            uint width = BitConverter.ToUInt32(size, 0);
            uint height = BitConverter.ToUInt32(size, size.Length / 2);
            return (width >= 48 && width <= 50) && (height >= 48 && height <= 50);
        }
        #endregion
    }

    #region Upload events
    /// <summary>
    /// Provides a callback function for notification of icon upload success
    /// </summary>
    public delegate void BuddyIconUploadCompletedHandler(object sender, BuddyIconUploadCompletedArgs e);

    /// <summary>
    /// Encapsulates the arguments to an upload completed event
    /// </summary>
    public class BuddyIconUploadCompletedArgs : EventArgs
    {
        private readonly BartID bartId;

        /// <summary>
        /// Initializes a new BuddyIconUploadCompletedArgs
        /// </summary>
        public BuddyIconUploadCompletedArgs(BartID bartId)
        {
            this.bartId = bartId;
        }

        /// <summary>
        /// The <see cref="BartID"/> identifying the icon that was uploaded
        /// </summary>
        public BartID BartID
        {
            get { return bartId; }
        }
    }

    /// <summary>
    /// Provides a callback function for notification of icon upload failure
    /// </summary>
    public delegate void BuddyIconUploadFailedHandler(object sender, BuddyIconUploadFailedArgs e);

    /// <summary>
    /// Encapsulates the error code of an upload failure event
    /// </summary>
    public class BuddyIconUploadFailedArgs : EventArgs
    {
        private readonly BartReplyCode errorCode;

        /// <summary>
        /// Initializes a new BuddyIconUploadFailedArgs
        /// </summary>
        internal BuddyIconUploadFailedArgs(BartReplyCode errorCode)
        {
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Gets the error code from the failed upload attempt
        /// </summary>
        public BartReplyCode ErrorCode
        {
            get { return errorCode; }
        }
    }
    #endregion Upload events

    #region Download events
    /// <summary>
    /// Provides a callback function for notification of receiving a buddy icon from the server
    /// </summary>
    public delegate void BuddyIconReceivedHandler(object sender, BuddyIconReceivedEventArgs e);

    /// <summary>
    /// Encapsulates the arguments from an icon received event
    /// </summary>
    public class BuddyIconReceivedEventArgs : EventArgs
    {
        private readonly BartID iconId;
        private readonly MemoryStream iconStream;
        private readonly string screenName;

        /// <summary>
        /// Initializes a new BuddyIconReceivedEventArgs
        /// </summary>
        public BuddyIconReceivedEventArgs(string screenName, BartID iconId, MemoryStream iconStream)
        {
            this.screenName = screenName;
            this.iconId = iconId;
            this.iconStream = iconStream;
        }

        /// <summary>
        /// Gets the <see cref="BartID"/> of the received icon
        /// </summary>
        public BartID IconID
        {
            get { return iconId; }
        }

        /// <summary>
        /// Gets the <see cref="MemoryStream"/> containing the icon data
        /// </summary>
        public MemoryStream IconStream
        {
            get { return iconStream; }
        }

        /// <summary>
        /// Gets the screenname to which the icon belongs
        /// </summary>
        public string ScreenName
        {
            get { return screenName; }
        }
    }

    /// <summary>
    /// Provides a callback function for notification of downloading a buddy icon from the server
    /// </summary>
    public delegate void BuddyIconDownloadedHandler(object sender, BuddyIconDownloadedEventArgs e);

    /// <summary>
    /// Encapsulates the arguments from an icon download event
    /// </summary>
    public class BuddyIconDownloadedEventArgs : EventArgs
    {
        private readonly BartID iconId;
        private readonly string iconFile;
        private readonly string screenName;

        /// <summary>
        /// Initializes a new BuddyIconDownloadedEventArgs
        /// </summary>
        public BuddyIconDownloadedEventArgs(string screenName, BartID iconId, string iconFile)
        {
            this.screenName = screenName;
            this.iconId = iconId;
            this.iconFile = iconFile;
        }

        /// <summary>
        /// Gets the <see cref="BartID"/> of the downloaded icon
        /// </summary>
        public BartID IconID
        {
            get { return iconId; }
        }

        /// <summary>
        /// Gets the file path of the downloaded icon
        /// </summary>
        public string IconFile
        {
            get { return iconFile; }
        }

        /// <summary>
        /// Gets the screenname to which the icon belongs
        /// </summary>
        public string ScreenName
        {
            get { return screenName; }
        }
    }
    #endregion

    /// <summary>
    /// Represents a BART item ID
    /// </summary>
    public class BartID : IComparable
    {
        private readonly BartTypeId type;
        private readonly BartFlags flags;
        private byte[] data;

        /// <summary>
        /// Initializes a new BartID from a byte stream
        /// </summary>
        internal BartID(ByteStream stream)
        {
            type = (BartTypeId)stream.ReadUshort();
            flags = (BartFlags)stream.ReadByte();
            byte dataLength = stream.ReadByte();
            data = stream.ReadByteArray(dataLength);
        }

        /// <summary>
        /// Gets the type of the BART item
        /// </summary>
        public BartTypeId Type
        {
            get { return type; }
        }

        /// <summary>
        /// Gets the flags set on the BART item
        /// </summary>
        public BartFlags Flags
        {
            get { return flags; }
        }

        /// <summary>
        /// Gets the data contained by the BART item
        /// </summary>
        public byte[] Data
        {
            get { return data; }
        }

        /// <summary>
        /// Gets a string that uniquely represents this BartID
        /// </summary>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0:x4}{1:x2}{2:x2}", (ushort)type, (byte)flags, (byte)data.Length);
            foreach (byte b in data)
            {
                builder.AppendFormat("{0:x2}", b);
            }
            return builder.ToString();
        }

        public override bool Equals(object obj)
        {
            BartID other = obj as BartID;
            return (other != null && ToString() == other.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        public int CompareTo(object obj)
        {
            BartID other = obj as BartID;
            if (other != null)
            {
                return ToString().CompareTo(other.ToString());
            }
            return -1;
        }

        #endregion
    }

    /// <summary>
    /// A reply code from a BART upload or download request
    /// </summary>
    public enum BartReplyCode : byte
    {
        /// <summary>
        /// The upload was successful
        /// </summary>
        Success = 0,
        /// <summary>
        /// The upload ID was malformed
        /// </summary>
        Invalid = 1,
        /// <summary>
        /// The upload type does not allow custom data
        /// </summary>
        NoCustomData = 2,
        /// <summary>
        /// The data was too small for the type
        /// </summary>
        TooSmall = 3,
        /// <summary>
        /// The data was too big for the type
        /// </summary>
        TooBig = 4,
        /// <summary>
        /// Unknown type
        /// </summary>
        InvalidType = 5,
        /// <summary>
        /// Banned type
        /// </summary>
        Banned = 6,
        /// <summary>
        /// The requested data was not found
        /// </summary>
        NotFound = 7,

        /// <summary>
        /// The image dimensions are too big for the type
        /// </summary>
        DimensionsTooBig = 128,
    }

    /// <summary>
    /// Possible types for a BART object
    /// </summary>
    public enum BartTypeId
    {
        /// <summary>
        /// GIF/JPG/BMP, &lt;= 32 pixels and 2k
        /// </summary>
        SmallBuddyIcon = 0,
        /// <summary>
        /// GIF/JPG/BMP, &lt;= 64 pixels and 7k
        /// </summary>
        BuddyIcon = 1,
        /// <summary>
        /// StringTLV format; DATA flag is always set
        /// </summary>
        StatusString = 2,
        /// <summary>
        /// WAV/MP3/MID, &lt;= 10K
        /// </summary>
        ArrivalSound = 3,
        /// <summary>
        /// Byte array of rich text codes; DATA flag is always set
        /// </summary>
        RichText = 4,
        /// <summary>
        /// XML
        /// </summary>
        SuperBuddyIcon = 5,
        /// <summary>
        /// Opaque struct; DATA flag is always set
        /// </summary>
        RadioStation = 6,
        /// <summary>
        /// SWF
        /// </summary>
        BigBuddyIcon = 12,
        /// <summary>
        /// Time when the status string is set
        /// </summary>
        StatusStringTimeOfDay = 13,
        /// <summary>
        /// XML file; Data flag should not be set
        /// </summary>
        CurrentAVTrack = 15,
        /// <summary>
        /// WAV/MP3/MID, &lt;= 10K
        /// </summary>
        DepartureSound = 96,
        /// <summary>
        /// GIF/JPG/BMP wallpaper
        /// </summary>
        IMChrome = 129,
        /// <summary>
        /// WAV/MP3, &lt;= 10K
        /// </summary>
        IncomingIMSound = 131,
        /// <summary>
        /// XML
        /// </summary>
        IMChromeXml = 136,
        /// <summary>
        /// Immersive Expressions
        /// </summary>
        IMChromeTimers = 137,
        /// <summary>
        /// Set of default Emoticons
        /// </summary>
        EmoticonSet = 1024,
        /// <summary>
        /// Cert chain for encryption certs
        /// </summary>
        EncryptionCertificationChain = 1026,
        /// <summary>
        /// Cert chain for signing certs
        /// </summary>
        SignatureCertificationChain = 1027,
        /// <summary>
        /// Cert for enterprise gateway
        /// </summary>
        GatewayCertificate = 1028,
    }

    /// <summary>
    /// Flags that can be applied to <see cref="BartID"/> objects
    /// </summary>
    [Flags]
    public enum BartFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0x00,
        /// <summary>
        /// A custom blob
        /// </summary>
        Custom = 0x01,
        /// <summary>
        /// The BartID data field is client-processed
        /// </summary>
        Data = 0x04,
        /// <summary>
        /// The identified BART object needs to be uploaded
        /// </summary>
        Unknown = 0x40,
        /// <summary>
        /// Use the current ID for the BART object type
        /// </summary>
        Redirect = 0x80,
    }
}
