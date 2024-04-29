import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { InfoRowSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { EditorItemSCSS } from "../../game-ui/editor/widgets/item/editor-item.module.scss";
import TransfromSectionSCSS from "./Styles/TransformSection.module.scss";
import { trigger, useValue } from "cs2/api";
import { bindValue } from "cs2/api";
import { FormEvent, MouseEvent, WheelEvent } from "react";
import { ActionButtonSCSS } from "../../game-ui/game/components/selected-info-panel/selected-info-sections/shared-sections/actions-section/action-button.module.scss";
import { useLocalization } from "cs2/l10n";
import { Tooltip, TooltipProps } from "cs2/ui";
import { TooltipTarget } from "cs2/bindings";
import classNames from "classnames";

export interface Float3 {
	x: number,
	y: number,
	z: number
}


export const pos$ = bindValue<Float3>("edt", 'transformsection_getpos');
export const rot$ = bindValue<Float3>("edt", 'transformsection_getrot');
export const incPos$ = bindValue<number>("edt", 'transformsection_getincpos');
export const incRot$ = bindValue<number>("edt", 'transformsection_getincrot');

export const TransformSection = (componentList: any): any => {

	interface InfoSection {
		group: string;
		tooltipKeys: Array<string>;
		tooltipTags: Array<string>;
	}

	componentList["ExtraDetailingTools.TransformSection"] = (e: InfoSection) => {
		const pos: Float3 = useValue(pos$);
		const rot: Float3 = useValue(rot$);
		var PositionIncrement: number = useValue(incPos$);
		var RotationIncrement: number = useValue(incRot$);
		const { translate } = useLocalization();

		function OnChange(event: FormEvent<HTMLInputElement>) {
			if (event.target instanceof HTMLInputElement) {
				let number = parseFloat(event.target.value);
				if (Number.isNaN(number)) number = 1;
				switch (event.target.id) {
					case "pI": PositionIncrement = number; triggerIncPos(); break;
					//case "pX": pos.x = number; triggerPos(); break;
					//case "pY": pos.y = number; triggerPos(); break;
					//case "pZ": pos.z = number; triggerPos(); break;
					case "rI": RotationIncrement = number; triggerIncRot(); break;
					//case "rX": rot.x = number; triggerRot(); break;
					//case "rY": rot.x = number; triggerRot(); break;
					//case "rZ": rot.x = number; triggerRot(); break;
				}
				trigger("audio", "playSound", "hover-item", 1)
			}
		}

		function OnScroll(event: WheelEvent) {

			if (event.target instanceof HTMLInputElement) {
				switch (event.target.id) {
					case "pI":
						if (event.deltaY < 0) {
							if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
							else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
							trigger("audio", "playSound", "increase-elevation", 1);
						} else {
							if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
							else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
							trigger("audio", "playSound" ,"decrease-elevation", 1);
						}
						PositionIncrement = parseFloat(event.target.value)
						triggerIncPos();
						break;
					case "pX":
						pos.x -= Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.x.toString();
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "pY":
						pos.y -= Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.y.toString();
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "pZ":
						pos.z -= Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.z.toString();
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "rI":
						if (event.deltaY < 0) {
							if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
							else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
							trigger("audio", "playSound", "increase-elevation", 1);
						} else {
							if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
							else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
							trigger("audio", "playSound", "decrease-elevation", 1);
						}
						RotationIncrement = parseFloat(event.target.value)
						triggerIncRot();
						break;
					case "rX":
						rot.x -= Math.sign(event.deltaY) * RotationIncrement;
						event.target.value = rot.x.toString();
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerRot()
						break;
					case "rY":
						rot.y -= Math.sign(event.deltaY) * RotationIncrement;
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						event.target.value = rot.y.toString();
						triggerRot()
						break;
					case "rZ":
						rot.z -= Math.sign(event.deltaY) * RotationIncrement;
						event.target.value = rot.z.toString();
						if (event.deltaY < 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerRot()
						break;
				}
			}
		}

		function triggerPos() {
			trigger("edt", "transformsection_getpos", pos)
		}

		function triggerRot() {
			trigger("edt", "transformsection_getrot", rot)
		}
		function triggerIncPos() {
			trigger("edt", "transformsection_getincpos", PositionIncrement)
		}

		function triggerIncRot() {
			trigger("edt", "transformsection_getincrot", RotationIncrement)
		}

		return <>
			<div className={classNames(InfoSectionSCSS.infoSection)} onMouseEnter={(e: MouseEvent) => { trigger("edt", "showhighlight", true) }} onMouseLeave={(e: MouseEvent) => { trigger("edt", "showhighlight", false) } }>
				<div className={classNames(InfoSectionSCSS.content, InfoSectionSCSS.disableFocusHighlight)}>
					<div className={InfoRowSCSS.infoRow}>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>{e.group}</div>
					</div>
					<div className={classNames(InfoRowSCSS.infoRow, InfoRowSCSS.subRow, InfoRowSCSS.link)}>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)}>
							{translate("Editor.POSITION") + " "}
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPYPOS")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_copypos") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
								</button>
							</Tooltip>
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PASTPOS")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_pastpos") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
								</button>
							</Tooltip>
						</div>
						<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
							↕<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSI")}>
								<input id="pI" value={PositionIncrement} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							X<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSX")}>
								<input id="pX" value={pos.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							Y<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSY")}>
								<input id="pY" value={pos.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							Z<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.POSZ")}>
								<input id="pZ" value={pos.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.link)}>
							{translate("PhotoMode.PROPERTY_TITLE[Rotation]") + " "}
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.COPYROT")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_copyrot") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Copy.svg"></img>
								</button>
							</Tooltip>
							<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.PASTROT")}>
								<button className={classNames(ActionButtonSCSS.button, TransfromSectionSCSS.TransfromSectionButton)} onClick={() => { trigger("edt", "transformsection_pastrot") }}>
									<img className={classNames(ActionButtonSCSS.icon, TransfromSectionSCSS.TransfromSectionButtonIcon)} src="coui://extralib/Icons/Misc/Past.svg"></img>
								</button>
							</Tooltip>
						</div>
						<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
							↕<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTI")}>
								<input id="rI" value={RotationIncrement} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							X<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTX")}>
								<input id="rX" value={rot.x.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							Y<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTY")}>
								<input id="rY" value={rot.y.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
							Z<Tooltip tooltip={translate("SelectedInfoPanel.TRANSFORMTOOL.ROTZ")}>
								<input id="rZ" value={rot.z.toString()} multiple={false} className={classNames(EditorItemSCSS.input, TransfromSectionSCSS.TransfromSectionInput)} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							</Tooltip>
						</div>
					</div>
				</div>
			</div>
		</>;
	};
	return componentList as any;
};
