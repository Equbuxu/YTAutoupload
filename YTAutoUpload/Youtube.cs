using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;


namespace YTAutoUpload
{
    public class Youtube
    {
        YouTubeService service;
        public Youtube()
        {
        }

        public bool UploadVideo(string videoPath, string title, string description, string[] tags, bool unlisted)
        {
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = title;
            video.Snippet.Description = description;
            video.Snippet.Tags = tags;
            video.Snippet.CategoryId = "22";
            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = unlisted ? "unlisted" : "public";
            var filePath = videoPath;

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = service.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                IUploadProgress result = videosInsertRequest.Upload();
                return (result.Status == UploadStatus.Completed);
            }
        }

        public bool Auth(string secretPath)
        {
            try
            {
                UserCredential credential;
                using (var stream = new FileStream(secretPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        new[] { YouTubeService.Scope.YoutubeUpload },
                        "user",
                        CancellationToken.None
                    ).Result;
                }

                service = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,

                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
                });
            }
            catch (AggregateException)
            {
                return false;
            }
            return true;
        }
    }

}