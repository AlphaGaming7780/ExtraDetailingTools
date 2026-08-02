import classNames from "classnames";
import { trigger } from "cs2/api";
import { Tooltip } from "cs2/ui";
import { ChangeEvent, KeyboardEvent as ReactKeyboardEvent, MouseEvent as ReactMouseEvent, ReactNode, WheelEvent, useEffect, useState } from "react";
import { EditorItemSCSS } from "../../../game-ui/editor/widgets/item/editor-item.module.scss";
import { remToPx } from "../RemHelper";
import styles from "./StepInput.module.scss";

export interface StepInputProps {
	id: string;
	value: number;
	onCommit: (value: number) => void;
	min?: number;
	tooltip?: any;
	label: ReactNode;
	showHandle?: boolean;
	className?: string;
	// "stacked" (default): label above the input, matching TransformPanel's POS_I/ROT_I/SCALE_I fields.
	// "inline": label and input side by side on one line.
	layout?: "stacked" | "inline";
}

// Canonicalizes to 6 decimal places so repeated *//10 stepping (and float32 round-trips through the C#
// binding) can't leave a binary-floating-point artifact like 0.009999999776482582 in the displayed text.
function round6(v: number): number {
	return Math.round(v * 1e6) / 1e6;
}

// Scroll/drag/edit numeric field shared by TransformPanel's increment fields (POS_I/ROT_I/SCALE_I) and
// the Transform Gizmo Tool's grid step fields. Scroll, or drag the label horizontally, to step the value
// with the classic increment heuristic (>=1: +/-1, else *//10). Click into the input to type an exact
// value, committed on Enter or blur.
//
// Call directly as a function (StepInput({...}), not <StepInput .../>) when a parent renders several of
// these per render (as TransformPanel's Inputs() does for X/Y/Z siblings): the hooks below then keep a
// stable call order across renders, same convention already used throughout this codebase.
export function StepInput(props: StepInputProps): JSX.Element {
	const { id, value, onCommit, min, tooltip, label, showHandle, className, layout } = props;
	const inline = layout === "inline";

	const [text, setText] = useState(round6(value).toString());
	const [editing, setEditing] = useState(false);

	useEffect(() => {
		if (!editing) setText(round6(value).toString());
	}, [value]);

	function clamp(v: number): number {
		return round6(min !== undefined ? Math.max(min, v) : v);
	}

	function step(current: number, direction: number): number {
		let next = current;
		if (direction > 0) next = current >= 1 ? current + 1 : current * 10;
		else if (current > 1) next = current - 1;
		else if (current > 0.001) next = current / 10;
		return clamp(next);
	}

	function commit(raw: string) {
		const parsed = parseFloat(raw);
		if (Number.isNaN(parsed)) {
			setText(value.toString());
			return;
		}
		onCommit(clamp(parsed));
	}

	function onWheel(e: WheelEvent) {
		e.stopPropagation();
		e.preventDefault();
		const direction = -Math.sign(e.deltaY);
		onCommit(step(value, direction));
		trigger("audio", "playSound", direction > 0 ? "increase-elevation" : "decrease-elevation", 1);
	}

	function onLabelMouseDown(e: ReactMouseEvent) {
		e.preventDefault();
		let lastX = e.clientX;
		const pixelsPerStep = remToPx(15);
		let accumulated = 0;
		let currentValue = value;
		let lastSoundTime = 0;
		const soundThrottleMs = 80;

		const onMouseMove = (moveEvent: globalThis.MouseEvent) => {
			accumulated += moveEvent.clientX - lastX;
			lastX = moveEvent.clientX;
			let lastDirection = 0;
			let stepped = false;

			while (Math.abs(accumulated) >= pixelsPerStep) {
				const direction = Math.sign(accumulated);
				accumulated -= direction * pixelsPerStep;
				lastDirection = direction;
				stepped = true;
				currentValue = step(currentValue, direction);
			}

			if (stepped) {
				onCommit(currentValue);
				const now = Date.now();
				if (now - lastSoundTime > soundThrottleMs) {
					trigger("audio", "playSound", lastDirection > 0 ? "increase-elevation" : "decrease-elevation", 1);
					lastSoundTime = now;
				}
			}
		};

		const onMouseUp = () => {
			document.removeEventListener('mousemove', onMouseMove);
			document.removeEventListener('mouseup', onMouseUp);
			document.body.style.cursor = '';
		};

		document.body.style.cursor = 'url(cursor://horizontal-can-resize)';
		document.addEventListener('mousemove', onMouseMove);
		document.addEventListener('mouseup', onMouseUp);
	}

	return <>
		{showHandle ? <span className={styles.handle} onMouseDown={onLabelMouseDown}>↕</span> : <></>}
		<Tooltip tooltip={tooltip}>
			<div className={inline ? styles.inlineWrapper : styles.wrapper}>
				<span className={classNames(inline ? styles.inlineLabelRow : styles.row, styles.draggableLabel)} onMouseDown={onLabelMouseDown}>{label}</span>
				<span className={inline ? styles.inlineInputRow : styles.row}>
					<input
						id={id}
						value={text}
						multiple={false}
						className={classNames(EditorItemSCSS.input, styles.input, className)}
						onChange={(e: ChangeEvent<HTMLInputElement>) => setText(e.target.value)}
						onFocus={() => setEditing(true)}
						onBlur={(e) => { setEditing(false); commit(e.target.value); }}
						onKeyDown={(e: ReactKeyboardEvent<HTMLInputElement>) => {
							// cohtml doesn't remap e.key per the OS keyboard layout, so "a" only matches on
							// QWERTY. AZERTY swaps A/Q, so also accept "q" for the same physical select-all key.
							console.log(`StepInput ${id} keydown: key=${e.key}, code=${e.code}, ctrl=${e.ctrlKey}, meta=${e.metaKey}`);
							if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === "a" || e.key.toLowerCase() === "q")) {
								e.stopPropagation();
								e.currentTarget.select();
								return;
							}
							if (e.key === "Enter") {
								commit((e.target as HTMLInputElement).value);
								(e.target as HTMLInputElement).blur();
							}
						}}
						onWheel={onWheel}
						onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)}
					/>
				</span>
			</div>
		</Tooltip>
	</>;
}
