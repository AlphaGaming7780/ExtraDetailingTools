import { ModuleRegistryExtend } from "cs2/modding";
import { PropsSection, Section } from "../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { PropsToolButton, ToolButton } from "../../game-ui/game/components/tool-options/tool-button/tool-button";
import { bindValue, trigger, useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";import { Tool, tool } from "cs2/bindings";
;

export const markerVisible$ = bindValue<boolean>("edt", 'showmarker');

export const ToolOption: ModuleRegistryExtend = (Component: any) => {
	return (props) => {

		let activeTool: Tool = useValue(tool.activeTool$);
		let markerVisible: boolean = useValue(markerVisible$)
		const { translate } = useLocalization();

		let PropsToolButton: PropsToolButton = {
			selected: markerVisible,
			tooltip: translate("ToolOptions.TOOLTIP[ShowMarker]"), 
			src: "Media/Tools/Snap Options/All.svg",
			onSelect: () => { trigger("edt", "showmarker") }
		}

		let ShowMarkerProps: PropsSection = {
			title: translate("Toolbar.SHOWMARKER"),//"Show Marker",
			children: ToolButton(PropsToolButton)
		}

		// This defines aspects of the components.
		const { children, ...otherProps } = props || {};

		// This gets the original component that we may alter and return.
		var result: JSX.Element = Component();

		if (activeTool.id === tool.OBJECT_TOOL || activeTool.id === tool.AREA_TOOL || activeTool.id === tool.NET_TOOL) {

			result.props.children?.unshift(
				Section(ShowMarkerProps),
			);
		}
		return result;
	};
}