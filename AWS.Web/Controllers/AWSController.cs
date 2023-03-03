using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AWS.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWSController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _client;

        public AWSController(IConfiguration config)
        {
            _config = config;

            var awsAccess = _config.GetValue<string>("AWSSDK:AccessKey");
            var awsSecret = _config.GetValue<string>("AWSSDK:SecretKey");

            _client = new AmazonS3Client(awsAccess, awsSecret, RegionEndpoint.EUCentral1);
        }
        [HttpGet("list-buckets")]
        public async Task<IActionResult> ListBuckets()
        {
            try
            {
                var result = await _client.ListBucketsAsync();
                return Ok (result);
            }
            catch (Exception)
            {
                return BadRequest("Bucket could not be listed");
            }

        }
        [HttpPost("create-bucket/{name}")]
        public async Task<IActionResult> CreatBucket(string name)
        {
            try
            {
                PutBucketRequest request = new PutBucketRequest() { BucketName = name };

                var result = await _client.PutBucketAsync(request);
                return Ok($"Bucket: {name} was created");
            }
            catch (Exception)
            {
                return BadRequest($"Bucket: {name} was not created");
            }

        }

        [HttpPost("create-object/{bucketName}/{objectName}")]
        public async Task<IActionResult> CreatObject(string bucketName, string objectName)
        {
            try
            {
                FileInfo file = new FileInfo(@"C:\AWSFiles\ThankYou.txt");
                PutObjectRequest request = new PutObjectRequest()
                {
                    InputStream = file.OpenRead(),
                    BucketName = bucketName,
                    Key = "ThankYou.txt",
                };

                await _client.PutObjectAsync(request);

                ListObjectsRequest objectsRequest = new ListObjectsRequest()
                {
                    BucketName = bucketName
                };
                ListObjectsResponse response = await _client.ListObjectsAsync(objectsRequest);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest($"File was not created/uploaded");
            }
        }

        [HttpGet("list-object/{bucketName}")]
        public async Task<IActionResult> ListObjects(string bucketName)
        {
            try
            {
                ListObjectsRequest objectsRequest = new ListObjectsRequest()
                {
                    BucketName = bucketName
                };
                ListObjectsResponse response = await _client.ListObjectsAsync(objectsRequest);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest($"Object couldn't be listed!");
            }
        }

       

        [HttpPost("create-folder/{bucketName}/{folderName}")]
        public async Task<IActionResult> CreateFolder(string bucketName, string folderName)
        {
            try
            {
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = folderName.Replace("%2F", "/")
                };

                var result = await _client.PutObjectAsync(request);

                return Ok($"Bucket: {folderName} folder was created inside {bucketName}");
            }
            catch (Exception)
            {
                return BadRequest("The folder couldn't be created");
            }

        }

        [HttpDelete("delete-bucket/{bucketName}")]
        public async Task<IActionResult> DeleteBucket(string bucketName)
        {
            try
            {
                DeleteBucketRequest request = new DeleteBucketRequest() { BucketName = bucketName };

                var result = await _client.DeleteBucketAsync(request);

                return Ok($"{bucketName} Bucket was deleted!");
            }
            catch (Exception)
            {
                return BadRequest($"{bucketName} Bucket was not deleted!");
            }

        }

        [HttpDelete("delete-bucket-object/{bucketName}/{objectName}")]
        public async Task<IActionResult> DeleteBucketObject(string bucketName, string objectName)
        {
            try
            {
                DeleteObjectRequest request = new DeleteObjectRequest() 
                { 
                    BucketName = bucketName,
                    Key = objectName
                };

                await _client.DeleteObjectAsync(request);

                return Ok($" {objectName} in {bucketName} Bucket was deleted!");
            }
            catch (Exception)
            {
                return BadRequest($" {objectName} in {bucketName} Bucket was not deleted!");
            }

        }

        [HttpDelete("cleanup-bucket/{bucketName}")]
        public async Task<IActionResult> CleanUpBucket(string bucketName)
        {
            try
            {
                DeleteObjectsRequest request = new DeleteObjectsRequest()
                {
                    BucketName = bucketName,
                    Objects  = new List<KeyVersion>
                    {
                        new KeyVersion(){Key = "old/ThankYouOld.txt"},
                        new KeyVersion(){Key = "ThankYou.txt"},
                    }
                };

                await _client.DeleteObjectsAsync(request);

                return Ok($"{bucketName} Bucket was cleaned up!");
            }
            catch (Exception)
            {
                return BadRequest($"{bucketName} Bucket was not cleaned up!");
            }

        }

        [HttpPost("enable-versioning/{bucketName}")]
        public async Task<IActionResult> EnableVersioning(string bucketName)
        {
            try
            {
                PutBucketVersioningRequest request = new PutBucketVersioningRequest
                {
                    BucketName = bucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                };

                var result = await _client.PutBucketVersioningAsync(request);

                return Ok($"Bucket: {bucketName} Bucket Versioning Enabled!");
            }
            catch (Exception)
            {
                return BadRequest($"Bucket: {bucketName} Bucket Versioning not Enabled!");
            }

        }

        [HttpPut("add-metadata/{bucketName}/{fileName}")]
        public async Task<IActionResult> AddMetadata(string bucketName, string fileName)
        {
            try
            {
                Tagging newTags = new Tagging()
                {
                    TagSet = new List<Tag>
                    {
                        new Tag {Key = "key1", Value="FirstTag"},
                        new Tag {Key = "key2", Value="SecondTag"}
                    }
                };
                PutObjectTaggingRequest request = new PutObjectTaggingRequest()
                {
                    BucketName = bucketName,
                    Key = fileName,
                    Tagging = newTags
                };
                var result = await _client.PutObjectTaggingAsync(request);

                return Ok($"Tag added");
            }
            catch (Exception)
            {
                return BadRequest($"Tag couldn't be added!");
            }

        }

        [HttpPut("copy-file/{sourceBucket}/{sourceKey}/{destinationBucket}/{destinationKey}")]
        public async Task<IActionResult> CopyFile(string sourceBucket, string sourceKey, string destinationBucket,
            string destinationKey)
        {
            try
            {
                CopyObjectRequest request = new CopyObjectRequest()
                {
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey,
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationKey
                };
                var result = await _client.CopyObjectAsync(request);

                return Ok($"Object/file copied");
            }
            catch (Exception)
            {
                return BadRequest($"Object/file not copied");
            }

        }

        [HttpGet("grnerate-download-link/{bucketName}/{keyName}")]
        public IActionResult GenerateDownloadLink(string bucketName, string keyName)
        {
            try
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    Expires = DateTime.Now.AddHours(5),
                    Protocol = Protocol.HTTP
                };
                string downloadLink = _client.GetPreSignedURL(request);

                return Ok($"Dewnload link: {downloadLink}");
            }
            catch (Exception)
            {
                return BadRequest($"Download Link was not generated");
            }

        }
    }
}
