using FMBot.Bot.Extensions;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static FMBot.Bot.FMBotUtil;
using static FMBot.Bot.Models.LastFMModels;

namespace FMBot.Services
{
    internal class LastFMService
    {
        public static JsonCfg.ConfigJson cfgjson = JsonCfg.GetJSONData();

        public LastfmClient lastfmClient = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);


        // Last scrobble
        public async Task<LastTrack> GetLastScrobbleAsync(string lastFMUserName)
        {
            PageResponse<LastTrack> tracks = await lastfmClient.User.GetRecentScrobbles(lastFMUserName, null, 1, 1);

            LastTrack track = tracks.Content.ElementAt(0);

            return track;
        }

        // Recent scrobbles
        public async Task<PageResponse<LastTrack>> GetRecentScrobblesAsync(string lastFMUserName, int count = 2)
        {
            PageResponse<LastTrack> tracks = await lastfmClient.User.GetRecentScrobbles(lastFMUserName, null, 1, count);

            return tracks;
        }

        // User
        public async Task<LastResponse<LastUser>> GetUserInfoAsync(string lastFMUserName)
        {
            try
            {
                LastResponse<LastUser> userInfo = await lastfmClient.User.GetInfoAsync(lastFMUserName);

                return userInfo;
            }
            catch (Exception e)
            {
                ExceptionReporter.ReportException(null, e);
            }
            return null;
        }

        // Album info
        public async Task<LastResponse<LastAlbum>> GetAlbumInfoAsync(string artistName, string albumName)
        {
            LastResponse<LastAlbum> album = await lastfmClient.Album.GetInfoAsync(artistName, albumName);

            return album;
        }


        // Album images
        public async Task<LastImageSet> GetAlbumImagesAsync(string artistName, string albumName)
        {
            LastResponse<LastAlbum> album = await lastfmClient.Album.GetInfoAsync(artistName, albumName);

            LastImageSet images = album != null ? album.Content != null ? album.Content.Images != null ? album.Content.Images : null : null : null;

            return images;
        }


        // Top albums
        public async Task<PageResponse<LastAlbum>> GetTopAlbumsAsync(string lastFMUserName, LastStatsTimeSpan timespan, int count = 2)
        {
            PageResponse<LastAlbum> albums = await lastfmClient.User.GetTopAlbums(lastFMUserName, timespan, 1, count);

            return albums;
        }


        // Artist info
        public async Task<LastResponse<LastArtist>> GetArtistInfoAsync(string artistName)
        {
            LastResponse<LastArtist> artist = await lastfmClient.Artist.GetInfoAsync(artistName);

            return artist;
        }


        // Artist info
        public async Task<LastImageSet> GetArtistImageAsync(string artistName)
        {
            LastResponse<LastArtist> artist = await lastfmClient.Artist.GetInfoAsync(artistName);

            LastImageSet image = artist != null ? artist.Content != null ? artist.Content.MainImage : null : null;

            return image;
        }


        // Top artists
        public async Task<PageResponse<LastArtist>> GetTopArtistsAsync(string lastFMUserName, LastStatsTimeSpan timespan, int count = 2)
        {
            PageResponse<LastArtist> artists = await lastfmClient.User.GetTopArtists(lastFMUserName, timespan, 1, count);

            return artists;
        }

        // Check if lastfm user exists
        public async Task<bool> LastFMUserExistsAsync(string lastFMUserName)
        {
            LastResponse<LastUser> lastFMUser = await lastfmClient.User.GetInfoAsync(lastFMUserName);

            return lastFMUser.Success;
        }


