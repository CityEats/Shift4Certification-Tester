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
                       DateTime.UtcNow.Year.ToString("00");
            var time = DateTime.UtcNow.Hour.ToString("00") + DateTime.UtcNow.Minute.ToString("00") +
                       DateTime.UtcNow.Second.ToString("00");

            // *** Auth -> Access code exchange
            //string exURI = "http://192.168.0.15:16448";
            //string exmyParameters = "STX=yes&Verbose=yes&FunctionRequestCode=CE&APIFormat=0&APISignature=$&AuthToken=A9194722-CD5D-EEA1-56615EDDC6183F88&ClientGUID=63CB3CB7-D244-86DD-F8009C8AFD210CCA&CONTENTTYPE=XML / text&Date=" + date + "&Time=" + time + "&APIOptions=ALLDATA&RequestorReference=" + Guid.NewGuid().ToString() + "&Vendor=Agilysys, Inc:rGuest Seat:4.9.4&ETX=yes";
            //using (WebClient wc = new WebClient())
            //{
            //    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            //    string HtmlResult = wc.UploadString(exURI, exmyParameters);


            //}

            // ***

            string URI = "https://access.shift4test.com";
            string myParameters = "i4go_accessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE&i4go_clientIp=74.135.96.118";

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

            var amount = "1200.00";

            var customerName = HttpUtility.UrlEncode("Andrew Ford");
            var vendorInfo = HttpUtility.UrlEncode("Agilysys, Inc:rGuest Seat:4.9.4");
            var contentType = HttpUtility.UrlEncode("XML / text");

            string uri = "http://192.168.0.17:16448";

            string myParameters = 
                "STX=yes&UniqueId=" + request.i4go_uniqueid 
                + "&CustomerName=" + customerName +
                "&PrimaryAmount=" + amount
                + "&CardEntryMode=M"
                + "&CardPresent=N" 
                + "&CardType=CC" 
                + "&Clerk=56000"
                + "&CustomerReference=" + HttpUtility.UrlEncode(Guid.NewGuid().ToString().Substring(0,12))  
                + "&DestinationZipCode=43206"
                + "&Invoice=" + new Random().Next(1000000000,2000000000)
                + "&Notes=" + HttpUtility.UrlEncode("<p>Reservation made through rGuest Seat</p>") 
                + "&ProductDescriptor1=" + HttpUtility.UrlEncode("Meal Reservation Pre-Payment at Momofuku") 
                + "&ReceiptTextColumns=1"
                + "&TaxAmount=0"
                + "&TaxIndicator=N"
                + "&Verbose=yes&" +
                "FunctionRequestCode=1D" +
                "&APIFormat=0" +
                "&APISignature=" + HttpUtility.UrlEncode("$") +
                "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                "&CONTENTTYPE=" + contentType + 
                "&Date=" + date + "&Time=" + time
                + "&APIOptions=" + HttpUtility.UrlEncode("ALLDATA,ENHANCEDRECEIPTS") 
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


                //Checking that the tran id is NotFiniteNumberException also zero because that indicates a timeout that doesn't need to be voided
                if (auth.xmldata.PrimaryErrorCode != "0" && auth.xmldata.TranId != "0")
                {
                    using (WebClient wcB = new WebClient())
                    {
                        //Let's void this failed transaction.
                        string myVoidParameters = "STX=yes&" +
                                                    "UniqueId=" + request.i4go_uniqueid +
                                                  "&CustomerName=" + customerName + 
                                                  "&Verbose=yes" + 
                                                  "&FunctionRequestCode=08" + 
                                                  "&APIFormat=0" + 
                                                  "&APISignature=" + HttpUtility.UrlEncode("$") +
                                                  "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE" +
                                                  "&CONTENTTYPE=XML / text" +
                                                  "&Date=" + date + 
                                                  "&Time=" + time + 
                                                  "&APIOptions=ALLDATA" +
                                                  "&RequestorReference=" + Guid.NewGuid().ToString().Substring(0,12) +
                                                  "&Vendor=" + vendorInfo + 
                                                  "&ETX=yes";

                        wcB.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string voidResult = wcB.UploadString(uri, myVoidParameters);
                    }
                    return "Sorry, no good!" + auth.xmldata.LongError;
                }

                //If there is a communication error, we need to wait 3 seconds, then issue a status call...
                if (auth.xmldata.TranId == "0")
                {
                    System.Threading.Thread.Sleep(3000);
                    
                    using (WebClient wcC = new WebClient())
                    {
                        string myStatusParameters = "STX=yes&" +
                                                    "UniqueId=" + request.i4go_uniqueid +
                                                    "&FunctionRequestCode=07"
                                                    + "&APIOptions=" + HttpUtility.UrlEncode("ALLDATA,ENHANCEDRECEIPTS")
                                                    + "&AccessToken=6050237A%2DBDD7%2D41E2%2DBB40%2D48E3500B94FE"
                                                    + "&Vendor=" + vendorInfo
                                                    + "&ETX=yes";

                        wcC.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string voidResult = wcC.UploadString(uri, myStatusParameters);

                    }

                    return "Sorry, that transaction didn't go through. Please try again later.";
                }

                //All good.
                return "OK";

            }

        }


        public ActionResult Success()
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
            public string LongError { get; set; }
        }

    }
}