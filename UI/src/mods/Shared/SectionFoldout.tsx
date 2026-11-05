import classNames from "classnames";
import { FocusKey } from "cs2/bindings";
import { FOCUS_AUTO } from "cs2/ui";
import { Fragment, ReactNode, isValidElement } from "react";
import { InfoSectionFoldout } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section-foldout";
import { InfoSectionSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { MouseToolOptionsSCSS } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss";
import { ToolButtonSCSS } from "../../../game-ui/game/components/tool-options/tool-button/tool-button.module.scss";
import styles from "./SectionFoldout.module.scss";

// Whether the children render nothing (falsy or empty Fragment, recursively).
export function isEmptyChildren(node: ReactNode): boolean {
    if (node === null || node === undefined || node === false || node === true || node === "") {
        return true;
    }
    if (Array.isArray(node)) {
        return node.every(isEmptyChildren);
    }
    if (isValidElement(node) && node.type === Fragment) {
        return isEmptyChildren((node.props as { children?: ReactNode }).children);
    }
    return false;
}

const persistedExpanded = new Map<string, boolean>();

export function getPersistedExpanded(persistKey: string): boolean | undefined {
	return persistedExpanded.get(persistKey);
}

export interface SectionFoldoutProps {
	title: ReactNode;
	headerRight?: ReactNode;
	initialExpanded?: boolean;
	persistKey?: string;
	onToggleExpanded?: (value: boolean) => void;
	focusKey?: FocusKey;
	className?: string;
	contentClassName?: string;
	children: ReactNode;
}

export function SectionFoldout(props: SectionFoldoutProps): JSX.Element {
	const { title, headerRight, initialExpanded, persistKey, onToggleExpanded, focusKey, className, contentClassName, children } = props;

	const startExpanded = persistKey !== undefined
		? persistedExpanded.get(persistKey) ?? initialExpanded ?? false
		: initialExpanded ?? false;

	return <InfoSectionFoldout
		header={
			<div className={styles.header}>
				<div className={MouseToolOptionsSCSS.label}>
					{title}
				</div>
				<div className={classNames(MouseToolOptionsSCSS.content, styles.headerContent)}>
					{isEmptyChildren(headerRight)
						// Invisible box with the game tool button size: without buttons, the row keeps the height of a classic Section.
						? <div className={classNames(ToolButtonSCSS.button, styles.placeholder)} />
						: headerRight}
				</div>
			</div>
		}
		initialExpanded={startExpanded}
		expandFromContent={false}
		focusKey={focusKey ?? FOCUS_AUTO}
		className={classNames(MouseToolOptionsSCSS.item, styles.foldout, className)}
		onToggleExpanded={(persistKey !== undefined || onToggleExpanded !== undefined)
			? (value: boolean) => {
				if (persistKey !== undefined) persistedExpanded.set(persistKey, value);
				onToggleExpanded?.(value);
			}
			: undefined}
	>
		<div className={classNames(InfoSectionSCSS.content, InfoSectionSCSS.disableFocusHighlight, styles.content, contentClassName)}>
			{children}
		</div>
	</InfoSectionFoldout>;
}
