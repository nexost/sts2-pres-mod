using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Biden.Mechanics;
using PresMod.Characters.Biden.Powers;

namespace PresMod.Characters.Biden.Cards;

/// <summary>Twice his Drowsy to ALL enemies, then he nods off (and his Drowsy still fires as Laser Eyes next turn).</summary>
public sealed class OutCold : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CalculationBaseVar(0m),
		new ExtraDamageVar(2m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => DrowsyCmd.GetDrowsy(card.Owner.Creature))
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { BidenHoverTips.Drowsy, BidenHoverTips.NodOff };

	public OutCold()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (DrowsyCmd.GetDrowsy(Owner.Creature) > 0)
		{
			await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).TargetingAllOpponents(CombatState!)
				.WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
				.Execute(choiceContext);
		}
		await DrowsyCmd.NodOff(choiceContext, Owner.Creature);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}
}
