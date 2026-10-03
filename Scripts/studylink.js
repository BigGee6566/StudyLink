/* StudyLink - small progressive enhancements. Every feature works without JavaScript;
   the server validates everything again. */
(function () {
  "use strict";
  var body = document.body;
  // Content that animates in starts hidden only when this script runs, so it is never lost without JS.
  document.documentElement.classList.add("js");

  // Public site: collapse the navigation behind a menu button on narrow screens.
  document.addEventListener("click", function (e) {
    var t = e.target.closest("[data-menu-toggle]");
    if (!t) return;
    body.classList.toggle("menu-open");
    t.setAttribute("aria-expanded", body.classList.contains("menu-open"));
  });
  document.addEventListener("keydown", function (e) { if (e.key === "Escape") body.classList.remove("menu-open"); });

  // Reveal-on-scroll ("How it works" steps). Shown at once when motion is reduced or unsupported.
  var revealables = document.querySelectorAll("[data-reveal], [data-reveal-item]");
  var still = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  if (still || !("IntersectionObserver" in window)) {
    revealables.forEach(function (el) { el.classList.add("is-visible"); });
  } else {
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (!entry.isIntersecting) return;
        entry.target.classList.add("is-visible");
        observer.unobserve(entry.target);
      });
    }, { threshold: 0.25 });
    revealables.forEach(function (el) { observer.observe(el); });
  }

  // Show / hide password, opt-in per box: <asp:TextBox TextMode="Password" data-password-toggle="true" />.
  var eye = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>';
  var eyeOff = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3l18 18M10.6 5.1A10 10 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-3.2 4.1M6.6 6.6C3.8 8.4 2 12 2 12s3.5 7 10 7a9.7 9.7 0 0 0 4.4-1M9.9 9.9a3 3 0 0 0 4.2 4.2"/></svg>';
  document.querySelectorAll("input[type=password][data-password-toggle]").forEach(function (input) {
    var wrap = document.createElement("span");
    wrap.className = "pw-wrap d-block";
    input.parentNode.insertBefore(wrap, input);
    wrap.appendChild(input);
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "pw-toggle";
    btn.setAttribute("aria-label", "Show password");
    btn.setAttribute("aria-pressed", "false");
    btn.innerHTML = eye;
    wrap.appendChild(btn);
    btn.addEventListener("click", function () {
      var showing = input.type === "text";
      var start = input.selectionStart, end = input.selectionEnd;
      input.type = showing ? "password" : "text";
      btn.innerHTML = showing ? eye : eyeOff;
      btn.setAttribute("aria-label", showing ? "Show password" : "Hide password");
      btn.setAttribute("aria-pressed", String(!showing));
      input.focus();
      try { input.setSelectionRange(start, end); } catch (ignore) { }
    });
  });

  // Temporary password generator: <button data-generate-password="ClientIdOfTheBox">. Letters and
  // digits only, without look-alikes (0/O, 1/l/I), so it can be read out or copied from an email.
  document.querySelectorAll("[data-generate-password]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var input = document.getElementById(btn.getAttribute("data-generate-password"));
      if (!input) return;
      var letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ", digits = "23456789";
      var all = letters + digits, bytes = new Uint32Array(12), out = "";
      window.crypto.getRandomValues(bytes);
      for (var i = 0; i < bytes.length; i++) out += all[bytes[i] % all.length];
      out = letters[bytes[0] % letters.length] + out.slice(1, 11) + digits[bytes[11] % digits.length];
      input.value = out;
      var toggle = input.parentNode.querySelector(".pw-toggle");
      if (input.type === "password" && toggle) toggle.click();
      input.focus();
      input.select();
    });
  });

  // Uploads: refuse an oversized file before it is sent, instead of failing on the server.
  document.querySelectorAll("input[type=file][data-max-mb]").forEach(function (input) {
    var max = +input.getAttribute("data-max-mb");
    var note = document.createElement("div");
    note.className = "form-error";
    note.setAttribute("role", "alert");
    note.hidden = true;
    input.insertAdjacentElement("afterend", note);
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      var tooBig = !!file && file.size > max * 1024 * 1024;
      note.hidden = !tooBig;
      note.textContent = tooBig ? "That file is " + (file.size / 1048576).toFixed(1) + " MB. Choose one of " + max + " MB or less." : "";
      if (tooBig) input.value = "";
    });
  });

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