namespace TrumpMod.Models.Cards;

/// <summary>Co-op only: every player Builds (their Wall shows up next to them and gives Section Block like The Donald's).</summary>
public sealed class CoalitionWall : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.Build, 12m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public CoalitionWall()
		: base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (Player player in CombatState!.Players.Where(p => p.Creature.IsAlive))
		{
			await WallCmd.Build(choiceContext, player.Creature, DynamicVars[CardVars.Build].BaseValue, this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.Build].UpgradeValueBy(4m);
	}
}
