using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;
using System.Net.Http;
using System.Collections.Generic;

namespace TSB.NET
{
    // TSB Account Requests
    public class API_Request
    {
        /// <summary> 
        /// TSB online banking URL
        /// </summary>
        private const string TSB_DOMAIN = "https://homebank.tsbbank.co.nz/online/";

        // GETTER / SETTER METHODS
        private string customerID;
        /// <summary> 
        /// Get TSB account customer ID
        /// </summary>
        public string CustomerID
        {
            get
            {
                return customerID;
            }
        }

        private int cardNumber;
        /// <summary> 
        /// Set TSB account card number
        /// </summary>
        public int CardNumber
        {
            set
            {
                cardNumber = value;
            }
        }

        private string password;
        /// <summary> 
        /// Set TSB account password
        /// </summary>
        public string Password
        {
            set
            {
                password = value;
            }
        }

        private string sessionID;
        /// <summary> 
        /// Gets or sets TSB account session ID
        /// </summary>
        public string SessionID
        {
            get
            {
                return sessionID;
            }
            set
            {
                sessionID = value;
            }
        }

        // CONSTRUCTORS
        /// <summary> 
        /// Constructs new TSB_Banking object
        /// </summary>
        public API_Request(int cardNumber, string password, string sessionID = null)
        {
            this.cardNumber = cardNumber;
            this.password = password;

            if (sessionID == null)
                NewSession();
            else
                this.sessionID = sessionID;
        }

        // PUBLIC METHODS
        /// <summary> 
        /// Validate current session ID
        /// </summary>
        public bool ValidateSession()
        {
            return Validate(sessionID);
        }

        /// <summary> 
        /// Validate given session ID
        /// </summary>
        public bool ValidateSession(string sessionID)
        {
            return Validate(sessionID);
        }

