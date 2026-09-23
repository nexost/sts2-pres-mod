using MegaCrit.Sts2.Core.Models.Powers;

namespace TrumpMod.Models.Cards;

public sealed class PowerHandshake : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(7m, ValueProp.Move),
		new PowerVar<VulnerablePower>(1m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<VulnerablePower>() };

	public PowerHandshake()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);
		await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars[nameof(VulnerablePower)].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1m);
	}
}
