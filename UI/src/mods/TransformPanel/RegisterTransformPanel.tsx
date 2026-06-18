import { useEffect } from "react";
import { registry } from "index";
import { TransformExtraPanel } from "mods/TransformExtraPanel/TransformExtraPanel";

export const RegisterTransformPanel = () => {

    useEffect(() => {
        console.log("Registering TransformExtraPanel...");
        if(registry.registry.has("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot"))
        {
            registry.extend("ExtraLib/ExtraPanels/ExtraPanelsRoot/ExtraPanelsRoot", "extraPanelsComponents", TransformExtraPanel)
            console.log("TransformExtraPanel registered.");
        }
        else
            console.warn("ExtraPanelsRoot not found, TransformExtraPanel will not be loaded");
    }, []);

    return null;
};
