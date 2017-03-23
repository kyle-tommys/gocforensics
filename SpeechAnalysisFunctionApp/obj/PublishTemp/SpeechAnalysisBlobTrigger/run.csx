#r "Microsoft.WindowsAzure.Storage" 
#r "Newtonsoft.Json"

using System;
using System.Net;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.CognitiveServices.SpeechRecognition;


public static async Task Run(CloudBlockBlob myBlob, string name, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Path: {myBlob.Uri} \n Size:{myBlob.Properties.Length} ");


    var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(AzureStorageAccount.ConnectionString);
    var blobClient = storageAccount.CreateCloudBlobClient();
    var container = blobClient.GetContainerReference(AzureStorageAccount.ContainerNameIn);

    log.Info($"Processing {myBlob.Uri}...");
    var audioHandler = new AudioHandler();
    string audioText = await audioHandler.ProcessBlob(container, name, log);

    container = blobClient.GetContainerReference(AzureStorageAccount.ContainerNameOut);
    container.CreateIfNotExists();
    var blob = container.GetBlockBlobReference(name + ".txt");
    blob.DeleteIfExists();
    blob.UploadText(audioText);

    log.Info($"Audiotext {audioText} ");
    // outputQueueItem = blob.Uri.AbsoluteUri;
    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

    // Retrieve a reference to a queue.
    var queue = queueClient.GetQueueReference(AzureStorageAccount.QueueNameOut);

    // Create the queue if it doesn't already exist.
    queue.CreateIfNotExists();

    // Create a message and add it to the queue.
    CloudQueueMessage message = new CloudQueueMessage(blob.Uri.AbsoluteUri);
    queue.AddMessage(message);

    log.Info($"Queue Item created");
}

public abstract class AzureStorageAccount
{
    public static string ConnectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);

    public static string ContainerNameIn = "speechfiles";

    public static string ContainerNameOut = "speechtextfiles";

    public static string QueueNameOut = "speechtextbloburls";

}

public class AudioHandler
{

    TaskCompletionSource<string> _tcs;
    static DataRecognitionClient _dataClient;

    static AudioHandler()
    {
        _dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                   SpeechRecognitionMode.LongDictation,
                   "en-US",
                    System.Environment.GetEnvironmentVariable("SpeechSubscriptionKey", EnvironmentVariableTarget.Process));
    }

    public AudioHandler()
    {
        _dataClient.OnResponseReceived += responseHandler;
    }

    private void responseHandler(object sender, SpeechResponseEventArgs args)
    {
        if (args.PhraseResponse.Results.Length == 0)
            _tcs.SetResult("ERROR: Bad audio");
        else
            _tcs.SetResult(args.PhraseResponse.Results[0].DisplayText);
        var client = sender as DataRecognitionClient;
        client.OnResponseReceived -= responseHandler;

    }

    public Task<string> ProcessBlob(Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container, string blobName, TraceWriter log)
    {
        _tcs = new TaskCompletionSource<string>();
        log.Info("Ready to read blob");
        var blockBlob = container.GetBlockBlobReference(blobName);
        
        using (Stream stream = new MemoryStream())
        {
            int bytesRead = 0;
            byte[] buffer = new byte[1024];

            blockBlob.DownloadToStream(stream);
            log.Info("Blob read - size=" + stream.Length);
            stream.Position = 0;

            try
            {
                do
                {
                    // Get more Audio data to send into byte buffer.
                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                    // Send of audio data to service. 
                    _dataClient.SendAudio(buffer, bytesRead);
                }
                while (bytesRead > 0);
            }
            finally
            {
                // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                _dataClient.EndAudio();
                log.Info("Finished");
            }

        }
        log.Info("Returning");

        return _tcs.Task;
    }
}

