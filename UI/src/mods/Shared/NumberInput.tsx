import classNames from "classnames";
import { trigger } from "cs2/api";
import { ChangeEvent, KeyboardEvent as ReactKeyboardEvent, MouseEvent as ReactMouseEvent, WheelEvent, useEffect, useState } from "react";
import { EditorItemSCSS } from "../../../game-ui/editor/widgets/item/editor-item.module.scss";
import { evaluateExpression } from "./MathExpression";
import { handleWordJump } from "./InputWordJump";

export interface NumberInputProps {
	id: string;
	value: number;
	onCommit: (value: number) => void;
	// What the wheel does is up to the caller (step, offset...).
	onWheel?: (e: WheelEvent<HTMLInputElement>) => void;
	format?: (value: number) => string;
	className?: string;
}

// Text input for a number: shows the value while not editing, accepts expressions ("200 + 30"),
// commits on Enter / blur and goes back to the value when the text is invalid.
export function NumberInput({ id, value, onCommit, onWheel, format = (v) => v.toString(), className }: NumberInputProps): JSX.Element {
	const [text, setText] = useState(format(value));
	const [editing, setEditing] = useState(false);

	useEffect(() => {
		if (!editing) setText(format(value));
	}, [value]);

	function commit(raw: string) {
		const parsed = evaluateExpression(raw);
		if (parsed === null) {
			setText(format(value));
			return;
		}
		onCommit(parsed);
	}

	function onKeyDown(e: ReactKeyboardEvent<HTMLInputElement>) {
		if (handleWordJump(e)) return;
		// cohtml ignores the keyboard layout: also accept "q" for AZERTY.
		if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === "a" || e.key.toLowerCase() === "q")) {
			e.stopPropagation();
			e.currentTarget.select();
			return;
		}
		if (e.key === "Enter") {
			commit(e.currentTarget.value);
			e.currentTarget.blur();
		}
	}

	return <input
		id={id}
		value={text}
		multiple={false}
		className={classNames(EditorItemSCSS.input, className)}
		onChange={(e: ChangeEvent<HTMLInputElement>) => { setEditing(true); setText(e.target.value); }}
		onFocus={() => setEditing(true)}
		onBlur={(e) => { setEditing(false); commit(e.target.value); }}
		onKeyDown={onKeyDown}
		// The wheel changes the value: show it instead of the typed text.
		onWheel={onWheel ? (e) => { setEditing(false); onWheel(e); } : undefined}
		onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)}
	/>;
}

// Drag on a label: onDelta gets the horizontal move in pixels and returns the direction to play
// a sound for (0: none). Sounds are throttled.
export function startHorizontalDrag(e: ReactMouseEvent, onDelta: (deltaX: number) => number) {
	e.preventDefault();
	let lastX = e.clientX;
	let lastSoundTime = 0;
	const soundThrottleMs = 80;

	const onMouseMove = (moveEvent: globalThis.MouseEvent) => {
		const deltaX = moveEvent.clientX - lastX;
		if (deltaX === 0) return;
		lastX = moveEvent.clientX;
		const direction = onDelta(deltaX);
		const now = Date.now();
		if (direction !== 0 && now - lastSoundTime > soundThrottleMs) {
			trigger("audio", "playSound", direction > 0 ? "increase-elevation" : "decrease-elevation", 1);
			lastSoundTime = now;
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
