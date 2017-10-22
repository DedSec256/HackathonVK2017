using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace BOT_Platform.Kernel
{
    public class RequestsManager
    {
        public string access_token { get; private set; }
        public string CompanyINN { get; private set; }

        public RequestsManager(string refresh_token, string device_id, string NewCompanyINN)
        {
            CompanyINN = NewCompanyINN;
            RequestJsons.Authorization_JSON auth = AuthorisationWithRefreshToken(refresh_token, device_id);
            access_token = auth.access_token;
        }

        public enum RequestMethod
        {
            GET, POST, DELETE, PUT
        }

        public enum RequestContentType
        {
            JSON, X_WWW_FROM_URLENCODED
        }

        public class RequestJsons
        {
            public struct Authorization_JSON
            {
                public string access_token { get; set; }
                public string token_type { get; set; }
                public string expires_in { get; set; }
                public string refresh_token { get; set; }
                public string sessionId { get; set; }
            }

            public struct Position_JSON
            {
                public string invoiceId { get; set; }
                public string name { get; set; }
                public int price { get; set; }
                public string unit { get; set; }
                public string sku { get; set; }
                public string vat { get; set; }
                public int productId { get; set; }
                public int amount { get; set; }
            }

            public struct AllPositions_JSON
            {
                public string requestId { get; set; }
                public Position_JSON[] result;
            }

            public struct RequestResult_JSON
            {
                public string requestId { get; set; }
                public string result { get; set; }
            }

            public struct RequestResultPos_JSON
            {
                public string requestId { get; set; }
                public bool success { get; set; }
            }

            public struct Product_JSON
            {
                public string companyInn { get; set; }
                public string sku { get; set; }
                public string name { get; set; }
                public string unitCode { get; set; }
                public string unit { get; set; }
                public int price { get; set; }
                public string vat { get; set; }
                public string originCountryCode { get; set; }
                public string originCountryName { get; set; }
                public int sumExcise { get; set; }
                public string declarationNumber { get; set; }
            }

            public struct AllProducts_JSON
            {
                public string requestId { get; set; }
                public Product_JSON[] result;
            }

            public struct InvoiceCreation_JSON
            {
                public string requestId { get; set; }
                [JsonProperty()]
                public InvoiceCreationResult_JSON result { get; set; }
            }

            public struct InvoiceCreationResult_JSON
            {
                public string id { get; set; }
                public string companyId { get; set; }
                [JsonProperty()]
                public Partner_JSON seller;
                [JsonProperty()]
                public Partner_JSON buyer;
                [JsonProperty()]
                public Payment_JSON payment;
                public int number { get; set; }
                public int priority { get; set; }
                public string status { get; set; }
                public string[] categories { get; set; }
                public string comment { get; set; }
                [JsonProperty()]
                public Modifications_JSON modifications;
                public string groupId { get; set; }
                public string purpose { get; set; }
            }

            public struct Partner_JSON
            {
                public string name { get; set; }
                public string account { get; set; }
                public string inn { get; set; }
                public string address { get; set; }
                public string kpp { get; set; }
                [JsonProperty()]
                public Bank_JSON bank;
            }

            public struct Bank_JSON
            {
                public string name { get; set; }
                public string location { get; set; }
                public string bic { get; set; }
                public string corrAccount { get; set; }
            }

            public struct Payment_JSON
            {
                public string id { get; set; }
                [JsonProperty()]
                public DateTime dueDate { get; set; }
                public string status { get; set; }
                public int sum { get; set; }
                public string operationId { get; set; }
                [JsonProperty()]
                public Acquiring_JSON acquiring;
                [JsonProperty()]
                public Order_JSON order;
            }

            public struct Acquiring_JSON
            {
                public string terminalKey { get; set; }
                public string name { get; set; }
                public int limitOneTime { get; set; }
                public string contactId { get; set; }
            }

            public struct Order_JSON
            {
                public string paymentId { get; set; }
                public string paymentUrl { get; set; }
                public string status { get; set; }
            }

            public struct Modifications_JSON
            {
                public string createdAt { get; set; }
                public string createdBy { get; set; }
                public string updatedAt { get; set; }
                public string updatedBy { get; set; }
                public string deletedAt { get; set; }
                public string deletedBy { get; set; }
                public string sentAt { get; set; }
                public string sentBy { get; set; }
                public string executedAt { get; set; }
                public string executedBy { get; set; }
            }
        }

        // +
        private T GetRequestBuf<T>(byte[] JsonData, string requestURI, RequestContentType requestContentType, RequestMethod method)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestURI);

            switch (requestContentType)
            {
                case RequestContentType.JSON:
                    httpWebRequest.ContentType = "application/json";
                    break;

                case RequestContentType.X_WWW_FROM_URLENCODED:
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    break;
            }
            switch (method)
            {
                case RequestMethod.GET:
                    httpWebRequest.Method = "GET";
                    break;
                case RequestMethod.PUT:
                    httpWebRequest.Method = "PUT";
                    break;
                case RequestMethod.DELETE:
                    httpWebRequest.Method = "DELETE";
                    break;
                case RequestMethod.POST:
                    httpWebRequest.Method = "POST";
                    break;
            }

            if (access_token != null)
                httpWebRequest.Headers.Add("Authorization: Bearer " + access_token);

            if (method != RequestMethod.GET && method != RequestMethod.DELETE && JsonData != null)
            {
                using (var streamWriter = httpWebRequest.GetRequestStream())
                {
                    streamWriter.Write(JsonData, 0, JsonData.Length);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string res;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                res = streamReader.ReadToEnd();
            }

            T result = JsonConvert.DeserializeObject<T>(res);
            return result;
        }

        // +
        private T GetRequest<T>(string requestInURL, string requestURI, RequestContentType requestContentType, RequestMethod method)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestURI);

            switch (requestContentType)
            {
                case RequestContentType.JSON:
                    httpWebRequest.ContentType = "application/json";
                    break;

                case RequestContentType.X_WWW_FROM_URLENCODED:
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    break;
            }

            switch (method)
            {
                case RequestMethod.GET:
                    httpWebRequest.Method = "GET";
                    break;
                case RequestMethod.PUT:
                    httpWebRequest.Method = "PUT";
                    break;
                case RequestMethod.DELETE:
                    httpWebRequest.Method = "DELETE";
                    break;
                case RequestMethod.POST:
                    httpWebRequest.Method = "POST";
                    break;
            }

            if (access_token != null)
                httpWebRequest.Headers.Add("Authorization: Bearer " + access_token);

            if (method != RequestMethod.GET && method != RequestMethod.DELETE && requestInURL != null)
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(requestInURL);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            string res;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                res = streamReader.ReadToEnd();
            }

            T result = JsonConvert.DeserializeObject<T>(res);
            return result;
        }

        // +
        public RequestJsons.Authorization_JSON AuthorisationWithRefreshToken(string refresh_token, string device_id)
        {
            string requestInURL = "grant_type=refresh_token" + "&" +
                                  "refresh_token=" + HttpUtility.UrlEncode(refresh_token) + "&" +
                                  "device_id=" + HttpUtility.UrlEncode(device_id);

            string requestURI = "https://openapi.tinkoff.ru/sso/secure/token";

            return GetRequest<RequestJsons.Authorization_JSON>(requestInURL, requestURI, RequestContentType.X_WWW_FROM_URLENCODED, RequestMethod.POST);
        }

        // -
        public RequestJsons.InvoiceCreation_JSON CreateInvoice(RequestsManager.RequestJsons.InvoiceCreationResult_JSON invoiceCreation)
        {
            string serialize = JsonConvert.SerializeObject(invoiceCreation);

            byte[] buf = Encoding.UTF8.GetBytes(serialize);
            string requestURI = String.Format("https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/{0}/invoice/outgoing", CompanyINN);

            return GetRequestBuf<RequestJsons.InvoiceCreation_JSON>(buf, requestURI, RequestContentType.JSON, RequestMethod.POST);
        }

        // + 
        public RequestJsons.RequestResultPos_JSON AddOrChangePosition(string invoiceId, int itemId, RequestJsons.Position_JSON position)
        {
            // string requestInURL = String.Format("name={0}&price={1}&unit={2}&sku={3}&vat={4}&productId={5}&amount={6}", HttpUtility.UrlEncode(position.name), position.price, position.unit, position.sku, position.vat, position.productId, position.amount);
            // string requestURI = String.Format("https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/{0}/invoice/outgoing/{1}/item/{2}", CompanyINN, invoiceId, itemId);

            byte[] buf = Encoding.Default.GetBytes(JsonConvert.SerializeObject(position));

            string requestURI = "https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/1239537766/invoice/outgoing/02a7086e-338b-4f7a-8189-14f964727732/item/2";

            return GetRequestBuf<RequestJsons.RequestResultPos_JSON>(buf, requestURI, RequestContentType.JSON, RequestMethod.PUT);
        }

        // +
        public RequestJsons.RequestResult_JSON DeletePosition(string invoiceId, int itemId)
        {
            string requestURI = String.Format("https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/{0}/invoice/outgoing/{1}/item/{2}", CompanyINN, invoiceId, itemId);

            return GetRequest<RequestJsons.RequestResult_JSON>(null, requestURI, RequestContentType.JSON, RequestMethod.DELETE);
        }

        // +
        public RequestJsons.AllPositions_JSON GetAllPositions(string invoiceId)
        {
            string requestURI = "https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/" + CompanyINN + "/invoice/outgoing/" + invoiceId + "/items";

            return GetRequest<RequestJsons.AllPositions_JSON>(null, requestURI, RequestContentType.JSON, RequestMethod.GET);
        }

        // +  
        public RequestJsons.RequestResult_JSON DeleteInvoice(string invoiceId)
        {
            string requestURI = String.Format("https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/{0}/invoice/outgoing/{1}", CompanyINN, invoiceId);

            return GetRequest<RequestJsons.RequestResult_JSON>(null, requestURI, RequestContentType.JSON, RequestMethod.DELETE);
        }

        // +
        public RequestJsons.RequestResult_JSON SendInvoice(string invoiceId)
        {
            string requestURI = String.Format("https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/{0}/invoice/outgoing/{1}/send", CompanyINN, invoiceId);

            return GetRequest<RequestJsons.RequestResult_JSON>(null, requestURI, RequestContentType.JSON, RequestMethod.POST);
        }

        // +
        public RequestJsons.AllProducts_JSON GetAllProducts()
        {
            string requestURI = "https://openapi.tinkoff.ru/invoicing/api/v1/partner/company/" + CompanyINN + "/invoice/products";

            return GetRequest<RequestJsons.AllProducts_JSON>(null, requestURI, RequestContentType.JSON, RequestMethod.GET);
        }
    }

    class Programm
    {
        static void Main(string[] args)
        {
            RequestsManager req =
                new RequestsManager(
                    "0c/e+6M/oLT2B9HoIdQGaPPoqmzz9pRxhzNHHrd/H+E38zKxPCX5RqFOxrcDnXqgpGIYjFVvppSk468MK6F6vg==",
                    "user31", "1239537766");

            // RequestsManager.RequestJsons.RequestResult_JSON delPos = req.DeletePosition("02a7086e-338b-4f7a-8189-14f964727732", "1");

            RequestsManager.RequestJsons.Position_JSON position = new RequestsManager.RequestJsons.Position_JSON();
            position.name = "jepa";
            position.productId = 2;
            position.price = 10;
            position.sku = "4258";
            position.unit = "g";
            position.amount = 10;
            position.vat = "18";


            RequestsManager.RequestJsons.AllPositions_JSON allPos =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");
            RequestsManager.RequestJsons.RequestResultPos_JSON addPos =
                req.AddOrChangePosition("02a7086e-338b-4f7a-8189-14f964727732", 2, position);
            RequestsManager.RequestJsons.AllPositions_JSON allPos1 =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");
            RequestsManager.RequestJsons.RequestResult_JSON delPos =
                req.DeletePosition("02a7086e-338b-4f7a-8189-14f964727732", 2);
            RequestsManager.RequestJsons.AllPositions_JSON allPos3 =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");

            RequestsManager.RequestJsons.InvoiceCreationResult_JSON invoiceCreate =
                new RequestsManager.RequestJsons.InvoiceCreationResult_JSON();
            invoiceCreate.seller = new RequestsManager.RequestJsons.Partner_JSON();

            invoiceCreate.seller.inn = "1239537766";
            invoiceCreate.seller.account = "40101810900000000974";
            invoiceCreate.seller.bank = new RequestsManager.RequestJsons.Bank_JSON();

            invoiceCreate.buyer = new RequestsManager.RequestJsons.Partner_JSON();
            invoiceCreate.buyer.inn = "1101714765";
            invoiceCreate.buyer.bank = new RequestsManager.RequestJsons.Bank_JSON();
            invoiceCreate.buyer.bank.name = "АО \"ТИНЬКОФФ БАНК\"";
            invoiceCreate.buyer.bank.location = "Москва, 123060, 1-й Волоколамский проезд, д. 10, стр. 1";
            invoiceCreate.buyer.bank.bic = "044525974";
            invoiceCreate.buyer.bank.corrAccount = "30101810145250000974";

            invoiceCreate.payment = new RequestsManager.RequestJsons.Payment_JSON();
            invoiceCreate.payment.status = "DRAFT";
            invoiceCreate.payment.sum = 24;

            invoiceCreate.number = 123;
            invoiceCreate.status = "DRAFT";

            invoiceCreate.modifications = new RequestsManager.RequestJsons.Modifications_JSON();
            invoiceCreate.modifications.createdAt = "2017-02-17T16:27:43.955+03:00";
            invoiceCreate.modifications.createdBy = "dehf";


            RequestsManager.RequestJsons.InvoiceCreation_JSON ic = req.CreateInvoice(invoiceCreate);
        }
    }
}
