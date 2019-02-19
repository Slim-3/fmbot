using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FMBot.Bot
{
    public class FMBotUtil
    {
        #region Database Functions

        public class DBase
        {
            #region User Settings

            public static async Task<IGuildUser> ConvertIDToGuildUser(IGuild guild, ulong id)
            {
                IReadOnlyCollection<IGuildUser> users = await guild.GetUsersAsync();

                foreach (IGuildUser user in users)
                {
                    if (user.Id == id)
                    {
                        return user;
                    }
                }

                return null;
            }

            #endregion
        }

        #endregion

        #region Configuration Data

        public class JsonCfg
        {
            // this structure will hold data from config.json
            public struct ConfigJson
            {
                [JsonProperty("token")]
                public string Token { get; private set; }

                [JsonProperty("fmkey")]
                public string FMKey { get; private set; }

                [JsonProperty("fmsecret")]
                public string FMSecret { get; private set; }

                [JsonProperty("prefix")]
                public string CommandPrefix { get; private set; }

                [JsonProperty("baseserver")]
                public string BaseServer { get; private set; }

                [JsonProperty("announcementchannel")]
                public string AnnouncementChannel { get; private set; }

                [JsonProperty("featuredchannel")]
                public string FeaturedChannel { get; private set; }

                [JsonProperty("botowner")]
                public string BotOwner { get; private set; }

                [JsonProperty("timerinit")]
                public string TimerInit { get; private set; }

                [JsonProperty("timerrepeat")]
                public string TimerRepeat { get; private set; }

                [JsonProperty("spotifykey")]
                public string SpotifyKey { get; private set; }

                [JsonProperty("spotifysecret")]
                public string SpotifySecret { get; private set; }

                [JsonProperty("exceptionchannel")]
                public string ExceptionChannel { get; private set; }

                [JsonProperty("cooldown")]
                public string Cooldown { get; private set; }

                [JsonProperty("nummessages")]
                public string NumMessages { get; private set; }

                [JsonProperty("inbetweentime")]
                public string InBetweenTime { get; private set; }
            }

            public static async Task<ConfigJson> GetJSONDataAsync()
            {
                // first, let's load our configuration file
                await GlobalVars.Log(new LogMessage(LogSeverity.Info, "JsonCfg", "Loading Configuration"));
                string json = "";
                using (FileStream fs = File.OpenRead(GlobalVars.ConfigFileName))
                using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = await sr.ReadToEndAsync();
                }

                // next, let's load the values from that file
                // to our client's configuration
                ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
                return cfgjson;
            }

            public static ConfigJson GetJSONData()
            {
                // first, let's load our configuration file
                GlobalVars.Log(new LogMessage(LogSeverity.Info, "JsonCfg", "Loading Configuration"));
                string json = "";
                using (FileStream fs = File.OpenRead(GlobalVars.ConfigFileName))
                using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = sr.ReadToEnd();
                }

                // next, let's load the values from that file
                // to our client's configuration
                ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
                return cfgjson;
            }
        }

        #endregion

        #region Exception Reporter

        public class ExceptionReporter
        {
            public static async void ReportException(DiscordSocketClient client = null, Exception e = null)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync();

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e.Message + "\nSource:\n" + e.Source + "\nStack Trace:\n" + e.StackTrace);

                    await channel.SendMessageAsync("", false, builder.Build());
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e.Message + "\n\nSource:\n" + e.Source + "\n\nStack Trace:\n" + e.StackTrace);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it."));
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e.Message, e), true);
            }

            public static async void ReportShardedException(DiscordShardedClient client = null, Exception e = null)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync();

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e.Message + "\nSource:\n" + e.Source + "\nStack Trace:\n" + e.StackTrace);

                    await channel.SendMessageAsync("", false, builder.Build());
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e.Message + "\n\nSource:\n" + e.Source + "\n\nStack Trace:\n" + e.StackTrace);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it."));
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e.Message, e), true);
            }

            public static async void ReportStringAsException(DiscordShardedClient client, string e)
            {
                JsonCfg.ConfigJson cfgjson = await JsonCfg.GetJSONDataAsync();

                try
                {
                    ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                    ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                    SocketGuild guild = client.GetGuild(BroadcastServerID);
                    SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.AddField("Exception:", e);

                    await channel.SendMessageAsync("", false, builder.Build());
                }
                catch (Exception)
                {
                    try
                    {
                        ulong BroadcastServerID = Convert.ToUInt64(cfgjson.BaseServer);
                        ulong BroadcastChannelID = Convert.ToUInt64(cfgjson.ExceptionChannel);

                        SocketGuild guild = client.GetGuild(BroadcastServerID);
                        SocketTextChannel channel = guild.GetTextChannel(BroadcastChannelID);

                        await channel.SendMessageAsync("Exception: " + e);
                    }
                    catch (Exception)
                    {
                        await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", "Unable to connect to the server/channel to report error. Look in the log.txt in the FMBot folder to see it."));
                    }
                }

                await GlobalVars.Log(new LogMessage(LogSeverity.Warning, "ExceptionReporter", e), true);
            }
        }

        #endregion

        #region Global Variables

        public class GlobalVars
        {
            public static string ConfigFileName = "config.json";
            public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;
            public static string CacheFolder = BasePath + "cache/";
            public static string FeaturedUserID = "";
            public static int MessageLength = 2000;
            public static int CommandExecutions = 0;
            public static int CommandExecutions_Servers = 0;
            public static int CommandExecutions_DMs = 0;
            public static Hashtable charts = new Hashtable();

            private static readonly bool IsUserInDM = false;

            public static string GetChartFileName(ulong id)
            {
                return GlobalVars.CacheFolder + id.ToString() + "-chart.png";
            }

            public static MemoryStream GetChartStream(ulong id)
            {
                MemoryStream dest = new MemoryStream();
                string fileName = GlobalVars.GetChartFileName(id);
                Bitmap chartBitmap = (Bitmap)(GlobalVars.charts[fileName]);
                chartBitmap.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
                dest.Position = 0;

                return dest;
            }

            public static TimeSpan SystemUpTime()
            {
                ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
                DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
                return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
            }

            public static Task Log(LogMessage arg, bool nowrite = false)
            {
                if (nowrite == false)
                {
                    Console.WriteLine(arg);
                }

                NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

                logger.Info(arg);

                return Task.CompletedTask;
            }


            public static Bitmap Combine(List<Bitmap> images, bool vertical = false)
            {
                //read all images into memory
                Bitmap finalImage = null;

                try
                {
                    int width = 0;
                    int height = 0;

                    foreach (Bitmap image in images.ToArray())
                    {
                        //create a Bitmap from the file and add it to the list
                        Bitmap bitmap = image;

                        //update the size of the final bitmap
                        if (vertical == true)
                        {
                            width = bitmap.Width > width ? bitmap.Width : width;
                            height += bitmap.Height;
                        }
                        else
                        {
                            width += bitmap.Width;
                            height = bitmap.Height > height ? bitmap.Height : height;
                        }

                        images.Add(bitmap);
                    }

                    //create a bitmap to hold the combined image
                    finalImage = new Bitmap(width, height);

                    //get a graphics object from the image so we can draw on it
                    using (Graphics g = Graphics.FromImage(finalImage))
                    {
                        //set background color
                        g.Clear(System.Drawing.Color.Black);

                        //go through each image and draw it on the final image
                        int offset = 0;
                        foreach (Bitmap image in images)
                        {
                            if (vertical == true)
                            {
                                g.DrawImage(image, new Rectangle(0, offset, image.Width, image.Height));
                                offset += image.Height;
                            }
                            else
                            {
                                g.DrawImage(image, new Rectangle(offset, 0, image.Width, image.Height));
                                offset += image.Width;
                            }
                        }
                    }

                    return finalImage;
                }
                catch (Exception ex)
                {
                    if (finalImage != null)
                    {
                        finalImage.Dispose();
                    }

                    throw ex;
                }
                finally
                {
                    //clean up memory
                    foreach (Bitmap image in images)
                    {
                        image.Dispose();
                    }
                }
            }

            public static List<List<Bitmap>> splitBitmapList(List<Bitmap> locations, int nSize)
            {
                List<List<Bitmap>> list = new List<List<Bitmap>>();

                for (int i = 0; i < locations.Count; i += nSize)
                {
                    list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
                }

                return list;
            }


            public static bool GetDMBool()
            {
                return IsUserInDM;
            }


            public static void ClearReadOnly(DirectoryInfo parentDirectory)
            {
                if (parentDirectory != null)
                {
                    parentDirectory.Attributes = FileAttributes.Normal;
                    foreach (FileInfo fi in parentDirectory.GetFiles())
                    {
                        fi.Attributes = FileAttributes.Normal;
                    }
                    foreach (DirectoryInfo di in parentDirectory.GetDirectories())
                    {
                        ClearReadOnly(di);
                    }
                }
            }
        }

        #endregion


    }
}