// One shot of the picture edit: a clip framed by a camera (zoom and pan inside the 4K source, eased over the shot),
// optionally slowed or frozen, with a grade ("look").
import React from 'react';
import {AbsoluteFill, Easing, Freeze, OffthreadVideo, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';
import {Cam, Shot, clamp01, videoUrl} from './data';

const LOOKS: Record<string, {filter: string; overlay?: string; blend?: React.CSSProperties['mixBlendMode']}> = {
	cold: {filter: 'saturate(0.8) contrast(1.08) brightness(0.92)', overlay: 'rgba(40, 70, 140, 0.16)', blend: 'multiply'},
	mono: {filter: 'grayscale(0.8) contrast(1.25) brightness(1.12)', overlay: 'rgba(190, 60, 50, 0.14)', blend: 'multiply'},
	dusk: {filter: 'saturate(0.82) brightness(0.9) contrast(1.05)', overlay: 'rgba(30, 40, 120, 0.22)', blend: 'multiply'},
	brandon: {filter: 'saturate(1.25) contrast(1.12)', overlay: 'rgba(255, 30, 40, 0.12)', blend: 'screen'},
};

/** Keeps the visible window inside the source (x can stay free where a mask hides the edge, as in the split). */
const clampCam = ([s, x, y]: Cam, clampX: boolean): Cam => {
	const h = 0.5 / s;
	return [s, clampX ? Math.min(1 - h, Math.max(h, x)) : x, Math.min(1 - h, Math.max(h, y))];
};

export const Framed: React.FC<{cam: Cam; clampX?: boolean; children: React.ReactNode}> = ({cam, clampX = true, children}) => {
	const {width, height} = useVideoConfig();
	const [s, x, y] = clampCam(cam, clampX);
	return (
		<AbsoluteFill style={{transform: `translate(${width * (0.5 - s * x)}px, ${height * (0.5 - s * y)}px) scale(${s})`, transformOrigin: '0 0'}}>
			{children}
		</AbsoluteFill>
	);
};

export const Clip: React.FC<{src: string; from: number; rate?: number; freeze?: boolean}> = ({src, from, rate = 1, freeze}) => {
	const {fps} = useVideoConfig();
	const video = <OffthreadVideo src={videoUrl(src)} trimBefore={Math.round(from * fps)} playbackRate={rate} muted style={{width: '100%', height: '100%', objectFit: 'cover'}} />;
	return freeze ? <Freeze frame={0}>{video}</Freeze> : video;
};

/** "Two new candidates": both select paintings slide in from their sides, split on a gold diagonal. */
const Split: React.FC<{shot: Shot; frames: number}> = ({shot, frames}) => {
	const frame = useCurrentFrame();
	const {fps, width} = useVideoConfig();
	const inP = interpolate(frame, [0, 0.28 * fps], [0, 1], {...clamp01, easing: Easing.out(Easing.cubic)});
	const drift = interpolate(frame, [0, frames], [0, 1], clamp01);
	const left = 'polygon(0 0, 54% 0, 46% 100%, 0 100%)';
	const right = 'polygon(54% 0, 100% 0, 100% 100%, 46% 100%)';
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			{/* Each face sits in the middle of its half: Donald's painting has his face at (0.645, 0.2), Joe's at (0.55, 0.22). */}
			<AbsoluteFill style={{clipPath: left, transform: `translateX(${(inP - 1) * width * 0.6}px)`}}>
				<Framed cam={[1.15 + drift * 0.05, 0.86, 0.44]} clampX={false}>
					<Clip src={shot.left!} from={shot.from} />
				</Framed>
			</AbsoluteFill>
			<AbsoluteFill style={{clipPath: right, transform: `translateX(${(1 - inP) * width * 0.6}px)`}}>
				<Framed cam={[1.15 + drift * 0.05, 0.34, 0.44]} clampX={false}>
					<Clip src={shot.right!} from={shot.from} />
				</Framed>
			</AbsoluteFill>
			<svg width="100%" height="100%" viewBox="0 0 100 100" preserveAspectRatio="none" style={{position: 'absolute', opacity: inP}}>
				<defs>
					<linearGradient id="splitgold" x1="0" y1="0" x2="0" y2="1">
						<stop offset="0" stopColor="#fff3c4" />
						<stop offset="0.5" stopColor="#e6b84a" />
						<stop offset="1" stopColor="#8a5a12" />
					</linearGradient>
				</defs>
				<polygon points="53.6,0 54.4,0 46.4,100 45.6,100" fill="url(#splitgold)" />
			</svg>
		</AbsoluteFill>
	);
};

export const ShotView: React.FC<{shot: Shot; frames: number}> = ({shot, frames}) => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	if (shot.src === 'split') {
		return <Split shot={shot} frames={frames} />;
	}
	const p = frames > 1 ? Easing.inOut(Easing.quad)(Math.min(1, frame / (frames - 1))) : 0;
	const cam0 = shot.cam ?? ([1, 0.5, 0.5] as Cam);
	const cam1 = shot.camTo ?? cam0;
	const cam = cam0.map((v, i) => v + (cam1[i] - v) * p) as Cam;
	const look = shot.look ? LOOKS[shot.look] : undefined;
	const opacity = shot.fadeIn ? interpolate(frame, [0, shot.fadeIn * fps], [0, 1], clamp01) : 1;
	return (
		<AbsoluteFill style={{opacity, filter: look?.filter}}>
			<Framed cam={cam}>
				<Clip src={shot.src} from={shot.from} rate={shot.rate} freeze={shot.freeze} />
			</Framed>
			{look?.overlay && <AbsoluteFill style={{backgroundColor: look.overlay, mixBlendMode: look.blend}} />}
		</AbsoluteFill>
	);
};
