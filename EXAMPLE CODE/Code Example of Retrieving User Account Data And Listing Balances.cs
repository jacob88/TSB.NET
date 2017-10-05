//Code Example of Retrieving User Account Data And Listing Balances
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

            //Output account balances
			Int64 accountNumber1 = 00000000000;
			Int64 accountNumber2 = 00000000000;
			string AccountBalance1 = null;
			string AccountBalance2 = null;
			string AccountName1 = null;
			string AccountName2 = null;
			foreach (XmlNode node in user.GetAccounts().GetElementsByTagName("accountList"))
			{
				if (node["number"].InnerText == accountNumber2.ToString())
				{
					AccountName2 = node["name"].InnerText;
					AccountBalance2 = node["balance"].InnerText;
				}
				else if (node["number"].InnerText == accountNumber1.ToString())
				{
					AccountName1 = node["name"].InnerText;
					AccountBalance1 = node["balance"].InnerText;
				}
			}
			
			//Beautify and output result
			Console.WriteLine("Balances, " + AccountName2 + ": $" + AccountBalance2 + ", " + AccountName1 + ": $" + AccountBalance1);
            Console.ReadKey();
        }
    }
}