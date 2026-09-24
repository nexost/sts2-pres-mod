using MegaCrit.Sts2.Core.Models.Powers;
using PresMod.Characters.Trump.Powers;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Cards;

/// <summary>Piercing Wail without the Exhaust.</summary>
public sealed class LowEnergy : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar(CardVars.StrengthLoss, 3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<StrengthPower>() };

	public LowEnergy()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		foreach (Creature enemy in CombatState!.HittableEnemies.ToList())
		{
			await PowerCmd.Apply<LowEnergyPower>(choiceContext, enemy, DynamicVars[CardVars.StrengthLoss].BaseValue, Owner.Creature, this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[CardVars.StrengthLoss].UpgradeValueBy(2m);
	}
}
