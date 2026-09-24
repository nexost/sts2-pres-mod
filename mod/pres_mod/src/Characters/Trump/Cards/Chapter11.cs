using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>The panic button: all your Gold becomes Block (300 Gold = 75 Block, 100 upgraded).</summary>
public sealed class Chapter11 : CardModel
{
	private const string _goldPerBlockVar = "GoldPerBlock";

	public override bool GainsBlock => true;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar(_goldPerBlockVar, 4m),
		new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
		new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => card.Owner.Gold / card.DynamicVars[_goldPerBlockVar].IntValue)
	};

	public Chapter11()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal block = DynamicVars.CalculatedBlock.Calculate(null);
		int gold = Owner.Gold;
		if (gold > 0)
		{
			await PlayerCmd.LoseGold(gold, Owner);
		}
		await CreatureCmd.GainBlock(Owner.Creature, block, DynamicVars.CalculatedBlock.Props, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[_goldPerBlockVar].UpgradeValueBy(-1m);
	}
}
