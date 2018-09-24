﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Objects;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeSearch;
using static FMBot_Discord.FMBotModules;
using static FMBot_Discord.FMBotUtil;

namespace FMBot_Discord
{
    public class FMBotCommands : ModuleBase
    {
        #region Constructor

        private readonly CommandService _service;
        private readonly TimerService _timer;

        public FMBotCommands(CommandService service, TimerService timer)
        {
            _service = service;
            _timer = timer;
        }

        #endregion

        #region Last.FM Commands

        [Command("fm"), Summary("Displays what a user is listening to.")]
        [Alias("qm", "wm", "em", "rm", "tm", "ym", "um", "im", "om", "pm", "dm", "gm", "sm", "am", "hm", "jm", "km", "lm", "zm", "xm", "cm", "vm", "bm", "nm", "mm", "lastfm")]
        public async Task fmAsync(IUser user = null)
        {
            try
            {
                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;

                int LastFMMode = 4;

                if (GlobalVars.GetDMBool())
                {
                    LastFMMode = DBase.GetModeIntForID(DiscordUser.Id.ToString());
                }
                else
                {
                    IGuildUser GuildUser = (IGuildUser)DiscordUser;
                    int GlobalServerMode = DBase.GetModeIntForServerID(GuildUser.GuildId.ToString());

                    if (GlobalServerMode > 3 || GlobalServerMode < 0)
                    {
                        LastFMMode = DBase.GetModeIntForID(GuildUser.Id.ToString());
                    }
                    else
                    {
                        LastFMMode = DBase.GetModeIntForServerID(GuildUser.GuildId.ToString());
                    }
                }


                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to show Last.FM info due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var cfgjson = await JsonCfg.GetJSONDataAsync();
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                    try
                    {
                        var tracks = await client.User.GetRecentScrobbles(LastFMName, null, 1, 2);
                        LastTrack currentTrack = tracks.Content.ElementAt(0);
                        LastTrack lastTrack = tracks.Content.ElementAt(1);

                        if (LastFMMode == 0)
                        {
                            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                            eab.IconUrl = DiscordUser.GetAvatarUrl();
                            eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                            var builder = new EmbedBuilder();
                            builder.WithAuthor(eab);
                            string URI = "https://www.last.fm/user/" + LastFMName;
                            builder.WithUrl(URI);
                            GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);
                            builder.WithDescription("Now Playing");

                            string nulltext = "[undefined]";

                            string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                            string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                            string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                            try
                            {
                                var AlbumInfo = await client.Album.GetInfoAsync(ArtistName, AlbumName);
                                var AlbumImages = (AlbumInfo.Content.Images != null) ? AlbumInfo.Content.Images : null;
                                var AlbumThumbnail = (AlbumImages != null) ? AlbumImages.Large.AbsoluteUri : null;
                                string ThumbnailImage = (AlbumThumbnail != null) ? AlbumThumbnail.ToString() : null;

                                if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                {
                                    builder.WithThumbnailUrl(ThumbnailImage);
                                }
                            }
                            catch (Exception e)
                            {
                                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                ExceptionReporter.ReportException(disclient, e);
                            }
                            builder.AddField("Current: " + TrackName, ArtistName + " | " + AlbumName);

                            EmbedFooterBuilder efb = new EmbedFooterBuilder();

                            var userinfo = await client.User.GetInfoAsync(LastFMName);
                            var playcount = userinfo.Content.Playcount;

                            efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                            builder.WithFooter(efb);

                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                        else if (LastFMMode == 1)
                        {
                            try
                            {
                                EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                                eab.IconUrl = DiscordUser.GetAvatarUrl();
                                eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                                var builder = new EmbedBuilder();
                                builder.WithAuthor(eab);
                                string URI = "https://www.last.fm/user/" + LastFMName;
                                builder.WithUrl(URI);
                                GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);
                                builder.WithDescription("Now Playing");

                                string nulltext = "[undefined]";

                                string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                                string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                                string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                                string LastTrackName = string.IsNullOrWhiteSpace(lastTrack.Name) ? nulltext : lastTrack.Name;
                                string LastArtistName = string.IsNullOrWhiteSpace(lastTrack.ArtistName) ? nulltext : lastTrack.ArtistName;
                                string LastAlbumName = string.IsNullOrWhiteSpace(lastTrack.AlbumName) ? nulltext : lastTrack.AlbumName;

                                try
                                {
                                    var AlbumInfo = await client.Album.GetInfoAsync(ArtistName, AlbumName);
                                    var AlbumImages = (AlbumInfo.Content.Images != null) ? AlbumInfo.Content.Images : null;
                                    var AlbumThumbnail = (AlbumImages != null) ? AlbumImages.Large.AbsoluteUri : null;
                                    string ThumbnailImage = (AlbumThumbnail != null) ? AlbumThumbnail.ToString() : null;

                                    if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                    {
                                        builder.WithThumbnailUrl(ThumbnailImage);
                                    }
                                }
                                catch (Exception e)
                                {
                                    DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                    ExceptionReporter.ReportException(disclient, e);
                                }

                                builder.AddField("Current: " + TrackName, ArtistName + " | " + AlbumName);
                                builder.AddField("Previous: " + LastTrackName, LastArtistName + " | " + LastAlbumName);

                                EmbedFooterBuilder efb = new EmbedFooterBuilder();

                                var userinfo = await client.User.GetInfoAsync(LastFMName);
                                var playcount = userinfo.Content.Playcount;

                                efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                                builder.WithFooter(efb);

                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                            }
                            catch (Exception)
                            {
                                EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                                eab.IconUrl = DiscordUser.GetAvatarUrl();
                                eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                                var builder = new EmbedBuilder();
                                builder.WithAuthor(eab);
                                string URI = "https://www.last.fm/user/" + LastFMName;
                                builder.WithUrl(URI);
                                GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);
                                builder.WithDescription("Now Playing");

                                string nulltext = "[undefined]";

                                string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                                string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                                string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                                try
                                {
                                    var AlbumInfo = await client.Album.GetInfoAsync(ArtistName, AlbumName);
                                    var AlbumImages = (AlbumInfo.Content.Images != null) ? AlbumInfo.Content.Images : null;
                                    var AlbumThumbnail = (AlbumImages != null) ? AlbumImages.Large.AbsoluteUri : null;
                                    string ThumbnailImage = (AlbumThumbnail != null) ? AlbumThumbnail.ToString() : null;

                                    if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                    {
                                        builder.WithThumbnailUrl(ThumbnailImage);
                                    }
                                }
                                catch (Exception e)
                                {
                                    DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                    ExceptionReporter.ReportException(disclient, e);
                                }

                                builder.AddField("Current: " + TrackName, ArtistName + " | " + AlbumName);

                                EmbedFooterBuilder efb = new EmbedFooterBuilder();

                                var userinfo = await client.User.GetInfoAsync(LastFMName);
                                var playcount = userinfo.Content.Playcount;

                                efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                                builder.WithFooter(efb);

                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                            }
                        }
                        else if (LastFMMode == 2)
                        {
                            try
                            {
                                string nulltext = "[undefined]";

                                string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                                string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                                string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                                string LastTrackName = string.IsNullOrWhiteSpace(lastTrack.Name) ? nulltext : lastTrack.Name;
                                string LastArtistName = string.IsNullOrWhiteSpace(lastTrack.ArtistName) ? nulltext : lastTrack.ArtistName;
                                string LastAlbumName = string.IsNullOrWhiteSpace(lastTrack.AlbumName) ? nulltext : lastTrack.AlbumName;

                                var userinfo = await client.User.GetInfoAsync(LastFMName);
                                var playcount = userinfo.Content.Playcount;

                                await Context.Channel.SendMessageAsync(GlobalVars.SetUserTitleText(DiscordUser, SelfUser) + "**Current** - " + ArtistName + " - " + TrackName + " [" + AlbumName + "]" + "\n" + "**Previous** - " + LastArtistName + " - " + LastTrackName + " [" + LastAlbumName + "]" + "\n" + "<https://www.last.fm/user/" + LastFMName + ">\n" + LastFMName + "'s Total Tracks: " + playcount.ToString("0"));
                            }
                            catch (Exception)
                            {
                                string nulltext = "[undefined]";

                                string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                                string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                                string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                                string LastTrackName = string.IsNullOrWhiteSpace(lastTrack.Name) ? nulltext : lastTrack.Name;
                                string LastArtistName = string.IsNullOrWhiteSpace(lastTrack.ArtistName) ? nulltext : lastTrack.ArtistName;
                                string LastAlbumName = string.IsNullOrWhiteSpace(lastTrack.AlbumName) ? nulltext : lastTrack.AlbumName;

                                var userinfo = await client.User.GetInfoAsync(LastFMName);
                                var playcount = userinfo.Content.Playcount;

                                await Context.Channel.SendMessageAsync(GlobalVars.SetUserTitleText(DiscordUser, SelfUser) + "**Current** - " + ArtistName + " - " + TrackName + " [" + AlbumName + "]" + "\n" + "<https://www.last.fm/user/" + LastFMName + ">\n" + LastFMName + "'s Total Tracks: " + playcount.ToString("0"));
                            }
                        }
                        else if (LastFMMode == 3)
                        {
                            string nulltext = "[undefined]";

                            string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? nulltext : currentTrack.Name;
                            string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? nulltext : currentTrack.ArtistName;
                            string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? nulltext : currentTrack.AlbumName;

                            string LastTrackName = string.IsNullOrWhiteSpace(lastTrack.Name) ? nulltext : lastTrack.Name;
                            string LastArtistName = string.IsNullOrWhiteSpace(lastTrack.ArtistName) ? nulltext : lastTrack.ArtistName;
                            string LastAlbumName = string.IsNullOrWhiteSpace(lastTrack.AlbumName) ? nulltext : lastTrack.AlbumName;

                            var userinfo = await client.User.GetInfoAsync(LastFMName);
                            var playcount = userinfo.Content.Playcount;

                            await Context.Channel.SendMessageAsync(GlobalVars.SetUserTitleText(DiscordUser, SelfUser) + "**Current** - " + ArtistName + " - " + TrackName + " [" + AlbumName + "]" + "\n" + "<https://www.last.fm/user/" + LastFMName + ">\n" + LastFMName + "'s Total Tracks: " + playcount.ToString("0"));
                        }
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);
                        await ReplyAsync("Unable to show Last.FM info due to an internal error. Try scrobbling something then use the command again.");
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);
                await ReplyAsync("Unable to show Last.FM info due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmyt"), Summary("Shares a link to a YouTube video based on what a user is listening to")]
        [Alias("fmyoutube")]
        public async Task fmytAsync(IUser user = null)
        {
            var DiscordUser = GlobalVars.CheckIfDM(user, Context);
            string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
            if (LastFMName.Equals("NULL"))
            {
                await ReplyAsync("Unable to show Last.FM info via YouTube due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
            else
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();

                var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                try
                {
                    var tracks = await client.User.GetRecentScrobbles(LastFMName, null, 1, 2);
                    LastTrack currentTrack = tracks.Content.ElementAt(0);

                    string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? null : currentTrack.Name;
                    string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? null : currentTrack.ArtistName;
                    string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? null : currentTrack.AlbumName;

                    try
                    {
                        string querystring = TrackName + " - " + ArtistName + " " + AlbumName;
                        var items = new VideoSearch();
                        var item = items.SearchQuery(querystring, 1).ElementAt(0);

                        await ReplyAsync(item.Url);
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);
                        await ReplyAsync("No results have been found for this track.");
                    }
                }
                catch (Exception e)
                {
                    DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                    ExceptionReporter.ReportException(disclient, e);
                    await ReplyAsync("Unable to show Last.FM info via YouTube due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
            }
        }

        [Command("fmspotify"), Summary("Shares a link to a Spotify track based on what a user is listening to")]
        public async Task fmspotifyAsync(IUser user = null)
        {
            var DiscordUser = GlobalVars.CheckIfDM(user, Context);
            string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
            if (LastFMName.Equals("NULL"))
            {
                await ReplyAsync("Unable to show Last.FM info via Spotify due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
            else
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();
                var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);

                try
                {
                    var tracks = await client.User.GetRecentScrobbles(LastFMName, null, 1, 2);
                    LastTrack currentTrack = tracks.Content.ElementAt(0);

                    string TrackName = string.IsNullOrWhiteSpace(currentTrack.Name) ? null : currentTrack.Name;
                    string ArtistName = string.IsNullOrWhiteSpace(currentTrack.ArtistName) ? null : currentTrack.ArtistName;
                    string AlbumName = string.IsNullOrWhiteSpace(currentTrack.AlbumName) ? null : currentTrack.AlbumName;

                    //Create the auth object
                    var auth = new ClientCredentialsAuth()
                    {
                        ClientId = cfgjson.SpotifyKey,
                        ClientSecret = cfgjson.SpotifySecret,
                        Scope = Scope.None,
                    };
                    //With this token object, we now can make calls
                    Token token = auth.DoAuth();

                    var _spotify = new SpotifyWebAPI()
                    {
                        TokenType = token.TokenType,
                        AccessToken = token.AccessToken,
                        UseAuth = true
                    };

                    string querystring = null;

                    querystring = TrackName + " - " + ArtistName + " " + AlbumName;

                    SearchItem item = _spotify.SearchItems(querystring, SearchType.Track);

                    if (item.Tracks.Items.Any())
                    {
                        FullTrack track = item.Tracks.Items.FirstOrDefault();
                        SimpleArtist trackArtist = track.Artists.FirstOrDefault();

                        await ReplyAsync("https://open.spotify.com/track/" + track.Id);
                    }
                    else
                    {
                        await ReplyAsync("No results have been found for this track.");
                    }
                }
                catch (Exception e)
                {
                    DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                    ExceptionReporter.ReportException(disclient, e);
                    await ReplyAsync("Unable to show Last.FM info via Spotify due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
            }
        }

        [Command("fmspotifysearch"), Summary("Shares a link to a Spotify track based on a user's search parameters")]
        [Alias("fmspotifyfind")]
        public async Task fmspotifysearchAsync(params string[] searchterms)
        {
            try
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();

                //Create the auth object
                var auth = new ClientCredentialsAuth()
                {
                    ClientId = cfgjson.SpotifyKey,
                    ClientSecret = cfgjson.SpotifySecret,
                    Scope = Scope.None,
                };
                //With this token object, we now can make calls
                Token token = auth.DoAuth();

                var _spotify = new SpotifyWebAPI()
                {
                    TokenType = token.TokenType,
                    AccessToken = token.AccessToken,
                    UseAuth = true
                };

                string querystring = null;

                if (searchterms.Any())
                {
                    querystring = string.Join(" ", searchterms);

                    SearchItem item = _spotify.SearchItems(querystring, SearchType.Track);

                    if (item.Tracks.Items.Any())
                    {
                        FullTrack track = item.Tracks.Items.FirstOrDefault();
                        SimpleArtist trackArtist = track.Artists.FirstOrDefault();

                        await ReplyAsync("https://open.spotify.com/track/" + track.Id);
                    }
                    else
                    {
                        await ReplyAsync("No results have been found for this track.");
                    }
                }
                else
                {
                    await ReplyAsync("Please specify what you want to search for.");
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);
                await ReplyAsync("Unable to search for music via Spotify due to an internal error.");
            }
        }

        [Command("fmchart"), Summary("Generates a chart based on a user's parameters.")]
        public async Task fmchartAsync(string time = "weekly", string chartsize = "3x3", string titlesetting = "titles", IUser user = null)
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            if (time == "help")
            {
                await ReplyAsync(cfgjson.CommandPrefix + "fmchart [weekly/monthly/yearly/overall] [3x3-10x10] [notitles/titles] [user]");
                return;
            }

            var SelfUser = Context.Client.CurrentUser;

            var loadingText = "";

            var titletext = "(With Titles)";

            if (titlesetting == "titles")
            {
                titletext = "(With Titles)";
            }
            else if (titlesetting == "notitles")
            {
                titletext = "(Without Titles)";
            }

            if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
            {
                loadingText = "Loading your Weekly " + chartsize + " " + SelfUser.Username + " chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
            {
                loadingText = "Loading your Monthly " + chartsize + " " + SelfUser.Username + " chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
            {
                loadingText = "Loading your Yearly " + chartsize + " " + SelfUser.Username + " chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
            {
                loadingText = "Loading your Overall " + chartsize + " " + SelfUser.Username + " chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else
            {
                loadingText = "Loading your " + chartsize + " " + SelfUser.Username + " chart " + titletext + "... (may take a while depending on the size of your chart)";
            }

            var loadingmsg = await Context.Channel.SendMessageAsync(loadingText);

            try
            {
                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to generate a FMChart due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);

                    string chartalbums = "";
                    string chartrows = "";

                    if (chartsize.Equals("3x3"))
                    {
                        chartalbums = "9";
                        chartrows = "3";
                    }
                    else if (chartsize.Equals("4x4"))
                    {
                        chartalbums = "16";
                        chartrows = "4";
                    }
                    else if (chartsize.Equals("5x5"))
                    {
                        chartalbums = "25";
                        chartrows = "5";
                    }
                    else if (chartsize.Equals("6x6"))
                    {
                        chartalbums = "36";
                        chartrows = "6";
                    }
                    else if (chartsize.Equals("7x7"))
                    {
                        chartalbums = "49";
                        chartrows = "7";
                    }
                    else if (chartsize.Equals("8x8"))
                    {
                        chartalbums = "64";
                        chartrows = "8";
                    }
                    else if (chartsize.Equals("9x9"))
                    {
                        chartalbums = "81";
                        chartrows = "9";
                    }
                    else if (chartsize.Equals("10x10"))
                    {
                        chartalbums = "100";
                        chartrows = "10";
                    }
                    else
                    {
                        await ReplyAsync("Your chart's size isn't valid. Sizes supported: 3x3-10x10");
                        return;
                    }

                    int max = int.Parse(chartalbums);
                    int rows = int.Parse(chartrows);

                    List<Bitmap> images = new List<Bitmap>();

                    bool TitleBool = true;

                    if (titlesetting == "titles")
                    {
                        TitleBool = true;
                    }
                    else if (titlesetting == "notitles")
                    {
                        TitleBool = false;
                    }

                    FMBotChart chart = new FMBotChart();
                    chart.time = time;
                    chart.client = client;
                    chart.LastFMName = LastFMName;
                    chart.max = max;
                    chart.rows = rows;
                    chart.images = images;
                    chart.DiscordUser = DiscordUser;
                    chart.disclient = Context.Client as DiscordSocketClient;
                    chart.mode = 0;
                    chart.titles = TitleBool;
                    await chart.ChartGenerate();

                    await Context.Channel.SendFileAsync(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png");

                    EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                    eab.IconUrl = DiscordUser.GetAvatarUrl();
                    eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                    var builder = new EmbedBuilder();
                    builder.WithAuthor(eab);
                    string URI = "https://www.last.fm/user/" + LastFMName;
                    builder.WithUrl(URI);
                    GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                    if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Weekly Chart for " + LastFMName);
                    }
                    else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Monthly Chart for " + LastFMName);
                    }
                    else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Yearly Chart for " + LastFMName);
                    }
                    else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Overall Chart for " + LastFMName);
                    }
                    else
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Chart for " + LastFMName);
                    }

                    var userinfo = await client.User.GetInfoAsync(LastFMName);
                    EmbedFooterBuilder efb = new EmbedFooterBuilder();
                    var playcount = userinfo.Content.Playcount;
                    efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                    builder.WithFooter(efb);

                    await loadingmsg.DeleteAsync();
                    await Context.Channel.SendMessageAsync("", false, builder.Build());

                    File.SetAttributes(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png", FileAttributes.Normal);
                    File.Delete(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png");
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await loadingmsg.DeleteAsync();
                await ReplyAsync("Unable to generate a FMChart due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmartistchart"), Summary("Generates an artist chart based on a user's parameters.")]
        public async Task fmartistchartAsync(string time = "weekly", string chartsize = "3x3", string titlesetting = "titles", IUser user = null)
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            if (time == "help")
            {
                await ReplyAsync(cfgjson.CommandPrefix + "fmartistchart [weekly/monthly/yearly/overall] [3x3-10x10] [notitles/titles] [user]");
                return;
            }

            var SelfUser = Context.Client.CurrentUser;

            var loadingText = "";

            var titletext = "(With Titles)";

            if (titlesetting == "titles")
            {
                titletext = "(With Titles)";
            }
            else if (titlesetting == "notitles")
            {
                titletext = "(Without Titles)";
            }

            if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
            {
                loadingText = "Loading your Weekly " + chartsize + " " + SelfUser.Username + " artist chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
            {
                loadingText = "Loading your Monthly " + chartsize + " " + SelfUser.Username + " artist chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
            {
                loadingText = "Loading your Yearly " + chartsize + " " + SelfUser.Username + " artist chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
            {
                loadingText = "Loading your Overall " + chartsize + " " + SelfUser.Username + " artist chart " + titletext + "... (may take a while depending on the size of your chart)";
            }
            else
            {
                loadingText = "Loading your " + chartsize + " " + SelfUser.Username + " artist chart " + titletext + "... (may take a while depending on the size of your chart)";
            }

            var loadingmsg = await Context.Channel.SendMessageAsync(loadingText);

            try
            {
                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to generate a FMChart due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);

                    string chartalbums = "";
                    string chartrows = "";

                    if (chartsize.Equals("3x3"))
                    {
                        chartalbums = "9";
                        chartrows = "3";
                    }
                    else if (chartsize.Equals("4x4"))
                    {
                        chartalbums = "16";
                        chartrows = "4";
                    }
                    else if (chartsize.Equals("5x5"))
                    {
                        chartalbums = "25";
                        chartrows = "5";
                    }
                    else if (chartsize.Equals("6x6"))
                    {
                        chartalbums = "36";
                        chartrows = "6";
                    }
                    else if (chartsize.Equals("7x7"))
                    {
                        chartalbums = "49";
                        chartrows = "7";
                    }
                    else if (chartsize.Equals("8x8"))
                    {
                        chartalbums = "64";
                        chartrows = "8";
                    }
                    else if (chartsize.Equals("9x9"))
                    {
                        chartalbums = "81";
                        chartrows = "9";
                    }
                    else if (chartsize.Equals("10x10"))
                    {
                        chartalbums = "100";
                        chartrows = "10";
                    }
                    else
                    {
                        await ReplyAsync("Your artist chart's size isn't valid. Sizes supported: 3x3-10x10");
                        return;
                    }

                    int max = int.Parse(chartalbums);
                    int rows = int.Parse(chartrows);

                    List<Bitmap> images = new List<Bitmap>();

                    bool TitleBool = true;

                    if (titlesetting == "titles")
                    {
                        TitleBool = true;
                    }
                    else if (titlesetting == "notitles")
                    {
                        TitleBool = false;
                    }

                    FMBotChart chart = new FMBotChart();
                    chart.time = time;
                    chart.client = client;
                    chart.LastFMName = LastFMName;
                    chart.max = max;
                    chart.rows = rows;
                    chart.images = images;
                    chart.DiscordUser = DiscordUser;
                    chart.disclient = Context.Client as DiscordSocketClient;
                    chart.mode = 1;
                    chart.titles = TitleBool;
                    await chart.ChartGenerate();

                    await Context.Channel.SendFileAsync(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png");

                    EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                    eab.IconUrl = DiscordUser.GetAvatarUrl();
                    eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                    var builder = new EmbedBuilder();
                    builder.WithAuthor(eab);
                    string URI = "https://www.last.fm/user/" + LastFMName;
                    builder.WithUrl(URI);
                    GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                    if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Weekly Artist Chart for " + LastFMName);
                    }
                    else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Monthly Artist Chart for " + LastFMName);
                    }
                    else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Yearly Artist Chart for " + LastFMName);
                    }
                    else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Overall Artist Chart for " + LastFMName);
                    }
                    else
                    {
                        builder.WithDescription("Last.FM " + chartsize + " Artist Chart for " + LastFMName);
                    }

                    var userinfo = await client.User.GetInfoAsync(LastFMName);
                    EmbedFooterBuilder efb = new EmbedFooterBuilder();
                    var playcount = userinfo.Content.Playcount;
                    efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                    builder.WithFooter(efb);

                    await loadingmsg.DeleteAsync();
                    await Context.Channel.SendMessageAsync("", false, builder.Build());

                    File.SetAttributes(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png", FileAttributes.Normal);
                    File.Delete(GlobalVars.UsersFolder + DiscordUser.Id + "-chart.png");
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await loadingmsg.DeleteAsync();
                await ReplyAsync("Unable to generate a FMChart due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmfriends"), Summary("Displays a user's friends and what they are listening to.")]
        [Alias("fmrecentfriends", "fmfriendsrecent")]
        public async Task fmfriendsrecentAsync(IUser user = null)
        {
            try
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();

                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to load your FMBot Friends due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    string[] LastFMFriends = DBase.GetFriendsForID(DiscordUser.Id.ToString());
                    if (LastFMFriends == null || !LastFMFriends.Any())
                    {
                        await ReplyAsync("Your FMBot Friends were unable to be found. Please use fmsetfriends with your friends' names to set your friends.");
                    }
                    else
                    {
                        var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                        try
                        {
                            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();

                            eab.IconUrl = DiscordUser.GetAvatarUrl();

                            eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                            var builder = new EmbedBuilder();
                            builder.WithAuthor(eab);
                            string URI = "https://www.last.fm/user/" + LastFMName;
                            builder.WithUrl(URI);
                            GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                            var amountOfScrobbles = "Amount of scrobbles of all your friends together: ";

                            if (LastFMFriends.Count() > 1)
                            {
                                builder.WithDescription("Songs from " + LastFMFriends.Count() + " friends");
                            }
                            else
                            {
                                builder.WithDescription("Songs from your friend");
                                amountOfScrobbles = "Amount of scrobbles from your friend: ";
                            }

                            string nulltext = "[undefined]";
                            int indexval = (LastFMFriends.Count() - 1);
                            int playcount = 0;

                            try
                            {
                                foreach (var friend in LastFMFriends)
                                {
                                    var tracks = await client.User.GetRecentScrobbles(friend, null, 1, 1);

                                    string TrackName = string.IsNullOrWhiteSpace(tracks.FirstOrDefault().Name) ? nulltext : tracks.FirstOrDefault().Name;
                                    string ArtistName = string.IsNullOrWhiteSpace(tracks.FirstOrDefault().ArtistName) ? nulltext : tracks.FirstOrDefault().ArtistName;
                                    string AlbumName = string.IsNullOrWhiteSpace(tracks.FirstOrDefault().AlbumName) ? nulltext : tracks.FirstOrDefault().AlbumName;

                                    builder.AddField(friend.ToString() + ":", TrackName + " - " + ArtistName + " | " + AlbumName);

                                    // count how many scrobbles everyone has together (if the bot is too slow, consider removing this?)
                                    if (LastFMFriends.Count() <= 8)
                                    {
                                        var userinfo = await client.User.GetInfoAsync(friend);
                                        playcount = playcount + userinfo.Content.Playcount;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                ExceptionReporter.ReportException(disclient, e);
                            }

                            if (LastFMFriends.Count() <= 8)
                            {
                                EmbedFooterBuilder efb = new EmbedFooterBuilder();
                                efb.Text = amountOfScrobbles + playcount.ToString("0");
                                builder.WithFooter(efb);
                            }

                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                        catch (Exception e)
                        {
                            DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                            ExceptionReporter.ReportException(disclient, e);

                            await ReplyAsync("Unable to load your FMBot Friends due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again. If nothing's fixed, see if your friends have done the same thing.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Your FMBot Friends were unable to be found. Please use fmsetfriends with your friends' names to set your friends.");
            }
        }

        [Command("fmrecent"), Summary("Displays a user's recent tracks.")]
        [Alias("fmrecenttracks")]
        public async Task fmrecentAsync(string list = "5", IUser user = null)
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            if (list == "help")
            {
                await ReplyAsync(cfgjson.CommandPrefix + "fmrecent [number of items] [user]");
                return;
            }

            try
            {
                int num = int.Parse(list);

                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to show your recent tracks on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                    try
                    {
                        var tracks = await client.User.GetRecentScrobbles(LastFMName, null, 1, num);

                        EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                        eab.IconUrl = DiscordUser.GetAvatarUrl();
                        eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                        var builder = new EmbedBuilder();
                        builder.WithAuthor(eab);
                        string URI = "https://www.last.fm/user/" + LastFMName;
                        builder.WithUrl(URI);
                        GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                        builder.WithDescription("Top " + num + " Recent Track List");

                        string nulltext = "[undefined]";
                        int indexval = (num - 1);
                        for (int i = 0; i <= indexval; i++)
                        {
                            LastTrack track = tracks.Content.ElementAt(i);

                            string TrackName = string.IsNullOrWhiteSpace(track.Name) ? nulltext : track.Name;
                            string ArtistName = string.IsNullOrWhiteSpace(track.ArtistName) ? nulltext : track.ArtistName;
                            string AlbumName = string.IsNullOrWhiteSpace(track.AlbumName) ? nulltext : track.AlbumName;

                            try
                            {
                                if (i == 0)
                                {
                                    var AlbumInfo = await client.Album.GetInfoAsync(ArtistName, AlbumName);
                                    var AlbumImages = (AlbumInfo.Content.Images != null) ? AlbumInfo.Content.Images : null;
                                    var AlbumThumbnail = (AlbumImages != null) ? AlbumImages.Large.AbsoluteUri : null;
                                    string ThumbnailImage = (AlbumThumbnail != null) ? AlbumThumbnail.ToString() : null;

                                    if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                    {
                                        builder.WithThumbnailUrl(ThumbnailImage);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                ExceptionReporter.ReportException(disclient, e);
                            }

                            int correctnum = (i + 1);
                            builder.AddField("Track #" + correctnum.ToString() + ":", TrackName + " - " + ArtistName + " | " + AlbumName);
                        }

                        EmbedFooterBuilder efb = new EmbedFooterBuilder();

                        var userinfo = await client.User.GetInfoAsync(LastFMName);
                        var playcount = userinfo.Content.Playcount;

                        efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                        builder.WithFooter(efb);

                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);

                        await ReplyAsync("Unable to show your recent tracks on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Unable to show your recent tracks on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmartists"), Summary("Displays artists that a user listened to.")]
        public async Task fmartistsAsync(string list = "5", string time = "overall", IUser user = null)
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            if (list == "help")
            {
                await ReplyAsync(cfgjson.CommandPrefix + "fmartists [number of items] [weekly/monthly/yearly/overall] [user]");
                return;
            }

            try
            {
                int num = int.Parse(list);
                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to show your artists on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                    try
                    {
                        LastStatsTimeSpan timespan = LastStatsTimeSpan.Overall;

                        if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                        {
                            timespan = LastStatsTimeSpan.Week;
                        }
                        else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                        {
                            timespan = LastStatsTimeSpan.Month;
                        }
                        else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                        {
                            timespan = LastStatsTimeSpan.Year;
                        }
                        else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                        {
                            timespan = LastStatsTimeSpan.Overall;
                        }

                        var artists = await client.User.GetTopArtists(LastFMName, timespan, 1, num);

                        EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                        eab.IconUrl = DiscordUser.GetAvatarUrl();
                        eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                        var builder = new EmbedBuilder();
                        builder.WithAuthor(eab);
                        string URI = "https://www.last.fm/user/" + LastFMName;
                        builder.WithUrl(URI);
                        GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                        if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                        {
                            builder.WithDescription("Top " + num + " Weekly Artist List");
                        }
                        else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                        {
                            builder.WithDescription("Top " + num + " Monthly Artist List");
                        }
                        else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                        {
                            builder.WithDescription("Top " + num + " Yearly Artist List");
                        }
                        else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                        {
                            builder.WithDescription("Top " + num + " Overall Artist List");
                        }

                        string nulltext = "[undefined]";
                        int indexval = (num - 1);
                        for (int i = 0; i <= indexval; i++)
                        {
                            LastArtist artist = artists.Content.ElementAt(i);

                            string ArtistName = string.IsNullOrWhiteSpace(artist.Name) ? nulltext : artist.Name;

                            try
                            {
                                if (i == 0)
                                {
                                    var ArtistInfo = await client.Artist.GetInfoAsync(ArtistName);
                                    var ArtistImages = (ArtistInfo.Content.MainImage != null) ? ArtistInfo.Content.MainImage : null;
                                    var ArtistThumbnail = (ArtistImages != null) ? ArtistImages.Large.AbsoluteUri : null;
                                    string ThumbnailImage = (ArtistThumbnail != null) ? ArtistThumbnail.ToString() : null;

                                    if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                    {
                                        builder.WithThumbnailUrl(ThumbnailImage);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                ExceptionReporter.ReportException(disclient, e);
                            }

                            int correctnum = (i + 1);
                            builder.AddField("Artist #" + correctnum.ToString() + ":", ArtistName);
                        }

                        EmbedFooterBuilder efb = new EmbedFooterBuilder();

                        var userinfo = await client.User.GetInfoAsync(LastFMName);
                        var playcount = userinfo.Content.Playcount;

                        efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                        builder.WithFooter(efb);

                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);

                        await ReplyAsync("Unable to show your artists on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Unable to show your artists on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmalbums"), Summary("Displays albums that a user listened to.")]
        public async Task fmalbumsAsync(string list = "5", string time = "overall", IUser user = null)
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            if (list == "help")
            {
                await ReplyAsync(cfgjson.CommandPrefix + "fmalbums [number of items] [weekly/monthly/yearly/overall] [user]");
                return;
            }

            try
            {
                int num = int.Parse(list);
                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to show your albums on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);
                    try
                    {
                        LastStatsTimeSpan timespan = LastStatsTimeSpan.Overall;

                        if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                        {
                            timespan = LastStatsTimeSpan.Week;
                        }
                        else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                        {
                            timespan = LastStatsTimeSpan.Month;
                        }
                        else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                        {
                            timespan = LastStatsTimeSpan.Year;
                        }
                        else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                        {
                            timespan = LastStatsTimeSpan.Overall;
                        }

                        var albums = await client.User.GetTopAlbums(LastFMName, timespan, 1, num);

                        EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                        eab.IconUrl = DiscordUser.GetAvatarUrl();
                        eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                        var builder = new EmbedBuilder();
                        builder.WithAuthor(eab);
                        string URI = "https://www.last.fm/user/" + LastFMName;
                        builder.WithUrl(URI);
                        GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);

                        if (time.Equals("weekly") || time.Equals("week") || time.Equals("w"))
                        {
                            builder.WithDescription("Top " + num + " Weekly Album List");
                        }
                        else if (time.Equals("monthly") || time.Equals("month") || time.Equals("m"))
                        {
                            builder.WithDescription("Top " + num + " Monthly Album List");
                        }
                        else if (time.Equals("yearly") || time.Equals("year") || time.Equals("y"))
                        {
                            builder.WithDescription("Top " + num + " Yearly Album List");
                        }
                        else if (time.Equals("overall") || time.Equals("alltime") || time.Equals("o") || time.Equals("at"))
                        {
                            builder.WithDescription("Top " + num + " Overall Album List");
                        }

                        string nulltext = "[undefined]";
                        int indexval = (num - 1);
                        for (int i = 0; i <= indexval; i++)
                        {
                            LastAlbum album = albums.Content.ElementAt(i);

                            string AlbumName = string.IsNullOrWhiteSpace(album.Name) ? nulltext : album.Name;
                            string ArtistName = string.IsNullOrWhiteSpace(album.ArtistName) ? nulltext : album.ArtistName;

                            try
                            {
                                if (i == 0)
                                {
                                    var AlbumInfo = await client.Album.GetInfoAsync(ArtistName, AlbumName);
                                    var AlbumImages = (AlbumInfo.Content.Images != null) ? AlbumInfo.Content.Images : null;
                                    var AlbumThumbnail = (AlbumImages != null) ? AlbumImages.Large.AbsoluteUri : null;
                                    string ThumbnailImage = (AlbumThumbnail != null) ? AlbumThumbnail.ToString() : null;

                                    if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                                    {
                                        builder.WithThumbnailUrl(ThumbnailImage);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                                ExceptionReporter.ReportException(disclient, e);
                            }

                            int correctnum = (i + 1);
                            builder.AddField("Album #" + correctnum.ToString() + ":", AlbumName + " | " + ArtistName);
                        }

                        EmbedFooterBuilder efb = new EmbedFooterBuilder();

                        var userinfo = await client.User.GetInfoAsync(LastFMName);
                        var playcount = userinfo.Content.Playcount;

                        efb.Text = LastFMName + "'s Total Tracks: " + playcount.ToString("0");

                        builder.WithFooter(efb);

                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);

                        await ReplyAsync("Unable to show your albums on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Unable to show your albums on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        [Command("fmstats"), Summary("Displays user stats related to Last.FM and FMBot")]
        [Alias("fminfo")]
        public async Task fmstatsAsync(IUser user = null)
        {
            try
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();

                var DiscordUser = GlobalVars.CheckIfDM(user, Context);
                var SelfUser = Context.Client.CurrentUser;
                string LastFMName = DBase.GetNameForID(DiscordUser.Id.ToString());
                if (LastFMName.Equals("NULL"))
                {
                    await ReplyAsync("Unable to show your stats on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
                }
                else
                {
                    var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);

                    EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
                    eab.IconUrl = DiscordUser.GetAvatarUrl();
                    eab.Name = GlobalVars.GetNameString(DiscordUser, Context);

                    var builder = new EmbedBuilder();
                    builder.WithAuthor(eab);
                    string URI = "https://www.last.fm/user/" + LastFMName;
                    builder.WithUrl(URI);
                    GlobalVars.SetUserTitleEmbed(builder, DiscordUser, LastFMName, SelfUser);
                    builder.WithDescription("Last.FM Statistics for " + LastFMName);

                    var userinfo = await client.User.GetInfoAsync(LastFMName);

                    try
                    {
                        var userinfoImages = (userinfo.Content.Avatar != null) ? userinfo.Content.Avatar : null;
                        var userinfoThumbnail = (userinfoImages != null) ? userinfoImages.Large.AbsoluteUri : null;
                        string ThumbnailImage = (userinfoThumbnail != null) ? userinfoThumbnail.ToString() : null;

                        if (!string.IsNullOrWhiteSpace(ThumbnailImage))
                        {
                            builder.WithThumbnailUrl(ThumbnailImage);
                        }
                    }
                    catch (Exception e)
                    {
                        DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                        ExceptionReporter.ReportException(disclient, e);
                    }

                    var playcount = userinfo.Content.Playcount;
                    var usertype = userinfo.Content.Type;
                    var playlists = userinfo.Content.Playlists;
                    var premium = userinfo.Content.IsSubscriber;

                    string LastFMMode = DBase.GetNameForModeInt(DBase.GetModeIntForID(DiscordUser.Id.ToString()));

                    builder.AddInlineField("Last.FM Name: ", LastFMName);
                    builder.AddInlineField(SelfUser.Username + " Mode: ", LastFMMode);
                    builder.AddInlineField("User Type: ", usertype.ToString());
                    builder.AddInlineField("Total Tracks: ", playcount.ToString("0"));
                    builder.AddInlineField("Has Last.FM Premium? ", premium.ToString());
                    builder.AddInlineField("Is " + SelfUser.Username + " Admin? ", FMBotAdminUtil.IsAdmin(DiscordUser).ToString());
                    builder.AddInlineField("Is " + SelfUser.Username + " Super Admin? ", FMBotAdminUtil.IsSuperAdmin(DiscordUser).ToString());
                    builder.AddInlineField("Is " + SelfUser.Username + " Owner? ", FMBotAdminUtil.IsOwner(DiscordUser).ToString());

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Unable to show your stats on Last.FM due to an internal error. Try setting a Last.FM name with the 'fmset' command, scrobbling something, and then use the command again.");
            }
        }

        #endregion

        #region FMBot Commands

        [Command("fmfeatured"), Summary("Displays the featured avatar.")]
        [Alias("fmfeaturedavatar", "fmfeatureduser", "fmfeaturedalbum")]
        public async Task fmfeaturedAsync()
        {
            try
            {
                var builder = new EmbedBuilder();
                var SelfUser = Context.Client.CurrentUser;
                builder.WithThumbnailUrl(SelfUser.GetAvatarUrl());
                builder.AddInlineField("Featured:", _timer.GetTrackString());

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                await ReplyAsync("Unable to show the featured avatar on FMBot due to an internal error. The timer service cannot be loaded. Please wait for the bot to fully load.");
            }
        }

        [Command("fmset"), Summary("Sets your Last.FM name and FM mode.")]
        [Alias("fmsetname", "fmsetmode")]
        public async Task fmsetAsync([Summary("Your Last.FM name")] string name, [Summary("The mode you want to use.")] string mode = "embedmini")
        {
            if (name == "help")
            {
                var cfgjson = await JsonCfg.GetJSONDataAsync();
                await ReplyAsync(cfgjson.CommandPrefix + "fmset <Last.FM Username> [embedmini/embedfull/textfull/textmini]");
                return;
            }

            string SelfID = Context.Message.Author.Id.ToString();
            var SelfUser = Context.Client.CurrentUser;
            if (DBase.EntryExists(SelfID))
            {
                int modeint = 0;

                if (!string.IsNullOrWhiteSpace(mode))
                {
                    modeint = DBase.GetIntForModeName(mode);
                    if (modeint > 3 || modeint < 0)
                    {
                        await ReplyAsync("Invalid mode. Please use 'embedmini', 'embedfull', 'textfull', or 'textmini'.");
                        return;
                    }
                }
                else
                {
                    modeint = DBase.GetModeIntForID(SelfID);
                }
                DBase.WriteEntry(SelfID, name, modeint);
            }
            else
            {
                int modeint = DBase.GetIntForModeName(mode);
                DBase.WriteEntry(SelfID, name, modeint);
            }
            string LastFMMode = DBase.GetNameForModeInt(DBase.GetModeIntForID(SelfID));
            await ReplyAsync("Your Last.FM name has been set to '" + name + "' and your " + SelfUser.Username + " mode has been set to '" + LastFMMode + "'.");
        }

        [Command("fmaddfriends"), Summary("Adds your friends' Last.FM names.")]
        [Alias("fmfriendsset", "fmsetfriends", "fmfriendsadd")]
        public async Task fmfriendssetAsync([Summary("Friend names")] params string[] friends)
        {
            try
            {
                string SelfID = Context.Message.Author.Id.ToString();

                var cfgjson = await JsonCfg.GetJSONDataAsync();
                var client = new LastfmClient(cfgjson.FMKey, cfgjson.FMSecret);

                var friendList = new List<string>();
                var friendNotFoundList = new List<string>();


                foreach (var friend in friends)
                {
                    var user = await client.User.GetInfoAsync(friend);

                    if (user.Content != null)
                    {
                        friendList.Add(user.Content.Name);
                    }
                    else
                    {
                        friendNotFoundList.Add(friend);
                    }
                }

                if (friendList.Any())
                {
                    int friendcount = DBase.AddFriendsEntry(SelfID, friendList.ToArray());

                    if (friendcount > 1)
                    {
                        await ReplyAsync("Succesfully added " + friendcount + " friends.");
                    }
                    else if (friendcount < 1)
                    {
                        await ReplyAsync("Didn't add  " + friendcount + " friends. Maybe they are already on your friendlist.");
                    }
                    else
                    {
                        await ReplyAsync("Succesfully added a friend.");
                    }
                }

                if (friendNotFoundList.Any())
                {
                    if (friendNotFoundList.Count > 1)
                    {
                        await ReplyAsync("Could not find " + friendNotFoundList.Count + " friends. Please ensure that you spelled their names correctly.");
                    }
                    else
                    {
                        await ReplyAsync("Could not find 1 friend. Please ensure that you spelled the name correctly.");
                    }
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                int friendcount = friends.Count();

                if (friendcount > 1)
                {
                    await ReplyAsync("Unable to add " + friendcount + " friends due to an internal error.");
                }
                else
                {
                    await ReplyAsync("Unable to add a friend due to an internal error.");
                }
            }
        }

        [Command("fmremovefriends"), Summary("Remove your friends' Last.FM names.")]
        [Alias("fmfriendsremove")]
        public async Task fmfriendsremoveAsync([Summary("Friend names")] params string[] friends)
        {
            if (!friends.Any())
            {
                await ReplyAsync("Please enter at least one friend to remove.");
                return;
            }

            try
            {
                string SelfID = Context.Message.Author.Id.ToString();

                int friendcount = DBase.RemoveFriendsEntry(SelfID, friends);

                if (friendcount > 1)
                {
                    await ReplyAsync("Succesfully removed " + friendcount + " friends.");
                }
                else if (friendcount < 1)
                {
                    await ReplyAsync("Couldn't remove " + friendcount + " friends. Please check if the user is on your friendslist.");
                }
                else
                {
                    await ReplyAsync("Succesfully removed a friend.");
                }
            }
            catch (Exception e)
            {
                DiscordSocketClient disclient = Context.Client as DiscordSocketClient;
                ExceptionReporter.ReportException(disclient, e);

                int friendcount = friends.Count();

                if (friendcount > 1)
                {
                    await ReplyAsync("Unable to remove " + friendcount + " friends due to an internal error. Did you add anyone?");
                }
                else
                {
                    await ReplyAsync("Unable to add a friend due to an internal error. Did you add anyone?");
                }
            }
        }

        [Command("fmremove"), Summary("Deletes your FMBot data.")]
        [Alias("fmdelete", "fmremovedata", "fmdeletedata")]
        public async Task fmremoveAsync()
        {
            string SelfID = Context.Message.Author.Id.ToString();
            var SelfUser = Context.Client.CurrentUser;
            DBase.RemoveEntry(SelfID);
            await ReplyAsync("Your " + SelfUser.Username + " settings and data have been successfully deleted.");
        }

        [Command("fmhelp"), Summary("Displays this list.")]
        [Alias("fmbot")]
        public async Task fmhelpAsync()
        {
            var cfgjson = await JsonCfg.GetJSONDataAsync();

            string prefix = cfgjson.CommandPrefix;

            var SelfUser = Context.Client.CurrentUser;

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        if (!string.IsNullOrWhiteSpace(cmd.Summary))
                        {
                            description += $"{prefix}{cmd.Aliases.First()} - {cmd.Summary}\n";
                        }
                        else
                        {
                            description += $"{prefix}{cmd.Aliases.First()}\n";
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    await Context.User.SendMessageAsync(module.Name + "\n" + description);
                }
            }

            string helpstring = SelfUser.Username + " Info\n\nBe sure to use 'help' after a command name to see the parameters.\n\nChart sizes range from 3x3 to 10x10.\n\nModes for the fmset command:\nembedmini\nembedfull\ntextfull\ntextmini\nuserdefined (fmserverset only)\n\nFMBot Time Periods for the fmchart, fmartistchart, fmartists, and fmalbums commands:\nweekly\nweek\nw\nmonthly\nmonth\nm\nyearly\nyear\ny\noverall\nalltime\no\nat\n\nFMBot Title options for FMChart:\ntitles\nnotitles";

            if (!GlobalVars.GetDMBool())
            {
                await Context.Channel.SendMessageAsync("Check your DMs!");
                await Context.User.SendMessageAsync(helpstring);
            }
            else
            {
                await Context.Channel.SendMessageAsync(helpstring);
            }

        }

        [Command("fmstatus"), Summary("Displays bot stats.")]
        public async Task statusAsync()
        {
            var SelfUser = Context.Client.CurrentUser;

            EmbedAuthorBuilder eab = new EmbedAuthorBuilder();
            eab.IconUrl = SelfUser.GetAvatarUrl();
            eab.Name = SelfUser.Username;

            var builder = new EmbedBuilder();
            builder.WithAuthor(eab);

            builder.WithDescription(SelfUser.Username + " Statistics");

            var startTime = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            var files = FastDirectoryEnumerator.EnumerateFiles(GlobalVars.UsersFolder, "*.txt");

            string pattern = "[0-9]{18}\\.txt";

            int filecount = 0;

            foreach (FileData file in files)
            {
                if (Regex.IsMatch(file.Name, pattern))
                {
                    filecount += 1;
                }
            }

            var SocketClient = Context.Client as DiscordSocketClient;
            var SelfGuilds = SocketClient.Guilds.Count();

            var SocketSelf = Context.Client.CurrentUser as SocketSelfUser;

            string status = "Online";

            switch (SocketSelf.Status)
            {
                case UserStatus.Offline: status = "Offline"; break;
                case UserStatus.Online: status = "Online"; break;
                case UserStatus.Idle: status = "Idle"; break;
                case UserStatus.AFK: status = "AFK"; break;
                case UserStatus.DoNotDisturb: status = "Do Not Disturb"; break;
                case UserStatus.Invisible: status = "Invisible/Offline"; break;
            }

            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            int fixedCmdGlobalCount = GlobalVars.CommandExecutions + 1;
            int fixedCmdGlobalCount_Servers = GlobalVars.CommandExecutions_Servers + 1;
            int fixedCmdGlobalCount_DMs = GlobalVars.CommandExecutions_DMs + 1;

            builder.AddInlineField("Bot Uptime: ", startTime.ToReadableString());
            builder.AddInlineField("Server Uptime: ", GlobalVars.SystemUpTime().ToReadableString());
            builder.AddInlineField("Number of users in the database: ", filecount);
            builder.AddInlineField("Total number of command executions since bot start: ", fixedCmdGlobalCount);
            builder.AddInlineField("Command executions in servers since bot start: ", fixedCmdGlobalCount_Servers);
            builder.AddInlineField("Command executions in DMs since bot start: ", fixedCmdGlobalCount_DMs);
            builder.AddField("Number of servers the bot is on: ", SelfGuilds);
            builder.AddField("Bot status: ", status);
            builder.AddField("Bot version: ", assemblyVersion);

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("fminvite"), Summary("Invites the bot to a server")]
        public async Task inviteAsync()
        {
            string SelfID = Context.Client.CurrentUser.Id.ToString();
            await ReplyAsync("https://discordapp.com/oauth2/authorize?client_id=" + SelfID + "&scope=bot&permissions=0");
        }

        [Command("fmdonate"), Summary("Please donate if you like this bot!")]
        public async Task donateAsync()
        {
            await ReplyAsync("If you like the bot and you would like to support its development, feel free to support the developer at: https://www.paypal.me/Bitl");
        }

        [Command("fmgithub"), Summary("GitHub Page")]
        public async Task githubAsync()
        {
            await ReplyAsync("https://github.com/Bitl/FMBot_Discord");
        }

        [Command("fmgitlab"), Summary("GitLab Page")]
        public async Task gitlabAsync()
        {
            await ReplyAsync("https://gitlab.com/Bitl/FMBot_Discord");
        }

        [Command("fmbugs"), Summary("Report bugs here!")]
        public async Task bugsAsync()
        {
            await ReplyAsync("Report bugs here:\nGithub: https://github.com/Bitl/FMBot_Discord/issues \nGitLab: https://gitlab.com/Bitl/FMBot_Discord/issues");
        }

        [Command("fmserver"), Summary("Join the Discord server!")]
        public async Task serverAsync()
        {
            await ReplyAsync("Join the Discord server! https://discord.gg/srmpCaa");
        }

        #endregion
    }
}