        public async Task GenerateChartAsync(FMBotChart chart)
        {
            try
            {
                LastStatsTimeSpan timespan = LastStatsTimeSpan.Week;

                if (chart.time.Equals("weekly") || chart.time.Equals("week") || chart.time.Equals("w"))
                {
                    timespan = LastStatsTimeSpan.Week;
                }
                else if (chart.time.Equals("monthly") || chart.time.Equals("month") || chart.time.Equals("m"))
                {
                    timespan = LastStatsTimeSpan.Month;
                }
                else if (chart.time.Equals("yearly") || chart.time.Equals("year") || chart.time.Equals("y"))
                {
                    timespan = LastStatsTimeSpan.Year;
                }
                else if (chart.time.Equals("overall") || chart.time.Equals("alltime") || chart.time.Equals("o") || chart.time.Equals("at"))
                {
                    timespan = LastStatsTimeSpan.Overall;
                }

                string nulltext = "[undefined]";

                if (chart.mode == 0)
                {
                    PageResponse<LastAlbum> albums = await GetTopAlbumsAsync(chart.LastFMName, timespan, chart.max);
                    for (int al = 0; al < chart.max; ++al)
                    {
                        LastAlbum track = albums.Content.ElementAt(al);

                        string ArtistName = string.IsNullOrWhiteSpace(track.ArtistName) ? nulltext : track.ArtistName;
                        string AlbumName = string.IsNullOrWhiteSpace(track.Name) ? nulltext : track.Name;

                        LastImageSet albumImages = await GetAlbumImagesAsync(ArtistName, AlbumName);

                        Bitmap cover;

                        if (albumImages != null && albumImages.Medium != null)
                        {
                            string url = albumImages.Large.AbsoluteUri.ToString();
                            string path = Path.GetFileName(url);

                            if (File.Exists(GlobalVars.CacheFolder + path))
                            {
                                cover = new Bitmap(GlobalVars.CacheFolder + path);
                            }
                            else
                            {
                                WebRequest request = WebRequest.Create(url);
                                using (WebResponse response = await request.GetResponseAsync())
                                {
                                    using (Stream responseStream = response.GetResponseStream())
                                    {
                                        Bitmap bitmap = new Bitmap(responseStream);

                                        cover = bitmap;
                                        using (MemoryStream memory = new MemoryStream())
                                        {
                                            using (FileStream fs = new FileStream(GlobalVars.CacheFolder + path, FileMode.Create, FileAccess.ReadWrite))
                                            {
                                                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                                byte[] bytes = memory.ToArray();
                                                fs.Write(bytes, 0, bytes.Length);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            cover = new Bitmap(GlobalVars.BasePath + "unknown.png");
                        }

                        if (chart.titles)
                        {
                            Graphics text = Graphics.FromImage(cover);
                            text.DrawColorString(cover, ArtistName, new Font("Arial", 8.0f, FontStyle.Bold), new PointF(2.0f, 2.0f));
                            text.DrawColorString(cover, AlbumName, new Font("Arial", 8.0f, FontStyle.Bold), new PointF(2.0f, 12.0f));
                        }

                        chart.images.Add(cover);
                    }
                }
                else if (chart.mode == 1)
                {
                    PageResponse<LastArtist> artists = await GetTopArtistsAsync(chart.LastFMName, timespan, chart.max);
                    for (int al = 0; al < chart.max; ++al)
                    {
                        LastArtist artist = artists.Content.ElementAt(al);

                        string ArtistName = string.IsNullOrWhiteSpace(artist.Name) ? nulltext : artist.Name;

                        LastImageSet artistImage = await GetArtistImageAsync(ArtistName);

                        Bitmap cover;

                        if (artistImage != null && artistImage.Large != null)
                        {
                            string url = artistImage.Large.AbsoluteUri.ToString();
                            string path = Path.GetFileName(url);


                            if (File.Exists(GlobalVars.CacheFolder + path))
                            {
                                cover = new Bitmap(GlobalVars.CacheFolder + path);
                            }
                            else
                            {
                                WebRequest request = WebRequest.Create(url);
                                using (WebResponse response = await request.GetResponseAsync())
                                {
                                    using (Stream responseStream = response.GetResponseStream())
                                    {
                                        Bitmap bitmap = new Bitmap(responseStream);

                                        cover = bitmap;
                                        using (MemoryStream memory = new MemoryStream())
                                        {
                                            using (FileStream fs = new FileStream(GlobalVars.CacheFolder + path, FileMode.Create, FileAccess.ReadWrite))
                                            {
                                                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                                                byte[] bytes = memory.ToArray();
                                                fs.Write(bytes, 0, bytes.Length);
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            cover = new Bitmap(GlobalVars.BasePath + "unknown.png");
                        }

                        if (chart.titles)
                        {
                            Graphics text = Graphics.FromImage(cover);
                            text.DrawColorString(cover, ArtistName, new Font("Arial", 8.0f, FontStyle.Bold), new PointF(2.0f, 2.0f));
                        }

                        chart.images.Add(cover);

                    }
                }
            }
            catch (Exception e)
            {
                ExceptionReporter.ReportException(chart.disclient, e);
            }
            finally
            {
                Bitmap finalImage;

                int width;
                int height;

                double root = Math.Sqrt(chart.images.Count);

                width = (int)Math.Ceiling(root) * chart.images.FirstOrDefault().Width;
                height = (int)Math.Ceiling(root) * chart.images.FirstOrDefault().Height;

                //create a bitmap to hold the combined image
                finalImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(Color.Black);

                    //go through each image and draw it on the final image
                    int offset = 0;
                    int heightOffset = 0;

                    for (int i = 1; i < chart.images.Count + 1; i++)
                    {
                        g.DrawImage(chart.images[i - 1],
                          new Rectangle(offset, heightOffset, chart.images[i -1].Width, chart.images[i -1].Height));

                        offset += chart.images[i - 1].Width;

                        // next row
                        if ((i % root) == 0 && (i - 1) != 0)
                        {
                            offset = 0;
                            heightOffset += chart.images[i - 1].Height;
                        }
                    }
                }

                lock (GlobalVars.charts.SyncRoot)
                {
                    GlobalVars.charts[GlobalVars.GetChartFileName(chart.DiscordUser.Id)] = finalImage;
                }

                foreach (Bitmap image in chart.images)
                {
                    image.Dispose();
                }
            }
        }
    }
}
