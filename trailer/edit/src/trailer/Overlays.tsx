// Text and card overlays: name cards, the Wall's stage stamps, card callouts, kinetic lines and the aviator glint.
// Every component runs in its own Sequence, so frame 0 is its "at".
import React from 'react';
import {AbsoluteFill, Easing, Img, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';
import {FONTS} from '../fonts';
import {Graphic, cardUrl, clamp01} from './data';

export const GOLD = 'linear-gradient(180deg, #fff7d6 0%, #ffe08a 22%, #f0b83c 48%, #b9791b 72%, #ffe9a8 100%)';
const SILVER = 'linear-gradient(180deg, #ffffff 0%, #dfe6ff 35%, #9aa8dc 65%, #e9eeff 100%)';

export const foil = (size: number, gradient = GOLD, stroke = 'rgba(60,30,5,0.9)'): React.CSSProperties => ({
	fontSize: size,
	lineHeight: 0.95,
	backgroundImage: gradient,
	WebkitBackgroundClip: 'text',
	backgroundClip: 'text',
	color: 'transparent',
	WebkitTextStroke: `${Math.max(2, size * 0.012)}px ${stroke}`,
});

/** 0 -> 1 over the first frames, 1 -> 0 over the last ones. */
const inOut = (frame: number, frames: number, inF: number, outF: number) =>
	Math.min(interpolate(frame, [0, inF], [0, 1], clamp01), interpolate(frame, [frames - outF, frames], [1, 0], clamp01));

// ---- Name cards ------------------------------------------------------------------------------------------------
export const NameCard: React.FC<{g: Graphic}> = ({g}) => {
	const frame = useCurrentFrame();
	const {fps, width, height} = useVideoConfig();
	const frames = Math.round((g.dur ?? 2) * fps);
	const slam = spring({frame: frame - 2, fps, config: {damping: 13, stiffness: 210, mass: 0.8}});
	const vis = inOut(frame, frames, 3, 7);
	const eyebrowIn = interpolate(frame, [0, 8], [0, 1], clamp01);
	const style = g.style as 'donald' | 'joe' | 'brandon';
	const eyebrow = {donald: 'CANDIDATE No. 1', joe: 'CANDIDATE No. 2', brandon: 'CANDIDATE No. 2  (AWAKE)'}[style];
	const eyebrowStyle: React.CSSProperties = {
		fontFamily: FONTS.kreon.family,
		fontSize: width * 0.0135,
		letterSpacing: width * 0.0045,
		color: style === 'brandon' ? '#ff8a80' : '#f4ecd8',
		textShadow: '0 2px 10px rgba(0,0,0,0.9)',
		opacity: eyebrowIn,
		transform: `translateX(${(1 - eyebrowIn) * -30}px)`,
		marginBottom: width * 0.004,
	};
	if (style === 'brandon') {
		// Red neon, flickering on like a tube, with an RGB split.
		const flicker = [1, 0.2, 1, 0.4, 1, 1, 0.7, 1][Math.min(7, frame)] ?? 1;
		const size = width * 0.092;
		return (
			<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'center', paddingBottom: height * 0.09, opacity: vis}}>
				<div style={{...eyebrowStyle, textAlign: 'center'}}>{eyebrow}</div>
				<div
					style={{
						fontFamily: FONTS.fira.family,
						fontSize: size,
						letterSpacing: size * 0.06,
						// A near-white core in a red glow reads as a lit tube even over the red painting.
						color: '#fff1ee',
						opacity: flicker,
						transform: `scale(${interpolate(slam, [0, 1], [1.25, 1])})`,
						textShadow: `-${size * 0.045}px 0 0 rgba(0,255,255,0.4), ${size * 0.045}px 0 0 rgba(255,0,60,0.7), 0 0 ${size * 0.08}px #ff2a2a, 0 0 ${size * 0.25}px rgba(255,20,20,0.95), 0 0 ${size * 0.8}px rgba(255,0,0,0.65)`,
					}}
				>
					{g.text}
				</div>
			</AbsoluteFill>
		);
	}
	const size = width * 0.078;
	const isJoe = style === 'joe';
	const letters = (g.text as string).split('');
	const face = foil(size, isJoe ? SILVER : GOLD, isJoe ? 'rgba(15,20,60,0.95)' : 'rgba(60,30,5,0.9)');
	return (
		<AbsoluteFill style={{padding: `${height * (isJoe ? 0.14 : 0.36)}px ${width * 0.06}px`, opacity: vis}}>
			<div style={eyebrowStyle}>{eyebrow}</div>
			<div
				style={{
					fontFamily: FONTS.spectral.family,
					...(isJoe ? {fontSize: size, lineHeight: 0.95} : face),
					letterSpacing: size * 0.05,
					transformOrigin: 'left center',
					transform: `scale(${interpolate(slam, [0, 1], [1.4, 1])})`,
					filter: `drop-shadow(0 ${size * 0.05}px 0 ${isJoe ? 'rgba(10,14,40,0.95)' : 'rgba(40,18,0,0.9)'}) drop-shadow(0 0 ${size * 0.3}px ${isJoe ? 'rgba(120,140,255,0.45)' : 'rgba(255,190,70,0.45)'})`,
				}}
			>
				{isJoe
					? letters.map((c, i) => {
							// The letters nod off one by one (each letter carries its own foil: a transformed box can't share its parent's).
							const d = Math.max(0, frame - fps * 0.9 - i * 2.5);
							if (c === ' ') return <span key={i} style={{display: 'inline-block', width: size * 0.32}} />;
							return (
								<span key={i} style={{...face, display: 'inline-block', transform: `translateY(${Math.min(d * 1.1, size * 0.22) * ((i % 3) * 0.4 + 0.6)}px) rotate(${Math.min(d * 0.35, 9) * (i % 2 ? 1 : -1)}deg)`}}>
									{c}
								</span>
							);
						})
					: g.text}
			</div>
			<div style={{width: interpolate(frame, [4, 16], [0, width * 0.26], {...clamp01, easing: Easing.out(Easing.cubic)}), height: 4, marginTop: width * 0.006, background: `linear-gradient(90deg, ${isJoe ? '#aeb8ff' : '#f3c65a'}, rgba(0,0,0,0))`}} />
		</AbsoluteFill>
	);
};

