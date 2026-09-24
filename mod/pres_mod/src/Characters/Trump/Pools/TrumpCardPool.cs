using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.Trump.Cards;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Pools;

public sealed class TrumpCardPool : CardPoolModel
{
	/// <summary>Also the portrait folder: images/packed/card_portraits/trump/&lt;card&gt;.png</summary>
	public override string Title => "trump";

	public override string EnergyColorName => Trump.energyColorName;

	/// <summary>res://materials/cards/frames/card_frame_trump_mat.tres (a hue shift of the shared frame).</summary>
	public override string CardFrameMaterialPath => "card_frame_trump";

	public override Color DeckEntryCardColor => new Color("D9A21B");

	public override Color EnergyOutlineColor => new Color("7A5200");

	public override bool IsColorless => false;

	/// <summary>Every card from characters/trump/design/cards.json except the Tweet token (which only borrows this pool for its frame).</summary>
	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<StrikeTrump>(),
			ModelDb.Card<DefendTrump>(),
			ModelDb.Card<BuildTheWall>(),
			ModelDb.Card<Deport>(),
			ModelDb.Card<BrickToss>(),
			ModelDb.Card<WallSlam>(),
			ModelDb.Card<EscortOut>(),
			ModelDb.Card<ShowThemTheDoor>(),
			ModelDb.Card<SlapATariff>(),
			ModelDb.Card<HostileTakeover>(),
			ModelDb.Card<MeanTweet>(),
			ModelDb.Card<AllCaps>(),
			ModelDb.Card<Sad>(),
			ModelDb.Card<Mulligan>(),
			ModelDb.Card<PowerHandshake>(),
			ModelDb.Card<Bricklayer>(),
			ModelDb.Card<Groundbreaking>(),
			ModelDb.Card<PourConcrete>(),
			ModelDb.Card<BarbedWire>(),
			ModelDb.Card<LateNightPosting>(),
			ModelDb.Card<CampaignDonation>(),
			ModelDb.Card<SmallLoan>(),
			ModelDb.Card<LowEnergy>(),
			ModelDb.Card<AlternativeFacts>(),
			ModelDb.Card<BorderPatrol>(),
			ModelDb.Card<OneWayTicket>(),
			ModelDb.Card<Demolition>(),
			ModelDb.Card<TradeWar>(),
			ModelDb.Card<NetWorth>(),
			ModelDb.Card<GoingViral>(),
			ModelDb.Card<FireAndFury>(),
			ModelDb.Card<Bigly>(),
			ModelDb.Card<WitchHunt>(),
			ModelDb.Card<SecurityDetail>(),
			ModelDb.Card<RallySpeech>(),
			ModelDb.Card<TenFeetHigher>(),
			ModelDb.Card<HoldTheLine>(),
			ModelDb.Card<BackgroundCheck>(),
			ModelDb.Card<HireContractors>(),
			ModelDb.Card<Bribe>(),
			ModelDb.Card<WallStreet>(),
			ModelDb.Card<Casino>(),
			ModelDb.Card<Tweetstorm>(),
			ModelDb.Card<Blocked>(),
			ModelDb.Card<Doomscrolling>(),
			ModelDb.Card<Covfefe>(),
			ModelDb.Card<TwoScoops>(),
			ModelDb.Card<Sharpie>(),
			ModelDb.Card<ExecutiveTime>(),
			ModelDb.Card<FakeNews>(),
			ModelDb.Card<TrickleDown>(),
			ModelDb.Card<Rebar>(),
			ModelDb.Card<GuardTowers>(),
			ModelDb.Card<BorderControl>(),
			ModelDb.Card<BorderWall>(),
			ModelDb.Card<Protectionism>(),
			ModelDb.Card<MerchStand>(),
			ModelDb.Card<SocialMediaIntern>(),
			ModelDb.Card<Ratings>(),
			ModelDb.Card<ExecutiveOrder>(),
			ModelDb.Card<WreckingBall>(),
			ModelDb.Card<Cornerstone>(),
			ModelDb.Card<MassDeportation>(),
			ModelDb.Card<TariffMan>(),
			ModelDb.Card<LeveragedBuyout>(),
			ModelDb.Card<MakeItRain>(),
			ModelDb.Card<Trending>(),
			ModelDb.Card<HoleInOne>(),
			ModelDb.Card<YoureFired>(),
			ModelDb.Card<GreatWall>(),
			ModelDb.Card<Chapter11>(),
			ModelDb.Card<LawyerUp>(),
			ModelDb.Card<GolfWeekend>(),
			ModelDb.Card<BurnerAccounts>(),
			ModelDb.Card<VeryStableGenius>(),
			ModelDb.Card<CoalitionWall>(),
			ModelDb.Card<NationalEmergency>(),
			ModelDb.Card<TheyllPayForIt>(),
			ModelDb.Card<InfrastructureWeek>(),
			ModelDb.Card<ReinforcedConcrete>(),
			ModelDb.Card<LawAndOrder>(),
			ModelDb.Card<ArtOfTheDeal>(),
			ModelDb.Card<GoldTower>(),
			ModelDb.Card<SoMuchWinning>(),
			ModelDb.Card<VerifiedAccount>(),
			ModelDb.Card<ThreeAmPosting>(),
			ModelDb.Card<GoldenEscalator>(),
			ModelDb.Card<MakeTheSpireGreatAgain>()
		};
	}
}
