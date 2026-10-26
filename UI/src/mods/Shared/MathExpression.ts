// Evaluates what the user typed in a number input: "200 + 30", "(4 - 1) * 2,5", "-2^2".
// + - * / ^, parentheses, unary signs, "," or "." as decimal separator. No eval, no variables.
// Returns null when the text isn't a valid expression or the result isn't finite (division by 0...).
export function evaluateExpression(text: string): number | null {
	const source = text.replace(/,/g, ".").replace(/\s+/g, "");
	if (source.length === 0) return null;

	let index = 0;

	function peek(): string {
		return source[index];
	}

	// expression := term (("+" | "-") term)*
	function parseExpression(): number | null {
		let value = parseTerm();
		while (value !== null && (peek() === "+" || peek() === "-")) {
			const op = source[index++];
			const right = parseTerm();
			if (right === null) return null;
			value = op === "+" ? value + right : value - right;
		}
		return value;
	}

	// term := unary (("*" | "/") unary)*
	function parseTerm(): number | null {
		let value = parseUnary();
		while (value !== null && (peek() === "*" || peek() === "/")) {
			const op = source[index++];
			const right = parseUnary();
			if (right === null) return null;
			value = op === "*" ? value * right : value / right;
		}
		return value;
	}

	// unary := ("+" | "-") unary | power. "-2^2" is -(2^2), like in math.
	function parseUnary(): number | null {
		if (peek() === "+" || peek() === "-") {
			const op = source[index++];
			const value = parseUnary();
			if (value === null) return null;
			return op === "-" ? -value : value;
		}
		return parsePower();
	}

	// power := primary ("^" unary)?, right associative: 2^3^2 = 2^9.
	function parsePower(): number | null {
		const base = parsePrimary();
		if (base === null || peek() !== "^") return base;
		index++;
		const exponent = parseUnary();
		if (exponent === null) return null;
		return Math.pow(base, exponent);
	}

	// primary := number | "(" expression ")"
	function parsePrimary(): number | null {
		if (peek() === "(") {
			index++;
			const value = parseExpression();
			if (value === null || peek() !== ")") return null;
			index++;
			return value;
		}

		const match = /^(\d+\.?\d*|\.\d+)(e[+-]?\d+)?/i.exec(source.slice(index));
		if (!match) return null;
		index += match[0].length;
		return parseFloat(match[0]);
	}

	const result = parseExpression();
	if (result === null || index !== source.length || !Number.isFinite(result)) return null;
	return result;
}
