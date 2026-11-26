async function postActivity(url, csrfToken, activity) {
    try {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            redirect: "manual",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": csrfToken
            },
            body: JSON.stringify(activity)
        });

        if (response.type === "opaqueredirect") {
            return 401;
        }

        return response.status;
    } catch {
        return null;
    }
}
