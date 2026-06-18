import { Pie } from "react-chartjs-2";
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from "chart.js";
import type { MediaAttributeStats } from "../models";
import { useEffect, useState, type FormEvent } from "react";
import { Button, Form, Card, Alert, ListGroup } from "react-bootstrap";
import { useSearchParams } from "react-router-dom";

ChartJS.register(ArcElement, Tooltip, Legend);

interface MediaAttributeChartOptions {
    outputType: string | 'MediaCount' | 'PlayCount',
    attributeType: string | 'albums' | 'genres' | 'artists'
}

function getColorForString(stringValue: string): string {
    let hash = 0;

    for (let i = 0; i < stringValue.length; i++) {
        hash = stringValue.charCodeAt(i) + ((hash << 5) - hash);
        hash |= 0;
    }

    const normalizedHash = Math.abs(hash);
    const hue = normalizedHash % 360;
    const saturation = 60 + (normalizedHash % 20);
    const lightness = 45 + (normalizedHash % 15);

    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
}

const MediaAttributePieChart = ({ stats, options }: { stats: MediaAttributeStats[]; options: MediaAttributeChartOptions }) => {
    const data = {
        labels: stats.map(s => s.attributeValue),
        datasets: [
            {
                label: options.outputType === 'MediaCount' ? "Count" : "Plays",
                data: stats.map(s => options.outputType === 'MediaCount' ? s.mediaCount : s.playCount),
                backgroundColor: stats.map(s => getColorForString(s.attributeValue)),
            },
        ],
    };

    const chartOptions = {
        plugins: {
            title: {
                text: `Media by ${options.attributeType.charAt(0).toUpperCase() + options.attributeType.slice(1)}`,
                align: "center" as const,
                position: "top" as const,
                display: true,
                font: {
                    size: 20,
                },
            },
            legend: {
                position: 'left' as const
            }
        }
    }

    return <Pie data={data} options={chartOptions} />;
}

export default function StatsTab() {
    const [stats, setStats] = useState<MediaAttributeStats[]>(
        [
            {
                mediaCount: 0,
                playCount: 0,
                attributeValue: "",
                lastPlayed: null
            }
        ]);

    const [error, setError] = useState<string | null>(null);

    const [searchParams, setSearchParams] = useSearchParams();

    const filter = searchParams.get('filter') ?? '';
    const mediaAttributeType = searchParams.get('attributeType') ?? 'albums' as MediaAttributeChartOptions['attributeType'];
    const mediaAttributeOutputType = searchParams.get('outputType') ?? 'MediaCount' as MediaAttributeChartOptions['outputType'];

    useEffect(() => {
        setPendingFilter(filter);
        setPendingMediaAttributeType(mediaAttributeType);
        setPendingMediaAttributeOutputType(mediaAttributeOutputType);
    }, [filter, mediaAttributeType, mediaAttributeOutputType]);

    const [pendingFilter, setPendingFilter] = useState(filter);
    const [pendingMediaAttributeType, setPendingMediaAttributeType] = useState(mediaAttributeType);
    const [pendingMediaAttributeOutputType, setPendingMediaAttributeOutputType] = useState(mediaAttributeOutputType);

    useEffect(() => {
        const fetchStats = async () => {
            const response = await fetch(`api/stats/${mediaAttributeType}?filter=${encodeURIComponent(filter)}&sortBy=${(mediaAttributeOutputType === 'PlayCount' ? 'plays' : 'count')}&limit=20`);

            if (response.status !== 200) {
                const error = await response.json();
                console.error(`Failed to fetch stats: ${error.details}`);
                setError(`Failed to fetch stats: ${error.details}`);
                return;
            }

            const data: MediaAttributeStats[] = await response.json();
            setStats(data);
            setError(null);
        };

        fetchStats();
    }, [mediaAttributeType, filter, mediaAttributeOutputType]);

    const handleChartOptionsSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        const nextParams = new URLSearchParams(searchParams);

        if (pendingFilter.trim()) {
            nextParams.set('filter', pendingFilter);
        } else {
            nextParams.delete('filter');
        }

        nextParams.set('attributeType', pendingMediaAttributeType);
        nextParams.set('outputType', pendingMediaAttributeOutputType);

        setSearchParams(nextParams);
    };

    const pieChartOptions: MediaAttributeChartOptions = {
        attributeType: mediaAttributeType,
        outputType: mediaAttributeOutputType
    }

    return (
        <div
            className="d-flex gap-4"
            style={{
                flex: 1,
                minHeight: 0,
                height: '90vh',
                overflow: 'hidden',
                boxSizing: 'border-box',
                paddingBottom: '50px'
            }}
        >
            <Card
                style={{
                    width: '20vw'
                }}>
                <Card.Header>Statistics</Card.Header>
                <Card.Body>

                    <ListGroup variant="flush">
                        <ListGroup.Item>
                            <strong>Media Count:</strong> {stats.reduce((acc, stat) => acc + stat.mediaCount, 0)}
                        </ListGroup.Item>

                        {error && (
                            <ListGroup.Item>
                                <Alert variant="danger">{error}</Alert>
                            </ListGroup.Item>
                        )}

                        <ListGroup.Item>
                            <Form onSubmit={handleChartOptionsSubmit}>

                                <Form.Group className="mb-3" controlId="mediaFilter">
                                    <Form.Label>Filter</Form.Label>
                                    <Form.Control
                                        type="text"
                                        placeholder="Enter filter"
                                        value={pendingFilter}
                                        onChange={(e) => setPendingFilter(e.target.value)}
                                    />
                                </Form.Group>

                                <Form.Group className="mb-3" controlId="mediaAttributeType">
                                    <Form.Label>Attribute Type</Form.Label>
                                    <Form.Select
                                        value={pendingMediaAttributeType}
                                        onChange={(e) => setPendingMediaAttributeType(e.target.value)}
                                    >
                                        <option value="albums">Albums</option>
                                        <option value="genres">Genres</option>
                                        <option value="artists">Artists</option>
                                    </Form.Select>
                                </Form.Group>

                                <Form.Group className="mb-3" controlId="mediaAttributeOutputType">
                                    <Form.Label>Output Type</Form.Label>
                                    <Form.Select
                                        value={pendingMediaAttributeOutputType}
                                        onChange={(e) => setPendingMediaAttributeOutputType(e.target.value)}
                                    >
                                        <option value="MediaCount">Media Count</option>
                                        <option value="PlayCount">Play Count</option>
                                    </Form.Select>
                                </Form.Group>

                                <Button variant="success" type="submit">
                                    Update Chart
                                </Button>
                            </Form>
                        </ListGroup.Item>
                    </ListGroup>
                </Card.Body>
            </Card>

            <div
                style={{
                    flex: '1 1 0',
                    width: '80vw',
                    height: '100%',
                    overflow: 'hidden',
                    display: 'flex',
                    boxSizing: 'border-box',
                    justifyContent: 'center',
                }}
            >
                <MediaAttributePieChart stats={stats} options={pieChartOptions} />

            </div>
        </div>
    );
}