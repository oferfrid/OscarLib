using System;
using System.Collections.Generic;
using System.Text;
using csammisrun.OscarLib.Utility;

namespace csammisrun.OscarLib
{
    public class ServerInfo
    {
        #region Proxy
        string proxyServer;
        int proxyPort;
        string proxyUsername;
        string proxyPassword;
        ProxyType proxySetting = ProxyType.None;

        /// <summary>
        /// The proxy server to connect through
        /// </summary>
        public string ProxyServer {
            get { return this.proxyServer; }
            set { this.proxyServer = value; }
        }

        /// <summary>
        /// The port to connect to on the proxy server
        /// </summary>
        public int ProxyPort {
            get { return this.proxyPort; }
            set { this.proxyPort = value; }
        }

        /// <summary>
        /// The username to use for a proxy server
        /// </summary>
        public string ProxyUsername {
            get { return this.proxyUsername; }
            set { this.proxyUsername = value; }
        }

        /// <summary>
        /// The password to use for a proxy server
        /// </summary>
        public string ProxyPassword {
            get { return this.proxyPassword; }
            set { this.proxyPassword = value; }
        }

        /// <summary>
        /// The <see cref="ProxyType"/> to use for this connection
        /// </summary>
        public ProxyType ProxySetting {
            get { return this.proxySetting; }
            set { this.proxySetting = value; }
        }
        #endregion Proxy



        #region Login
        string loginServer = "login.icq.com";
        int loginPort = 5190;
        bool loginSsl = false;

        /// <summary>
        /// Gets or sets the server address used for OSCAR logins
        /// </summary>
        /// <remarks>
        /// Traditionally, this is login.icq.com.
        /// </remarks>
        public string LoginServer {
            get { return this.loginServer; }
            set { this.loginServer = value; }
        }

        /// <summary>
        /// Gets or sets the port number used for OSCAR logins
        /// </summary>
        /// <remarks>
        /// Traditionally, this is port 5190; however, AIM 6 has been caught using port 443 to negotiate
        /// connections with login.oscar.aol.com and ars.oscar.aol.com.  Future versions of OscarLib may use
        /// this property to support login via port 443.
        /// </remarks>
        public int LoginPort {
            get { return this.loginPort; }
            set { this.loginPort = value; }
        }

        /// <summary>
        /// Enables Ssl login to auth server
        /// </summary>
        public bool LoginSsl {
            get { return this.loginSsl; }
            set { this.loginSsl = value; }
        }
        #endregion Login



        #region IcqProxy
        string icqProxyServer = "ars.icq.com";      // bevor June 2011 -> "ars.oscar.aol.com"

        /// <summary>
        /// The icq proxy server, needed e.g. <see cref="Session.SendFileProxied"/>
        /// </summary>
        public string IcqProxyServer {
            get { return this.icqProxyServer; }
            set { this.icqProxyServer = value; }
        }
        #endregion IcqProxy
    }
}
