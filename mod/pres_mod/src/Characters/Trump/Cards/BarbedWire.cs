using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Build, plus a Flame Barrier that also counts Sections when you're hit.</summary>
public sealed class BarbedWire : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(CardVars.Build, 4m),
		new PowerVar<BarbedWirePower>(2m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Section };

	public BarbedWire()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await WallCmd.Build(choiceContext, Owner.Creature, DynamicVars[CardVars.Build].BaseValue, this);
		await PowerCmd.Apply<BarbedWirePower>(choiceContext, Owner.Creature, DynamicVars[nameof(BarbedWirePower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.Build].UpgradeValueBy(2m);
		DynamicVars[nameof(BarbedWirePower)].UpgradeValueBy(1m);
	}
}
