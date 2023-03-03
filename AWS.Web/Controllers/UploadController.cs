using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AWS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _client;

        public UploadController(IConfiguration config)
        {
            _config = config;

            var awsAccess = _config.GetValue<string>("AWSSDK:AccessKey");
            var awsSecret = _config.GetValue<string>("AWSSDK:SecretKey");

            _client = new AmazonS3Client(awsAccess, awsSecret, RegionEndpoint.EUCentral1);
        }

        public EventHandler<StreamTransferProgressArgs> UploadPartProgresEventCallback { get; private set; }

        [HttpPost("upload-file/{bucketName}")]
        public async Task<IActionResult> UploadFile(string bucketName)
        {
            var fileStream = CreateDataStream();
            var fileKey = "multipart_upload_file.mp4";

           
                //Initial multipart upload
                InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest()
                {
                    BucketName = bucketName,
                    Key = fileKey
                };
                InitiateMultipartUploadResponse initiateResponse = await _client.InitiateMultipartUploadAsync(initiateRequest);

                var contentLength = fileStream.Length;
                int chunkSize = 5 * (int)Math.Pow(2, 10);
                var chunkList = new List<PartETag>();

                try
                {
                    int filePosition = 0;
                    for (int i = 0; filePosition < contentLength; i++)
                    {
                        UploadPartRequest uploadPartRequest = new UploadPartRequest()
                        {
                            BucketName = bucketName,
                            Key = fileKey,
                            UploadId = initiateResponse.UploadId,
                            PartSize = chunkSize,
                            InputStream = fileStream
                        };

                    /*uploadPartRequest.StreamTransferProgress += new EventHandler<StreamTransferProgressArgs>
                        (UploadPartProgresEventCallback);*/

                        UploadPartResponse uploadPartResponse = await _client.UploadPartAsync(uploadPartRequest);
                        chunkList.Add(new PartETag()
                        {
                            ETag = uploadPartResponse.ETag,
                            PartNumber = 1
                        });
                        filePosition += chunkSize;
                    }
                    //complete multipart upload
                    CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest()
                    {
                        BucketName = bucketName,
                        Key = fileKey,
                        UploadId = initiateResponse.UploadId,
                        PartETags = chunkList
                    };
                    await _client.CompleteMultipartUploadAsync(completeRequest);
                return Ok("file was uploaded");
                }
                catch (Exception)
                {
                  AbortMultipartUploadRequest abortRequest = new AbortMultipartUploadRequest()
                  {
                      BucketName = bucketName,
                      Key=fileKey,
                      UploadId=initiateResponse.UploadId
                  };
                  await _client.AbortMultipartUploadAsync(abortRequest);
                  return BadRequest("File Couldn't be uploaded");
                }
           
        }
        private FileStream CreateDataStream()
        {
            FileStream fileStream = System.IO.File.OpenRead("Uploads/Tera_Ban.mp3");
            return fileStream;
        }
    }
}
