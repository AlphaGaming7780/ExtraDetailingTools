import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import { kGroupName, kTransformGizmoToolId } from "../../BindingConst";
import { FOCUS_DISABLED$ } from "../../../game-ui/common/focus/focus-key";

const anarchyAvailable$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.AnarchyAvailable`, false);
const addPreventOverride$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.AddPreventOverride`, false);
const addTransformLock$ = bindValue<boolean>(kGroupName, `${kTransformGizmoToolId}.AddTransformLock`, false);

export const AnarchyButtons = () => {
	const anarchyAvailable: boolean = useValue(anarchyAvailable$);
	const addPreventOverride: boolean = useValue(addPreventOverride$);
	const addTransformLock: boolean = useValue(addTransformLock$);
	const { translate } = useLocalization();

	if (!anarchyAvailable) return <></>;

	return <>
		<ToolButton
			focusKey={FOCUS_DISABLED$}
			tooltip={translate("Tool.TransformGizmoTool.AddPreventOverride.Tooltip", "Prevent Override")}
			src="coui://uil/Standard/Anarchy.svg"
			selected={addPreventOverride}
			onSelect={() => trigger(kGroupName, `${kTransformGizmoToolId}.AddPreventOverride`, !addPreventOverride)}
		/>
		<ToolButton
			focusKey={FOCUS_DISABLED$}
			tooltip={translate("Tool.TransformGizmoTool.AddTransformLock.Tooltip", "Transform Lock")}
			src="coui://uil/Standard/ArrowsHeightLocked.svg"
			selected={addTransformLock}
			onSelect={() => trigger(kGroupName, `${kTransformGizmoToolId}.AddTransformLock`, !addTransformLock)}
		/>
	</>;
};
