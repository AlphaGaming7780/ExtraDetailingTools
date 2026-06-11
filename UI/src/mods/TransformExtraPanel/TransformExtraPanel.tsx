import { ExtraPanelType } from "mods/ExtraPanelType";
import { TransformPanel } from "mods/TransformPanel/TransformPanel";
import styles from "./TransformExtraPanel.module.scss";


export const TransformExtraPanel = (ComponentList: { [x: string]: any; }): any => {
    ComponentList["ExtraDetailingTools.Systems.UI.TransformPanel.TransformExtraPanel"] = (extraPanel: ExtraPanelType) => {
        return <div className={styles.transformExtraPanelContent}>
            <TransformPanel />
        </div>
    }
    return ComponentList;
}