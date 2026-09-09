const selector = "time[data-local-date-time]";
const pad = value => String(value).padStart(2, "0");
const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

function formatLocalDateTime(element) {
    const date = new Date(element.dateTime);
    if (Number.isNaN(date.getTime())) {
        return;
    }

    const datePart = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    const timePart = `${pad(date.getHours())}:${pad(date.getMinutes())}`;
    const seconds = element.dataset.precision === "minute" ? "" : `:${pad(date.getSeconds())}`;

    element.textContent = `${datePart} ${timePart}${seconds}`;
    element.title = timeZone;
}

function formatWithin(root) {
    if (root instanceof Element && root.matches(selector)) {
        formatLocalDateTime(root);
    }

    root.querySelectorAll?.(selector).forEach(formatLocalDateTime);
}

formatWithin(document);

new MutationObserver(mutations => {
    for (const mutation of mutations) {
        if (mutation.type === "attributes") {
            formatLocalDateTime(mutation.target);
            continue;
        }

        mutation.addedNodes.forEach(formatWithin);
    }
}).observe(document.body, {
    attributes: true,
    attributeFilter: ["datetime"],
    childList: true,
    subtree: true
});
