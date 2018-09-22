﻿using Discord;
using Discord.Commands;
using FMBot.Data.Entities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace FMBot.Services
{
    public class UserService
    {
        private FMBotDbContext db = new FMBotDbContext();

        // User settings
        public async Task<Settings> GetUserSettingsAsync(IUser discordUser)
        {
            string discordUserID = discordUser.Id.ToString();

            User user = await db.Users.FirstOrDefaultAsync(f => f.DiscordUserID == discordUserID);

            if (user == null)
            {
                return null;
            }

            return user.Settings;
        }

        // Discord nickname/username
        public async Task<string> GetNameAsync(ICommandContext context)
        {
            if (context.Guild == null)
            {
                return context.User.Username;
            }

            IGuildUser guildUser = await context.Guild.GetUserAsync(context.User.Id);

            return guildUser.Nickname ?? context.User.Username;
        }

        // Rank
        public async Task<UserType> GetRankAsync(IUser discordUser)
        {
            string discordUserID = discordUser.Id.ToString();

            User user = await db.Users.FirstOrDefaultAsync(f => f.DiscordUserID == discordUserID);

            if (user == null)
            {
                return UserType.User;
            }

            return user.UserType;
        }


        // UserTitle
        public async Task<string> GetUserTitleAsync(ICommandContext context)
        {
            string name = await GetNameAsync(context);
            UserType rank = await GetRankAsync(context.User);

            // TODO, add 'featured user'

            return name + " " + rank.ToString();
        }

        // Set LastFM Name
        public void SetLastFM(IUser discordUser, string lastFMName, ChartType chartType)
        {
            string discordUserID = discordUser.Id.ToString();

            User user = db.Users.FirstOrDefault(f => f.DiscordUserID == discordUserID);

            if (user == null)
            {
                User newUser = new User
                {
                    DiscordUserID = discordUserID,
                    UserType = UserType.User
                };

                Settings newUserSetting = new Settings
                {
                    User = newUser,
                    UserNameLastFM = lastFMName,
                    TitlesEnabled = true,
                    ChartTimePeriod = ChartTimePeriod.Monthly,
                    ChartType = chartType,
                };

                db.Users.Add(newUser);
                db.Settings.Add(newUserSetting);

                db.SaveChanges();
            }
            else
            {
                user.Settings.UserNameLastFM = lastFMName;
                user.Settings.ChartType = chartType;

                db.Entry(user.Settings).State = EntityState.Modified;

                db.SaveChanges();
            }
        }
    }
}
