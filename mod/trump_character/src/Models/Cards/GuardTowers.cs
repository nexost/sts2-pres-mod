using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>The Wall deck's main damage engine; copies stack.</summary>
public sealed class GuardTowers : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<GuardTowersPower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Section };

	public GuardTowers()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<GuardTowersPower>(choiceContext, Owner.Creature, DynamicVars[nameof(GuardTowersPower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(GuardTowersPower)].UpgradeValueBy(1m);
	}
}