// ---- The Wall's stages: each new stamp slams in, the earlier ones shrink above it, struck through ----------------
const STAGE_FILL = ['#c9d2da', '#e27a5b', '#dcd6c8'];
export const WallStamps: React.FC<{stamps: Graphic[]; t0: number}> = ({stamps, t0}) => {
	const frame = useCurrentFrame();
	const {fps, width, height} = useVideoConfig();
	const t = t0 + frame / fps;
	const shown = stamps.filter((s) => t >= s.at);
	const last = stamps[stamps.length - 1];
	const end = last.at + (last.dur ?? 1);
	const out = interpolate(t, [end - 0.12, end], [1, 0], clamp01);
	return (
		<AbsoluteFill style={{padding: `${height * 0.1}px ${width * 0.05}px`, opacity: out}}>
			{shown.map((s, i) => {
				const current = i === shown.length - 1;
				const f = (t - s.at) * fps;
				const slam = spring({frame: f, fps, config: {damping: 11, stiffness: 260, mass: 0.6}});
				const gold = s.level === 3;
				const size = width * (current ? (gold ? 0.07 : 0.05) : 0.024);
				return (
					<div key={s.text} style={{position: 'relative', alignSelf: 'flex-start', marginBottom: width * 0.004, transformOrigin: 'left center', transform: `rotate(${current ? -3 : -1}deg) scale(${current ? interpolate(slam, [0, 1], [1.9, 1]) : 1})`, opacity: current ? 1 : 0.75}}>
						<div
							style={{
								fontFamily: FONTS.kreon.family,
								...(gold ? foil(size) : {fontSize: size, color: STAGE_FILL[s.level], WebkitTextStroke: `${Math.max(2, size * 0.03)}px rgba(15,10,5,0.95)`}),
								letterSpacing: size * 0.04,
								filter: `drop-shadow(0 ${size * 0.06}px 0 rgba(0,0,0,0.85))${gold ? ' drop-shadow(0 0 40px rgba(255,190,70,0.7))' : ''}`,
							}}
						>
							{s.text}
						</div>
						{!current && <div style={{position: 'absolute', left: -6, right: -6, top: '52%', height: Math.max(4, size * 0.12), background: '#d42a2a', transform: 'rotate(-2deg)', boxShadow: '0 2px 4px rgba(0,0,0,0.6)'}} />}
					</div>
				);
			})}
		</AbsoluteFill>
	);
};

