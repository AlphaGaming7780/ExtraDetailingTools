import classNames from "classnames";
import { trigger } from "cs2/api";
import { Tooltip } from "cs2/ui";
import { MouseEvent as ReactMouseEvent, ReactNode, WheelEvent } from "react";
import { remToPx } from "../RemHelper";
import { NumberInput, startHorizontalDrag } from "./NumberInput";
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

// Rounded to 6 decimals to hide float artifacts.
function round6(v: number): number {
	return Math.round(v * 1e6) / 1e6;
}

// Scroll / drag the label to step, click to type.
export function StepInput(props: StepInputProps): JSX.Element {
	const { id, value, onCommit, min, tooltip, label, showHandle, className, layout } = props;
	const inline = layout === "inline";

	function clamp(v: number): number {
		return round6(min !== undefined ? Math.max(min, v) : v);
	}

	function step(current: number, direction: number): number {
		let next = current;
		if (direction > 0) next = current >= 1 ? current + 1 : current > 0 ? current * 10 : 0.001;
		else if (current > 1) next = current - 1;
		else if (current > 0.001) next = current / 10;
		return clamp(next);
	}

	function onWheel(e: WheelEvent) {
		e.stopPropagation();
		e.preventDefault();
		const direction = -Math.sign(e.deltaY);
		onCommit(step(value, direction));
		trigger("audio", "playSound", direction > 0 ? "increase-elevation" : "decrease-elevation", 1);
	}

	function onLabelMouseDown(e: ReactMouseEvent) {
		const pixelsPerStep = remToPx(15);
		let accumulated = 0;
		let currentValue = value;

		startHorizontalDrag(e, (deltaX) => {
			accumulated += deltaX;
			let lastDirection = 0;
			while (Math.abs(accumulated) >= pixelsPerStep) {
				lastDirection = Math.sign(accumulated);
				accumulated -= lastDirection * pixelsPerStep;
				currentValue = step(currentValue, lastDirection);
			}
			if (lastDirection !== 0) onCommit(currentValue);
			return lastDirection;
		});
	}

	return <>
		{showHandle ? <span className={styles.handle} onMouseDown={onLabelMouseDown}>↕</span> : <></>}
		<Tooltip tooltip={tooltip}>
			<div className={inline ? styles.inlineWrapper : styles.wrapper}>
				<span className={classNames(inline ? styles.inlineLabelRow : styles.row, styles.draggableLabel)} onMouseDown={onLabelMouseDown}>{label}</span>
				<span className={inline ? styles.inlineInputRow : styles.row}>
					<NumberInput
						id={id}
						value={value}
						onCommit={(v) => onCommit(clamp(v))}
						onWheel={onWheel}
						format={(v) => round6(v).toString()}
						className={classNames(styles.input, className)}
					/>
				</span>
			</div>
		</Tooltip>
	</>;
}
