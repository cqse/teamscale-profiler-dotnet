import { html, render, useEffect, useState } from './htm-preact/standalone.js';

function Main() {
    return html`
        <main class="d-flex align-items-center justify-content-center flex-column">
            <${Header}/>
            <${Controls}/>
            <${Footer}/>
        </main>`;
}

function Header() {
    return html`
        <header>
            <img class="mb-2" src="./logo.svg" alt="" width="200" height="72" />
            <h1 class="h3 mb-4 fw-bold">TIA Commander</h1>
        </header>`;
}

function Footer() {
    return html`
        <p class="text-muted">
            © <a class="link-secondary" href="https://teamscale.com">CQSE GmbH</a>
        </p>`;
}

function Controls() {
    const [running, setRunning] = useState(false);
    const [testName, setTestName] = useState("");
    const [status, setStatus] = useState({});

    function onTestNameChanged(e) {
        setTestName(e.target.value);
    }

    async function startTest() {
        setRunning(true);
        const response = await fetch("test/start/" + encodeURIComponent(testName), { method: 'POST' });
        if (!response.ok) {
            setStatus({ title: "Request error", text: response.statusText });
            return;
        }
    }

    async function stopTest(e) {
        setRunning(false);
        const result = e.target.value;
        setStatus();
        setStatus({ title: "Last test result", text: result + ": " + testName });

        const response = await fetch("test/stop/" + result, { method: 'POST' });
        if (!response.ok) {
            setStatus({ title: "Request error", text: response.statusText });
            return;
        }
    }

    async function loadCurrentTest() {
        const response = await fetch("test");
        if (!response.ok) {
            setStatus({ title: "Request error", text: response.statusText });
            return;
        }

        const testName = await response.text()
        if (testName) {
            setTestName(testName);
            setRunning(true);
        }
    }

    useEffect(loadCurrentTest, []);

    return html`
        <form class="mb-4">
            <div class="form-floating mb-4">
                <input type="text" class="form-control" id="testName" placeholder="Test Name" autofocus value=${testName} onInput=${onTestNameChanged} disabled=${running}/>
                <label for="testName">Test Name</label>
            </div>
            <button type="button" class="w-100 btn btn-lg btn-primary" onClick=${startTest} hidden=${running} disabled=${!testName}>Start</button>
            <div class="input-group" role="group" hidden=${!running}>
                <button type="button" class="form-control btn btn-lg btn-success" value="PASSED" onClick=${stopTest}>Success</button>
                <button type="button" class="form-control btn btn-lg btn-danger" value="FAILURE" onClick=${stopTest}>Failure</button>
                <button type="button" class="form-control btn btn-lg btn-warning" value="SKIPPED" onClick=${stopTest}>Skip</button>
            </div>
            <div class="form-floating mt-4" hidden=${!status}>
                <p class="h6">${status.title}</p>
                <p class="text-muted mb-0">${status.text}</p>
            </div>
        </form>`;
}

render(html`<${Main}/>`, document.body);