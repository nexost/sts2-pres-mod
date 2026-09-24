using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Vulnerable on one enemy; as Dark Brandon, on all of them.</summary>
public sealed class StareDown : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VulnerablePower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<VulnerablePower>(), BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public StareDown()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		decimal amount = DynamicVars[nameof(VulnerablePower)].BaseValue;
		if (this.IsAwake())
		{
			await PowerCmd.Apply<VulnerablePower>(choiceContext, CombatState!.HittableEnemies, amount, Owner.Creature, this);
			return;
		}
		await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, amount, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);
	}
}
