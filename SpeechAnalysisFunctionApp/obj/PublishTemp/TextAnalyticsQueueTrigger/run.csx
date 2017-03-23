#r "Microsoft.WindowsAzure.Storage" 
#r "Newtonsoft.Json"

using System;
using System.Web;
using System.Text;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
//additions for database
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//additions for database.

public static void Run(string myQueueItem, out object textDocument, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {myQueueItem}");

    string file_text = getBlobText(myQueueItem, log);

    string outPut_KeyPhrases = AnalyseText(file_text, log);

    string outPut_Sentiment = AnalyseSentiment(file_text, log);

    log.Info($"Computer Vision Response : {outPut_KeyPhrases}");


    JObject textDoc = JObject.Parse(outPut_KeyPhrases);
    JObject sentDoc = JObject.Parse(outPut_Sentiment);

    string id_Val = (string)textDoc["documents"][0]["id"];
    JArray keyphrases_val = (JArray)textDoc["documents"][0]["keyPhrases"];
    string sent_Val = (string)sentDoc["documents"][0]["score"];

    //makes JSON to sent to DB
    textDocument = new
    {
        fileType = "document",
        action = "textanalytics",
        filetext = file_text,
        url = myQueueItem,
        id = id_Val,
        keyphrases = keyphrases_val,
        sentiment = sent_Val
    };


}

private static string getBlobText(string blobUrl, TraceWriter log)
{
    Uri uri = new Uri(blobUrl);
    CloudBlockBlob blob = new CloudBlockBlob(uri);
    var blobText = blob.DownloadText();
    log.Info($"Blob Text : {blobText}");
    return blobText;
}

private static string AnalyseText(string file_text, TraceWriter log)
{


    var BaseUrl = "https://westus.api.cognitive.microsoft.com/";

    log.Info($"Request body: {BaseUrl}");

    //Keyphrases

    var urikp = "text/analytics/v2.0/keyPhrases";

    var textString = file_text;

    var idValue = Guid.NewGuid();

    var uri = BaseUrl + urikp;

    log.Info($"Calling Uri: {uri}");

    byte[] byteData = Encoding.UTF8.GetBytes("{\"documents\":[" +
                    "{\"id\":\"" + idValue + "\",\"text\":\"" + textString + "\"}]}");

    string response = HttpPost(uri, byteData, log);

    return response;
}

private static string AnalyseSentiment(string file_text, TraceWriter log)
{


    var BaseUrl = "https://westus.api.cognitive.microsoft.com/";

    log.Info($"Request body: {BaseUrl}");

    //Keyphrases

    var urikp = "text/analytics/v2.0/sentiment";

    var textString = file_text;

    var idValue = Guid.NewGuid();

    var uri = BaseUrl + urikp;

    log.Info($"Calling Uri: {uri}");

    byte[] byteData = Encoding.UTF8.GetBytes("{\"documents\":[" +
                    "{\"id\":\"" + idValue + "\",\"text\":\"" + textString + "\"}]}");

    string response = HttpPost(uri, byteData, log);

    return response;
}

public static string HttpPost(string URI, byte[] bytes, TraceWriter log)
{

    var subscriptionKey = System.Environment.GetEnvironmentVariable("TextAnalyticsSubscriptionKey", EnvironmentVariableTarget.Process);
    System.Net.WebRequest req = System.Net.WebRequest.Create(URI);
    req.Method = "POST";
    req.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

    log.Info($"Posting data");

    req.ContentLength = bytes.Length;
    System.IO.Stream os = req.GetRequestStream();
    os.Write(bytes, 0, bytes.Length);
    os.Close();
    System.Net.WebResponse resp = req.GetResponse();
    if (resp == null) return null;
    System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
    return sr.ReadToEnd().Trim();
}
