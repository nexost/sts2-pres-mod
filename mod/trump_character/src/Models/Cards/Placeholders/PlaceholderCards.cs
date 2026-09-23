using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TrumpMod.Models.Cards.Placeholders;

/// <summary>
/// Step 2 filler so card rewards, shops and boss rewards have something to offer at every rarity and type
/// (the game needs 2 attacks + 2 skills + 1 power for the shop and 3 rares for boss rewards).
/// Attack: deal N. Skill: gain N block. Power: gain N Strength. All replaced by the real card set in Step 5.
/// </summary>
public abstract class PlaceholderCard : CardModel
{
	private readonly decimal _amount;

	protected PlaceholderCard(int cost, CardType type, CardRarity rarity, decimal amount)
		: base(cost, type, rarity, type == CardType.Attack ? TargetType.AnyEnemy : TargetType.Self)
	{
		_amount = amount;
	}

	public override bool GainsBlock => Type == CardType.Skill;

	protected override IEnumerable<DynamicVar> CanonicalVars => Type switch
	{
		CardType.Attack => new DynamicVar[] { new DamageVar(_amount, ValueProp.Move) },
		CardType.Skill => new DynamicVar[] { new BlockVar(_amount, ValueProp.Move) },
		_ => new DynamicVar[] { new PowerVar<StrengthPower>(_amount) }
	};

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		switch (Type)
		{
			case CardType.Attack:
				ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
				await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_attack_blunt")
					.Execute(choiceContext);
				break;
			case CardType.Skill:
				await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
				break;
			default:
				await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
				await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars[nameof(StrengthPower)].BaseValue, Owner.Creature, this);
				break;
		}
	}

	protected override void OnUpgrade()
	{
		switch (Type)
		{
			case CardType.Attack:
				DynamicVars.Damage.UpgradeValueBy(3m);
				break;
			case CardType.Skill:
				DynamicVars.Block.UpgradeValueBy(3m);
				break;
			default:
				DynamicVars[nameof(StrengthPower)].UpgradeValueBy(1m);
				break;
		}
	}
}

public sealed class PlaceholderCommonAttackA() : PlaceholderCard(1, CardType.Attack, CardRarity.Common, 8m);
public sealed class PlaceholderCommonAttackB() : PlaceholderCard(2, CardType.Attack, CardRarity.Common, 13m);
public sealed class PlaceholderCommonSkillA() : PlaceholderCard(1, CardType.Skill, CardRarity.Common, 7m);
public sealed class PlaceholderCommonSkillB() : PlaceholderCard(2, CardType.Skill, CardRarity.Common, 12m);
public sealed class PlaceholderCommonPower() : PlaceholderCard(1, CardType.Power, CardRarity.Common, 1m);
// Room Full of Cheese ("Gorge") offers 8 commons at once, so the pool needs at least 8.
public sealed class PlaceholderCommonAttackC() : PlaceholderCard(0, CardType.Attack, CardRarity.Common, 4m);
public sealed class PlaceholderCommonSkillC() : PlaceholderCard(0, CardType.Skill, CardRarity.Common, 4m);
public sealed class PlaceholderCommonAttackD() : PlaceholderCard(1, CardType.Attack, CardRarity.Common, 9m);

public sealed class PlaceholderUncommonAttackA() : PlaceholderCard(1, CardType.Attack, CardRarity.Uncommon, 10m);
public sealed class PlaceholderUncommonAttackB() : PlaceholderCard(2, CardType.Attack, CardRarity.Uncommon, 16m);
public sealed class PlaceholderUncommonSkillA() : PlaceholderCard(1, CardType.Skill, CardRarity.Uncommon, 9m);
public sealed class PlaceholderUncommonSkillB() : PlaceholderCard(2, CardType.Skill, CardRarity.Uncommon, 15m);
public sealed class PlaceholderUncommonPower() : PlaceholderCard(2, CardType.Power, CardRarity.Uncommon, 2m);

public sealed class PlaceholderRareAttackA() : PlaceholderCard(1, CardType.Attack, CardRarity.Rare, 12m);
public sealed class PlaceholderRareAttackB() : PlaceholderCard(3, CardType.Attack, CardRarity.Rare, 28m);
public sealed class PlaceholderRareSkillA() : PlaceholderCard(1, CardType.Skill, CardRarity.Rare, 11m);
public sealed class PlaceholderRareSkillB() : PlaceholderCard(3, CardType.Skill, CardRarity.Rare, 24m);
public sealed class PlaceholderRarePower() : PlaceholderCard(3, CardType.Power, CardRarity.Rare, 3m);
