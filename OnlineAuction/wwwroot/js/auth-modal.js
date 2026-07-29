(function () {
  var overlay = document.getElementById("authModalOverlay");
  var backdrop = document.getElementById("authModalBackdrop");
  var closeBtn = document.getElementById("authModalClose");
  var tabsContainer = document.getElementById("authTabs");
  var tabs = document.querySelectorAll(".home-auth-tab");
  var modal = overlay ? overlay.querySelector(".home-auth-modal") : null;
  var modalTitle = document.getElementById("authModalTitle");
  var modalSubtitle = document.getElementById("authModalSubtitle");
  var legalText = document.getElementById("authLegalText");
  var loginPanel = document.getElementById("authPanelLogin");
  var signupPanel = document.getElementById("authPanelSignup");
  var forgotPanel = document.getElementById("authPanelForgot");
  var forgotOtpPanel = document.getElementById("authPanelForgotOtp");
  var forgotResetPanel = document.getElementById("authPanelForgotReset");
  var forgotPasswordBtn = document.getElementById("authForgotPasswordBtn");
  var backToLoginBtn = document.getElementById("authBackToLoginBtn");
  var backToForgotEmailBtn = document.getElementById(
    "authBackToForgotEmailBtn",
  );
  var backToForgotOtpBtn = document.getElementById("authBackToForgotOtpBtn");
  var forgotOtpForm = document.getElementById("authForgotOtpForm");
  var otpHiddenInput = document.getElementById("modalOtpValue");
  var otpInputs = document.querySelectorAll(".home-auth-otp-input");
  var triggers = document.querySelectorAll("[data-auth-tab]");
  var mobileNav = document.getElementById("mobileNav");
  var mobileMenuBtn = document.getElementById("mobileMenuBtn");
  var slides = document.querySelectorAll(".auth-visual-slide");
  var dots = document.querySelectorAll(".auth-visual-dot");
  var visualTitle = document.getElementById("authVisualTitle");
  var visualDesc = document.getElementById("authVisualDesc");
  var visualEyebrow = document.getElementById("authVisualEyebrow");
  var slideTimer = null;
  var currentSlide = 0;

  var i18n = (window.authModalConfig && window.authModalConfig.i18n) || {};

  var slideCopy = [
    {
      eyebrow: i18n.slide2Eyebrow || "Pokemon & TCG",
      title: i18n.slide2Title || "Discover Rare Holos",
      desc:
        i18n.slide2Desc ||
        "Base Set Charizards, Illustrator promos and manga rare parallels.",
    },
    {
      eyebrow: i18n.slide1Eyebrow || "RareCard Vault",
      title: i18n.slide1Title || "Welcome to RareCard",
      desc:
        i18n.slide1Desc ||
        "Bid on authenticated graded cards from PSA, BGS and CGC vaults.",
    },
    {
      eyebrow: i18n.slide3Eyebrow || "Sports & MTG",
      title: i18n.slide3Title || "Legends on Auction",
      desc:
        i18n.slide3Desc ||
        "From Mickey Mantle rookies to Alpha Black Lotus, curated daily.",
    },
  ];

  var forgotCopy = {
    forgot: {
      title: i18n.forgotTitle || "Forgot password?",
      subtitle:
        i18n.forgotSubtitle ||
        "Enter your email to receive a verification code.",
    },
    "forgot-otp": {
      title: i18n.forgotOtpTitle || "Enter verification code",
      subtitle:
        i18n.forgotOtpSubtitle || "We sent a 6-digit code to your email.",
    },
    "forgot-reset": {
      title: i18n.forgotResetTitle || "Create new password",
      subtitle:
        i18n.forgotResetSubtitle ||
        "Choose a new password for your RareCard account.",
    },
  };

  if (!overlay) return;

  function sanitizeReturnUrl(url) {
    if (!url || url.charAt(0) !== "/") {
      return "/";
    }

    if (url.toLowerCase().indexOf("/auth") === 0) {
      return "/";
    }

    try {
      var parsed = new URL(url, window.location.origin);
      if (parsed.pathname.toLowerCase().indexOf("/auth") === 0) {
        return "/";
      }

      parsed.searchParams.delete("authTab");
      return parsed.pathname + (parsed.search || "");
    } catch (e) {
      return "/";
    }
  }

  var returnUrlInputs = overlay.querySelectorAll('input[name="returnUrl"]');
  var defaultReturnUrl = sanitizeReturnUrl(
    returnUrlInputs.length
      ? returnUrlInputs[0].value
      : window.location.pathname + window.location.search,
  );

  function setReturnUrl(url) {
    var nextUrl = sanitizeReturnUrl(url || defaultReturnUrl);
    returnUrlInputs.forEach(function (input) {
      input.value = nextUrl;
    });
  }

  function isForgotStep(tabName) {
    return (
      tabName === "forgot" ||
      tabName === "forgot-otp" ||
      tabName === "forgot-reset"
    );
  }

  function setPanelVisibility(panel, visible) {
    if (!panel) return;
    panel.classList.toggle("home-auth-panel--active", visible);
    panel.hidden = !visible;
  }

  function updateForgotHeading(tabName) {
    if (!isForgotStep(tabName)) {
      if (modalTitle)
        modalTitle.textContent = i18n.defaultTitle || modalTitle.textContent;
      if (modalSubtitle)
        modalSubtitle.textContent =
          i18n.defaultSubtitle || modalSubtitle.textContent;
      return;
    }

    var copy = forgotCopy[tabName] || forgotCopy.forgot;
    if (modalTitle) modalTitle.textContent = copy.title;
    if (modalSubtitle) modalSubtitle.textContent = copy.subtitle;
  }

  function switchTab(tabName) {
    var nextTab = tabName;
    if (nextTab !== "signup" && !isForgotStep(nextTab)) {
      nextTab = "login";
    }

    var forgotFlow = isForgotStep(nextTab);

    if (tabsContainer) {
      tabsContainer.classList.toggle("home-auth-tabs--collapsed", forgotFlow);
    }

    if (legalText) {
      legalText.classList.toggle("home-auth-legal--collapsed", forgotFlow);
    }

    tabs.forEach(function (tab) {
      var isActive = !forgotFlow && tab.dataset.tab === nextTab;
      tab.classList.toggle("home-auth-tab--active", isActive);
      tab.setAttribute("aria-selected", String(isActive));
      tab.setAttribute("tabindex", isActive ? "0" : "-1");
    });

    setPanelVisibility(loginPanel, nextTab === "login");
    setPanelVisibility(signupPanel, nextTab === "signup");
    setPanelVisibility(forgotPanel, nextTab === "forgot");
    setPanelVisibility(forgotOtpPanel, nextTab === "forgot-otp");
    setPanelVisibility(forgotResetPanel, nextTab === "forgot-reset");

    updateForgotHeading(nextTab);
  }

  function getPanelForTab(tabName) {
    switch (tabName) {
      case "signup":
        return signupPanel;
      case "forgot":
        return forgotPanel;
      case "forgot-otp":
        return forgotOtpPanel;
      case "forgot-reset":
        return forgotResetPanel;
      default:
        return loginPanel;
    }
  }

  function collectOtpValue() {
    var value = "";
    otpInputs.forEach(function (input) {
      value += (input.value || "").replace(/\D/g, "").slice(0, 1);
    });
    return value;
  }

  function syncOtpHiddenInput() {
    if (otpHiddenInput) {
      otpHiddenInput.value = collectOtpValue();
    }
  }

  function clearOtpInputs() {
    otpInputs.forEach(function (input) {
      input.value = "";
    });
    syncOtpHiddenInput();
  }

  function setupOtpInputs() {
    otpInputs.forEach(function (input, index) {
      input.addEventListener("input", function () {
        input.value = input.value.replace(/\D/g, "").slice(0, 1);
        syncOtpHiddenInput();

        if (input.value && index < otpInputs.length - 1) {
          otpInputs[index + 1].focus();
        }
      });

      input.addEventListener("keydown", function (event) {
        if (event.key === "Backspace" && !input.value && index > 0) {
          otpInputs[index - 1].focus();
        }
      });

      input.addEventListener("paste", function (event) {
        event.preventDefault();
        var pasted =
          (event.clipboardData || window.clipboardData).getData("text") || "";
        var digits = pasted.replace(/\D/g, "").slice(0, 6);

        digits.split("").forEach(function (digit, digitIndex) {
          if (otpInputs[digitIndex]) {
            otpInputs[digitIndex].value = digit;
          }
        });

        syncOtpHiddenInput();

        var focusIndex = Math.min(digits.length, otpInputs.length - 1);
        if (otpInputs[focusIndex]) {
          otpInputs[focusIndex].focus();
        }
      });
    });

    if (forgotOtpForm) {
      forgotOtpForm.addEventListener("submit", function (event) {
        syncOtpHiddenInput();
        if (!otpHiddenInput || otpHiddenInput.value.length !== 6) {
          event.preventDefault();
          if (otpInputs[0]) otpInputs[0].focus();
        }
      });
    }
  }

  function showSlide(index) {
    if (!slides.length) return;

    currentSlide = index;
    slides.forEach(function (slide, i) {
      slide.classList.toggle("is-active", i === index);
    });
    dots.forEach(function (dot, i) {
      dot.classList.toggle("is-active", i === index);
    });

    var copy = slideCopy[index] || slideCopy[0];
    if (visualTitle) visualTitle.textContent = copy.title;
    if (visualDesc) visualDesc.textContent = copy.desc;
    if (visualEyebrow) visualEyebrow.textContent = copy.eyebrow;
  }

  function nextSlide() {
    showSlide((currentSlide + 1) % slides.length);
  }

  function startCarousel() {
    stopCarousel();
    if (slides.length <= 1) return;
    slideTimer = window.setInterval(nextSlide, 5000);
  }

  function stopCarousel() {
    if (slideTimer) {
      window.clearInterval(slideTimer);
      slideTimer = null;
    }
  }

  function getReturnUrlFromQuery() {
    var params = new URLSearchParams(window.location.search);
    var returnUrl = params.get("returnUrl");
    if (returnUrl && returnUrl.charAt(0) === "/") {
      return sanitizeReturnUrl(returnUrl);
    }

    return null;
  }

  function stripAuthTabFromUrl() {
    var params = new URLSearchParams(window.location.search);
    if (!params.has("authTab")) {
      return null;
    }

    var authTab = params.get("authTab");
    params.delete("authTab");
    var newSearch = params.toString();
    var newUrl =
      window.location.pathname +
      (newSearch ? "?" + newSearch : "") +
      window.location.hash;
    history.replaceState(null, "", newUrl);
    return authTab;
  }

  var alertDismissTimers = [];

  function clearAlertDismissTimers() {
    alertDismissTimers.forEach(function (timerId) {
      window.clearTimeout(timerId);
    });
    alertDismissTimers = [];
  }

  function dismissAuthAlert(alert) {
    if (!alert || alert.classList.contains("is-hiding")) {
      return;
    }

    alert.classList.add("is-hiding");
    window.setTimeout(function () {
      if (alert.parentNode) {
        alert.parentNode.removeChild(alert);
      }
    }, 300);
  }

  function initAuthAlerts() {
    clearAlertDismissTimers();
    overlay
      .querySelectorAll("[data-auth-auto-dismiss]")
      .forEach(function (alert) {
        var delay =
          Number(alert.getAttribute("data-auth-auto-dismiss")) || 5000;
        var timerId = window.setTimeout(function () {
          dismissAuthAlert(alert);
        }, delay);
        alertDismissTimers.push(timerId);
      });
  }

  function openModal(tabName, returnUrl) {
    var nextTab = tabName || "login";
    if (nextTab !== "signup" && !isForgotStep(nextTab)) {
      nextTab = "login";
    }

    setReturnUrl(returnUrl);
    switchTab(nextTab);
    overlay.hidden = false;
    overlay.classList.remove("home-auth-overlay--hidden");
    overlay.setAttribute("aria-hidden", "false");
    document.body.classList.add("home-auth-open");
    showSlide(currentSlide);
    startCarousel();

    if (mobileNav && !mobileNav.classList.contains("hidden")) {
      mobileNav.classList.add("hidden");
      if (mobileMenuBtn) mobileMenuBtn.setAttribute("aria-expanded", "false");
    }

    var panel = getPanelForTab(nextTab);
    var focusTarget =
      panel && panel.querySelector('input:not([type="hidden"])');
    if (focusTarget) {
      window.setTimeout(function () {
        focusTarget.focus();
      }, 120);
    }

    initAuthAlerts();
  }

  function closeModal() {
    clearAlertDismissTimers();
    overlay.hidden = true;
    overlay.classList.add("home-auth-overlay--hidden");
    overlay.setAttribute("aria-hidden", "true");
    document.body.classList.remove("home-auth-open");
    stopCarousel();
    setReturnUrl(null);
    clearOtpInputs();
    switchTab("login");
  }

  setupOtpInputs();
  setupSignupPhoneInput();

  function setupSignupPhoneInput() {
    var phoneInput = document.getElementById("modalPhone");
    var signupForm = signupPanel ? signupPanel.querySelector("form") : null;
    if (!phoneInput || !signupForm) {
      return;
    }

    var invalidMessage =
      (window.authModalConfig &&
        window.authModalConfig.i18n &&
        window.authModalConfig.i18n.phoneInvalidLength) ||
      "Phone number must be exactly 11 digits.";

    function normalizePhoneValue() {
      var digits = (phoneInput.value || "").replace(/\D/g, "").slice(0, 11);
      if (phoneInput.value !== digits) {
        phoneInput.value = digits;
      }

      if (digits.length === 0 || digits.length === 11) {
        phoneInput.setCustomValidity("");
      } else {
        phoneInput.setCustomValidity(invalidMessage);
      }
    }

    phoneInput.addEventListener("input", normalizePhoneValue);
    phoneInput.addEventListener("blur", function () {
      normalizePhoneValue();
      if (phoneInput.value && phoneInput.value.length !== 11) {
        phoneInput.reportValidity();
      }
    });
    phoneInput.addEventListener("keydown", function (event) {
      var allowedKeys = [
        "Backspace",
        "Delete",
        "Tab",
        "ArrowLeft",
        "ArrowRight",
        "Home",
        "End",
      ];
      if (
        allowedKeys.indexOf(event.key) >= 0 ||
        event.ctrlKey ||
        event.metaKey
      ) {
        return;
      }

      if (!/^\d$/.test(event.key)) {
        event.preventDefault();
      }
    });
    phoneInput.addEventListener("paste", function (event) {
      event.preventDefault();
      var pasted =
        (event.clipboardData || window.clipboardData).getData("text") || "";
      phoneInput.value = ((phoneInput.value || "") + pasted)
        .replace(/\D/g, "")
        .slice(0, 11);
      normalizePhoneValue();
    });

    signupForm.addEventListener("submit", function (event) {
      normalizePhoneValue();
      if (!phoneInput.checkValidity()) {
        event.preventDefault();
        phoneInput.reportValidity();
      }
    });
  }

  var queryReturnUrl = getReturnUrlFromQuery();
  if (queryReturnUrl) {
    setReturnUrl(queryReturnUrl);
  }

  var authTabFromUrl = stripAuthTabFromUrl();
  if (authTabFromUrl) {
    openModal(authTabFromUrl, queryReturnUrl);
  } else {
    initAuthAlerts();
  }

  triggers.forEach(function (trigger) {
    trigger.addEventListener("click", function (e) {
      e.preventDefault();
      openModal(
        trigger.getAttribute("data-auth-tab") || "login",
        trigger.getAttribute("data-auth-return-url"),
      );
    });
  });

  tabs.forEach(function (tab) {
    tab.addEventListener("click", function () {
      switchTab(tab.dataset.tab);
    });
  });

  if (forgotPasswordBtn) {
    forgotPasswordBtn.addEventListener("click", function () {
      switchTab("forgot");
      var focusTarget =
        forgotPanel && forgotPanel.querySelector('input:not([type="hidden"])');
      if (focusTarget) focusTarget.focus();
    });
  }

  if (backToLoginBtn) {
    backToLoginBtn.addEventListener("click", function () {
      switchTab("login");
      var focusTarget =
        loginPanel && loginPanel.querySelector('input:not([type="hidden"])');
      if (focusTarget) focusTarget.focus();
    });
  }

  if (backToForgotEmailBtn) {
    backToForgotEmailBtn.addEventListener("click", function () {
      clearOtpInputs();
      switchTab("forgot");
      var focusTarget =
        forgotPanel && forgotPanel.querySelector('input:not([type="hidden"])');
      if (focusTarget) focusTarget.focus();
    });
  }

  if (backToForgotOtpBtn) {
    backToForgotOtpBtn.addEventListener("click", function () {
      switchTab("forgot-otp");
      if (otpInputs[0]) otpInputs[0].focus();
    });
  }

  dots.forEach(function (dot) {
    dot.addEventListener("click", function () {
      showSlide(Number(dot.getAttribute("data-slide")) || 0);
      startCarousel();
    });
  });

  if (closeBtn) closeBtn.addEventListener("click", closeModal);
  if (backdrop) backdrop.addEventListener("click", closeModal);

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && !overlay.hidden) {
      closeModal();
    }
  });

  window.openAuthModal = openModal;
})();
