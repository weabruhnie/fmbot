using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FMBot.Bot.Configurations;
using FMBot.Bot.Extensions;
using FMBot.Bot.Models;
using FMBot.Bot.Resources;
using FMBot.LastFM.Domain.Models;
using FMBot.LastFM.Domain.ResponseModels;
using FMBot.LastFM.Domain.Types;
using FMBot.LastFM.Services;
using FMBot.Persistence.Domain.Models;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using Track = FMBot.LastFM.Domain.Models.Track;

namespace FMBot.Bot.Services
{
    internal class LastFMService
    {
        private readonly LastfmClient _lastFMClient = new LastfmClient(ConfigData.Data.LastFm.Key, ConfigData.Data.LastFm.Secret);

        private readonly ILastfmApi _lastfmApi;

        public LastFMService(ILastfmApi lastfmApi)
        {
            this._lastfmApi = lastfmApi;
        }

        // Recent scrobbles
        public async Task<PageResponse<LastTrack>> GetRecentScrobblesAsync(string lastFMUserName, int count = 2)
        {
            var recentScrobbles = await this._lastFMClient.User.GetRecentScrobbles(lastFMUserName, null, count: count);
            Statistics.LastfmApiCalls.Inc();

            return recentScrobbles;
        }


        public static string TrackToLinkedString(LastTrack track)
        {
            if (track.Url.ToString().IndexOfAny(new[] { '(', ')' }) >= 0)
            {
                return TrackToString(track);
            }

            return $"[{track.Name}]({track.Url})\n" +
                   $"By **{track.ArtistName}**" +
                   (string.IsNullOrWhiteSpace(track.AlbumName)
                       ? "\n"
                       : $" | *{track.AlbumName}*\n");
        }

        public static string TrackToString(LastTrack track)
        {
            return $"{track.Name}\n" +
                   $"By **{track.ArtistName}**" +
                   (string.IsNullOrWhiteSpace(track.AlbumName)
                       ? "\n"
                       : $" | *{track.AlbumName}*\n");
        }

        public static string TrackToOneLinedString(LastTrack track)
        {
            return $"{track.Name} By **{track.ArtistName}**" +
                   (string.IsNullOrWhiteSpace(track.AlbumName)
                       ? ""
                       : $" | *{track.AlbumName}*");
        }

        public string TagsToLinkedString(Tags tags)
        {
            var tagString = "";
            for (var i = 0; i < tags.Tag.Length; i++)
            {
                if (i != 0)
                {
                    tagString += " - ";
                }
                var tag = tags.Tag[i];
                tagString += $"[{tag.Name}]({tag.Url})";
            }

            return tagString;
        }

        public string TopTagsToString(Toptags tags)
        {
            var tagString = "";
            for (var i = 0; i < tags.Tag.Length; i++)
            {
                if (i != 0)
                {
                    tagString += " - ";
                }
                var tag = tags.Tag[i];
                tagString += $"{tag.Name}";
            }

            return tagString;
        }

        // User
        public async Task<LastResponse<LastUser>> GetUserInfoAsync(string lastFMUserName)
        {
            var user = await this._lastFMClient.User.GetInfoAsync(lastFMUserName);
            Statistics.LastfmApiCalls.Inc();

            return user;
        }

        public async Task<PageResponse<LastTrack>> SearchTrackAsync(string searchQuery)
        {
            var trackSearch = await this._lastFMClient.Track.SearchAsync(searchQuery, itemsPerPage: 1);
            Statistics.LastfmApiCalls.Inc();

            return trackSearch;
        }

        // Track info
        public async Task<Track> GetTrackInfoAsync(string trackName, string artistName, string username = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                {"artist", artistName },
                {"track", trackName },
                {"username", username },
                {"autocorrect", "1"}
            };

            var trackCall = await this._lastfmApi.CallApiAsync<TrackResponse>(queryParams, Call.TrackInfo);
            Statistics.LastfmApiCalls.Inc();

            return !trackCall.Success ? null : trackCall.Content.Track;
        }

        public async Task<Response<ArtistResponse>> GetArtistInfoAsync(string artistName, string username = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                {"artist", artistName },
                {"username", username },
                {"autocorrect", "1"}
            };

