import { Fragment, ReactNode, isValidElement } from "react";
import { Section } from "../../../game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options";
import { SectionFoldout, SectionFoldoutProps } from "./SectionFoldout";

// Detects "nothing would actually render" without requiring a different style of conditional content -
// handles both `{condition && <X/>}` (a falsy child) and `condition ? <X/> : <></>` (an empty Fragment),
// recursively, since both patterns show up across this codebase. Can't see into a custom component's own
// eventual render output, so this only catches emptiness expressed directly in the children passed here.
function isEmptyChildren(node: ReactNode): boolean {
    if (node === null || node === undefined || node === false || node === true || node === "") {
        return true;
    }
    if (Array.isArray(node)) {
        return node.every(isEmptyChildren);
    }
    if (isValidElement(node) && node.type === Fragment) {
        return isEmptyChildren((node.props as { children?: ReactNode }).children);
    }
    return false;
}

export interface SectionOrFoldoutProps extends Omit<SectionFoldoutProps, "title"> {
    title: string | null;
}

// Renders as a plain (non-collapsible) Section when there's nothing inside worth folding away, and as a
// real SectionFoldout once there's actual content - so an empty foldout never shows a pointless expand
// arrow for a section that has nothing behind it.
export function SectionOrFoldout(props: SectionOrFoldoutProps): JSX.Element {
    const { title, children } = props;

    if (isEmptyChildren(children)) {
        return <Section title={title}>{children as string | JSX.Element | JSX.Element[]}</Section>;
    }

    return <SectionFoldout {...props} />;
}
