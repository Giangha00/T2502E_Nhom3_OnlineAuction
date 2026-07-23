(function () {
  const form = document.getElementById("refundForm");
  if (!form) return;

  const config = window.refundConfig || {};
  const i18n = config.i18n || {};

  const orderSelect = document.getElementById("orderReference");
  const manualOrderField = document.getElementById("manualOrderField");
  const refundAmount = document.getElementById("refundAmount");
  const formError = document.getElementById("formError");

  function setError(id, message) {
    const el = document.getElementById(id);
    if (el) el.textContent = message;
  }

  function clearErrors() {
    [
      "orderReferenceError",
      "fullNameError",
      "emailError",
      "refundReasonError",
      "descriptionError",
      "evidenceLinksError",
      "agreePolicyError",
      "formError",
    ].forEach(function (id) {
      setError(id, "");
    });
  }

  function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  function parseEvidenceLinks(value) {
    return value
      .split(/[\r\n,;]+/)
      .map(function (url) {
        return url.trim();
      })
      .filter(Boolean);
  }

  function isValidEvidenceUrl(value) {
    try {
      const url = new URL(value);
      return url.protocol === "http:" || url.protocol === "https:";
    } catch {
      return false;
    }
  }

  if (orderSelect) {
    orderSelect.addEventListener("change", function () {
      const isOther = orderSelect.value === "other";
      manualOrderField?.classList.toggle("hidden", !isOther);

      if (!isOther && orderSelect.selectedOptions[0]) {
        const amount =
          orderSelect.selectedOptions[0].getAttribute("data-amount");
        if (amount && refundAmount && !refundAmount.value) {
          refundAmount.placeholder = amount;
        }
      }
    });
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    clearErrors();

    if (config.isAuthenticated === false) {
      setError(
        "formError",
        i18n.loginRequired || "Please log in to submit a refund request.",
      );
      if (config.loginUrl) {
        window.location.href = config.loginUrl;
      }
      return;
    }

    let valid = true;
    const orderValue = orderSelect?.value || "";
    const manualRef =
      document.getElementById("manualOrderRef")?.value.trim() || "";
    const fullName = document.getElementById("fullName")?.value.trim() || "";
    const email = document.getElementById("email")?.value.trim() || "";
    const reason = document.getElementById("refundReason")?.value || "";
    const description =
      document.getElementById("description")?.value.trim() || "";
    const evidenceValue =
      document.getElementById("evidenceLinks")?.value.trim() || "";
    const evidenceLinks = parseEvidenceLinks(evidenceValue);
    const agreePolicy = document.getElementById("agreePolicy")?.checked;
    const amountValue = refundAmount?.value.trim() || "";

    let orderRef = orderValue;
    let orderId = null;

    if (!orderValue) {
      setError(
        "orderReferenceError",
        i18n.orderRequired || "Please select an order.",
      );
      valid = false;
    } else if (orderValue === "other" && !manualRef) {
      setError(
        "orderReferenceError",
        i18n.manualRequired || "Please enter your order reference.",
      );
      valid = false;
    } else if (orderValue === "other") {
      orderRef = manualRef;
    } else if (orderSelect?.selectedOptions[0]) {
      const selectedOrderId =
        orderSelect.selectedOptions[0].getAttribute("data-order-id");
      if (selectedOrderId) {
        orderId = selectedOrderId;
      }
    }

    if (!fullName) {
      setError(
        "fullNameError",
        i18n.fullNameRequired || "Full name is required.",
      );
      valid = false;
    }
    if (!email || !isValidEmail(email)) {
      setError(
        "emailError",
        i18n.emailInvalid || "Please enter a valid email address.",
      );
      valid = false;
    }
    if (!reason) {
      setError(
        "refundReasonError",
        i18n.reasonRequired || "Please select a refund reason.",
      );
      valid = false;
    }
    if (!description || description.length < 20) {
      setError(
        "descriptionError",
        i18n.descriptionRequired ||
          "Please provide a detailed description (at least 20 characters).",
      );
      valid = false;
    }
    if (!agreePolicy) {
      setError(
        "agreePolicyError",
        i18n.policyRequired || "You must agree to the refund policy.",
      );
      valid = false;
    }
    if (
      evidenceLinks.length > 5 ||
      evidenceLinks.some(function (url) {
        return !isValidEvidenceUrl(url);
      })
    ) {
      setError(
        "evidenceLinksError",
        "Evidence links must be valid http/https URLs. Add no more than 5 links.",
      );
      valid = false;
    }

    if (!valid) return;

    const token =
      form.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      "";
    const body = new URLSearchParams();
    body.append("__RequestVerificationToken", token);
    if (orderId) body.append("OrderId", orderId);
    if (orderRef) body.append("OrderReference", orderRef);
    body.append("ContactName", fullName);
    body.append("ContactEmail", email);
    body.append("ReasonCode", reason);
    body.append("Description", description);
    if (evidenceLinks.length) body.append("EvidenceUrls", evidenceLinks.join("\n"));
    if (amountValue) body.append("RequestedAmount", amountValue);

    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) submitButton.disabled = true;

    fetch(config.submitUrl || "/Refund/Submit", {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        "X-Requested-With": "XMLHttpRequest",
      },
      body: body.toString(),
      credentials: "same-origin",
    })
      .then(function (response) {
        if (response.status === 401 || response.status === 403) {
          window.location.href =
            config.loginUrl || "/Auth/Login?returnUrl=/Refund";
          return null;
        }

        return response
          .json()
          .then(function (data) {
            return { ok: response.ok, data: data };
          })
          .catch(function () {
            return { ok: response.ok, data: null };
          });
      })
      .then(function (result) {
        if (!result) return;

        if (result.ok && result.data && result.data.redirectUrl) {
          window.location.href = result.data.redirectUrl;
          return;
        }

        const message =
          (result.data && result.data.message) ||
          i18n.submitFailed ||
          "Unable to submit your refund request. Please try again.";
        setError("formError", message);
      })
      .catch(function () {
        setError(
          "formError",
          i18n.submitFailed ||
            "Unable to submit your refund request. Please try again.",
        );
      })
      .finally(function () {
        if (submitButton)
          submitButton.disabled = config.isAuthenticated === false;
      });
  });
})();
