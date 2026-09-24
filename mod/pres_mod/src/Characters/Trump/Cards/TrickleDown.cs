using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Co-op only: every player gains Gold.</summary>
public sealed class TrickleDown : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new GoldVar(12) };

	public TrickleDown()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (Player player in CombatState!.Players)
		{
			await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, player);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Gold.UpgradeValueBy(6m);
	}
}