// ---- Card callouts: the real card slides in beside the action --------------------------------------------------
export const Callout: React.FC<{g: Graphic}> = ({g}) => {
	const frame = useCurrentFrame();
	const {fps, width, height} = useVideoConfig();
	const frames = Math.round((g.dur ?? 1.4) * fps);
	const inS = spring({frame, fps, config: {damping: 15, stiffness: 170, mass: 0.9}});
	const outP = interpolate(frame, [frames - 9, frames], [0, 1], {...clamp01, easing: Easing.in(Easing.cubic)});
	const side = g.side === 'left' ? -1 : 1;
	const h = height * 0.64;
	const glow = (g.card as string).startsWith('trump') ? 'rgba(255, 196, 80, 0.55)' : 'rgba(110, 150, 255, 0.55)';
	const x = (1 - inS) * width * 0.45 * side + outP * width * 0.5 * side;
	const bob = Math.sin(frame / 14) * 6;
	const valign = g.valign === 'bottom' ? 'flex-end' : 'center';
	return (
		<AbsoluteFill style={{justifyContent: valign, alignItems: side > 0 ? 'flex-end' : 'flex-start', padding: `${height * 0.04}px ${width * 0.05}px`}}>
			<Img
				src={cardUrl(g.card)}
				style={{
					height: h,
					transform: `translate(${x}px, ${bob}px) rotate(${side * interpolate(inS, [0, 1], [16, 3])}deg)`,
					filter: `drop-shadow(0 18px 30px rgba(0,0,0,0.75)) drop-shadow(0 0 40px ${glow})`,
				}}
			/>
		</AbsoluteFill>
	);
};

// ---- Kinetic lines ("TWO PRESIDENTS.") -------------------------------------------------------------------------
export const Kinetic: React.FC<{g: Graphic}> = ({g}) => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const frames = Math.round((g.dur ?? 1.2) * fps);
	const slam = spring({frame, fps, config: {damping: 12, stiffness: 300, mass: 0.6}});
	const size = width * 0.085;
	const spread = interpolate(frame, [0, frames], [0.03, 0.07]);
	return (
		<AbsoluteFill style={{justifyContent: 'flex-start', alignItems: 'center', paddingTop: width * 0.06, background: `linear-gradient(180deg, rgba(0,0,0,${interpolate(frame, [0, 6], [0.6, 0.4], clamp01)}) 0%, rgba(0,0,0,0) 45%)`}}>
			<div
				style={{
					fontFamily: FONTS.spectral.family,
					...foil(size),
					letterSpacing: size * spread,
					transform: `scale(${interpolate(slam, [0, 1], [1.7, 1])})`,
					opacity: frame < frames - 1 ? 1 : 0,
					filter: `drop-shadow(0 ${size * 0.05}px 0 rgba(40,18,0,0.9)) drop-shadow(0 0 ${size * 0.35}px rgba(255,190,70,0.5))`,
				}}
			>
				{g.text}
			</div>
		</AbsoluteFill>
	);
};

// ---- Lightning over the Spire: a double flicker of cold light ---------------------------------------------------
export const Lightning: React.FC = () => {
	const frame = useCurrentFrame();
	const pulse = [1, 0.35, 0.9, 0.2, 0.55, 0.1, 0.25, 0][frame] ?? 0;
	return <AbsoluteFill style={{background: 'radial-gradient(ellipse at 62% 20%, rgba(210,225,255,1) 0%, rgba(150,170,230,0.55) 45%, rgba(80,90,140,0.2) 100%)', mixBlendMode: 'screen', opacity: pulse}} />;
};

// ---- The glint on the aviators ---------------------------------------------------------------------------------
export const Glint: React.FC<{g: Graphic}> = ({g}) => {
	const frame = useCurrentFrame();
	const {fps, width, height} = useVideoConfig();
	const p = interpolate(frame, [0, 0.12 * fps, 0.38 * fps], [0, 1, 0], clamp01);
	const size = width * 0.11 * p;
	return (
		<svg
			width={size * 2}
			height={size * 2}
			viewBox="-100 -100 200 200"
			style={{position: 'absolute', left: g.x * width - size, top: g.y * height - size, transform: `rotate(${frame * 2}deg)`, mixBlendMode: 'screen'}}
		>
			<defs>
				<radialGradient id="glint">
					<stop offset="0" stopColor="#ffffff" />
					<stop offset="0.35" stopColor="#fff2c8" stopOpacity="0.9" />
					<stop offset="1" stopColor="#ffd27a" stopOpacity="0" />
				</radialGradient>
			</defs>
			<circle r="28" fill="url(#glint)" />
			<path d="M0 -100 L7 -7 L100 0 L7 7 L0 100 L-7 7 L-100 0 L-7 -7 Z" fill="#fffbe8" />
			<path d="M0 -45 L4 -4 L45 0 L4 4 L0 45 L-4 4 L-45 0 L-4 -4 Z" fill="#ffffff" transform="rotate(45)" />
		</svg>
	);
};
