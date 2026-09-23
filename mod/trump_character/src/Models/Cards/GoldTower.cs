using TrumpMod.Models.Powers;

namespace TrumpMod.Models.Cards;

/// <summary>1 Block per 15 Gold (10 upgraded), paid as Gold Tower Amount per 30 Gold so copies simply add up.</summary>
public sealed class GoldTower : CardModel
{
	private const string _goldPerBlockVar = "GoldPerBlock";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(_goldPerBlockVar, 15m) };

	public GoldTower()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		decimal perStep = GoldTowerPower.GoldPerStep / DynamicVars[_goldPerBlockVar].BaseValue;
		await PowerCmd.Apply<GoldTowerPower>(choiceContext, Owner.Creature, perStep, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[_goldPerBlockVar].UpgradeValueBy(-5m);
	}
}
