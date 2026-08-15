import { Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import { FOCUS_DISABLED$ } from "../../../game-ui/common/focus/focus-key";
import { ExtraSnapBase, registerExtraSnapRenderer, setExtraSnap } from "./ExtraSnap";
import { useLocalization } from "cs2/l10n";
import { SectionFoldout } from "mods/Shared/SectionFoldout";
import { SectionOrFoldout } from "mods/Shared/SectionOrFoldout";
import Styles from "mods/Shared/SectionFoldout.module.scss";

const kObjectToolExtraSnapType = "ExtraDetailingTools.ExtraSnap.ObjectToolSystemExtraSnap";

// Mirrors ObjectToolSystemExtraSnap.ObjectToolExtraSnap in ObjectTool.cs.
enum ObjectToolExtraSnap {
    ObjectSurface = 1 << 0,
    Upright = 1 << 1,
    ObjectSide = 1 << 2,
}

enum ObjectSideSnapMode {
    FreeMove,
    SnapToCenter,
    SnapToCorner,
}

interface ObjectToolExtra extends ExtraSnapBase {
    ObjectSideSnapMode: ObjectSideSnapMode;
}

function ObjectToolExtraSnapRenderer(extraSnap: ObjectToolExtra): JSX.Element {
    const { translate } = useLocalization();

    const isAvailable = (flag: ObjectToolExtraSnap) => (extraSnap.SnapOnMask & flag) !== 0;
    const isForced = (flag: ObjectToolExtraSnap) => isAvailable(flag) && (extraSnap.SnapOffMask & flag) === 0;
    const isSelected = (flag: ObjectToolExtraSnap) => (extraSnap.SelectedSnap & flag) !== 0;

    const SetObjectSideSnapMode = (snapMode: ObjectSideSnapMode) =>
    {
        extraSnap.Trigger(`SetObjectSideSnapMode`, snapMode);
    }


    // Mask math can involve the high bit (SelectedSnap starts as ALL minus a couple of flags), and JS
    // bitwise operators return signed 32-bit results - >>> 0 forces the result back to the unsigned
    // representation the underlying C# uint expects.
    const toggleFlag = (flag: ObjectToolExtraSnap) => {
        const current = extraSnap.SelectedSnap;
        const next = isSelected(flag) ? current & ~flag : current | flag;
        setExtraSnap(next >>> 0);
    };

    return (

        <SectionOrFoldout 
            title={translate("ExtraSnap.Title", "Extra Snap")}
            persistKey="ExtraSnap"
            headerRight={
                <>
                    {isAvailable(ObjectToolExtraSnap.ObjectSurface) ?
                        <ToolButton
                            focusKey={FOCUS_DISABLED$}
                            tooltip={translate("ExtraSnap.ObjectSurface.Tooltip", "Snap to the surface of any object.")}
                            src="Media/Tools/Snap Options/ObjectSurface.svg"
                            selected={isSelected(ObjectToolExtraSnap.ObjectSurface)}
                            disabled={isForced(ObjectToolExtraSnap.ObjectSurface)}
                            onSelect={() => toggleFlag(ObjectToolExtraSnap.ObjectSurface)}
                        /> : <></>
                    }

                    {isAvailable(ObjectToolExtraSnap.Upright) ?
                        <ToolButton
                            focusKey={FOCUS_DISABLED$}
                            tooltip={translate("ExtraSnap.Upright.Tooltip", "Keep the object upright while snapping to a surface.")}
                            src="Media/Tools/Snap Options/Upright.svg"
                            selected={isSelected(ObjectToolExtraSnap.Upright)}
                            disabled={isForced(ObjectToolExtraSnap.Upright)}
                            onSelect={() => toggleFlag(ObjectToolExtraSnap.Upright)}
                        /> : <></>
                    }

                    {isAvailable(ObjectToolExtraSnap.ObjectSide) ?
                        <ToolButton
                            focusKey={FOCUS_DISABLED$}
                            tooltip={translate("ExtraSnap.ObjectSide.Tooltip", "Snap and align the object flush against the side of the nearest object.")}
                            src="Media/Tools/Snap Options/ObjectSide.svg"
                            selected={isSelected(ObjectToolExtraSnap.ObjectSide)}
                            disabled={isForced(ObjectToolExtraSnap.ObjectSide)}
                            onSelect={() => toggleFlag(ObjectToolExtraSnap.ObjectSide)}
                        /> : <></>
                    }
                </>
            }
        >

            {isAvailable(ObjectToolExtraSnap.ObjectSide) ? <Section
                title={translate("ExtraSnap.ObjectSideSnapMode.Title", "Object Side Snap Mode")}
            >
                        
                <ToolButton
                    focusKey={FOCUS_DISABLED$}
                    tooltip={translate("ExtraSnap.ObjectSide.FreeMove.Tooltip", "Allow free movement of the object without snapping.")}
                    src="coui://extradetailingtools/Icons/ExtraSnap/FreeMove.svg"
                    selected={extraSnap.ObjectSideSnapMode === ObjectSideSnapMode.FreeMove}
                    disabled={!isSelected(ObjectToolExtraSnap.ObjectSide)}
                    onSelect={() => SetObjectSideSnapMode(ObjectSideSnapMode.FreeMove)}
                    className={Styles.toolButtonFoldout}
                />

                <ToolButton
                    focusKey={FOCUS_DISABLED$}
                    tooltip={translate("ExtraSnap.ObjectSide.SnapToCenter.Tooltip", "Snap and align the object to the center of the nearest side of the nearest object.")}
                    src="coui://extradetailingtools/Icons/ExtraSnap/SnapToCenter.svg"
                    selected={extraSnap.ObjectSideSnapMode === ObjectSideSnapMode.SnapToCenter}
                    disabled={!isSelected(ObjectToolExtraSnap.ObjectSide)}
                    onSelect={() => SetObjectSideSnapMode(ObjectSideSnapMode.SnapToCenter)}
                    className={Styles.toolButtonFoldout}
                />

                <ToolButton
                    focusKey={FOCUS_DISABLED$}
                    tooltip={translate("ExtraSnap.ObjectSide.SnapToCorner.Tooltip", "Snap and align the object flush against the corner of the nearest object.")}
                    src="coui://extradetailingtools/Icons/ExtraSnap/SnapToCorner.svg"
                    selected={extraSnap.ObjectSideSnapMode === ObjectSideSnapMode.SnapToCorner}
                    disabled={!isSelected(ObjectToolExtraSnap.ObjectSide)}
                    onSelect={() => SetObjectSideSnapMode(ObjectSideSnapMode.SnapToCorner)}
                    className={Styles.toolButtonFoldout}
                />
                        
            </Section> : <></> }

        </SectionOrFoldout>
    );
}

registerExtraSnapRenderer(kObjectToolExtraSnapType, ObjectToolExtraSnapRenderer);
