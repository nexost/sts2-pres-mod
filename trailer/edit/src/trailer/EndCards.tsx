// The title (over the painted walk shot) and the final joke (the Tweet, then Joe cut off mid-word).
import React from 'react';
import {AbsoluteFill, Easing, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {FONTS} from '../fonts';
import {BUTTON, clamp01} from './data';
import {GOLD, foil} from './Overlays';

const Star: React.FC<{size: number; spin: number}> = ({size, spin}) => (
	<svg width={size} height={size} viewBox="0 0 24 24" style={{display: 'block', transform: `rotate(${spin}deg)`}}>
		<path d="M12 1.8l3.1 6.6 7.2.9-5.3 5 1.4 7.1L12 18l-6.4 3.4 1.4-7.1-5.3-5 7.2-.9z" fill="#f3c65a" stroke="rgba(60,30,5,0.9)" strokeWidth="0.6" />
	</svg>
);

export const TitleCard: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps, width, height} = useVideoConfig();
	const slam = spring({frame, fps, config: {damping: 14, stiffness: 160, mass: 0.9}});
	const sub = spring({frame: frame - 0.35 * fps, fps, config: {damping: 18, stiffness: 160}});
	const rule = interpolate(frame, [0.2 * fps, 0.7 * fps], [0, 1], {...clamp01, easing: Easing.out(Easing.cubic)});
	const cta = interpolate(frame, [0.9 * fps, 1.2 * fps], [0, 1], clamp01);
	const small = interpolate(frame, [1.3 * fps, 1.6 * fps], [0, 1], clamp01);
	const shine = interpolate(frame, [0.15 * fps, 1.0 * fps], [-30, 130], clamp01);
	const main = width * 0.09;
	return (
		<AbsoluteFill>
			<AbsoluteFill style={{background: 'linear-gradient(180deg, rgba(0,0,0,0.2) 0%, rgba(0,0,0,0) 28%, rgba(0,0,0,0) 44%, rgba(0,0,0,0.7) 68%, rgba(0,0,0,0.93) 100%)'}} />
			<AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-end', paddingBottom: height * 0.075}}>
				<div
					style={{
						fontFamily: FONTS.spectral.family,
						...foil(main),
						backgroundImage: `linear-gradient(105deg, rgba(255,255,255,0) ${shine - 12}%, rgba(255,255,255,0.95) ${shine}%, rgba(255,255,255,0) ${shine + 12}%), ${GOLD}`,
						letterSpacing: main * 0.035,
						transform: `scale(${interpolate(slam, [0, 1], [1.35, 1])})`,
						opacity: Math.min(1, slam * 1.6),
						filter: `drop-shadow(0 ${main * 0.045}px 0 rgba(40,18,0,0.9)) drop-shadow(0 0 ${main * 0.4}px rgba(255,190,70,0.45))`,
					}}
				>
					PRESIDENTS
				</div>
				<div style={{display: 'flex', alignItems: 'center', gap: width * 0.013, marginTop: height * 0.012, opacity: Math.min(1, sub * 1.5), transform: `translateY(${(1 - sub) * 30}px)`}}>
					<div style={{width: width * 0.094 * rule, height: 4, background: 'linear-gradient(90deg, rgba(243,198,90,0), #f3c65a)'}} />
					<Star size={width * 0.021} spin={(1 - sub) * -180} />
					<div style={{fontFamily: FONTS.spectral.family, ...foil(width * 0.0375), letterSpacing: width * 0.007, filter: 'drop-shadow(0 4px 0 rgba(40,18,0,0.9))'}}>OF THE SPIRE</div>
					<Star size={width * 0.021} spin={(1 - sub) * 180} />
					<div style={{width: width * 0.094 * rule, height: 4, background: 'linear-gradient(90deg, #f3c65a, rgba(243,198,90,0))'}} />
				</div>
				<div style={{marginTop: height * 0.03, fontFamily: FONTS.kreon.family, fontSize: width * 0.017, letterSpacing: width * 0.004, color: '#f4ecd8', textShadow: '0 3px 12px rgba(0,0,0,0.9)', opacity: cta}}>
					A FREE MOD FOR SLAY THE SPIRE 2
				</div>
				<div style={{marginTop: height * 0.012, fontFamily: FONTS.kreon.family, fontSize: width * 0.0125, letterSpacing: width * 0.002, color: '#e6b84a', textShadow: '0 3px 12px rgba(0,0,0,0.9)', opacity: cta}}>
					Free on GitHub · github.com/nexost/sts2-pres-mod · link in the description
				</div>
			</AbsoluteFill>
			<div style={{position: 'absolute', bottom: height * 0.018, width: '100%', textAlign: 'center', fontFamily: FONTS.kreon.family, fontSize: width * 0.0075, color: '#9c9583', opacity: small}}>
				A fan-made parody mod. Not affiliated with Mega Crit.
			</div>
		</AbsoluteFill>
	);
};