        /// <summary> 
        /// Authenticate new TSB_Banking session
        /// </summary>
        public void NewSession()
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("op", "signon"),
                    new KeyValuePair<string, string>("card", cardNumber.ToString()),
                    new KeyValuePair<string, string>("password", password)
                });
                var response = client.PostAsync(TSB_DOMAIN, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    string responseString = responseContent.ReadAsStringAsync().Result;

                    if (responseString.Contains("signonForm"))
                    {
                        throw new Exception("Access Denied, Incorrect Credentials");
                    }
                    else
                    {
                        using (StringReader lineReader = new StringReader(responseString))
                        {
                            string line;
                            while ((line = lineReader.ReadLine()) != null)
                            {
                                if (line.Contains("NEXT_SEQUENCE_ID ="))
                                {
                                    sessionID = ((line.Split('"'))[1]);
                                }
                                else if (line.Contains("CUSTOMER_NUMBER ="))
                                {
                                    customerID = ((line.Split('"'))[1]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Connection Error " + ((int)response.StatusCode).ToString() + ", Couldn't Reach " + TSB_DOMAIN);
                }
            }
        }

        /// <summary>
        /// Return list of accounts as XmlDocument
        /// </summary>
        public XmlDocument GetAccounts()
        {
            JObject jsonObject = JObject.Parse(GetJson(TSB_DOMAIN + "api/accounts/" + customerID + "?nextSequenceId=" + sessionID));
            XmlDocument doc = JsonConvert.DeserializeXmlNode(jsonObject["data"].ToString(), "data");
            return doc;
        }

        /// <summary>
        /// Return customer data as XmlDocument
        /// </summary>
        public XmlDocument GetCustomerData()
        {
            JObject jsonObject = JObject.Parse(GetJson(TSB_DOMAIN + "api/customer/" + customerID + "?nextSequenceId=" + sessionID));
            XmlDocument doc = JsonConvert.DeserializeXmlNode(jsonObject["data"].ToString(), "data");
            return doc;
        }

        /// <summary>
        /// Return account transaction data as XmlDocument
        /// </summary>
        public XmlDocument GetTransactions(long accountNumber, long numberOfTransactions = 20)
        {
            JObject jsonObject = JObject.Parse(GetJson(TSB_DOMAIN + "api/transactions/" + customerID + "/account/" + accountNumber.ToString() + "?numberOfTransactions=" + numberOfTransactions.ToString() + "&nextSequenceId=" + sessionID));
            XmlDocument doc = JsonConvert.DeserializeXmlNode(jsonObject["data"].ToString(), "data");
            return doc;
        }

        /// <summary>
        /// Return account transaction data as XmlDocument
        /// </summary>
        public XmlDocument GetTransactions(long accountNumber, string startDate, string endDate = "", bool direction = false, long numberOfTransactions = 20)
        {
            JObject jsonObject = JObject.Parse(GetJson(TSB_DOMAIN + "api/transactions/" + customerID + "/account/" + accountNumber.ToString() + "?direction=" + GetDirection(direction) + "&numberOfTransactions=" + numberOfTransactions.ToString() + "&startDate=" + startDate + "&endDate=" + endDate + "&nextSequenceId=" + sessionID));
            XmlDocument doc = JsonConvert.DeserializeXmlNode(jsonObject["data"].ToString(), "data");
            return doc;
        }

        /// <summary>
        /// Process Account Transfer Immeadiately, returns true when transaction successful
        /// </summary>
        public bool AccountTransfer(decimal amount, Int64 fromAccountNumber, string fromAccountReference, Int64 toAccountNumber, string toAccountReference)
        {
            try
            {
                decimal fromAccountBalance = 0;
                string fromAccountOID = null;
                string toAccountOID = null;
                string fromAccountName = null;
                string toAccountName = null;
                string fromAccountNumberFormatted = null;
                string toAccountNumberFormatted = null;
                string toAccountType = null;

                foreach (XmlNode node in GetAccounts().GetElementsByTagName("accountList"))
                {
                    if (node["number"].InnerText == fromAccountNumber.ToString())
                    {
                        fromAccountOID = node["oid"].InnerText;
                        fromAccountName = node["name"].InnerText;
                        fromAccountNumberFormatted = node["numberFormatted"].InnerText;
                        fromAccountBalance = Convert.ToDecimal(node["balance"].InnerText);
                    }
                    else if (node["number"].InnerText == toAccountNumber.ToString())
                    {
                        toAccountOID = node["oid"].InnerText;
                        toAccountName = node["name"].InnerText;
                        toAccountNumberFormatted = node["numberFormatted"].InnerText;
                        toAccountType = node["type"].InnerText;
                    }
                }

                if (fromAccountOID == null)
                    throw new Exception("Transaction Error, Check \"From\" Account Number");
                if (toAccountOID == null)
                    throw new Exception("Transaction Error, Check \"To\" Account Number");

                if (fromAccountBalance < amount)
                    throw new Exception("Transaction Error, Insufficient Funds");

                JObject jsonObject = JObject.Parse(SubmitJson((TSB_DOMAIN + "api/transfers/" + customerID + "?nextSequenceId=" + sessionID),
                    ("{\"primaryCustomerNumber\": \"" + customerID + "\", " +
                    "\"transfer\": { " +
                    "\"amount\": \"" + amount.ToString() + "\", " +
                    "\"date\": \"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz") + "\", " +
                    "\"fromAccountIdentifier\": \"" + fromAccountOID + "\", " +
                    "\"fromAccountName\": \"" + fromAccountName + "\", " +
                    "\"fromAccountNumber\": \"" + fromAccountNumber.ToString() + "\", " +
                    "\"fromAccountNumberFormatted\": \"" + fromAccountNumberFormatted + "\", " +
                    "\"fromAccountReference\": \"" + fromAccountReference + "\", " +
                    "\"toAccountIdentifier\": \"" + toAccountOID + "\", " +
                    "\"toAccountName\": \"" + toAccountName + "\", " +
                    "\"toAccountNumber\": \"" + toAccountNumber.ToString() + "\", " +
                    "\"toAccountNumberFormatted\": \"" + toAccountNumberFormatted + "\", " +
                    "\"toAccountReference\": \"" + toAccountReference + "\", " +
                    "\"toAccountType\": \"" + toAccountType + "\"}}")));
                return Convert.ToBoolean(jsonObject["success"].ToString());
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Process Account Transfer On Specified Date, returns true when transaction successful
        /// </summary>
        public bool AccountTransfer(decimal amount, Int64 fromAccountNumber, string fromAccountReference, Int64 toAccountNumber, string toAccountReference, string date)
        {
            try
            {
                decimal fromAccountBalance = 0;
                string fromAccountOID = null;
                string toAccountOID = null;
                string fromAccountName = null;
                string toAccountName = null;
                string fromAccountNumberFormatted = null;
                string toAccountNumberFormatted = null;
                string toAccountType = null;

                foreach (XmlNode node in GetAccounts().GetElementsByTagName("accountList"))
                {
                    if (node["number"].InnerText == fromAccountNumber.ToString())
                    {
                        fromAccountOID = node["oid"].InnerText;
                        fromAccountName = node["name"].InnerText;
                        fromAccountNumberFormatted = node["numberFormatted"].InnerText;
                        fromAccountBalance = Convert.ToDecimal(node["balance"].InnerText);
                    }
                    else if (node["number"].InnerText == toAccountNumber.ToString())
                    {
                        toAccountOID = node["oid"].InnerText;
                        toAccountName = node["name"].InnerText;
                        toAccountNumberFormatted = node["numberFormatted"].InnerText;
                        toAccountType = node["type"].InnerText;
                    }
                }

                if (fromAccountOID == null)
                    throw new Exception("Transaction Error, Check \"From\" Account Number");
                if (toAccountOID == null)
                    throw new Exception("Transaction Error, Check \"To\" Account Number");

                if (fromAccountBalance < amount)
                    throw new Exception("Transaction Error, Insufficient Funds");

                JObject jsonObject = JObject.Parse(SubmitJson((TSB_DOMAIN + "api/transfers/" + customerID + "?nextSequenceId=" + sessionID),
                    ("{\"primaryCustomerNumber\": \"" + customerID + "\", " +
                    "\"transfer\": { " +
                    "\"amount\": \"" + amount.ToString() + "\", " +
                    "\"date\": \"" + date + "\", " +
                    "\"fromAccountIdentifier\": \"" + fromAccountOID + "\", " +
                    "\"fromAccountName\": \"" + fromAccountName + "\", " +
                    "\"fromAccountNumber\": \"" + fromAccountNumber.ToString() + "\", " +
                    "\"fromAccountNumberFormatted\": \"" + fromAccountNumberFormatted + "\", " +
                    "\"fromAccountReference\": \"" + fromAccountReference + "\", " +
                    "\"toAccountIdentifier\": \"" + toAccountOID + "\", " +
                    "\"toAccountName\": \"" + toAccountName + "\", " +
                    "\"toAccountNumber\": \"" + toAccountNumber.ToString() + "\", " +
                    "\"toAccountNumberFormatted\": \"" + toAccountNumberFormatted + "\", " +
                    "\"toAccountReference\": \"" + toAccountReference + "\", " +
                    "\"toAccountType\": \"" + toAccountType + "\"}}")));
                return Convert.ToBoolean(jsonObject["success"].ToString());
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Process Account Payment Immeadiately, returns true when transaction successful
        /// </summary>
        public bool AccountPayment(decimal amount, Int64 fromAccountNumber, string userParticulars, string userCode, string userReference, string payeeName, Int64 payeeAccount, string payeeParticulars, string payeeCode, string payeeReference)
        {
            try
            {
                decimal userAccountBalance = 0;
                string userAccountOid = null;
                string userAccountName = null;
                string userAccountNumberFormatted = null;

                foreach (XmlNode node in GetAccounts().GetElementsByTagName("accountList"))
                {
                    if (node["number"].InnerText == fromAccountNumber.ToString())
                    {
                        userAccountOid = node["oid"].InnerText;
                        userAccountName = node["name"].InnerText;
                        userAccountNumberFormatted = node["numberFormatted"].InnerText;
                        userAccountBalance = Convert.ToDecimal(node["balance"].InnerText);
                    }
                }

                if (userAccountOid == null)
                    throw new Exception("Transaction Error, Check \"From\" Account Number");

                if (userAccountBalance < amount)
                    throw new Exception("Transaction Error, Insufficient Funds");

                JObject jsonObject = JObject.Parse(SubmitJson((TSB_DOMAIN + "api/payments?nextSequenceId=" + sessionID),
                    ("{\"payee\":{\"statementDetails\":{" +
                    "\"particulars\":\"" + userParticulars + "\"," +
                    "\"code\":\"" + userCode + "\",\"reference\":\"" + userReference +
                    "\"},\"payeeType\":\".PaymentPayeeNewPersonal\",\"saveThisPayee\":false," +
                    "\"name\":\"" + payeeName + "\",\"accountNumber\":\"" + payeeAccount +
                    "\"},\"fromAccountOid\":\"" + userAccountOid +
                    "\",\"date\":\"" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz") + "\",\"payerStatementDetails\":{" +
                    "\"particulars\":\"" + payeeParticulars + "\",\"code\":\"" + payeeCode +
                    "\",\"reference\":\"" + payeeReference + "\"},\"amount\":\"" + amount +
                    "\",\"paymentProcessingTime\":\"TODAY\"}")));
                return Convert.ToBoolean(jsonObject["success"].ToString());
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Output XmlDocument as a human readable string for debugging
        /// </summary>
        public string XmlToString(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        // PRIVATE METHODS
        /// <summary>
        /// Submit JSON query to url as string, returns JSON response as string
        /// </summary>
        private string SubmitJson(string url, string jsonData)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json"); ;
                var response = client.PostAsync(url, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    return responseString;
                }
                else
                {
                    throw new Exception("Connection Error " + ((int)response.StatusCode).ToString() + ", Couldn't Reach " + TSB_DOMAIN);
                }
            }
        }

        /// <summary>
        /// Return JSON response from url as string
        /// </summary>
        private string GetJson(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    return responseString;
                }
                else
                {
                    throw new Exception("Connection Error " + ((int)response.StatusCode).ToString() + ", Couldn't Reach " + TSB_DOMAIN);
                }
            }
        }

        /// <summary> 
        /// Validate session ID
        /// </summary>
        private bool Validate(string sessionID)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(TSB_DOMAIN + "api/customer/" + customerID + "/rwt/rates?nextSequenceId=" + sessionID).Result;

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    if (((int)response.StatusCode) == 403)
                        return false;
                    else
                        throw new Exception("Connection Error " + ((int)response.StatusCode).ToString() + ", Couldn't Reach " + TSB_DOMAIN);
                }
            }
        }

        /// <summary>
        /// Return direction string from boolean
        /// </summary>
        private string GetDirection(bool direction)
        {
            if (direction)
            {
                return "FORWARDS";
            }
            else
            {
                return "BACKWARDS";
            }
        }
    }
}