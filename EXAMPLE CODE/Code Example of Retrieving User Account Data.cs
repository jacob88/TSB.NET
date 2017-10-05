//Code Example of Retrieving User Account Data
using System;
using System.IO;
using System.Xml;
using TSB.NET;

namespace TSB.NET_Example
{
    class Program
    {
        static void Main(string[] args)
        {
			//Authenticate new user session
            int userCardNumber = 00000000;
			String userPassword = "";
            API_Request user = new API_Request(userCardNumber, userPassword);

            //Retrieve user account data
			XmlDocument userAccounts = user.GetAccounts();
			
			//Beautify XML and output result
			Console.WriteLine(user.XmlToString(userAccounts));
            Console.ReadKey();
        }
    }
}