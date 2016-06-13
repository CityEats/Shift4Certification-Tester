using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace Shift4Certification.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {

            var date = DateTime.UtcNow.Month.ToString("00") + DateTime.UtcNow.Day.ToString("00") +
                       DateTime.UtcNow.Year.ToString("00").Substring(2, 2);
            var time = DateTime.UtcNow.Hour.ToString("00") + DateTime.UtcNow.Minute.ToString("00") +
                       DateTime.UtcNow.Second.ToString("00");

            // *** Auth -> Access code exchange
            //string exURI = "http://192.168.0.8:16448";
            //string exmyParameters = "STX=yes&Verbose=yes&FunctionRequestCode=CE&APIFormat=0&APISignature=$&AuthToken=1407566D-A60B-485E-AF02-625C0D441D3B&ClientGUID=63CB3CB7-D244-86DD-F8009C8AFD210CCA&CONTENTTYPE=XML / text&Date=" + date + "&Time=" + time + "&APIOptions=ALLDATA&RequestorReference=" + Guid.NewGuid().ToString() + "&Vendor=Agilysys, Inc:rGuest Seat:4.9.4&ETX=yes";
            //using (WebClient wc = new WebClient())
            //{
            //    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            //    string HtmlResult = wc.UploadString(exURI, exmyParameters);


            //}

            // ***

            string URI = "https://access.shift4test.com";

            var accessToken = "6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE";
            //var accessToken = HttpUtility.UrlEncode("1407566D-A60B-485E-AF02-625C0D441D3B");

            string myParameters = "i4go_accessToken=" + accessToken + "&i4go_clientIp=74.135.96.118";

            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string HtmlResult = wc.UploadString(URI, myParameters);
                var auth = Newtonsoft.Json.JsonConvert.DeserializeObject<Shift4Auth>(HtmlResult);
                ViewBag.Server = auth.i4go_server;
                ViewBag.AccessBlock = auth.i4go_accessblock;

            }

            return View();
        }


        public string AuthorizeCard(Shift4TokenizeResponse request)
        {
            var date = DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                       DateTime.Now.Year.ToString().Substring(2,2);
            var time = DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") +
                       DateTime.Now.Second.ToString("00");

            var amount = "100.00";

            var customerName = HttpUtility.UrlEncode("Andrew Ford");

            var vendorInfo = HttpUtility.UrlEncode("Agilysys, Inc:rGuest Seat:4.9.4");
            //vendorInfo = @"A?6$b&C%d/1=2\4+|://www.shift4.";
            
            var customerReference = "HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0,12))";
            //customerReference = @"A?6$b&C%d/1=2\4+|://www.shift4.";

            var productDescriptor = HttpUtility.UrlEncode("Meal Reservation Pre - Payment at Momofuku");
            //productDescriptor = @"A?6$b&C%d/1=2\4+|://www.shift4.";

            var notes = HttpUtility.UrlEncode("<p>Reservation made through rGuest Seat</p>");
            notes = @"A?6$b&C%d/1=2\4+|://www.shift4.";

            var contentType = HttpUtility.UrlEncode("XML / text");

            string uri = "http://192.168.0.8:16448";

            var invoiceNum = new Random().Next(1000000000, 2000000000);

            string myParameters = 
                "STX=yes&UniqueId=" + request.i4go_uniqueid 
                + "&CustomerName=" + customerName +
                "&PrimaryAmount=" + amount
                + "&CardEntryMode=M"
                + "&CardPresent=N" 
                + "&CardType=CC"
                + "&SaleFlag=S"
                + "&Clerk=56000"
                + "&CustomerReference=" + customerReference 
                + "&DestinationZipCode=43206"
                + "&Invoice=" + invoiceNum
                + "&Notes=" + notes
                + "&ProductDescriptor1=" + productDescriptor 
                + "&ReceiptTextColumns=999"
                + "&TaxAmount=0"
                + "&TaxIndicator=N"
                + "&TokenSerialNumber=266"
                + "&Verbose=yes&" +
                "FunctionRequestCode=1D" +
                "&APIFormat=0" +
                "&APISignature=" + HttpUtility.UrlEncode("$") +
                "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                "&CONTENTTYPE=" + contentType + 
                "&Date=" + date + "&Time=" + time
                + "&APIOptions=" + HttpUtility.UrlEncode("ALLDATA,ENHANCEDRECEIPTS,RETURNMETATOKEN") 
                + "&MetaTokenType=IL"
                + "&RequestorReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0,12))
                + "&Vendor=" + vendorInfo + 
                "&ETX=yes";

            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string htmlResult = wc.UploadString(uri, myParameters);
                
                var doc = new XmlDocument();
                doc.LoadXml(htmlResult);
                var json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc.LastChild);
                var auth = Newtonsoft.Json.JsonConvert.DeserializeObject<Shift4CardAuth>(json);

                //Store the unique ID returned and deserialized into the auth object above. This is used for refunding if needed later.

                //Checking that the tran id is NotFiniteNumberException also zero because that indicates a timeout that doesn't need to be voided
                if ((auth.xmldata.response == "D" | 
                    auth.xmldata.response == "X" | 
                    auth.xmldata.response == "R" | 
                    auth.xmldata.response == "f" | 
                    auth.xmldata.PrimaryErrorCode != "0" |
                    auth.xmldata.CVV2Valid == "N" |
                    auth.xmldata.ValidAVS == "N") 
                    && auth.xmldata.TranId != "0")
                {

                    if (auth.xmldata.PrimaryErrorCode == "9951" && auth.xmldata.SecondaryErrorCode == "4")
                    {
                        return "OK";
                    }

                    date = DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                       DateTime.Now.Year.ToString().Substring(2, 2);
                    time = DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") +
                               DateTime.Now.Second.ToString("00");

                    using (WebClient wcB = new WebClient())
                    {
                        //Let's void this failed transaction.
                        string myVoidParameters = "STX=yes&" +
                                                    "UniqueId=" + auth.xmldata.UniqueId +
                                                  "&Verbose=yes" + 
                                                  "&FunctionRequestCode=08" + 
                                                  "&APIFormat=0" +
                                                  "&Invoice=" + invoiceNum + 
                                                  "&ReceiptTextColumns=999" + 
                                                  "&APISignature=" + HttpUtility.UrlEncode("$") +
                                                  "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                                                  "&CONTENTTYPE=" + contentType +
                                                  "&Date=" + date + 
                                                  "&Time=" + time + 
                                                  "&APIOptions=ALLDATA" +
                                                  "&RequestorReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0, 12)) +
                                                  "&Vendor=" + vendorInfo + 
                                                  "&ETX=yes";

                        wcB.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string voidResult = wcB.UploadString(uri, myVoidParameters);
                    }
                    return "Sorry, no good!";
                }

                //If there is a communication error, we need to wait 3 seconds, then issue a status call...
                if (auth.xmldata.TranId == "0")
                {
                    System.Threading.Thread.Sleep(3000);

                    date = DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                       DateTime.Now.Year.ToString().Substring(2, 2);
                    time = DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") +
                               DateTime.Now.Second.ToString("00");

                    using (WebClient wcC = new WebClient())
                    {
                        string myStatusParameters = "STX=yes&" +
                                                    "UniqueId=" + auth.xmldata.UniqueId +
                                                    "&FunctionRequestCode=07"
                                                    + "&Invoice=" + invoiceNum
                                                    + "&APIFormat=0" 
                                                    + "&APISignature=" + HttpUtility.UrlEncode("$") 
                                                    + "&CONTENTTYPE=" + contentType
                                                    + "&TokenSerialNumber=266"
                                                    + "&Date=" + date 
                                                    + "&Time=" + time 
                                                    + "&ReceiptTextColumns=999" 
                                                    + "&Verbose=yes" 
                                                    + "&RequestorReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0, 12))
                                                    + "&MetaTokenType=IL"
                                                    + "&APIOptions=" + HttpUtility.UrlEncode("ALLDATA,ENHANCEDRECEIPTS,RETURNMETATOKEN")
                                                    + "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE"
                                                    + "&Vendor=" + vendorInfo
                                                    + "&ETX=yes";

                        wcC.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string statusResult = wcC.UploadString(uri, myStatusParameters);

                        var docB = new XmlDocument();
                        docB.LoadXml(statusResult);
                        var jsonB = Newtonsoft.Json.JsonConvert.SerializeXmlNode(docB.LastChild);
                        var authB = Newtonsoft.Json.JsonConvert.DeserializeObject<Shift4CardAuth>(jsonB);

                        if (authB.xmldata.TranId == "0" | (authB.xmldata.response == "e" && (authB.xmldata.authorization == "009951" | authB.xmldata.authorization == "009902" | authB.xmldata.authorization == "009020" | authB.xmldata.authorization == "009957")))
                        {
                            //Status didn't return before the global timeout, therefore we need to log the transaction, and alert the merchant.
                            //*** ALERT MERCHANT VIA EMAIL HERE
                            return "Sorry, that transaction didn't go through. Please try again later.";
                        }

                        if (authB.xmldata.response == "A" | authB.xmldata.response == "C" | (auth.xmldata.PrimaryErrorCode == "9951" && auth.xmldata.SecondaryErrorCode == "4"))
                        {
                            return "OK";
                        }

                        date = DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                       DateTime.Now.Year.ToString().Substring(2, 2);
                        time = DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") +
                                   DateTime.Now.Second.ToString("00");

                        //Status inidcates that the transaction was not approved but the invoice was found in DOTN, so we need to void it.
                        using (WebClient wcB = new WebClient())
                        {
                            //Let's void this failed transaction.
                            string myVoidParameters = "STX=yes&" +
                                                        "UniqueId=" + authB.xmldata.UniqueId +
                                                      "&CustomerName=" + customerName +
                                                      "&Verbose=yes" +
                                                      "&FunctionRequestCode=08" +
                                                      "&APIFormat=0" +
                                                      "&Invoice=" + invoiceNum +
                                                      "&ReceiptTextColumns=999" +
                                                      "&APISignature=" + HttpUtility.UrlEncode("$") +
                                                      "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                                                      "&CONTENTTYPE=" + contentType +
                                                      "&Date=" + date +
                                                      "&Time=" + time +
                                                      "&APIOptions=ALLDATA" +
                                                      "&RequestorReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0, 12)) +
                                                      "&Vendor=" + vendorInfo +
                                                      "&ETX=yes";

                            wcB.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                            string voidResult = wcB.UploadString(uri, myVoidParameters);
                        }
                        return "Sorry, that transaction didn't go through. Please try again later.";

                    }
                }

                //All good.
                return "OK";

            }

        }

        public string RefundTransaction(string uniqueId)
        {
            var date = DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                       DateTime.Now.Year.ToString().Substring(2, 2);
            var time = DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") +
                       DateTime.Now.Second.ToString("00");

            var amount = "300.00";

            var customerName = HttpUtility.UrlEncode("Andrew Ford");
            var vendorInfo = HttpUtility.UrlEncode("Agilysys, Inc:rGuest Seat:4.9.4");
            var contentType = HttpUtility.UrlEncode("XML / text");

            string uri = "http://192.168.0.17:16448";

            var invoiceNum = new Random().Next(1000000000, 2000000000);

            string myParameters =
                "STX=yes&UniqueId=" + uniqueId 
                + "&CustomerName=" + customerName +
                "&PrimaryAmount=" + amount
                + "&CardEntryMode=M"
                + "&CardPresent=N"
                + "&CardType=CC"
                + "&SaleFlag=C"
                + "&Clerk=56000"
                + "&CustomerReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0, 12))
                + "&DestinationZipCode=43206"
                + "&Invoice=" + invoiceNum
                + "&Notes=" + HttpUtility.UrlEncode("<p>Reservation made through rGuest Seat</p>")
                + "&ProductDescriptor1=" + HttpUtility.UrlEncode("Meal Reservation Pre-Payment at Momofuku")
                + "&ReceiptTextColumns=999"
                + "&TaxAmount=0"
                + "&TaxIndicator=N"
                + "&TokenSerialNumber=266"
                + "&Verbose=yes&" +
                "FunctionRequestCode=1D" +
                "&APIFormat=0" +
                "&APISignature=" + HttpUtility.UrlEncode("$") +
                "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                "&CONTENTTYPE=" + contentType +
                "&Date=" + date + "&Time=" + time
                + "&APIOptions=" + HttpUtility.UrlEncode("ALLDATA,ENHANCEDRECEIPTS,RETURNMETATOKEN")
                + "&MetaTokenType=IL"
                + "&RequestorReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0, 12))
                + "&Vendor=" + vendorInfo +
                "&ETX=yes";

            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string htmlResult = wc.UploadString(uri, myParameters);

                var doc = new XmlDocument();
                doc.LoadXml(htmlResult);
                var json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc.LastChild);
                var auth = Newtonsoft.Json.JsonConvert.DeserializeObject<Shift4CardAuth>(json);


            }

            return "Refund OK";
        }

        public
            ActionResult Success()
        {
            return View();
        }

        public class Shift4Auth
        {
            public string i4go_countrycode { get; set; }
            public string i4go_accessblock { get; set; }
            public string i4go_response { get; set; }
            public string i4go_server { get; set; }
            public string i4go_responsecode { get; set; }
        }

        public class Shift4TokenizeResponse
        {
            public string i4go_response { get; set; }
            public string i4go_responsecode { get; set; }
            public string i4go_responsetext { get; set; }
            public string i4go_maskedcard { get; set; }
            public string i4go_uniqueid { get; set; }
            public string i4go_expirationmonth { get; set; }
            public string i4go_expirationyear { get; set; }
            public string i4go_cardtype { get; set; }   
        }

        public class Shift4CardAuth
        {
            public XmlData xmldata { get; set; }
        }

        public class XmlData
        {
            public string TranId { get; set; }
            public string PrimaryErrorCode { get; set; }
            public string SecondaryErrorCode { get; set; }
            public string LongError { get; set; }
            public string response { get; set; }
            public string authorization { get; set; }
            public string invoice { get; set; }
            public string merchantreceipttext { get; set; }
            public string customerreceipttext { get; set; }
            public string UniqueId { get; set; }
            public string ValidAVS { get; set; }
            public string CVV2Valid { get; set; }
        }

    }
}