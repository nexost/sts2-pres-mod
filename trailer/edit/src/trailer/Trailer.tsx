// The trailer (T6): the picture edit from timeline.json (shots), the overlays (graphics), the "holy shit" hits and the
// finished mix (build/trailer/media/mix.wav, made by trailer/tools/mix.py from the same timeline).
import React from 'react';
import {AbsoluteFill, Audio, Sequence, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {loadFonts} from '../fonts';
import markers from '../vo_markers.json';
import {CollectionBeat} from './Collection';
import {ButtonCard, TitleCard} from './EndCards';
import {GRAPHICS, Graphic, SHOTS, toFrames} from './data';
import {kick} from './fx';
import {Callout, Glint, Kinetic, Lightning, NameCard, WallStamps} from './Overlays';
import {ShotView} from './Shot';

export type TrailerProps = {captions: boolean};

/** A graphic in its own Sequence (frame 0 = its "at"). */
const Timed: React.FC<{at: number; dur: number; children: React.ReactNode}> = ({at, dur, children}) => {
	const {fps} = useVideoConfig();
	return (
		<Sequence from={toFrames(at, fps)} durationInFrames={Math.max(1, toFrames(at + dur, fps) - toFrames(at, fps))} layout="none">
			{children}
		</Sequence>
	);
};

const Picture: React.FC = () => {
	const {fps} = useVideoConfig();
	return (
		<>
			{SHOTS.filter((s) => s.src !== 'graphics').map((s, i) => {
				const from = toFrames(s.at, fps);
				const frames = Math.max(1, toFrames(s.at + s.dur, fps) - from);
				return (
					<Sequence key={i} from={from} durationInFrames={frames}>
						<ShotView shot={s} frames={frames} />
					</Sequence>
				);
			})}
		</>
	);
};

const Overlays: React.FC = () => {
	const stamps = GRAPHICS.filter((g) => g.type === 'stamp');
	const one = (g: Graphic) => {
		switch (g.type) {
			case 'name':
				return <NameCard g={g} />;
			case 'callout':
				return <Callout g={g} />;
			case 'kinetic':
				return <Kinetic g={g} />;
			case 'glint':
				return <Glint g={g} />;
			case 'lightning':
				return <Lightning />;
			case 'collection':
				return <CollectionBeat g={g} />;
			case 'title':
				return <TitleCard />;
			case 'button':
				return <ButtonCard />;
			default:
				return null;
		}
	};
	return (
		<>
			{stamps.length > 0 && (
				<Timed at={stamps[0].at} dur={stamps[stamps.length - 1].at + (stamps[stamps.length - 1].dur ?? 1) - stamps[0].at}>
					<WallStamps stamps={stamps} t0={stamps[0].at} />
				</Timed>
			)}
			{GRAPHICS.filter((g) => g.type !== 'stamp').map((g, i) => (
				<Timed key={i} at={g.at} dur={g.dur ?? 0.4}>
					{one(g)}
				</Timed>
			))}
		</>
	);
};

/** Film grain: a little moving noise, so gameplay, paintings and graphics sit in one image. */
const Grain: React.FC = () => {
	const frame = useCurrentFrame();
	return (
		<AbsoluteFill style={{mixBlendMode: 'overlay', opacity: 0.1, pointerEvents: 'none'}}>
			<svg width="100%" height="100%">
				<filter id="grain">
					<feTurbulence type="fractalNoise" baseFrequency="0.85" numOctaves="2" seed={frame % 97} stitchTiles="stitch" />
					<feColorMatrix type="saturate" values="0" />
				</filter>
				<rect width="100%" height="100%" filter="url(#grain)" />
			</svg>
		</AbsoluteFill>
	);
};

const Captions: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const t = frame / fps;
	const line = (markers as {at: number; end: number; text: string}[]).find((m) => t >= m.at && t < m.end + 0.2);
	if (!line) return null;
	return (
		<div style={{position: 'absolute', bottom: width * 0.03, width: '100%', textAlign: 'center', fontFamily: 'TrailerKreon', fontSize: width * 0.019, color: '#fff', textShadow: '0 0 10px #000, 0 2px 4px #000'}}>
			{line.text}
		</div>
	);
};

export const Trailer: React.FC<TrailerProps> = ({captions}) => {
	loadFonts();
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const k = kick(frame / fps, frame);
	return (
		<AbsoluteFill style={{backgroundColor: 'black', overflow: 'hidden'}}>
			<Audio src={staticFile('mix.wav')} />
			<AbsoluteFill style={{transform: `translate(${k.x}px, ${k.y}px) scale(${k.scale})`, filter: k.filter}}>
				<Picture />
			</AbsoluteFill>
			<AbsoluteFill style={{background: 'radial-gradient(ellipse at 50% 50%, rgba(0,0,0,0) 55%, rgba(0,0,0,0.42) 100%)'}} />
			<Grain />
			<AbsoluteFill style={{transform: `translate(${k.x * 0.4}px, ${k.y * 0.4}px)`}}>
				<Overlays />
			</AbsoluteFill>
			<AbsoluteFill style={{backgroundColor: 'white', opacity: k.flash}} />
			{captions && <Captions />}
		</AbsoluteFill>
	);
};
