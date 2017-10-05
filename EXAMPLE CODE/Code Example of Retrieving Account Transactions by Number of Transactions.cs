//Code Example of Retrieving Account Transactions by Number of Transactions
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
			string userPassword = "";
            API_Request user = new API_Request(userCardNumber, userPassword);

            //Retrieve account transactions
			Int64 accountNumber = 0000000000;
			int entriesLimit = 30;
			XmlDocument transactionData = user.GetTransactions(accountNumber, entriesLimit);
			
			//Beautify XML and output result
			Console.WriteLine(user.XmlToString(transactionData));
            Console.ReadKey();
        }
    }
}