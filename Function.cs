using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson.Converters;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;

            if (s3Event == null)
            {
                context.Logger.LogError("S3Event is null.");
                return;
            }

            try
            {
                var tempFile = Path.GetTempFileName();

                var getObjectRequest = new GetObjectRequest() { BucketName = s3Event.Bucket.Name, Key = s3Event.Object.Key };
                using var response = await S3Client.GetObjectAsync(getObjectRequest);
                if (response != null)
                {
                    await response.WriteResponseStreamToFileAsync(tempFile, true, new CancellationToken());

                    await using var fs = new FileStream(tempFile, FileMode.Open);
                    ZipArchive zipData = new ZipArchive(fs, ZipArchiveMode.Read);

                    var tasks = new List<Task>();

                    foreach (var entry in zipData.Entries)
                    {
                        tasks.Add(Extract(s3Event.Bucket.Name, Path.GetDirectoryName(s3Event.Object.Key), entry));
                    }
                    await Task.WhenAll(tasks);
                }

                await S3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = s3Event.Bucket.Name, Key = s3Event.Object.Key });

            }
            catch (Exception e)
            {
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
    public async Task Extract(string bucket, string path, ZipArchiveEntry zipData)
    {

        try
        {
            using (var entryStream = zipData.Open())
            {
                StreamReader sr = new StreamReader(entryStream);

                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = Path.Combine(path, zipData.FullName),
                    ContentBody = sr.ReadToEnd(),
                    ContentType = "text/html"
                };
                await S3Client.PutObjectAsync(putObjectRequest);
            }
        }
        catch (Exception)
        {
            //do nothing
        }
    }
}