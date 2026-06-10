import { ExtraPanelType } from "mods/ExtraPanelType";
import { TransformPanel } from "mods/TransformPanel/TransformPanel";


export const TransformExtraPanel = (ComponentList: { [x: string]: any; }): any => {
    ComponentList["ExtraDetailingTools.Systems.UI.TransformPanel.TransformExtraPanel"] = (extraPanel: ExtraPanelType) => {
        return <TransformPanel />
    }
    return ComponentList;
}