            var artistCall = await this._lastfmApi.CallApiAsync<ArtistResponse>(queryParams, Call.ArtistInfo);
            Statistics.LastfmApiCalls.Inc();

            return artistCall;
        }

        public async Task<Response<AlbumResponse>> GetAlbumInfoAsync(string artistName, string albumName, string username = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                {"artist", artistName },
                {"album", albumName },
                {"username", username }
            };

            var albumCall = await this._lastfmApi.CallApiAsync<AlbumResponse>(queryParams, Call.AlbumInfo);
            Statistics.LastfmApiCalls.Inc();

            return albumCall;
        }

        public async Task<PageResponse<LastAlbum>> SearchAlbumAsync(string searchQuery)
        {
            var albumSearch = await this._lastFMClient.Album.SearchAsync(searchQuery, itemsPerPage: 1);
            Statistics.LastfmApiCalls.Inc();

            return albumSearch;
        }

        // Album images
        public async Task<LastImageSet> GetAlbumImagesAsync(string artistName, string albumName)
        {
            var album = await this._lastFMClient.Album.GetInfoAsync(artistName, albumName);
            Statistics.LastfmApiCalls.Inc();

            return album?.Content?.Images;
        }

        public async Task<Bitmap> GetAlbumImageAsBitmapAsync(Uri largestImageSize)
        {
            try
            {
                var request = WebRequest.Create(largestImageSize);
                using var response = await request.GetResponseAsync();
                await using var responseStream = response.GetResponseStream();

                return new Bitmap(responseStream);
            }
            catch
            {
                return null;
            }
        }

        // Top albums
        public async Task<PageResponse<LastAlbum>> GetTopAlbumsAsync(string lastFMUserName, LastStatsTimeSpan timespan, int count = 2)
        {
            var topAlbums = await this._lastFMClient.User.GetTopAlbums(lastFMUserName, timespan, 1, count);
            Statistics.LastfmApiCalls.Inc();

            return topAlbums;
        }


        // Top artists
        public async Task<PageResponse<LastArtist>> GetTopArtistsAsync(string lastFMUserName,
            LastStatsTimeSpan timespan, int count = 2)
        {
            var topArtists = await this._lastFMClient.User.GetTopArtists(lastFMUserName, timespan, 1, count);
            Statistics.LastfmApiCalls.Inc();

            return topArtists;
        }

        // Top tracks
        public async Task<Response<TopTracksResponse>> GetTopTracksAsync(string lastFMUserName,
            string period, int count = 2)
        {
            var queryParams = new Dictionary<string, string>
            {
                {"limit", count.ToString() },
                {"username", lastFMUserName },
                {"period", period },
            };

            var artistCall = await this._lastfmApi.CallApiAsync<TopTracksResponse>(queryParams, Call.TopTracks);

            Statistics.LastfmApiCalls.Inc();

            return artistCall;
        }

        // Check if lastfm user exists
        public async Task<bool> LastFMUserExistsAsync(string lastFMUserName)
        {
            var lastFMUser = await this._lastFMClient.User.GetInfoAsync(lastFMUserName);
            Statistics.LastfmApiCalls.Inc();

            return lastFMUser.Success;
        }

        public static ChartTimePeriod StringToChartTimePeriod(string timeString)
        {
            if (Enum.TryParse(timeString, true, out ChartTimePeriod timePeriod))
            {
                return timePeriod;
            }

            return timeString switch
            {
                "w" => ChartTimePeriod.Weekly,
                "m" => ChartTimePeriod.Monthly,
                "q" => ChartTimePeriod.Quarterly,
                "h" => ChartTimePeriod.Half,
                "y" => ChartTimePeriod.Yearly,
                "a" => ChartTimePeriod.AllTime,
                "overall" => ChartTimePeriod.AllTime,
                _ => ChartTimePeriod.Weekly
            };
        }

        public static LastStatsTimeSpan ChartTimePeriodToLastStatsTimeSpan(ChartTimePeriod timePeriod)
        {
            return timePeriod switch
            {
                ChartTimePeriod.Weekly => LastStatsTimeSpan.Week,
                ChartTimePeriod.Monthly => LastStatsTimeSpan.Month,
                ChartTimePeriod.Quarterly => LastStatsTimeSpan.Quarter,
                ChartTimePeriod.Half => LastStatsTimeSpan.Half,
                ChartTimePeriod.Yearly => LastStatsTimeSpan.Year,
                ChartTimePeriod.AllTime => LastStatsTimeSpan.Overall,
                _ => LastStatsTimeSpan.Week
            };
        }

        public static string ChartTimePeriodToSiteTimePeriodUrl(ChartTimePeriod timePeriod)
        {
            return timePeriod switch
            {
                ChartTimePeriod.Weekly => "LAST_7_DAYS",
                ChartTimePeriod.Monthly => "LAST_30_DAYS",
                ChartTimePeriod.Quarterly => "LAST_90_DAYS",
                ChartTimePeriod.Half => "LAST_180_DAYS",
                ChartTimePeriod.Yearly => "LAST_365_DAYS",
                ChartTimePeriod.AllTime => "ALL",
                _ => "LAST_7_DAYS"
            };
        }

        public static string ChartTimePeriodToCallTimePeriod(ChartTimePeriod timePeriod)
        {
            return timePeriod switch
            {
                ChartTimePeriod.Weekly => TimePeriod.Week,
                ChartTimePeriod.Monthly => TimePeriod.Month,
                ChartTimePeriod.Quarterly => TimePeriod.Quarter,
                ChartTimePeriod.Half => TimePeriod.Half,
                ChartTimePeriod.Yearly => TimePeriod.Year,
                ChartTimePeriod.AllTime => TimePeriod.Overall,
                _ => TimePeriod.Week
            };
        }


        public static TimeModel OptionsToTimeModel(
            string[] extraOptions,
            LastStatsTimeSpan defaultLastStatsTimeSpan = LastStatsTimeSpan.Week,
            ChartTimePeriod defaultChartTimePeriod = ChartTimePeriod.Weekly,
            string defaultUrlParameter = "LAST_7_DAYS")
        {
            var timeModel = new TimeModel();

            // time period
            if (extraOptions.Contains("weekly") || extraOptions.Contains("week") || extraOptions.Contains("w"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Week;
                timeModel.ChartTimePeriod = ChartTimePeriod.Weekly;
                timeModel.Description = "Weekly";
                timeModel.UrlParameter = "LAST_7_DAYS";
            }
            else if (extraOptions.Contains("monthly") || extraOptions.Contains("month") || extraOptions.Contains("m"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Month;
                timeModel.ChartTimePeriod = ChartTimePeriod.Monthly;
                timeModel.Description = "Monthly";
                timeModel.UrlParameter = "LAST_30_DAYS";
            }
            else if (extraOptions.Contains("quarterly") || extraOptions.Contains("quarter") || extraOptions.Contains("q"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Quarter;
                timeModel.ChartTimePeriod = ChartTimePeriod.Quarterly;
                timeModel.Description = "Quarterly";
                timeModel.UrlParameter = "LAST_90_DAYS";
            }
            else if (extraOptions.Contains("halfyearly") || extraOptions.Contains("half") || extraOptions.Contains("h"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Half;
                timeModel.ChartTimePeriod = ChartTimePeriod.Half;
                timeModel.Description = "Half-yearly";
                timeModel.UrlParameter = "LAST_180_DAYS";
            }
            else if (extraOptions.Contains("yearly") || extraOptions.Contains("year") || extraOptions.Contains("y"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Year;
                timeModel.ChartTimePeriod = ChartTimePeriod.Yearly;
                timeModel.Description = "Yearly";
                timeModel.UrlParameter = "LAST_365_DAYS";
            }
            else if (extraOptions.Contains("overall") || extraOptions.Contains("alltime") || extraOptions.Contains("o") ||
                     extraOptions.Contains("at") ||
                     extraOptions.Contains("a"))
            {
                timeModel.LastStatsTimeSpan = LastStatsTimeSpan.Overall;
                timeModel.ChartTimePeriod = ChartTimePeriod.AllTime;
                timeModel.Description = "Overall";
                timeModel.UrlParameter = "ALL";
            }
            else
            {
                timeModel.LastStatsTimeSpan = defaultLastStatsTimeSpan;
                timeModel.ChartTimePeriod = defaultChartTimePeriod;
                timeModel.Description = "";
                timeModel.UrlParameter = defaultUrlParameter;
            }

            return timeModel;
        }
    }
}
