using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>Bridges the Wall and Deport; neither needs the other.</summary>
public sealed class BorderWall : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<BorderWallPower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport, TrumpHoverTips.Section };

	public BorderWall()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<BorderWallPower>(choiceContext, Owner.Creature, DynamicVars[nameof(BorderWallPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BorderWallPower)].UpgradeValueBy(1m);
	}
}
