using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Server.Database._Blimpuf; // Blimpuf
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Preferences
{
    [TestFixture]
    public sealed class ServerDbSqliteTests : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: dataset
  id: sqlite_test_names_first_male
  values:
  - Aaden

- type: dataset
  id: sqlite_test_names_first_female
  values:
  - Aaliyah

- type: dataset
  id: sqlite_test_names_last
  values:
  - Ackerley";

        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new()
            {
                Name = "Charlie Charlieson",
                FlavorText = "The biggest boy around.",
                Species = "Human",
                CustomSpecieName = "", // Starlight
                Age = 21,
                Appearance = new(
                    "Afro",
                    Color.Aqua,
                    false, //starlight
                    "Shaved",
                    Color.Aquamarine,
                    false, //starlight
                    Color.Azure,
                    false, //starlight
                    Color.Beige,
                    new(),
                    1, //starlight
                    1) //starlight
            };
        }

        private static ServerDbSqlite GetDb(RobustIntegrationTest.ServerIntegrationInstance server)
        {
            var cfg = server.ResolveDependency<IConfigurationManager>();
            var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            builder.UseSqlite(conn);
            return new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog);
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var pair = Pair;
            var db = GetDb(pair.Server);
            // Database should be empty so a new GUID should do it.
            Assert.That(await db.GetPlayerPreferencesAsync(NewUserId()), Is.Null);
        }

        // Blimpuf start - secondary pronoun validation
        [TestCase(null)]
        [TestCase(Gender.Female)]
        [TestCase(Gender.Epicene)]
        [TestCase(Gender.Neuter)]
        public async Task TestInitPrefs(Gender? secondaryGender)
        {
            var pair = Pair;
            var db = GetDb(pair.Server);
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            const int slot = 0;
            var originalProfile = CharlieCharlieson().WithSecondaryGender(secondaryGender);
            await db.InitPrefsAsync(username, originalProfile);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.Single(p => p.Key == slot).Value.MemberwiseEquals(originalProfile));
        }

        [Test]
        public async Task TestSaveAndClearSecondaryGender()
        {
            var db = GetDb(Pair.Server);
            var username = NewUserId();
            var originalProfile = CharlieCharlieson();
            await db.InitPrefsAsync(username, originalProfile);

            await db.SaveCharacterSlotAsync(username, originalProfile.WithSecondaryGender(Gender.Female), 0);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            var saved = (HumanoidCharacterProfile)prefs.Characters[0];
            Assert.That(saved.Gender, Is.EqualTo(originalProfile.Gender));
            Assert.That(saved.SecondaryGender, Is.EqualTo(Gender.Female));

            await db.SaveCharacterSlotAsync(username, saved.WithSecondaryGender(null), 0);
            prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters[0].MemberwiseEquals(originalProfile), Is.True);
        }

        [Test]
        public async Task TestBlimpufProfileRelationship()
        {
            var server = Pair.Server;
            using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            connection.Open();
            var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options;
            var db = new ServerDbSqlite(() => options, true,
                server.ResolveDependency<IConfigurationManager>(), true,
                server.ResolveDependency<ILogManager>().GetSawmill("db.ops"));
            var username = NewUserId();
            var profile = CharlieCharlieson();
            await db.InitPrefsAsync(username, profile);
            await db.SaveCharacterSlotAsync(username, profile.WithSecondaryGender(Gender.Female), 1);

            int profileId;
            using (var context = new SqliteServerDbContext(options))
            {
                profileId = context.Profile.Single(p => p.Slot == 1).Id;
                await context.Set<BlimpufProfile>().Where(p => p.ProfileId == profileId).ExecuteDeleteAsync();
            }

            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters[1].MemberwiseEquals(profile), Is.True);

            await db.SaveCharacterSlotAsync(username, profile.WithSecondaryGender(Gender.Female), 1);
            using (var context = new SqliteServerDbContext(options))
            {
                Assert.That(context.Set<BlimpufProfile>().Single(p => p.ProfileId == profileId).SecondaryGender,
                    Is.EqualTo(nameof(Gender.Female)));
            }

            await db.SaveCharacterSlotAsync(username, null, 1);
            using (var context = new SqliteServerDbContext(options))
            {
                Assert.That(context.Set<BlimpufProfile>().Any(p => p.ProfileId == profileId), Is.False);
            }
            // Blimpuf end
        }

        [Test]
        public async Task TestDeleteCharacter()
        {
            var pair = Pair;
            var server = pair.Server;
            var db = GetDb(server);
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            await db.InitPrefsAsync(username, new HumanoidCharacterProfile());
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), 1);
            await db.SaveCharacterSlotAsync(username, null, 1);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(!prefs.Characters.Any(p => p.Key != 0));
        }

        // Blimpuf start
        [Test]
        public void TestNoPendingSqliteDatabaseChanges()
        {
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
            builder.UseSqlite("Data Source=:memory:");
            using var context = new SqliteServerDbContext(builder.Options);
            Assert.That(context.Database.HasPendingModelChanges(), Is.False,
                "The database has pending model changes. Add a new migration to apply them. See https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations");
        }

        [Test]
        public void TestNoPendingPostgresDatabaseChanges()
        {
            var builder = new DbContextOptionsBuilder<PostgresServerDbContext>();
            builder.UseNpgsql("Server=localhost");
            using var context = new PostgresServerDbContext(builder.Options);
            Assert.That(context.Database.HasPendingModelChanges(), Is.False,
                "The database has pending model changes. Add a new migration to apply them. See https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations");
        }
        // Blimpuf end

        private static NetUserId NewUserId()
        {
            return new(Guid.NewGuid());
        }
    }
}
