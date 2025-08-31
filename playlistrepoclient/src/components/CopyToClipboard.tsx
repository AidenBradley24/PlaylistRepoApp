import React, { useState } from "react";
import { Spinner } from "react-bootstrap";
import { BsCheckCircle } from "react-icons/bs";

type CopyToClipboardButtonProps = {
    getText: () => Promise<string>;
    children: React.ReactNode; // Your icon (e.g., from react-icons)
};

export const CopyToClipboardButton: React.FC<CopyToClipboardButtonProps> = ({
    getText,
    children,
}) => {
    const [status, setStatus] = useState<"idle" | "loading" | "copied">("idle");

    const handleClick = async () => {
        try {
            setStatus("loading");
            const text = await getText();
            await navigator.clipboard.writeText(text);
            setStatus("copied");

            // Reset back to idle after 2s
            setTimeout(() => setStatus("idle"), 2000);
        } catch (err) {
            console.error("Failed to copy text:", err);
            setStatus("idle");
        }
    };

    return (
        <button
            onClick={handleClick}
            className="btn btn-outline-primary d-flex align-items-center gap-2"
            disabled={status === "loading"}
        >
            {status === "idle" && children}
            {status === "loading" && <Spinner animation="grow" size="sm" />}
            {status === "copied" && (
                <>
                    <BsCheckCircle /> Copied to clipboard
                </>
            )}
        </button>
    );
};
