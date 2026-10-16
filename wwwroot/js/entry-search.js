
(() => {
    const search = document.getElementById("testSearch");
    const results = document.getElementById("testSearchResults");

    if (!search || !results) return;

    const fields = Array.from(document.querySelectorAll(".entry-field"));
    let matches = [];
    let activeIndex = 0;
    let searchOpen = false;
    let lastTarget = null;

    const normalize = value =>
        (value || "").toLocaleLowerCase().trim();

    function getFieldInput(field) {
        // Prefer the actual editable input, not a hidden input used for binding.
        return field.querySelector(
            'input:not([type="hidden"]):not([type="radio"]):not([type="checkbox"]):not([disabled]),' +
            'select:not([disabled]), textarea:not([disabled]),' +
            'input[type="radio"]:not([disabled]), input[type="checkbox"]:not([disabled])'
        );
    }

    function openSearch() {
        searchOpen = true;
        results.hidden = false;
        search.setAttribute("aria-expanded", "true");
    }

    function closeSearch({ clear = false } = {}) {
        searchOpen = false;
        results.hidden = true;
        results.replaceChildren();
        search.setAttribute("aria-expanded", "false");

        if (clear) {
            search.value = "";
            matches = [];
            activeIndex = 0;
        }
    }

    function renderResults() {
        const query = normalize(search.value);
        results.replaceChildren();

        if (!query) {
            matches = [];
            closeSearch();
            return;
        }

        matches = fields.filter(field => {
            const name = normalize(field.dataset.testName);
            const code = normalize(field.dataset.testCode);
            return name.includes(query) || code.includes(query);
        });

        activeIndex = Math.min(activeIndex, Math.max(matches.length - 1, 0));
        openSearch();

        if (matches.length === 0) {
            const empty = document.createElement("div");
            empty.className = "list-group-item text-muted";
            empty.textContent = "No matching tests found";
            results.appendChild(empty);
            return;
        }

        matches.forEach((field, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "list-group-item list-group-item-action";
            button.setAttribute("role", "option");
            button.setAttribute("aria-selected", String(index === activeIndex));

            if (index === activeIndex) button.classList.add("active");

            const name = field.dataset.testName || "Unnamed test";
            const code = field.dataset.testCode || "";
            const unit = field.dataset.unit || "";

            const title = document.createElement("div");
            title.className = "fw-semibold";
            title.textContent = name;

            const detail = document.createElement("small");
            detail.className = "text-body-secondary";
            detail.textContent = [code, unit].filter(Boolean).join(" · ");

            button.append(title, detail);

            button.addEventListener("click", () => selectField(index));
            button.addEventListener("mouseenter", () => {
                activeIndex = index;
                updateActiveResult();
            });

            results.appendChild(button);
        });
    }

    function updateActiveResult() {
        Array.from(results.querySelectorAll("button")).forEach((button, index) => {
            const active = index === activeIndex;
            button.classList.toggle("active", active);
            button.setAttribute("aria-selected", String(active));
        });

        results.querySelectorAll("button")[activeIndex]
            ?.scrollIntoView({ block: "nearest" });
    }

    function selectField(index) {
        const field = matches[index];
        if (!field) return;

        const input = getFieldInput(field);
        if (!input) return;

        if (lastTarget) lastTarget.classList.remove("search-target");
        lastTarget = field;
        field.classList.add("search-target");

        closeSearch({ clear: true });
        input.scrollIntoView({ behavior: "smooth", block: "center" });

        // Wait for scrolling to start before focusing the field.
        requestAnimationFrame(() => input.focus({ preventScroll: true }));
    }

    search.addEventListener("input", () => {
        activeIndex = 0;
        renderResults();
    });

    search.addEventListener("keydown", event => {
        if (event.key === "ArrowDown" && matches.length) {
            event.preventDefault();
            activeIndex = (activeIndex + 1) % matches.length;
            updateActiveResult();
        } else if (event.key === "ArrowUp" && matches.length) {
            event.preventDefault();
            activeIndex = (activeIndex - 1 + matches.length) % matches.length;
            updateActiveResult();
        } else if (event.key === "Enter") {
            event.preventDefault();
            if (matches.length) selectField(activeIndex);
        } else if (event.key === "Escape") {
            event.preventDefault();
            closeSearch({ clear: true });
            search.blur();
        } else if (event.key === "Tab" && searchOpen && matches.length) {
            // Let Tab move naturally through the visible result buttons.
            // If Shift+Tab is pressed at the search box, normal tab behaviour applies.
        }
    });

    // Return to search after committing a text/number/date value.
    // Do not intercept Enter inside dropdowns, radios or checkboxes.
    document.addEventListener("keydown", event => {
        const target = event.target;

        if (event.key !== "Enter" || !(target instanceof HTMLElement)) return;
        if (target === search || results.contains(target)) return;
        if (!target.closest(".entry-field")) return;

        if (
            target.matches('input[type="text"], input[type="number"], input[type="date"], textarea')
        ) {
            event.preventDefault();
            if (lastTarget) lastTarget.classList.remove("search-target");
            lastTarget = null;
            search.value = "";
            search.focus();
        }
    });

    // "/" focuses search when the user isn't already typing in another control.
    document.addEventListener("keydown", event => {
        if (
            event.key === "/" &&
            !event.ctrlKey &&
            !event.altKey &&
            !event.metaKey &&
            !(event.target instanceof HTMLInputElement) &&
            !(event.target instanceof HTMLTextAreaElement) &&
            !(event.target instanceof HTMLSelectElement) &&
            !event.target.isContentEditable
        ) {
            event.preventDefault();
            search.focus();
        }
    });

    // Escape from a result button closes the overlay and returns to search.
    results.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            event.preventDefault();
            closeSearch({ clear: true });
            search.focus();
        }
    });

    // Clicking outside the search UI closes the results without removing the form.
    document.addEventListener("click", event => {
        if (!event.target.closest(".entry-search")) {
            closeSearch();
        }
    });
})();
