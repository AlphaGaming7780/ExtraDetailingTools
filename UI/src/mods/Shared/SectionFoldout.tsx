import classNames from "classnames";
import { FocusKey } from "cs2/bindings";
import { FOCUS_AUTO } from "cs2/ui";
import { ReactNode } from "react";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { InfoSectionFoldout } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section-foldout";
import { InfoSectionSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { MouseToolOptionsSCSS } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss";
import styles from "./SectionFoldout.module.scss";

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
			<div className={InfoRowSCSS.infoRow} style={{ paddingLeft: "0rem", paddingTop: "0rem", paddingBottom: "0rem" }}>
				<div className={classNames(MouseToolOptionsSCSS.label, InfoRowSCSS.left)} style={{ flexGrow: 1, height: "28rem" }}>
					{title}
				</div>
				{headerRight ?? <></>}
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
