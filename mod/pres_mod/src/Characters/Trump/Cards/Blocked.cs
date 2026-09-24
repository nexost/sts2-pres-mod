using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

public sealed class Blocked : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<BlockedPower>(4m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Tweet };

	public Blocked()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<BlockedPower>(choiceContext, Owner.Creature, DynamicVars[nameof(BlockedPower)].BaseValue, Owner.Creature, this);
		await Tweet.CreateInHand(Owner, 1, CombatState!);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(BlockedPower)].UpgradeValueBy(2m);
	}
}
