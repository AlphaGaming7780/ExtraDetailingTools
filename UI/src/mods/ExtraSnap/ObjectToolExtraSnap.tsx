import { Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { ToolButton } from "../../../game-ui/game/components/tool-options/tool-button/tool-button";
import { FOCUS_DISABLED$ } from "../../../game-ui/common/focus/focus-key";
import { ExtraSnapBase, registerExtraSnapRenderer, setExtraSnap } from "./ExtraSnap";
import { useLocalization } from "cs2/l10n";

const kObjectToolExtraSnapType = "ExtraDetailingTools.ExtraSnap.ObjectToolSystemExtraSnap";

// Mirrors ObjectToolSystemExtraSnap.ObjectToolExtraSnap in ObjectTool.cs.
enum ObjectToolExtraSnap {
    ObjectSurface = 1 << 0,
    Upright = 1 << 1,
    ObjectSide = 1 << 2,
}

function ObjectToolExtraSnapRenderer(extraSnap: ExtraSnapBase): JSX.Element {
    const { translate } = useLocalization();

    const isAvailable = (flag: ObjectToolExtraSnap) => (extraSnap.SnapOnMask & flag) !== 0;
    const isForced = (flag: ObjectToolExtraSnap) => isAvailable(flag) && (extraSnap.SnapOffMask & flag) === 0;
    const isSelected = (flag: ObjectToolExtraSnap) => (extraSnap.SelectedSnap & flag) !== 0;

    // Mask math can involve the high bit (SelectedSnap starts as ALL minus a couple of flags), and JS
    // bitwise operators return signed 32-bit results - >>> 0 forces the result back to the unsigned
    // representation the underlying C# uint expects.
    const toggleFlag = (flag: ObjectToolExtraSnap) => {
        const current = extraSnap.SelectedSnap;
        const next = isSelected(flag) ? current & ~flag : current | flag;
        setExtraSnap(next >>> 0);
    };

    return (
        <Section title={translate("ExtraSnap.Title", "Extra Snap")}>
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
        </Section>
    );
}

registerExtraSnapRenderer(kObjectToolExtraSnapType, ObjectToolExtraSnapRenderer);
