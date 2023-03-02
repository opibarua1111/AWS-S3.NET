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
    }
}
