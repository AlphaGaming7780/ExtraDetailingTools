import classNames from "classnames";
import { trigger, useValue } from "cs2/api";
import { SelectedInfoSectionBase } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { FOCUS_AUTO, Tooltip } from "cs2/ui";
import { MouseEvent } from "react";
import { ActionButtonSCSS } from "../../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { InfoRowSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { InfoSectionFoldout } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section-foldout";
import { InfoSectionSCSS } from "../../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { kTransformGizmoToolId } from "../TransformGizmosTool/TransformGizmoTool";
import { TransformPanel } from "../TransformPanel/TransformPanel";
import styles from "./TransformSection.module.scss";

export const kTransformSection$ = "TransformSection";

var PanelOpen: boolean = false;

export const TransformSection = (componentList: { [x: string]: any; }): any => {

	componentList["ExtraDetailingTools.Systems.UI.TransformSection"] = (e: SelectedInfoSectionBase) => {
		const { translate } = useLocalization();

		return <>
			<InfoSectionFoldout
				header={
					<div className={InfoRowSCSS.infoRow}>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>{e.group}</div>
						<Tooltip tooltip={translate("Tool.TransformGizmoTool.Tooltip", "Tool.TransformGizmoTool.Tooltip")} className={InfoRowSCSS.right}>
							<button className={classNames(ActionButtonSCSS.button, styles.headerButton)} onClick={(ev) => { ev.preventDefault(); ev.stopPropagation(); trigger("EDT", `${kTransformGizmoToolId}.SelectTransformGizmosTool`); }}>
								<img className={classNames(ActionButtonSCSS.icon, styles.headerButtonIcon)} src="coui://extradetailingtools/Icons/TransformGizmosTool/Icon.svg"></img>
							</button>
						</Tooltip>
					</div>
				}
				initialExpanded={PanelOpen}
				expandFromContent={false}
				focusKey={FOCUS_AUTO}
				onToggleExpanded={(value: boolean) => { trigger("EDT", "TransformSection.Opened", value); PanelOpen = value; }}
			>
				<div className={classNames(InfoSectionSCSS.content, InfoSectionSCSS.disableFocusHighlight)}
					onMouseEnter={(ev: MouseEvent) => { trigger("EDT", "TransformSection.ShowHighlight", true) }}
					onMouseLeave={(ev: MouseEvent) => { trigger("EDT", "TransformSection.ShowHighlight", false) }}
				>
					<TransformPanel />
				</div>
			</InfoSectionFoldout>
		</>;
	};
	return componentList as any;
};
