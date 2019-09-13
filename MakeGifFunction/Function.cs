using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Demo.GifMaker.MakeGifFunction {

    public class Function : ALambdaFunction<S3Event, string> {

        //--- Fields ---
        private IAmazonS3 _s3Client;
        private string _destinationBucketName;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _s3Client = new AmazonS3Client();

            // show contents of /opt folder; the 'ffmpeg' file must have execution permissions to be invocable
            LogInfo(Exec("/bin/bash", "-c \"ls -al /opt\"").Output);

            // read configuration parameters
            _destinationBucketName = config.ReadS3BucketName("AnimatedGifBucket");
        }

        public override async Task<string> ProcessMessageAsync(S3Event request) {
            LogInfo($"received {request.Records.Count} records");

            // process records
            foreach(var record in request.Records) {
                string originalFilePath = null;
                string convertedFilePath = null;
                try {

                    // download file to temporary storage
                    LogInfo($"downloading 's3://{record.S3.Bucket.Name}/{record.S3.Object.Key}'");
                    var response = await _s3Client.GetObjectAsync(new GetObjectRequest {
                        BucketName = record.S3.Bucket.Name,
                        Key = record.S3.Object.Key
                    });
                    originalFilePath = $"/tmp/{Path.GetFileName(record.S3.Object.Key)}";
                    await response.WriteResponseStreamToFileAsync(
                        filePath: originalFilePath,
                        append: false,
                        cancellationToken: CancellationToken.None
                    );

                    // run ffmpeg process to convert file to gif
                    LogInfo("invoking ffmpeg converter");
                    convertedFilePath = Path.ChangeExtension(originalFilePath, ".gif");
                    var outcome = Exec("/opt/ffmpeg", $"-i \"{originalFilePath}\" -f gif \"{convertedFilePath}\"");
                    if(outcome.ExitCode != 0) {
                        LogWarn(
                            "FFmpeg exit code: {0}\nError Output: {1}\nStandard Output: {2}",
                            outcome.ExitCode,
                            outcome.Error,
                            outcome.Output
                        );
                        continue;
                    }

                    // upload converted file
                    var targetKey = Path.Combine(Path.GetDirectoryName(record.S3.Object.Key), Path.GetFileName(convertedFilePath));
                    LogInfo($"uploading 's3://{_destinationBucketName}/{targetKey}'");
                    await _s3Client.PutObjectAsync(new PutObjectRequest {
                        BucketName = _destinationBucketName,
                        Key = targetKey,
                        FilePath = convertedFilePath
                    });
                } catch(Exception e) {

                    // log the error and continue to the next item
                    LogError(e);
                } finally {

                    // delete the original and converted files
                    DeleteFile(originalFilePath);
                    DeleteFile(convertedFilePath);
                }
            }
            return "Ok";
        }

        private (int ExitCode, string Output, string Error) Exec(string application, string arguments) {
            LogInfo($"executing: {application} {arguments}");
            using(var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = application,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            }) {
                process.Start();
                var output = Task.Run(() => process.StandardOutput.ReadToEndAsync());
                var error = Task.Run(() => process.StandardError.ReadToEndAsync());
                process.WaitForExit();
                return (ExitCode: process.ExitCode, Output: output.Result, Error: error.Result);
            }
        }

        private void DeleteFile(string filePath) {
            if(filePath == null) {
                return;
            }
            try {
                File.Delete(filePath);
            } catch {
                LogWarn("unable to delete: {0}", filePath);
            }
        }
    }
}
