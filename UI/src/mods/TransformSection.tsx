
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

	const pos$ = bindValue<Float3>("edt", 'transformsection_getpos');
	const rot$ = bindValue<Float3>("edt", 'transformsection_getrot');

	const pos: Float3 = useValue(pos$);
	const rot: Float3 = useValue(rot$);

	console.log(pos)
	console.log(rot)

	function OnChange(value: Event) {
		if (value?.target instanceof HTMLTextAreaElement) {
			let number: number = parseInt(value.target.value, 10)
			value.target.value = number.toString()
			console.log(number)
		}
	}

	componentList["ExtraDetailingTools.TransformSection"] = () => <>
		<div className= {InfoSectionSCSS.infoSection}>
			<div className={InfoSectionSCSS.content + " " + InfoSectionSCSS.disableFocusHighlight}>
				<div className={InfoRowSCSS.infoRow}>
					<div className={InfoRowSCSS.left + " " + InfoRowSCSS.uppercase}>Transform Section</div>
					{/*<div className={InfoRowSCSS.right}>Right</div>*/}
				</div>
				<div className={InfoRowSCSS.infoRow + " " + InfoRowSCSS.subRow + " " + InfoRowSCSS.link}>
					<div className={ InfoRowSCSS.left + " " + InfoRowSCSS.link }>
						Position
					</div>
					<div className={InfoRowSCSS.right} style={{justifyContent: "flex-end", alignContent: "flex-end"}}>
						↕<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						X<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Y<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Z<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
					</div>
					<div className={InfoRowSCSS.left + " " + InfoRowSCSS.link}>
						Rotation
					</div>
					<div className={InfoRowSCSS.right} style={{ justifyContent: "flex-end", alignContent: "flex-end" }}>
						↕<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						X<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Y<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
						Z<TextInput multiline={1} className={EditorItemSCSS.sliderInput + " " + TransfromSectionSCSS.TransfromSectionInput} onChange={OnChange} />
					</div>
				</div>
			</div>
		</div>
	</>
	return componentList as any;
} 