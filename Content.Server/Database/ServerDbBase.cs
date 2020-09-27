﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public abstract class ServerDbBase
    {
        public async Task<PlayerPreferences> GetPlayerPreferencesAsync(NetUserId userId)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext
                .Preferences
                .Include(p => p.Profiles).ThenInclude(h => h.Jobs)
                .Include(p => p.Profiles).ThenInclude(h => h.Antags)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId);

            if (prefs is null) return null;

            var maxSlot = prefs.Profiles.Max(p => p.Slot)+1;
            var profiles = new ICharacterProfile[maxSlot];
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            return new PlayerPreferences
            (
                profiles,
                prefs.SelectedCharacterSlot
            );
        }

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext.Preferences.SingleAsync(p => p.UserId == userId.UserId);
            prefs.SelectedCharacterSlot = index;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile profile, int slot)
        {
            if (profile is null)
            {
                await DeleteCharacterSlotAsync(userId, slot);
                return;
            }

            await using var db = await GetDb();
            if (!(profile is HumanoidCharacterProfile humanoid))
            {
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();
            }

            var entity = ConvertProfiles(humanoid, slot);

            var prefs = await db.DbContext
                .Preferences
                .SingleAsync(p => p.UserId == userId.UserId);

            var oldProfile = prefs
                .Profiles
                .SingleOrDefault(h => h.Slot == entity.Slot);

            if (!(oldProfile is null))
            {
                prefs.Profiles.Remove(oldProfile);
            }

            prefs.Profiles.Add(entity);
            await db.DbContext.SaveChangesAsync();
        }

        private async Task DeleteCharacterSlotAsync(NetUserId userId, int slot)
        {
            await using var db = await GetDb();

            db.DbContext
                .Preferences
                .Single(p => p.UserId == userId.UserId)
                .Profiles
                .RemoveAll(h => h.Slot == slot);

            await db.DbContext.SaveChangesAsync();
        }

        public async Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            await using var db = await GetDb();

            var profile = ConvertProfiles((HumanoidCharacterProfile) defaultProfile, 0);
            var prefs = new Prefs
            {
                UserId = userId.UserId,
                SelectedCharacterSlot = 0
            };

            prefs.Profiles.Add(profile);

            db.DbContext.Preferences.Add(prefs);

            await db.DbContext.SaveChangesAsync();

            return new PlayerPreferences(new []{defaultProfile}, 0);
        }

        private static HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => a.AntagName);
            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.Age,
                profile.Sex == "Male" ? Sex.Male : Sex.Female,
                new HumanoidCharacterAppearance
                (
                    profile.HairName,
                    Color.FromHex(profile.HairColor),
                    profile.FacialHairName,
                    Color.FromHex(profile.FacialHairColor),
                    Color.FromHex(profile.EyeColor),
                    Color.FromHex(profile.SkinColor)
                ),
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToList()
            );
        }

        private static Profile ConvertProfiles(HumanoidCharacterProfile humanoid, int slot)
        {
            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;

            var entity = new Profile
            {
                CharacterName = humanoid.Name,
                Age = humanoid.Age,
                Sex = humanoid.Sex.ToString(),
                HairName = appearance.HairStyleName,
                HairColor = appearance.HairColor.ToHex(),
                FacialHairName = appearance.FacialHairStyleName,
                FacialHairColor = appearance.FacialHairColor.ToHex(),
                EyeColor = appearance.EyeColor.ToHex(),
                SkinColor = appearance.SkinColor.ToHex(),
                Slot = slot,
                PreferenceUnavailable = (DbPreferenceUnavailableMode) humanoid.PreferenceUnavailable
            };
            entity.Jobs.AddRange(
                humanoid.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = (DbJobPriority) j.Value})
            );
            entity.Antags.AddRange(
                humanoid.AntagPreferences
                    .Select(a => new Antag {AntagName = a})
            );

            return entity;
        }

        protected abstract Task<DbGuard> GetDb();

        protected abstract class DbGuard : IAsyncDisposable
        {
            public abstract ServerDbContext DbContext { get; }

            public abstract ValueTask DisposeAsync();
        }
    }
}
