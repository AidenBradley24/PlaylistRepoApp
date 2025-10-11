import { Form } from "react-bootstrap";
import type { Media, Patch } from "../models";
import MassOperationModal from "./MassOperationModal";
import { useRefresh } from "../components/RefreshContext"

interface MassOperationMediaProps {
    massUrl: string;
    massDeleting: boolean;
    setMassDeleting: (value: boolean) => void;
    massPatching: boolean;
    setMassPatching: (value: boolean) => void;
    query: string;
}

const MassOperationMedia: React.FC<MassOperationMediaProps> = ({ massUrl, massDeleting, massPatching, setMassDeleting, setMassPatching, query }) => {

    const { triggerRefresh } = useRefresh()!;

    return (
        <>
            <MassOperationModal
                title="Mass Media Deletion"
                show={massDeleting}
                onHide={() => setMassDeleting(false)}
                initialValues={{ userQuery: query, alsoDeleteFile: true }}
                onSubmit={async (values) => {
                    const response = await fetch(massUrl, {
                        method: "DELETE",
                        headers: { "query": values.userQuery, "alsoDeleteFile": values.alsoDeleteFile ? "true" : "false" }
                    });
                    if (!response.ok) {
                        throw new Error(await response.text());
                    }
                    triggerRefresh();
                }}
            >
                {(values, updateField) => (
                    <Form.Group className="mb-3">
                        <Form.Check id="alsoDeleteFile" label="also delete file" checked={values.alsoDeleteFile} onChange={e => updateField("alsoDeleteFile", e.target.value === "true")} />
                    </Form.Group>
                )}
            </MassOperationModal>
            <MassOperationModal<Patch<Media>>
                title="Mass Edit Fields"
                show={massPatching}
                onHide={() => setMassPatching(false)}
                initialValues={{ userQuery: query, propertyName: "title", propertyValue: "", type: "replace" }}
                onSubmit={async (values) => {
                    const response = await fetch(massUrl, {
                        method: "PATCH",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(values),
                    });
                    if (!response.ok) {
                        throw new Error(await response.text());
                    }
                    triggerRefresh();
                }}
            >
                {(values, updateField) => (
                    <>
                        {/* Property selector */}
                        <Form.Group className="mb-3">
                            <Form.Label>Property to Edit</Form.Label>
                            <Form.Select
                                value={values.propertyName}
                                onChange={(e) => {
                                    const key = e.target.value as keyof Media;
                                    updateField("propertyName", key);
                                    let defaultVal: any = "";
                                    if (key === "rating") defaultVal = "0";
                                    else if (key === "locked") defaultVal = "false";
                                    updateField("propertyValue", defaultVal);
                                    if (key === "rating" ||
                                        key === "lengthMilliseconds" ||
                                        key === "order" ||
                                        key === "locked")
                                        updateField("type", "replace");
                                }}
                            >
                                {(
                                    [
                                        "title",
                                        "primaryArtist",
                                        "artists",
                                        "album",
                                        "description",
                                        "rating",
                                        "order",
                                        "locked",
                                        "genre"
                                    ] as (keyof Media)[]
                                ).map((key) => (
                                    <option key={key} value={key}>
                                        {key}
                                    </option>
                                ))}
                            </Form.Select>
                        </Form.Group>

                        <Form.Group className="mb-3">
                            <Form.Label>Modification Type</Form.Label>
                            <Form.Select
                                disabled={
                                    values.propertyName === "rating" ||
                                    values.propertyName === "lengthMilliseconds" ||
                                    values.propertyName === "order" ||
                                    values.propertyName === "locked"
                                }
                                value={values.type}
                                onChange={(e) => updateField("type", e.target.value as any)}
                            >
                                {["replace", "append", "prepend"].map((key) =>
                                (
                                    <option key={key} value={key}>
                                        {key}
                                    </option>
                                ))}
                            </Form.Select>
                        </Form.Group>

                        {/* Property value input (dynamic based on type) */}
                        <Form.Group className="mb-3">
                            <Form.Label>Modification</Form.Label>
                            {(() => {
                                switch (values.propertyName) {
                                    case "rating":
                                    case "lengthMilliseconds":
                                    case "order":
                                        return (
                                            <Form.Control
                                                type="number"
                                                value={Number(values.propertyValue)}
                                                onChange={(e) =>
                                                    updateField("propertyValue", String(e.target.value))
                                                }
                                            />
                                        );
                                    case "artists":
                                        return (
                                            <Form.Control
                                                type="text"
                                                value={(values.propertyValue as string[] | undefined)?.join(", ") ?? ""}
                                                onChange={(e) =>
                                                    updateField(
                                                        "propertyValue",
                                                        e.target.value
                                                            .split(",")
                                                            .map((s) => s.trim())
                                                            .filter(Boolean)
                                                    )
                                                }
                                                placeholder="Comma separated list of artists"
                                            />
                                        );
                                    case "locked":
                                        return (
                                        <Form.Check
                                            type="switch"
                                            label="Locked"
                                            checked={values.propertyValue === "true"}
                                            onChange={(e) => {
                                                updateField("propertyValue", String(e.target.checked))
                                            }}
                                        />)
                                    default:
                                        return (
                                            <Form.Control
                                                type="text"
                                                value={(values.propertyValue as string) ?? ""}
                                                onChange={(e) =>
                                                    updateField("propertyValue", e.target.value)
                                                }
                                            />
                                        );
                                }
                            })()}
                        </Form.Group>
                    </>
                )}
            </MassOperationModal>
        </>
    );
}

export default MassOperationMedia;