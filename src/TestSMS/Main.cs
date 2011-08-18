using System;
using csammisrun.OscarLib;

namespace TestSMS
{
	/// <summary>
	/// Contains the program's entry point
	/// </summary>
	public class MainClass
	{
		[STAThread]
		public static void Main()
		{
			string name = @"625850626";//"<Uid for the ICQ acount>";
			string password = @"ofer1234";//"<password for the ICQ Acount>";
			Console.WriteLine("Beginning login sequence");
			SMSManager  mc = new SMSManager(name, password);
			
			Console.WriteLine("Write sendicq for sending to icq and sendicq for SMS sending");
			
			string line;
			while ((line = Console.ReadLine()) != "exit")
			{
				if (line.Equals("sendicq"))
				{
					mc.SendICQMessage("<the Uid Of>","<message to send>");
				}
				if (line.Equals("sendsms"))
				{
					mc.SendSMSMessage("<phone to send to (+972XXXXXXXXX)>","<messege to send>");
				}
				
			}
			
			
			

		}
	}
}