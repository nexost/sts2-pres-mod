using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Tangent: (1) a small hit and a card, (2) a big hit.</summary>
public sealed class Anyway : TangentCard
{
	public override int LineCount => 2;

	protected override TargetType[] LineTargets => new[] { TargetType.AnyEnemy, TargetType.AnyEnemy };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(5m, ValueProp.Move),
		new CardsVar(1),
		new DamageVar("BigDamage", 12m, ValueProp.Move)
	};

	public Anyway()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task PlayLine(int line, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (EnemyFor(cardPlay) is not Creature enemy)
		{
			return;
		}
		if (line == 1)
		{
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemy)
				.WithHitFx("vfx/vfx_attack_blunt")
				.Execute(choiceContext);
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
			return;
		}
		await DamageCmd.Attack(DynamicVars["BigDamage"].BaseValue).FromCard(this).Targeting(enemy)
			.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars["BigDamage"].UpgradeValueBy(3m);
	}
}
