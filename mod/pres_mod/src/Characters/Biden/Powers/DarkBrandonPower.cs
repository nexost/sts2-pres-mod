using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Framework.Nodes;

namespace PresMod.Characters.Biden.Powers;

/// <summary>
/// Awake: Dark Brandon for the rest of this turn (DrowsyCmd.WakeUp). Tangent cards do all their lines, Drowsy can't build,
/// and "As Dark Brandon" bonuses apply. At the end of the turn he falls back asleep (Sleepy Joe).
/// On screen his combat body switches to the dark_ pose set (red-tinted until it has its own paintings).
/// </summary>
public sealed class DarkBrandonPower : PowerModel
{
	/// <summary>Figure colour while Dark Brandon has no paintings of his own yet.</summary>
	private static readonly Color Tint = new Color(1f, 0.62f, 0.6f);

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Tangent };

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		ShowForm(Owner, dark: true);
		return Task.CompletedTask;
	}

	public override Task AfterRemoved(Creature oldOwner)
	{
		ShowForm(oldOwner, dark: false);
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(Owner))
		{
			await DrowsyCmd.FallBackAsleep(Owner);
		}
	}

	private static void ShowForm(Creature creature, bool dark)
	{
		NCharacterPoses? poses = NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals?.GetNodeOrNull<NCharacterPoses>(NCharacterPoses.NodeName);
		poses?.SetVariant(dark ? "dark_" : null, Tint);
	}
}
