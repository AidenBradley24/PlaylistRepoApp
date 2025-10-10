import Accordion from 'react-bootstrap/Accordion';

const HomePage: React.FC = () => {

    return (
        <div className="page-content-narrow">
            <br />
            <h1>Welcome to your media repository!</h1>
            <br />
            <Accordion defaultActiveKey="0">
                <Accordion.Item eventKey="0">
                    <Accordion.Header>Basic Instructions</Accordion.Header>
                    <Accordion.Body>
                        <h4>Create a Repo</h4>
                        <p>To create a Playlist Repo run <code>playlistrepo init</code> inside of a directory within the terminal.</p>
                        <hr />
                        <h4>Media Tab</h4>
                        <p>The media tab contains all the media inside of the repository directory.</p>
                        <p>To add media, simply drag media files into the media window.
                            You can also move media files directly into the repo folder and click refresh in the options menu.</p>
                        <hr />
                        <h4>Playlists Tab</h4>
                        <p>The playlists tab allows the creation of collections of media. These collections can be exported into various formats.</p>
                        <hr />
                        <h4>Remote Playlists Tab</h4>
                        <p>The remote playlists tab allows the importation of playlists and media from external platforms such as YouTube.</p>
                        <p>Uses <a href="https://github.com/yt-dlp/yt-dlp">YT-DLP</a> to download media.</p>
                    </Accordion.Body>
                </Accordion.Item>
                <Accordion.Item eventKey="1">
                    <Accordion.Header>Searching and querying media</Accordion.Header>
                    <Accordion.Body>
                        <p>
                            You can use queries to search and filter media using simple expressions.
                            Queries let you match, compare, and order results in powerful ways.
                        </p>

                        <h5>Basic operators</h5>
                        <ul>
                            <li><code>=</code> equals</li>
                            <li><code>!=</code> does not equal</li>
                            <li><code>^</code> starts with</li>
                            <li><code>!^</code> does not start with</li>
                            <li><code>$</code> ends with</li>
                            <li><code>!$</code> does not end with</li>
                            <li><code>*</code> contains</li>
                            <li><code>!*</code> does not contain</li>
                            <li><code>&lt;</code> less than</li>
                            <li><code>&gt;</code> greater than</li>
                            <li><code>&lt;=</code> less than or equal</li>
                            <li><code>&gt;=</code> greater than or equal</li>
                        </ul>
                        <p>
                            Use a comma <code>,</code> to mean "OR" between terms, and an ampersand <code>&amp;</code> to mean "AND."
                        </p>

                        <h5>Using values</h5>
                        <ul>
                            <li>
                                Text values should be wrapped in quotes (<code>"example"</code> or <code>'example'</code>).
                                Text searches are not case-sensitive.
                            </li>
                            <li>
                                Numbers don't need quotes. Both whole numbers and decimals are supported
                                (<code>42</code>, <code>3.14</code>, <code>-123</code>).
                            </li>
                        </ul>

                        <h5>Ordering results</h5>
                        <p>
                            At the end of your query, you can add <code>orderby</code> or <code>orderbydescending</code>
                            {" "} followed by a property name to sort the results.
                        </p>

                        <code>
                            name * "waffles"
                        </code>
                        <p>Finds items where the name contains "waffles."</p>
                        <hr />

                        <code>
                            rating &lt; 10 &amp; type = "audio/mp3"
                        </code>
                        <p>
                            Finds items with a rating below 10 <em>and</em> a type of exactly "audio/mp3."
                        </p>
                        <hr />

                        <code>
                            length &lt; "01:00:00" orderby length
                        </code>
                        <p>
                            Finds items shorter than one hour, sorted by length from shortest to longest.
                        </p>
                        <br />
                        <h5>Property shortcuts and ranges</h5>
                        <p>
                            You can set a property to be used for all following comparisons by adding a colon.
                            For example:
                        </p>
                        <code>
                            rating: &lt; 2, &gt; 8
                        </code>
                        <p>Finds items with a rating below 2 or above 8.</p>
                        <hr />
                        <p>You can also use a dash <code>-</code> for ranges:</p>
                        <code>
                            rating: 1-5
                        </code>
                        <p>Finds items with a rating between 1 and 5.</p>
                    </Accordion.Body>
                </Accordion.Item>

            </Accordion>
        </div>

    );
}

export default HomePage;