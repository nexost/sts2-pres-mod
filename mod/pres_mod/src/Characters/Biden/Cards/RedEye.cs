using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Pommel Strike that only draws as Dark Brandon.</summary>
public sealed class RedEye : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(8m, ValueProp.Move), new CardsVar(1) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.DarkBrandon };

	protected override bool ShouldGlowGoldInternal => this.IsAwake();

	public RedEye()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
		if (this.IsAwake())
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
