"use client";

import { useMemo } from "react";
import DOMPurify from "dompurify";

interface RichTextContentProps {
  html: string;
  className?: string;
}

export function RichTextContent({ html, className }: RichTextContentProps) {
  const safeHtml = useMemo(() => DOMPurify.sanitize(html ?? ""), [html]);

  return (
    <div
      className={"rich-text " + (className ?? "")}
      dangerouslySetInnerHTML={{ __html: safeHtml }}
    />
  );
}
