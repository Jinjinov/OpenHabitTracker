// The account requests go from the browser, so a response can set or clear the login cookie,
// which a request sent from the server over the circuit cannot.
export async function send(method, url, body) {
    const response = await fetch(url, {
        method: method,
        credentials: "same-origin",
        headers: body === null ? {} : { "Content-Type": "application/json" },
        body: body === null ? null : JSON.stringify(body)
    });

    return { status: response.status, body: await response.text() };
}
