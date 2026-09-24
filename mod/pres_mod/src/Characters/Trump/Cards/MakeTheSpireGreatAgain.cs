using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Ancient: every play style at once, every turn.</summary>
public sealed class MakeTheSpireGreatAgain : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(CardVars.Build, MakeTheSpireGreatAgainPower.BuildPerStack),
		new GoldVar(MakeTheSpireGreatAgainPower.GoldPerStack)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Tweet };

	public MakeTheSpireGreatAgain()
		: base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<MakeTheSpireGreatAgainPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
