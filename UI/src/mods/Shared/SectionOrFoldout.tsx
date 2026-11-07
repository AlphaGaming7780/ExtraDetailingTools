import { Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { SectionFoldout, SectionFoldoutProps, isEmptyChildren } from "./SectionFoldout";

export interface SectionOrFoldoutProps extends Omit<SectionFoldoutProps, "title"> {
    title: string | null;
}

// Plain Section when empty, SectionFoldout otherwise.
export function SectionOrFoldout(props: SectionOrFoldoutProps): JSX.Element {
    const { title, children, headerRight } = props;

    if (isEmptyChildren(children)) {
        return <Section title={title}>{headerRight as string | JSX.Element | JSX.Element[]}</Section>;
    }

    return <SectionFoldout {...props} />;
}
