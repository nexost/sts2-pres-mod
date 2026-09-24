using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TrumpMod.Models.CardPools;

namespace TrumpMod.Models.Cards;

/// <summary>
/// Token: 0-cost, 3 (5) damage to ALL enemies, Exhaust. Not in any reward pool; it only claims the Donald pool
/// so it gets his frame and portrait folder.
/// </summary>
public sealed class Tweet : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(3m, ValueProp.Move) };

	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	public override CardPoolModel Pool => ModelDb.CardPool<TrumpCardPool>();

	public Tweet()
		: base(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
			.WithHitFx("vfx/vfx_attack_lightning")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}

	/// <summary>Tweets this player has played this combat (the history is cleared when a combat ends).</summary>
	public static int PlayedThisCombat(Player player)
	{
		return CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => e.CardPlay.Card is Tweet && e.CardPlay.Card.Owner == player);
	}

	public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState, bool upgraded = false)
	{
		if (count <= 0 || CombatManager.Instance.IsOverOrEnding)
		{
			return Enumerable.Empty<CardModel>();
		}
		var tweets = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			CardModel tweet = combatState.CreateCard<Tweet>(owner);
			if (upgraded)
			{
				CardCmd.Upgrade(tweet);
			}
			tweets.Add(tweet);
		}
		await CardPileCmd.AddGeneratedCardsToCombat(tweets, PileType.Hand, owner);
		return tweets;
	}
}
