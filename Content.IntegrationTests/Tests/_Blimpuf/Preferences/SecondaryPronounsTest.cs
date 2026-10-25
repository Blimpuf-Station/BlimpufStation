using Content.IntegrationTests.Fixtures;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests._Blimpuf.Preferences;

[TestFixture]
public sealed class SecondaryPronounsTest : GameTest
{
    [Test]
    public async Task ExaminePreservesGrammarAndChoosesEitherPronoun()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var target = Server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            Server.System<SharedHumanoidAppearanceSystem>().LoadProfile(target,
                new HumanoidCharacterProfile().WithAge(18).WithGender(Gender.Female).WithSecondaryGender(Gender.Epicene));
            Server.System<IdentitySystem>().Update(0);
            var identity = Identity.Entity(target, Server.EntMan);

            // Use fixed random seeds to reliably test both pronoun choices.
            foreach (var (seed, prefix) in new[] { (0, "She is"), (1, "They are") })
            {
                Server.ResolveDependency<IRobustRandom>().SetSeed(seed);
                var text = Server.System<ExamineSystemShared>().GetExamineText(target, target, out _).ToString();
                Assert.That(text, Does.Contain($"{prefix} a young human."));
                Assert.That(text, Does.Contain($"{prefix} not holding anything."));
                Assert.That(Server.EntMan.GetComponent<GrammarComponent>(target).Gender, Is.EqualTo(Gender.Female));
                Assert.That(Server.EntMan.GetComponent<GrammarComponent>(identity).Gender, Is.EqualTo(Gender.Female));
            }
            Server.EntMan.DeleteEntity(target);
        });
    }
}
