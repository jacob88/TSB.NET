//Code Example of Transfering Money Between User Accounts
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

            //Transfer between internal accounts
			decimal amount = 3.63m;
			Int64 fromAccountNumber = 00000000000;
			Int64 toAccountNumber = 00000000000;
			string fromAccountReference = "Test";
			string toAccountReference = "Test";
			user.AccountTransfer(amount, fromAccountNumber, fromAccountReference, toAccountNumber, toAccountReference).ToString();
			
            Console.ReadKey();
        }
    }
}