using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

public sealed class SoMuchWinning : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<SoMuchWinningPower>(1m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Deport, HoverTipFactory.ForEnergy(this) };

	public SoMuchWinning()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<SoMuchWinningPower>(choiceContext, Owner.Creature, DynamicVars[nameof(SoMuchWinningPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
