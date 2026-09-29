// Shared by the two token generators under /Tools: the symmetric (HS256) and asymmetric
// (RS256) pages build a token through the same staged pipeline and behave identically here.

// Copy buttons. Any button with data-value copies that value and says so briefly.
document.querySelectorAll(".copy-button").forEach(function (button) {
    button.addEventListener("click", function () {
        navigator.clipboard.writeText(button.dataset.value).then(function () {
            var original = button.textContent;
            button.textContent = "Copied";
            setTimeout(function () { button.textContent = original; }, 1200);
        });
    });
});

// The results below a field are only ever a snapshot of the last post. As soon as an input
// is edited they no longer describe what is on screen, so they are dimmed and the matching
// "press the button again" notice is shown. Editing the JSON invalidates both steps;
// editing the shared secret, where there is one, invalidates only the signature.
(function () {
    var form = document.querySelector(".token-builder-form");
    if (!form) {
        return;
    }

    function markStale(ids) {
        ids.forEach(function (id) {
            var group = document.getElementById(id);
            if (group) {
                group.classList.add("is-stale");
            }

            form.querySelectorAll("[data-stale-for='" + id + "']").forEach(function (notice) {
                notice.classList.add("is-visible");
            });
        });
    }

    form.querySelectorAll(".jwt-source").forEach(function (field) {
        field.addEventListener("input", function () {
            markStale(["encoded-steps", "signed-steps"]);
        });
    });

    form.querySelectorAll(".jwt-secret").forEach(function (field) {
        field.addEventListener("input", function () {
            markStale(["signed-steps"]);
        });
    });
})();
