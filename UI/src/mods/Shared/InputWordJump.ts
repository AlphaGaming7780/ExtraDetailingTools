import { KeyboardEvent as ReactKeyboardEvent } from "react";

// cohtml has no Ctrl + Left / Right in inputs: jump by number / operator, Shift extends the selection.
// Returns true when the key was handled.
export function handleWordJump(e: ReactKeyboardEvent<HTMLInputElement>): boolean {
	if (!(e.ctrlKey || e.metaKey)) return false;
	const direction = getArrowDirection(e);
	if (direction === 0) return false;
	e.preventDefault();
	e.stopPropagation();

	const input = e.currentTarget;
	const text = input.value;
	const start = input.selectionStart ?? 0;
	const end = input.selectionEnd ?? start;
	const backward = input.selectionDirection === "backward";
	const anchor = backward ? end : start;
	const focus = backward ? start : end;

	const next = direction > 0 ? nextBoundary(text, focus) : previousBoundary(text, focus);
	const shift = e.shiftKey;

	// Also applied after the default action, in case cohtml still moves the caret by one character.
	const apply = () => {
		if (shift) input.setSelectionRange(Math.min(anchor, next), Math.max(anchor, next), next < anchor ? "backward" : "forward");
		else input.setSelectionRange(next, next);
	};
	apply();
	setTimeout(apply, 0);
	return true;
}

// -1 left, 1 right, 0 other. cohtml may report the old key names or only the key code.
function getArrowDirection(e: ReactKeyboardEvent<HTMLInputElement>): number {
	if (e.key === "ArrowLeft" || e.key === "Left" || e.code === "ArrowLeft" || e.keyCode === 37) return -1;
	if (e.key === "ArrowRight" || e.key === "Right" || e.code === "ArrowRight" || e.keyCode === 39) return 1;
	return 0;
}

// 0: space, 1: digits, 2: operator / other, 3: decimal separator (stops inside "12.345").
function charClass(c: string): number {
	if (/\s/.test(c)) return 0;
	if (/[0-9]/.test(c)) return 1;
	if (/[.,]/.test(c)) return 3;
	return 2;
}

// End of the current group, then the spaces after it.
function nextBoundary(text: string, from: number): number {
	let i = from;
	if (i < text.length && charClass(text[i]) !== 0) {
		const cls = charClass(text[i]);
		while (i < text.length && charClass(text[i]) === cls) i++;
	}
	while (i < text.length && charClass(text[i]) === 0) i++;
	return i;
}

// Spaces before, then the start of the previous group.
function previousBoundary(text: string, from: number): number {
	let i = from;
	while (i > 0 && charClass(text[i - 1]) === 0) i--;
	if (i > 0) {
		const cls = charClass(text[i - 1]);
		while (i > 0 && charClass(text[i - 1]) === cls) i--;
	}
	return i;
}
