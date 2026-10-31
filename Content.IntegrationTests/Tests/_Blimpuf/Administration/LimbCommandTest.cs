using System.Linq;
using Content.IntegrationTests.Tests.Toolshed;
using Content.Shared._Starlight.Medical.Body.Part;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Blimpuf.Administration;

[TestFixture]
public sealed class LimbCommandTest : ToolshedTest
{
    [Test]
    public async Task ReplaceArmsGrantsAndRemovesHandActions()
    {
        await Server.WaitAssertion(() =>
        {
            var entities = Server.ResolveDependency<IEntityManager>();
            var maps = entities.System<SharedMapSystem>();
            var containers = entities.System<SharedContainerSystem>();
            maps.CreateMap(out var mapId);
            var human = entities.SpawnEntity("MobHuman", new MapCoordinates(0, 0, mapId));
            Variables["target"] = human;
            var oldArm = GetPart(entities, human, BodyPartType.Arm, BodyPartSymmetry.Right);

            var result = InvokeCommand<EntityUid>("var $target limb:replace \"left arm\" LeftArmCyberScribe limb:replace \"right arm\" RightArmCyberHerakles");
            Assert.That(result, Is.EqualTo(human));
            Assert.That(entities.GetComponent<BodyPartComponent>(oldArm).Body, Is.Null);
            Assert.That(entities.GetComponent<TransformComponent>(oldArm).MapID, Is.EqualTo(mapId));

            var rightHand = GetPart(entities, human, BodyPartType.Hand, BodyPartSymmetry.Right);
            foreach (var side in new[] { BodyPartSymmetry.Left, BodyPartSymmetry.Right })
            {
                var hand = GetPart(entities, human, BodyPartType.Hand, side);
                var actions = containers.GetContainer(hand, ActionsContainerComponent.ContainerId).ContainedEntities;
                Assert.That(actions, Is.Not.Empty);
                foreach (var action in actions)
                {
                    Assert.That(entities.GetComponent<ActionComponent>(action).AttachedEntity, Is.EqualTo(human));
                    Assert.That(entities.GetComponent<ActionComponent>(action).Container, Is.EqualTo(hand));
                    Assert.That(entities.GetComponent<ActionsComponent>(human).Actions, Does.Contain(action));
                }
            }

            Assert.That(entities.System<SharedHandsSystem>().TryGetHand((human, null), "body_part_slot_right hand", out _), Is.True);
            var oldActions = containers.GetContainer(rightHand, ActionsContainerComponent.ContainerId).ContainedEntities.ToArray();
            InvokeCommand<EntityUid>("var $target limb:replace \"right arm\" RightArmCyberScribe");
            Assert.That(entities.GetComponent<BodyPartComponent>(rightHand).Body, Is.Null);
            foreach (var action in oldActions)
            {
                Assert.That(entities.GetComponent<ActionComponent>(action).AttachedEntity, Is.Null);
                Assert.That(entities.GetComponent<ActionsComponent>(human).Actions, Does.Not.Contain(action));
            }

            Assert.That(entities.System<SharedHandsSystem>().TryGetHand((human, null), "body_part_slot_right hand", out _), Is.True);
            maps.DeleteMap(mapId);
        });
    }

    [TestCase("right arm", "LeftArmCyberScribe")]
    [TestCase("right arm", "RightLegCyberSpeed")]
    [TestCase("right arm", "Crowbar")]
    [TestCase("missing slot", "RightArmCyberHerakles")]
    [TestCase("head", "RightArmCyberHerakles")]
    public async Task InvalidReplacementPreservesOriginal(string slot, string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var entities = Server.ResolveDependency<IEntityManager>();
            var maps = entities.System<SharedMapSystem>();
            var bodies = entities.System<SharedBodySystem>();
            maps.CreateMap(out var mapId);
            var human = entities.SpawnEntity("MobHuman", new MapCoordinates(0, 0, mapId));
            Variables["target"] = human;
            var original = bodies.GetBodyChildren(human).Select(part => part.Id).ToArray();

            var result = InvokeCommand<EntityUid>($"var $target limb:replace \"{slot}\" {prototype}");
            Assert.That(result, Is.EqualTo(human));
            Assert.That(bodies.GetBodyChildren(human).Select(part => part.Id), Is.EquivalentTo(original));
            foreach (var part in original)
                Assert.That(entities.GetComponent<BodyPartComponent>(part).Body, Is.EqualTo(human));

            maps.DeleteMap(mapId);
        });
    }

    private static EntityUid GetPart(IEntityManager entities, EntityUid body, BodyPartType type, BodyPartSymmetry side)
    {
        return entities.System<SharedBodySystem>().GetBodyChildrenOfType(body, type)
            .Single(part => part.Component.Symmetry == side).Id;
    }
}
