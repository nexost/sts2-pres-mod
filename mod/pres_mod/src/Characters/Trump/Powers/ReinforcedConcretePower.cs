using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Powers;

/// <summary>Reinforced Concrete: each Section gives Amount more Block. Copies stack, so the Wall's defense has no ceiling.</summary>
public sealed class ReinforcedConcretePower : PowerModel, ISectionBlockModifier
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public decimal ModifySectionBlock(Creature owner, decimal blockPerSection)
	{
		return owner == Owner ? blockPerSection + Amount : blockPerSection;
	}
}
