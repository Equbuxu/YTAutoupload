using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace YTAutoUpload
{
    public class Youtube
    {
        private YouTubeService service;
        private Dictionary<string, string> cachedPlaylists = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> CachedPlaylists
        {
            get
            {
                return cachedPlaylists;
            }
        }

        public Youtube()
        {
        }

        public bool UploadVideo(string videoPath, string title, string description, string[] tags, bool unlisted, out string id)
        {
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = title;
            video.Snippet.Description = description;
            video.Snippet.Tags = tags;
            video.Snippet.CategoryId = "20"; //gaming
            video.Status = new VideoStatus();
            video.Status.MadeForKids = false;
            video.Status.PrivacyStatus = unlisted ? "unlisted" : "public";

            using (var fileStream = new FileStream(videoPath, FileMode.Open))
            {
                var videosInsertRequest = service.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                IUploadProgress result = videosInsertRequest.Upload();
                id = (result.Status == UploadStatus.Completed) ? videosInsertRequest.ResponseBody.Id : null;
                return (result.Status == UploadStatus.Completed);
            }
        }

        public bool AddVideoToPlaylist(string videoId, string playlistId)
        {
            PlaylistItem body = new PlaylistItem();
            body.Snippet = new PlaylistItemSnippet();
            body.Snippet.PlaylistId = playlistId;
            body.Snippet.ResourceId = new ResourceId();
            body.Snippet.ResourceId.Kind = "youtube#video";
            body.Snippet.ResourceId.VideoId = videoId;
            try
            {
                service.PlaylistItems.Insert(body, "snippet").Execute();
            }
            catch (Google.GoogleApiException)
            {
                return false;
            }
            return true;
        }

        public bool CreatePlaylist(string title, string description, bool unlisted, string[] tags)
        {
            Playlist body = new Playlist();
            body.Snippet = new PlaylistSnippet();
            body.Snippet.Title = title;
            body.Snippet.Description = description;
            body.Status = new PlaylistStatus();
            body.Status.PrivacyStatus = unlisted ? "unlisted" : "public";
            body.Snippet.Tags = tags;

            Playlist result;
            try
            {
                result = service.Playlists.Insert(body, "snippet,status").Execute();
            }
            catch (Google.GoogleApiException)
            {
                return false;
            }

            if (!cachedPlaylists.ContainsKey(result.Snippet.Title))
                cachedPlaylists.Add(result.Snippet.Title, result.Id);

            return true;
        }

        public bool UpdateCachedPlaylists()
        {
            Dictionary<string, string> requestedPlaylists = new Dictionary<string, string>();

            string pageToken = null;

            while (true)
            {
                var request = service.Playlists.List("snippet,contentDetails");
                request.MaxResults = 50;
                request.Mine = true;
                if (pageToken != null)
                    request.PageToken = pageToken;
                PlaylistListResponse response;
                try
                {
                    response = request.Execute();
                }
                catch (Google.GoogleApiException)
                {
                    return false;
                }


                foreach (var item in response.Items)
                {
                    if (!requestedPlaylists.ContainsKey(item.Snippet.Title))
                        requestedPlaylists.Add(item.Snippet.Title, item.Id);
                }

                if (response.NextPageToken != null)
                {
                    pageToken = response.NextPageToken;
                    continue;
                }
                break;
            }

            cachedPlaylists = requestedPlaylists;

            return true;
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
                        new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
                        "user_new2",
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