using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Relics;

/// <summary>
/// The starter and its Ancient version. They carry Sleepy Joe's own sleepiness, the way Bound Phylactery carries Osty:
/// at the end of each Sleepy Joe turn, Doze 3 (not while he's Dark Brandon or already asleep), and when he nods off,
/// gain Block ("nobody can tell whether his eyes are open").
/// </summary>
public abstract class AviatorsRelic : RelicModel, IAfterNodOff
{
	protected abstract decimal NapBlock { get; }

	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar("Doze", 3m),
		new BlockVar(NapBlock, ValueProp.Unpowered)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
	{
		BidenHoverTips.Doze, BidenHoverTips.Drowsy, BidenHoverTips.NodOff, HoverTipFactory.Static(StaticHoverTip.Block)
	};

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		Creature joe = Owner.Creature;
		if (side != CombatSide.Player || !participants.Contains(joe) || DrowsyCmd.IsDarkBrandon(joe) || DrowsyCmd.HasNoddedOff(joe))
		{
			return;
		}
		Flash();
		await DrowsyCmd.Doze(choiceContext, joe, DynamicVars["Doze"].BaseValue);
	}

	public async Task AfterNodOff(PlayerChoiceContext choiceContext, Creature owner)
	{
		if (owner != Owner.Creature)
		{
			return;
		}
		Flash();
		await CreatureCmd.GainBlock(owner, DynamicVars.Block, null);
	}
}
