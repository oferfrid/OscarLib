/*
 * Created by SharpDevelop.
 * User: oferfrid
 * Date: 27/04/2011
 * Time: 13:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using csammisrun.OscarLib;

namespace TestSMS
{
	/// <summary>
	/// Description of SMSManager.
	/// </summary>
	public class SMSManager
	{

		private Session sess;
		private string number=string.Empty;
		private string message=string.Empty;
		private string Uid=string.Empty;
		
		int LoginRetrys = 3;
		

		public SMSManager(string name, string pw)
		{
			sess = new Session(name, pw);
			//sess.InitializeLogger(Environment.CurrentDirectory);

			sess.ClientCapabilities = Capabilities.Chat | Capabilities.SendFiles | Capabilities.OscarLib |
				Capabilities.DirectIM;
		}

		void Messages_MessageDeliveryUpdate(object sender, MessageStatusEventArgs e)
		{
			//Console.WriteLine("Message to {0} has status {1}", e.Destination, e.Status);
			System.Threading.Thread.Sleep(500);
			LogOf();
		}

		

		private void ICQ_OfflineMessagesReceived(object sender, OfflineMessagesReceivedEventArgs e)
		{
			//Console.WriteLine("Received {0} messages for {1}", e.ReceivedMessages.Count, e.ScreenName);
			//foreach (OfflineIM oim in e.ReceivedMessages)
			//{
			//Console.WriteLine("{0}: {1}", oim.ReceivedOn, oim.Message);
			//}
		}

		private void sess_MessageReceived(object sender, MessageReceivedEventArgs e)
		{
			Console.WriteLine("ICBM message received from {0}: {1}", e.Message.ScreenName, e.Message.Message);
		}


		

		//*************Do Not Delete****************
		private void sess_ContactListFinished(Session sess, DateTime dt)
		{
			sess.ActivateBuddyList();
		}

		

		private void Login()
		{
			sess.Logon("login.icq.com", 5190,false);
			
		}
		private void LogOf()
		{
			sess.Logoff();
			
		}


		private void sess_LoginCompleted(Session sess)
		{
			//Console.WriteLine("Login complete");
			if (number!=string.Empty)
			{
				sess.ICQ.SendSMSMessage(number,message,"EmailSMSSender");
			}
			if(Uid!=string.Empty)
			{
				sess.Messages.SendMessage(Uid,message);
			}
			this.number = string.Empty;
			this.message = string.Empty;
			this.Uid = string.Empty;
			LoginRetrys = 3;
		}

		private void sess_LoginFailed(Session sess, LoginErrorCode reason)
		{
			if (LoginRetrys >0)
			{
				LoginRetrys--;
				Login();
			}
			else
			{
				LoginRetrys = 3;
				Console.WriteLine("LoginFailed: " + reason);
			}
			
			
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
//		
		
		/// <summary>
		/// sends an sms using ICQ getway
		/// </summary>
		/// <param name="number">Phone nubmer to send to (+XXX-XX-XXXXXXX)</param>
		/// <param name="message">Text mesege to send</param>
		/// 
		
		private void InitSession()
		{
			sess.LoginCompleted += new LoginCompletedHandler(sess_LoginCompleted);
			sess.ErrorMessage += new ErrorMessageHandler(sess_ErrorMessage);
			sess.WarningMessage += new WarningMessageHandler(sess_WarningMessage);
			sess.StatusUpdate += new InformationMessageHandler(sess_StatusUpdate);
			sess.LoginFailed += new LoginFailedHandler(sess_LoginFailed);

			sess.ContactListFinished += new ContactListFinishedHandler(sess_ContactListFinished);

			sess.Messages.MessageReceived += new MessageReceivedHandler(sess_MessageReceived);
			sess.Messages.OfflineMessagesReceived += new OfflineMessagesReceivedEventHandler(ICQ_OfflineMessagesReceived);
			sess.Messages.MessageDeliveryUpdate += new MessageDeliveryUpdateEventHandler(Messages_MessageDeliveryUpdate);
			
			Login();
		}
		
		public void SendSMSMessage(string _number, string _message)
		{
			this.number = _number;
			this.message = _message;
			InitSession();
		}
		
		/// <summary>
		/// sends an ICQ messege using ICQ getway
		/// </summary>
		/// <param name="Uid">ICQ Uid</param>
		/// <param name="message">Text mesege to send</param>
		public void SendICQMessage(string Uid, string message)
		{
			this.Uid = Uid;
			this.message = message;
			InitSession();
		}
	}
}
