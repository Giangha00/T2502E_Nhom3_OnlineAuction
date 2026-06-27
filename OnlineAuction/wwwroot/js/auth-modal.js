(function () {
  var overlay = document.getElementById('authModalOverlay');
  var backdrop = document.getElementById('authModalBackdrop');
  var closeBtn = document.getElementById('authModalClose');
  var tabs = document.querySelectorAll('.home-auth-tab');
  var modal = overlay ? overlay.querySelector('.home-auth-modal') : null;
  var modalTitle = document.getElementById('authModalTitle');
  var modalSubtitle = document.getElementById('authModalSubtitle');
  var loginPanel = document.getElementById('authPanelLogin');
  var signupPanel = document.getElementById('authPanelSignup');
  var forgotPanel = document.getElementById('authPanelForgot');
  var forgotPasswordBtn = document.getElementById('authForgotPasswordBtn');
  var backToLoginBtn = document.getElementById('authBackToLoginBtn');
  var triggers = document.querySelectorAll('[data-auth-tab]');
  var mobileNav = document.getElementById('mobileNav');
  var mobileMenuBtn = document.getElementById('mobileMenuBtn');
  var slides = document.querySelectorAll('.auth-visual-slide');
  var dots = document.querySelectorAll('.auth-visual-dot');
  var visualTitle = document.getElementById('authVisualTitle');
  var visualDesc = document.getElementById('authVisualDesc');
  var visualEyebrow = document.getElementById('authVisualEyebrow');
  var slideTimer = null;
  var currentSlide = 0;

  var slideCopy = (function () {
    var i18n = (window.authModalConfig && window.authModalConfig.i18n) || {};
    return [
      {
        eyebrow: i18n.slide2Eyebrow || 'Pokemon & TCG',
        title: i18n.slide2Title || 'Discover Rare Holos',
        desc: i18n.slide2Desc || 'Base Set Charizards, Illustrator promos and manga rare parallels.'
      },
      {
        eyebrow: i18n.slide1Eyebrow || 'RareCard Vault',
        title: i18n.slide1Title || 'Welcome to RareCard',
        desc: i18n.slide1Desc || 'Bid on authenticated graded cards from PSA, BGS and CGC vaults.'
      },
      {
        eyebrow: i18n.slide3Eyebrow || 'Sports & MTG',
        title: i18n.slide3Title || 'Legends on Auction',
        desc: i18n.slide3Desc || 'From Mickey Mantle rookies to Alpha Black Lotus, curated daily.'
      }
    ];
  })();

  if (!overlay) return;

  var returnUrlInputs = overlay.querySelectorAll('input[name="returnUrl"]');
  var defaultReturnUrl = returnUrlInputs.length
    ? returnUrlInputs[0].value
    : window.location.pathname + window.location.search;

  function setReturnUrl(url) {
    var nextUrl = url || defaultReturnUrl;
    returnUrlInputs.forEach(function (input) {
      input.value = nextUrl;
    });
  }

  function switchTab(tabName) {
    var nextTab = tabName === 'signup' || tabName === 'forgot' ? tabName : 'login';
    var isForgot = nextTab === 'forgot';

    tabs.forEach(function (tab) {
      var isActive = !isForgot && tab.dataset.tab === nextTab;
      tab.classList.toggle('home-auth-tab--active', isActive);
      tab.setAttribute('aria-selected', String(isActive));
      tab.setAttribute('tabindex', isActive ? '0' : '-1');
    });

    if (!loginPanel || !signupPanel) return;

    var showLogin = nextTab === 'login';
    var showSignup = nextTab === 'signup';
    loginPanel.classList.toggle('hidden', !showLogin);
    loginPanel.hidden = !showLogin;
    signupPanel.classList.toggle('hidden', !showSignup);
    signupPanel.hidden = !showSignup;

    if (forgotPanel) {
      forgotPanel.classList.toggle('hidden', !isForgot);
      forgotPanel.hidden = !isForgot;
    }

    if (modal) modal.classList.toggle('home-auth-modal--compact', showLogin || isForgot);
    if (modalTitle) {
      modalTitle.textContent = isForgot
        ? modalTitle.getAttribute('data-forgot-title') || 'Quên mật khẩu?'
        : modalTitle.getAttribute('data-default-title') || modalTitle.textContent;
    }
    if (modalSubtitle) {
      modalSubtitle.textContent = isForgot
        ? modalSubtitle.getAttribute('data-forgot-subtitle') || 'Nhập email để nhận liên kết đặt lại mật khẩu.'
        : modalSubtitle.getAttribute('data-default-subtitle') || modalSubtitle.textContent;
    }
  }

  function showSlide(index) {
    if (!slides.length) return;

    currentSlide = index;
    slides.forEach(function (slide, i) {
      slide.classList.toggle('is-active', i === index);
    });
    dots.forEach(function (dot, i) {
      dot.classList.toggle('is-active', i === index);
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

  function openModal(tabName, returnUrl) {
    var nextTab = tabName === 'signup' || tabName === 'forgot' ? tabName : 'login';
    setReturnUrl(returnUrl);
    switchTab(nextTab);
    overlay.classList.remove('home-auth-overlay--hidden');
    overlay.setAttribute('aria-hidden', 'false');
    document.body.classList.add('home-auth-open');
    showSlide(currentSlide);
    startCarousel();

    if (mobileNav && !mobileNav.classList.contains('hidden')) {
      mobileNav.classList.add('hidden');
      if (mobileMenuBtn) mobileMenuBtn.setAttribute('aria-expanded', 'false');
    }

    var panel = nextTab === 'signup' ? signupPanel : (nextTab === 'forgot' ? forgotPanel : loginPanel);
    var focusTarget = panel && panel.querySelector('input:not([type="hidden"])');
    if (focusTarget) {
      window.setTimeout(function () { focusTarget.focus(); }, 120);
    }
  }

  function closeModal() {
    overlay.classList.add('home-auth-overlay--hidden');
    overlay.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('home-auth-open');
    stopCarousel();
    setReturnUrl(null);
  }

  triggers.forEach(function (trigger) {
    trigger.addEventListener('click', function (e) {
      e.preventDefault();
      openModal(
        trigger.getAttribute('data-auth-tab') || 'login',
        trigger.getAttribute('data-auth-return-url')
      );
    });
  });

  tabs.forEach(function (tab) {
    tab.addEventListener('click', function () {
      switchTab(tab.dataset.tab);
    });
  });

  if (forgotPasswordBtn) {
    forgotPasswordBtn.addEventListener('click', function () {
      switchTab('forgot');
      var focusTarget = forgotPanel && forgotPanel.querySelector('input:not([type="hidden"])');
      if (focusTarget) focusTarget.focus();
    });
  }

  if (backToLoginBtn) {
    backToLoginBtn.addEventListener('click', function () {
      switchTab('login');
      var focusTarget = loginPanel && loginPanel.querySelector('input:not([type="hidden"])');
      if (focusTarget) focusTarget.focus();
    });
  }

  dots.forEach(function (dot) {
    dot.addEventListener('click', function () {
      showSlide(Number(dot.getAttribute('data-slide')) || 0);
      startCarousel();
    });
  });

  if (closeBtn) closeBtn.addEventListener('click', closeModal);
  if (backdrop) backdrop.addEventListener('click', closeModal);

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && !overlay.classList.contains('home-auth-overlay--hidden')) {
      closeModal();
    }
  });

  window.openAuthModal = openModal;
})();
