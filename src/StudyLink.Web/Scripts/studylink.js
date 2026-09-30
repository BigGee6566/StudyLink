/* StudyLink - small progressive enhancements. Every feature works without JavaScript;
   the server validates everything again. */
(function () {
  "use strict";
  var body = document.body;

  // Mobile sidebar drawer (TutorConnect sidebar.js).
  document.addEventListener("click", function (e) {
    var t = e.target.closest("[data-nav-toggle]");
    if (t) { body.classList.toggle("nav-open"); t.setAttribute("aria-expanded", body.classList.contains("nav-open")); return; }
    if (e.target.closest("[data-nav-close]")) body.classList.remove("nav-open");
  });
  document.addEventListener("keydown", function (e) { if (e.key === "Escape") body.classList.remove("nav-open"); });

  // Confirm destructive actions: <asp:Button data-confirm="…" />
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-confirm]");
    if (b && !window.confirm(b.getAttribute("data-confirm"))) { e.preventDefault(); e.stopImmediatePropagation(); }
  }, true);

  // SC8 slot picker: clicking a slot selects it and fills the hidden field the server reads.
  document.querySelectorAll("[data-slot-target]").forEach(function (group) {
    var hidden = document.getElementById(group.getAttribute("data-slot-target"));
    group.addEventListener("click", function (e) {
      var btn = e.target.closest(".slot-btn");
      if (!btn) return;
      e.preventDefault();
      group.querySelectorAll(".slot-btn").forEach(function (b) { b.classList.remove("is-selected"); b.setAttribute("aria-pressed", "false"); });
      btn.classList.add("is-selected");
      btn.setAttribute("aria-pressed", "true");
      if (hidden) hidden.value = btn.getAttribute("data-slot");
      var summary = document.querySelector("[data-slot-summary]");
      if (summary) {
        summary.hidden = false;
        ["time", "fee", "hours", "campus", "subject"].forEach(function (k) {
          var el = summary.querySelector("[data-field='" + k + "']");
          if (el) el.textContent = btn.getAttribute("data-" + k) || "";
        });
      }
      var go = document.querySelector("[data-slot-submit]");
      if (go) go.disabled = false;
    });
  });

  // Card number: group digits in fours while typing (display only).
  document.querySelectorAll("[data-card-number]").forEach(function (input) {
    input.addEventListener("input", function () {
      var digits = input.value.replace(/\D/g, "").slice(0, 19);
      input.value = digits.replace(/(\d{4})(?=\d)/g, "$1 ");
    });
  });
  document.querySelectorAll("[data-card-expiry]").forEach(function (input) {
    input.addEventListener("input", function () {
      var d = input.value.replace(/\D/g, "").slice(0, 4);
      input.value = d.length > 2 ? d.slice(0, 2) + "/" + d.slice(2) : d;
    });
  });

  // Character counters: <textarea data-max="500">
  document.querySelectorAll("[data-max]").forEach(function (el) {
    var max = +el.getAttribute("data-max");
    var counter = document.createElement("div");
    counter.className = "form-hint text-end";
    el.insertAdjacentElement("afterend", counter);
    var update = function () { counter.textContent = el.value.length + " / " + max; };
    el.addEventListener("input", update);
    update();
  });

  // Prevent double submits of forms that move money or stock.
  document.querySelectorAll("[data-once]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      setTimeout(function () { btn.disabled = true; btn.value = btn.getAttribute("data-once") || "Please wait…"; }, 0);
    });
  });
})();