// ---- The button -------------------------------------------------------------------------------------------------
const Bubble: React.FC<{who: 'trump' | 'biden'; name: string; text: string; shown: number; appear: number; typing?: boolean}> = ({who, name, text, shown, appear, typing}) => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const pop = spring({frame: frame - appear, fps, config: {damping: 14, stiffness: 260, mass: 0.6}});
	if (frame < appear) return null;
	const left = who === 'trump';
	const dots = typing ? '•••'.slice(0, 1 + (Math.floor(frame / 6) % 3)) : '';
	return (
		<div style={{display: 'flex', flexDirection: left ? 'row' : 'row-reverse', alignItems: 'flex-end', gap: width * 0.012, alignSelf: left ? 'flex-start' : 'flex-end', transform: `scale(${interpolate(pop, [0, 1], [0.7, 1])})`, transformOrigin: left ? 'left bottom' : 'right bottom', opacity: pop}}>
			<Img src={staticFile(`ui/portrait_${who}.png`)} style={{height: width * 0.075, borderRadius: 8, border: `3px solid ${left ? '#e6b84a' : '#8fa2e8'}`}} />
			<div style={{maxWidth: width * 0.56}}>
				<div style={{fontFamily: FONTS.kreon.family, fontSize: width * 0.015, color: left ? '#e6b84a' : '#9fb0f0', marginBottom: 8, textAlign: left ? 'left' : 'right'}}>{name}</div>
				<div
					style={{
						fontFamily: left ? FONTS.fira.family : FONTS.kreon.family,
						fontSize: width * (left ? 0.037 : 0.031),
						letterSpacing: left ? width * 0.0008 : 0,
						color: left ? '#ffffff' : '#e9eef2',
						background: left ? '#1d5f99' : '#35414f',
						padding: `${width * 0.01}px ${width * 0.016}px`,
						borderRadius: width * 0.018,
						[left ? 'borderBottomLeftRadius' : 'borderBottomRightRadius']: 6,
						boxShadow: '0 10px 30px rgba(0,0,0,0.6)',
						minHeight: width * 0.02,
					}}
				>
					{typing ? dots : text.slice(0, shown)}
				</div>
			</div>
		</div>
	);
};

export const ButtonCard: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const tweet = 'BEST MOD EVER. MANY PEOPLE ARE SAYING.';
	const joe = "Here's the deal, folks. Back in—";
	const tShown = Math.floor(interpolate(frame, [BUTTON.tweetFrom * fps, BUTTON.tweetTo * fps], [0, tweet.length], clamp01));
	const jShown = Math.floor(interpolate(frame, [BUTTON.joeFrom * fps, BUTTON.joeTo * fps], [0, joe.length], clamp01));
	const joeTyping = frame >= (BUTTON.joeFrom - 0.45) * fps && frame < BUTTON.joeFrom * fps;
	return (
		<AbsoluteFill style={{backgroundColor: '#050505', justifyContent: 'center', padding: `0 ${width * 0.09}px`, gap: width * 0.03, flexDirection: 'column'}}>
			<Bubble who="trump" name="The Donald" text={tweet} shown={tShown} appear={Math.round(BUTTON.tweetFrom * fps) - 4} />
			<Bubble who="biden" name="Sleepy Joe" text={joe} shown={jShown} appear={Math.round((BUTTON.joeFrom - 0.45) * fps)} typing={joeTyping} />
		</AbsoluteFill>
	);
};
