namespace TrumpMod.Models.Cards;

/// <summary>Comes back from the Discard Pile whenever you play a Tweet (Right Hand Hand's pattern).</summary>
public sealed class Trending : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(3m, ValueProp.Move) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public Trending()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_attack_lightning")
			.Execute(choiceContext);
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card is Tweet && cardPlay.Card.Owner == Owner && Pile?.Type == PileType.Discard)
		{
			await CardPileCmd.Add(this, PileType.Hand);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}
