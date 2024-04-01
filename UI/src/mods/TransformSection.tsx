
import { TextInput } from "../../game-ui/common/input/text/text-input";
import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";
import { InfoRowSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss";
import { EditorItemSCSS } from "../../game-ui/editor/widgets/item/editor-item.module.scss";
import TransfromSectionSCSS from "./Styles/TransformSection.module.scss"
import { bindValue, useValue } from "cs2/api";
import { ModuleRegistryExtend } from "cs2/modding";

export interface Float3 {
	x: number,
	y: number,
	z: number
}


export const TransformSection = (componentList: any): any => { //: ModuleRegistryExtend

	function OnChange(value: Event) {
		if (value?.target instanceof HTMLTextAreaElement) {
			let number: number = parseInt(value.target.value, 10)
			console.log(number)
		}
	}

	interface TransformSection {
		group: string;
		tooltipKeys: Array<string>;
		tooltipTags: Array<string>;
		posX: number;
		posY: number;
		posZ: number;
		rotX: number;
		rotY: number;
		rotZ: number;
	}

	componentList["ExtraDetailingTools.TransformSection"] = (e: TransformSection) => <>
		<div className= {InfoSectionSCSS.infoSection}>
			<div className={InfoSectionSCSS.content + " " + InfoSectionSCSS.disableFocusHighlight}>
				<div className={InfoRowSCSS.infoRow}>
					<div className={InfoRowSCSS.left + " " + InfoRowSCSS.uppercase}>{e.group}</div>
					{/*<div className={InfoRowSCSS.right}>Right</div>*/}
				</div>
				<div className={InfoRowSCSS.infoRow + " " + InfoRowSCSS.subRow + " " + InfoRowSCSS.link}>
					<div className={ InfoRowSCSS.left + " " + InfoRowSCSS.link }>
						Position
					</div>
					<div className={InfoRowSCSS.right} style={{justifyContent: "flex-end", alignContent: "flex-end"}}>
						↕<TextInput value={"1"} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						X<TextInput value={e.posX.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Y<TextInput value={e.posY.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Z<TextInput value={e.posZ.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
					</div>
					<div className={InfoRowSCSS.left + " " + InfoRowSCSS.link}>
						Rotation
					</div>
					<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
						↕<TextInput value={"1"} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						X<TextInput value={e.rotX.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Y<TextInput value={e.rotY.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Z<TextInput value={e.rotZ.toString()} multiline={1} className={EditorItemSCSS.input + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
					</div>
				</div>
			</div>
		</div>
	</>
	return componentList as any;
} 