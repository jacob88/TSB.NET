//Code Example of Process Payment to External Account
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

            //Process payment to external account
			decimal amount = 1.20m;
			Int64 fromAccountNumber = 00000000000;
			string userParticulars = "Test";
			string userCode = "";
			string userReference = "Payment";
			string payeeName = "Dave O Devon";
			Int64 payeeAccount = 00000000000;
			string payeeParticulars = "Test";
			string payeeCode = "";
			string payeeReference = "Payment";
			user.AccountPayment(amount, fromAccountNumber, userParticulars, userCode, userReference, payeeName, payeeAccount, payeeParticulars, payeeCode, payeeReference);
			
            Console.ReadKey();
        }
    }
}