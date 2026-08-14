import { ModuleRegistryExtend } from "cs2/modding";
import { TypedRenderer } from "../../../game-ui/common/typed-renderer/typed-renderer";
import { bindValue, trigger, useValue } from "cs2/api";
import { Typed } from "cs2/bindings";
import { useMemo } from "react";
import { kGroupName } from "../../BindingConst";

export interface ExtraSnapData extends Typed<""> {
    SnapOnMask: number;
    SnapOffMask: number;
    SelectedSnap: number;
}

export interface ExtraSnapBase extends ExtraSnapData {
    Trigger(key: string, ...args: any[]): void;
}

function withTrigger(data: ExtraSnapData | null): ExtraSnapBase | null {
    if (!data) return null;
    return {
        ...data,
        Trigger: (key: string, ...args: any[]) => trigger(kGroupName, `${data.__Type}.${key}`, ...args),
    };
}

export const activeExtraSnapData$ = bindValue<ExtraSnapData | null>(kGroupName, "ActiveExtraSnap", null);

export function useActiveExtraSnap(): ExtraSnapBase | null {
    const data = useValue(activeExtraSnapData$);
    return useMemo(() => withTrigger(data), [data]);
}

export function setExtraSnap(value: number) {
    trigger(kGroupName, "SetExtraSnap", value);
}

// Each ExtraSnapBase subclass (identified by its C# GetType().FullName, i.e. __Type) registers its
// own renderer here instead of this file needing to know about every subclass that will ever exist.
const extraSnapRenderers: { [type: string]: (props: any) => JSX.Element } = {};

export function registerExtraSnapRenderer(type: string, component: (props: any) => JSX.Element) {
    extraSnapRenderers[type] = component;
}

// C# already resolves which (if any) registered ExtraSnapBase matches the active tool
// (ExtraSnapUISystem.OnToolChanged) - a null extraSnap here just means "nothing for this tool",
// no need to re-check activeTool.id like TransformGizmoTool does.
export const ExtraSnapToolOptions: ModuleRegistryExtend = (Component: any) => {
    return (props) => {
        const extraSnap = useActiveExtraSnap();

        var result: JSX.Element = Component();

        if (!extraSnap) return result;

        result.props.children?.unshift(
            <TypedRenderer components={extraSnapRenderers} data={extraSnap} props={{}} />
        );

        return result;
    };
};
