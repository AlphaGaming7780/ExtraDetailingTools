﻿import { TextInput } from "../../game-ui/common/input/text/text-input";
import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { InfoRowSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { EditorItemSCSS } from "../../game-ui/editor/widgets/item/editor-item.module.scss";
import TransfromSectionSCSS from "./Styles/TransformSection.module.scss";
import { trigger, useValue } from "cs2/api";
import { bindValue } from "cs2/api";
import { ModuleRegistryExtend } from "cs2/modding";
import React, { FormEvent, MouseEvent, ReactNode, WheelEvent } from "react";

export interface Float3 {
	x: number,
	y: number,
	z: number
}

export const pos$ = bindValue<Float3>("edt", 'transformsection_getpos');
export const rot$ = bindValue<Float3>("edt", 'transformsection_getrot');

let PositionIncrement = 1;
let RotationIncrement = 1;

export const TransformSection = (componentList: any): any => {

	interface InfoSection {
		group: string;
		tooltipKeys: Array<string>;
		tooltipTags: Array<string>;
	}

	componentList["ExtraDetailingTools.TransformSection"] = (e: InfoSection) => {
		const pos: Float3 = useValue(pos$);
		const rot: Float3 = useValue(rot$);

		function OnChange(event: FormEvent<HTMLInputElement>) {
			return;
			//if (event.target instanceof HTMLInputElement) {
			//	let number = parseFloat(event.target.value);
			//	switch (event.target.id) {
			//		case "pI": PositionIncrement = number; break;
			//		case "pX": pos.x = number; break;
			//		case "pY": pos.y = number; break;
			//		case "pZ": pos.z = number; break;
			//		case "rI": RotationIncrement = number; break;
			//		case "rX": rot.x = number; break;
			//		case "rY": rot.x = number; break;
			//		case "rZ": rot.x = number; break;
			//	}
			//}
		}

		function OnScroll(event: WheelEvent) {

			if (event.target instanceof HTMLInputElement) {
				switch (event.target.id) {
					case "pI":
						if (event.deltaY > 0) {
							if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
							else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
							trigger("audio", "playSound", "increase-elevation", 1);
						} else {
							if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
							else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
							trigger("audio", "playSound" ,"decrease-elevation", 1);
						}
						PositionIncrement = parseFloat(event.target.value)
						break;
					case "pX":
						pos.x += Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.x.toString();
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "pY":
						pos.y += Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.y.toString();
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "pZ":
						pos.z += Math.sign(event.deltaY) * PositionIncrement;
						event.target.value = pos.z.toString();
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerPos();
						break;
					case "rI":
						if (event.deltaY < 0) {
							if (parseFloat(event.target.value) >= 1) { event.target.value = (parseFloat(event.target.value) + 1).toString() }
							else { event.target.value = (parseFloat(event.target.value) * 10).toString() }
							trigger("audio", "playSound", "increase-elevation", 1);
						} else if (event.deltaY > 0) {
							if (parseFloat(event.target.value) > 1) { event.target.value = (parseFloat(event.target.value) - 1).toString() }
							else if (parseFloat(event.target.value) > 0.001) { event.target.value = (parseFloat(event.target.value) / 10).toString() }
							trigger("audio", "playSound", "decrease-elevation", 1);
						}
						RotationIncrement = parseFloat(event.target.value)
						break;
					case "rX":
						rot.x += Math.sign(event.deltaY) * RotationIncrement;
						event.target.value = rot.x.toString();
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						triggerRot()
						break;
					case "rY":
						rot.y += Math.sign(event.deltaY) * RotationIncrement;
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
						else trigger("audio", "playSound", "decrease-elevation", 1);
						event.target.value = rot.y.toString();
						triggerRot()
						break;
					case "rZ":
						rot.z += Math.sign(event.deltaY) * RotationIncrement;
						event.target.value = rot.z.toString();
						if (event.deltaY > 0) trigger("audio", "playSound", "increase-elevation", 1);
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

		return <>
			<div className={InfoSectionSCSS.infoSection} onMouseEnter={(e: MouseEvent) => { trigger("edt", "showhighlight", true) }} onMouseLeave={(e: MouseEvent) => { trigger("edt", "showhighlight", false) } }>
				<div className={InfoSectionSCSS.content + " " + InfoSectionSCSS.disableFocusHighlight}>
					<div className={InfoRowSCSS.infoRow}>
						<div className={InfoRowSCSS.left + " " + InfoRowSCSS.uppercase}>{e.group}</div>
						{/*<div className={InfoRowSCSS.right}>Right</div>*/}
					</div>
					<div className={InfoRowSCSS.infoRow + " " + InfoRowSCSS.subRow + " " + InfoRowSCSS.link}>
						<div className={InfoRowSCSS.left + " " + InfoRowSCSS.link}>
							Position
						</div>
						<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
							↕<input id="pI" value={PositionIncrement} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							X<input id="pX" value={pos.x.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							Y<input id="pY" value={pos.y.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							Z<input id="pZ" value={pos.z.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
						</div>
						<div className={InfoRowSCSS.left + " " + InfoRowSCSS.link}>
							Rotation
						</div>
						<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
							↕<input id="rI" value={RotationIncrement} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							X<input id="rX" value={rot.x.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							Y<input id="rY" value={rot.y.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
							Z<input id="rZ" value={rot.z.toString()} multiple={false} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onInput={OnChange} onWheel={OnScroll} onMouseEnter={() => trigger("audio", "playSound", "hover-item", 1)} />
						</div>
					</div>
				</div>
			</div>
		</>;
	};
	return componentList as any;
};
