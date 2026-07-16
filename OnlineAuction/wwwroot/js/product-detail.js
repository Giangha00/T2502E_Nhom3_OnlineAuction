(function () {
  'use strict';

  var config = window.productDetailConfig || {};
  var i18n = config.i18n || {};
  var mainImage = document.getElementById('mainProductImage');
  var thumbs = document.querySelectorAll('.gallery-thumb');
  var bidInput = document.getElementById('bidAmount');
  var bidDecreaseBtn = document.getElementById('bidAmountDecrease');
  var bidIncreaseBtn = document.getElementById('bidAmountIncrease');
  var tabButtons = document.querySelectorAll('.product-detail-tabs__tab');
  var tabPanels = document.querySelectorAll('.product-detail-tabs__panel');
  var bidPanel = document.querySelector('.product-bid-panel');
  var placeBidBtn = document.getElementById('placeBidBtn');
  var registerAuctionBtn = document.getElementById('registerAuctionBtn');
  var cancelRegistrationBtn = document.getElementById('cancelRegistrationBtn');
  var currentPriceDisplay = document.getElementById('currentPriceDisplay');
  var bidCountLabel = document.getElementById('bidCountLabel');
  var registrationCountLabel = document.getElementById('registrationCountLabel');
  var minBidDisplay = document.getElementById('minBidDisplay');
  var bidFeedback = document.getElementById('bidFeedback');
  var registrationFeedback = document.getElementById('registrationFeedback');
  var bidHistoryBody = document.getElementById('bidHistoryBody');
  var countdownSummary = document.getElementById('countdownSummary');
  var cdDays = document.getElementById('cdDays');
  var cdHours = document.getElementById('cdHours');
  var cdMinutes = document.getElementById('cdMinutes');
  var cdSeconds = document.getElementById('cdSeconds');

  var bidStep = parseFloat((bidPanel && bidPanel.getAttribute('data-bid-step')) || config.bidStep || '0');
  var endDateMs = parseUtcDateMs((bidPanel && bidPanel.getAttribute('data-end-date')) || config.endDate || '');
  var canBid = (bidPanel && bidPanel.getAttribute('data-can-bid') === 'true') || config.canBid === true;
  var canPlaceBid = bidPanel && bidPanel.getAttribute('data-can-place-bid') === 'true';
  var canRegister = bidPanel && bidPanel.getAttribute('data-can-register') === 'true';
  var countdownKind = (bidPanel && bidPanel.getAttribute('data-countdown-kind')) || config.countdownKind || 'live_end';
  var requiresRegistration = bidPanel && bidPanel.getAttribute('data-requires-registration') === 'true';
  var countdownTimer = null;
  var countdownReloadScheduled = false;

  function formatCurrency(value) {
    var amount = Number(value);
    if (Number.isNaN(amount)) {
      return '$0';
    }

    return '$' + amount.toLocaleString(undefined, { maximumFractionDigits: 0 });
  }

  function formatDepositAmount(value) {
    var amount = Number(value);
    if (Number.isNaN(amount)) {
      return '$0.00';
    }

    return '$' + amount.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  function formatBidTime(value) {
    var date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toLocaleString(undefined, {
      month: 'short',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    }).replace(',', ' ·');
  }

  function pad(value) {
    return String(value).padStart(2, '0');
  }

  function parseUtcDateMs(value) {
    if (!value) {
      return NaN;
    }

    var text = String(value).trim();
    if (!text) {
      return NaN;
    }

    if (!/[zZ]$|[+-]\d{2}:\d{2}$/.test(text)) {
      text += 'Z';
    }

    return Date.parse(text);
  }

  function getCsrfToken() {
    var csrfMeta = document.querySelector('meta[name="request-verification-token"]');
    return csrfMeta ? csrfMeta.getAttribute('content') : '';
  }

  var bidChallengeToken = '';

  function postForm(url, payload, extraHeaders) {
    var body = new URLSearchParams();
    Object.keys(payload).forEach(function (key) {
      body.append(key, payload[key]);
    });

    var csrfToken = getCsrfToken();
    if (csrfToken) {
      body.append('__RequestVerificationToken', csrfToken);
    }

    var headers = {
      'Content-Type': 'application/x-www-form-urlencoded',
      'X-Requested-With': 'XMLHttpRequest'
    };

    if (extraHeaders) {
      Object.keys(extraHeaders).forEach(function (key) {
        if (extraHeaders[key]) {
          headers[key] = extraHeaders[key];
        }
      });
    }

    return fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: headers,
      body: body.toString()
    });
  }

  function promptBidChallengeToken() {
    var message = i18n.challengePrompt || 'Enter challenge token to continue bidding:';
    var token = window.prompt(message, bidChallengeToken || '');
    if (token === null) {
      return null;
    }

    bidChallengeToken = String(token).trim();
    return bidChallengeToken;
  }

  function showFeedback(element, message, isSuccess) {
    if (!element) {
      return;
    }

    element.textContent = message;
    element.classList.remove('hidden', 'bg-emerald-50', 'text-emerald-700', 'bg-red-50', 'text-red-700');
    element.classList.add(isSuccess ? 'bg-emerald-50' : 'bg-red-50');
    element.classList.add(isSuccess ? 'text-emerald-700' : 'text-red-700');
  }

  function showPageToast(message, isSuccess) {
    if (!message) {
      return;
    }

    var toast = document.createElement('div');
    toast.className = 'fixed bottom-4 right-4 z-[100] max-w-sm rounded-lg border px-4 py-3 text-sm shadow-lg ' +
      (isSuccess ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800');
    toast.setAttribute('role', 'status');
    toast.setAttribute('aria-live', 'polite');
    toast.textContent = message;
    document.body.appendChild(toast);
    window.setTimeout(function () {
      toast.remove();
    }, 5000);
  }

  function getCurrentPrice() {
    if (bidPanel) {
      var price = parseFloat(bidPanel.getAttribute('data-current-price') || '0');
      if (!Number.isNaN(price)) {
        return price;
      }
    }

    return 0;
  }

  function getMinBid() {
    if (!bidStep || bidStep <= 0) {
      return getCurrentPrice();
    }

    return getCurrentPrice() + bidStep;
  }

  function parseBidInput(value) {
    if (value === null || value === undefined) {
      return NaN;
    }

    var normalized = String(value).replace(/[$,\s]/g, '');
    if (!normalized) {
      return NaN;
    }

    return Number(normalized);
  }

  function snapBidAmount(rawAmount) {
    var currentPrice = getCurrentPrice();
    var minBid = getMinBid();

    if (!bidStep || bidStep <= 0) {
      return Number.isNaN(rawAmount) ? minBid : rawAmount;
    }

    var amount = Number(rawAmount);
    if (Number.isNaN(amount) || amount <= 0) {
      return minBid;
    }

    if (amount <= minBid) {
      return minBid;
    }

    var increment = amount - currentPrice;
    var steps = Math.round(increment / bidStep);
    if (steps < 1) {
      steps = 1;
    }

    return currentPrice + steps * bidStep;
  }

  function formatBidInputValue(amount) {
    var value = Number(amount);
    if (Number.isNaN(value)) {
      return '';
    }

    if (Number.isInteger(value)) {
      return String(value);
    }

    return value.toFixed(2).replace(/\.?0+$/, '');
  }

  function setBidInputValue(amount) {
    if (!bidInput) {
      return;
    }

    bidInput.value = formatBidInputValue(amount);
    updateBidStepButtons();
  }

  function updateBidStepButtons() {
    if (!bidDecreaseBtn || !bidIncreaseBtn) {
      return;
    }

    var amount = snapBidAmount(parseBidInput(bidInput ? bidInput.value : ''));
    var minBid = getMinBid();
    bidDecreaseBtn.disabled = !canPlaceBid || amount <= minBid;
    bidIncreaseBtn.disabled = !canPlaceBid;
  }

  function refreshBidAmount(currentPrice) {
    if (bidPanel && !Number.isNaN(currentPrice)) {
      bidPanel.setAttribute('data-current-price', String(currentPrice));
    }

    var minBid = getMinBid();
    setBidInputValue(minBid);

    if (minBidDisplay) {
      minBidDisplay.textContent = formatCurrency(minBid);
    }
  }

  function renderBidHistory(items) {
    if (!bidHistoryBody) {
      return;
    }

    if (!items || !items.length) {
      bidHistoryBody.innerHTML =
        '<tr><td colspan="4" class="px-5 py-8 text-center text-sm text-slate-400">' +
        (i18n.noBidsYet || 'No bids yet') +
        '</td></tr>';
      return;
    }

    bidHistoryBody.innerHTML = items.map(function (bid) {
      var status = (bid.status || '').toUpperCase();
      var badgeClass;
      var statusLabel;

      if (status === 'WINNING' || bid.isWinning) {
        badgeClass = 'inline-flex rounded-full bg-emerald-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-emerald-700';
        statusLabel = i18n.winning || 'Highest bid';
      } else if (status === 'RAISED') {
        badgeClass = 'inline-flex rounded-full bg-blue-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-blue-700';
        statusLabel = i18n.raised || 'Raised';
      } else {
        badgeClass = 'inline-flex rounded-full bg-slate-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-500';
        statusLabel = i18n.outbid || 'Outbid';
      }

      return '<tr>' +
        '<td class="px-5 py-4 font-medium text-slate-800">' + bid.bidderName + '</td>' +
        '<td class="px-5 py-4 font-bold tabular-nums text-slate-900">' + formatCurrency(bid.amount) + '</td>' +
        '<td class="px-5 py-4 text-slate-500">' + formatBidTime(bid.bidTime) + '</td>' +
        '<td class="px-5 py-4"><span class="' + badgeClass + '">' + statusLabel + '</span></td>' +
        '</tr>';
    }).join('');
  }

  function updateBidCount(count) {
    if (!bidCountLabel) {
      return;
    }

    var template = i18n.bidsCount || '{0} bid(s)';
    bidCountLabel.textContent = template.replace('{0}', String(count));
  }

  function updateRegistrationCount(count) {
    if (!registrationCountLabel) {
      return;
    }

    var template = i18n.registeredCount || '{0} registered';
    registrationCountLabel.textContent = template.replace('{0}', String(count));
  }

  function openAuthModal() {
    if (typeof window.openAuthModal === 'function') {
      window.openAuthModal('signup');
    }
  }

  function requestConfirm(options) {
    if (typeof window.showConfirmModal === 'function') {
      return window.showConfirmModal(options);
    }

    var message = options.message || '';
    if (options.note) {
      message += '\n\n' + options.note;
    }

    return Promise.resolve(window.confirm(message));
  }

  function submitAuctionRegistration(auctionId) {
    registerAuctionBtn.disabled = true;
    registerAuctionBtn.textContent = i18n.registering || 'Creating deposit…';

    postForm('/Auction/InitiateDeposit', { auctionId: auctionId })
      .then(function (response) {
        if (response.status === 401) {
          openAuthModal();
          throw new Error(i18n.registrationFailed || 'Please sign in to register.');
        }

        return response.json().then(function (data) {
          if (!response.ok || !data.success) {
            throw new Error(data.message || i18n.registrationFailed || 'Unable to create deposit.');
          }

          return data;
        });
      })
      .then(function (data) {
        if (!data.approvalUrl) {
          throw new Error(i18n.registrationFailed || 'Unable to create deposit.');
        }

        showFeedback(
          registrationFeedback,
          data.message || i18n.registrationSuccess || 'Redirecting to PayPal…',
          true
        );

        window.location.href = data.approvalUrl;
      })
      .catch(function (error) {
        showFeedback(registrationFeedback, error.message, false);
      })
      .finally(function () {
        registerAuctionBtn.disabled = false;
        registerAuctionBtn.textContent = i18n.registerForAuction || 'Register for auction';
      });
  }

  function submitCancelRegistration(auctionId) {
    cancelRegistrationBtn.disabled = true;

    postForm('/Auction/CancelRegistration', { auctionId: auctionId })
      .then(function (response) {
        return response.json().then(function (data) {
          if (!response.ok || !data.success) {
            throw new Error(data.message || i18n.cancelRegistrationFailed || 'Unable to cancel registration.');
          }

          return data;
        });
      })
      .then(function (data) {
        var message = data.message;
        if (data.refundedAmount != null && data.refundedAmount > 0) {
          var refundTemplate = i18n.registrationCancelledWithRefund ||
            'Registration cancelled. Your deposit of {0} has been refunded.';
          message = refundTemplate.replace('{0}', formatCurrency(data.refundedAmount));
        } else {
          message = data.message || i18n.registrationCancelled || 'Registration cancelled.';
        }

        showFeedback(registrationFeedback, message, true);
        showPageToast(message, true);
        window.setTimeout(function () {
          window.location.reload();
        }, 2000);
      })
      .catch(function (error) {
        showFeedback(registrationFeedback, error.message, false);
        showPageToast(error.message, false);
      })
      .finally(function () {
        cancelRegistrationBtn.disabled = false;
      });
  }

  function disableBidding(message) {
    canBid = false;
    canPlaceBid = false;

    if (bidPanel) {
      bidPanel.setAttribute('data-can-bid', 'false');
      bidPanel.setAttribute('data-can-place-bid', 'false');
    }

    if (placeBidBtn) {
      placeBidBtn.disabled = true;
      placeBidBtn.textContent = message || i18n.auctionEnded || 'Auction ended';
    }

    if (bidInput) {
      bidInput.disabled = true;
    }

    if (bidDecreaseBtn) {
      bidDecreaseBtn.disabled = true;
    }

    if (bidIncreaseBtn) {
      bidIncreaseBtn.disabled = true;
    }

    if (countdownTimer) {
      window.clearInterval(countdownTimer);
      countdownTimer = null;
    }

    if (cdDays) cdDays.textContent = '00';
    if (cdHours) cdHours.textContent = '00';
    if (cdMinutes) cdMinutes.textContent = '00';
    if (cdSeconds) cdSeconds.textContent = '00';

    if (countdownSummary) {
      countdownSummary.textContent = message || i18n.auctionEnded || 'Auction ended';
    }
  }

  function getCountdownSummaryText(days, hours, minutes, seconds) {
    var templates = {
      registration_end: i18n.countdownRegistrationEnd || 'Registration closes in {0}d {1}h {2}m {3}s',
      live_start: i18n.countdownLiveStart || 'Live starts in {0}d {1}h {2}m {3}s',
      live_end: i18n.countdownLiveEnd || '{0}d {1}h {2}m {3}s remaining'
    };

    var template = templates[countdownKind] || templates.live_end;
    return template
      .replace('{0}', String(days))
      .replace('{1}', String(hours))
      .replace('{2}', String(minutes))
      .replace('{3}', String(seconds));
  }

  function handleCountdownEnded() {
    if (countdownTimer) {
      window.clearInterval(countdownTimer);
      countdownTimer = null;
    }

    if (cdDays) cdDays.textContent = '00';
    if (cdHours) cdHours.textContent = '00';
    if (cdMinutes) cdMinutes.textContent = '00';
    if (cdSeconds) cdSeconds.textContent = '00';

    if (countdownSummary) {
      countdownSummary.textContent = i18n.auctionEnded || 'Auction ended';
    }

    if (countdownKind === 'live_end' && canPlaceBid) {
      disableBidding(i18n.auctionEnded || 'Auction ended');
      return;
    }

    if (!countdownReloadScheduled) {
      countdownReloadScheduled = true;
      window.setTimeout(function () {
        window.location.reload();
      }, 1200);
    }
  }

  function updateCountdown() {
    if (!endDateMs || Number.isNaN(endDateMs)) {
      return;
    }

    var remainingMs = endDateMs - Date.now();
    if (remainingMs <= 0) {
      handleCountdownEnded();
      return;
    }

    var totalSeconds = Math.floor(remainingMs / 1000);
    var days = Math.floor(totalSeconds / 86400);
    var hours = Math.floor((totalSeconds % 86400) / 3600);
    var minutes = Math.floor((totalSeconds % 3600) / 60);
    var seconds = totalSeconds % 60;

    if (cdDays) cdDays.textContent = pad(days);
    if (cdHours) cdHours.textContent = pad(hours);
    if (cdMinutes) cdMinutes.textContent = pad(minutes);
    if (cdSeconds) cdSeconds.textContent = pad(seconds);

    if (countdownSummary) {
      countdownSummary.textContent = getCountdownSummaryText(days, hours, minutes, seconds);
    }
  }

  function startCountdown() {
    if (!endDateMs || Number.isNaN(endDateMs)) {
      return;
    }

    updateCountdown();

    if (endDateMs <= Date.now()) {
      return;
    }

    if (countdownTimer) {
      window.clearInterval(countdownTimer);
    }

    countdownTimer = window.setInterval(updateCountdown, 1000);
  }

  function applyBidSuccess(data) {
    var currentPrice = Number(data.currentPrice);
    var bidCount = Number(data.bidCount);

    if (bidPanel && !Number.isNaN(currentPrice)) {
      bidPanel.setAttribute('data-current-price', String(currentPrice));
    }

    if (currentPriceDisplay && !Number.isNaN(currentPrice)) {
      currentPriceDisplay.textContent = formatCurrency(currentPrice);
    }

    if (!Number.isNaN(bidCount)) {
      updateBidCount(bidCount);
    }

    if (!Number.isNaN(currentPrice)) {
      refreshBidAmount(currentPrice);
    }

    if (data.endDate) {
      var parsedEnd = parseUtcDateMs(data.endDate);
      if (!Number.isNaN(parsedEnd)) {
        endDateMs = parsedEnd;
        countdownKind = 'live_end';
        countdownReloadScheduled = false;
        if (bidPanel) {
          bidPanel.setAttribute('data-end-date', data.endDate);
          bidPanel.setAttribute('data-countdown-kind', 'live_end');
        }
        startCountdown();
      }
    }

    if (Array.isArray(data.bidHistory)) {
      renderBidHistory(data.bidHistory);
    }

    showFeedback(bidFeedback, data.message || i18n.bidSuccess || 'Bid placed successfully!', true);
  }

  function handleRegistrationSuccess(data) {
    if (typeof data.registrationCount === 'number') {
      updateRegistrationCount(data.registrationCount);
    }

    showFeedback(registrationFeedback, data.message || i18n.registrationSuccess || 'Registration successful.', true);

    window.setTimeout(function () {
      window.location.reload();
    }, 600);
  }

  if (mainImage && thumbs.length) {
    thumbs.forEach(function (thumb) {
      thumb.addEventListener('click', function () {
        var src = thumb.getAttribute('data-image');
        if (!src) return;

        mainImage.style.opacity = '0';
        setTimeout(function () {
          mainImage.src = src;
          mainImage.style.opacity = '1';
        }, 150);

        thumbs.forEach(function (t) {
          t.classList.remove('border-slate-900');
          t.classList.add('border-transparent');
        });
        thumb.classList.remove('border-transparent');
        thumb.classList.add('border-slate-900');
      });
    });

    mainImage.style.transition = 'opacity 0.3s ease';
  }

  tabButtons.forEach(function (button) {
    button.addEventListener('click', function () {
      var target = button.getAttribute('data-tab');
      if (!target) return;

      tabButtons.forEach(function (btn) {
        btn.classList.remove('is-active');
        btn.setAttribute('aria-selected', 'false');
      });
      button.classList.add('is-active');
      button.setAttribute('aria-selected', 'true');

      tabPanels.forEach(function (panel) {
        var isMatch = panel.getAttribute('data-panel') === target;
        panel.classList.toggle('is-active', isMatch);
        panel.hidden = !isMatch;
      });
    });
  });

  if (registerAuctionBtn && bidPanel) {
    registerAuctionBtn.addEventListener('click', function () {
      var isLoggedIn = bidPanel.getAttribute('data-is-logged-in') === 'true';
      if (!isLoggedIn) {
        openAuthModal();
        return;
      }

      var auctionId = bidPanel.getAttribute('data-auction-id');
      if (!auctionId) {
        showFeedback(registrationFeedback, i18n.registrationFailed || 'Unable to register.', false);
        return;
      }

      var depositAmount = formatDepositAmount(config.registrationDepositAmount || 0);

      requestConfirm({
        title: i18n.confirmRegisterTitle || 'Register for this auction?',
        message: i18n.confirmRegisterMessage || 'You will pay a {0} deposit via PayPal.',
        messageArgs: [depositAmount],
        note: i18n.confirmRegisterNote || '',
        confirmText: i18n.registerForAuction || 'Register for auction'
      }).then(function (confirmed) {
        if (!confirmed) {
          return;
        }

        submitAuctionRegistration(auctionId);
      });
    });
  }

  if (cancelRegistrationBtn && bidPanel) {
    cancelRegistrationBtn.addEventListener('click', function () {
      var auctionId = bidPanel.getAttribute('data-auction-id');
      if (!auctionId) {
        return;
      }

      requestConfirm({
        title: i18n.confirmCancelRegistrationTitle || 'Cancel auction registration?',
        message: i18n.confirmCancelRegistrationMessage || 'Are you sure you want to cancel your registration?',
        note: i18n.confirmCancelRegistrationNote || '',
        confirmText: i18n.confirmCancelRegistrationConfirm || 'Cancel registration',
        variant: 'danger'
      }).then(function (confirmed) {
        if (!confirmed) {
          return;
        }

        submitCancelRegistration(auctionId);
      });
    });
  }

  if (bidInput) {
    bidInput.addEventListener('blur', function () {
      var snapped = snapBidAmount(parseBidInput(bidInput.value));
      setBidInputValue(snapped);
    });

    bidInput.addEventListener('keydown', function (event) {
      if (event.key === 'Enter') {
        event.preventDefault();
        var snapped = snapBidAmount(parseBidInput(bidInput.value));
        setBidInputValue(snapped);
        bidInput.blur();
      }
    });
  }

  if (bidDecreaseBtn && bidInput) {
    bidDecreaseBtn.addEventListener('click', function () {
      if (!canPlaceBid) {
        return;
      }

      var current = snapBidAmount(parseBidInput(bidInput.value));
      var next = Math.max(getMinBid(), current - bidStep);
      setBidInputValue(next);
    });
  }

  if (bidIncreaseBtn && bidInput) {
    bidIncreaseBtn.addEventListener('click', function () {
      if (!canPlaceBid) {
        return;
      }

      var current = snapBidAmount(parseBidInput(bidInput.value));
      setBidInputValue(current + bidStep);
    });
  }

  if (placeBidBtn && bidPanel) {
    placeBidBtn.addEventListener('click', function () {
      if (!canPlaceBid) {
        return;
      }

      var isLoggedIn = bidPanel.getAttribute('data-is-logged-in') === 'true';
      if (!isLoggedIn) {
        openAuthModal();
        return;
      }

      if (requiresRegistration && !canBid) {
        showFeedback(bidFeedback, i18n.mustRegisterToBid || 'You must register before placing a bid.', false);
        return;
      }

      var auctionId = bidPanel.getAttribute('data-auction-id');
      var amount = bidInput ? snapBidAmount(parseBidInput(bidInput.value)) : null;
      if (!auctionId || amount === null || Number.isNaN(amount)) {
        return;
      }

      setBidInputValue(amount);
      placeBidBtn.disabled = true;
      placeBidBtn.textContent = i18n.placingBid || 'Placing bid…';

      function placeBidRequest(challengeToken) {
        var headers = {};
        if (challengeToken) {
          headers['X-Bid-Challenge-Token'] = challengeToken;
        }

        return postForm('/Auction/PlaceBid', {
          auctionId: auctionId,
          amount: amount,
          challengeToken: challengeToken || ''
        }, headers).then(function (response) {
          if (response.status === 401) {
            openAuthModal();
            throw new Error(i18n.bidFailed || 'Please sign in to place a bid.');
          }

          return response.json().then(function (data) {
            data.__status = response.status;
            return data;
          });
        });
      }

      placeBidRequest(bidChallengeToken)
        .then(function (data) {
          if (data && data.requiresChallenge) {
            var token = promptBidChallengeToken();
            if (!token) {
              throw new Error(data.message || i18n.challengeRequired || 'Please complete the bid challenge and try again.');
            }

            return placeBidRequest(token);
          }

          return data;
        })
        .then(function (data) {
          if (!data || !data.success) {
            throw new Error((data && data.message) || i18n.bidFailed || 'Unable to place bid. Please try again.');
          }

          applyBidSuccess(data);
        })
        .catch(function (error) {
          showFeedback(bidFeedback, error.message || i18n.bidFailed || 'Unable to place bid. Please try again.', false);
        })
        .finally(function () {
          if (canPlaceBid && (!requiresRegistration || canBid)) {
            placeBidBtn.disabled = false;
            placeBidBtn.textContent = i18n.bid || 'Bid';
          }
        });
    });
  }

  window.addEventListener('auction:bid-updated', function (event) {
    var data = event.detail;
    if (!data) return;

    var pageAuctionId = config.auctionId || (bidPanel && Number(bidPanel.getAttribute('data-auction-id')));
    if (pageAuctionId && data.auctionId && Number(data.auctionId) !== Number(pageAuctionId)) {
      return;
    }

    if (data.isEnded && countdownKind === 'live_end') {
      disableBidding(i18n.auctionEnded || 'Auction ended');
    }

    applyBidSuccess(data);
  });

  startCountdown();

  if (config.flashMessage) {
    showPageToast(config.flashMessage, config.flashMessageType !== 'error');
  }

  if (bidInput && canPlaceBid) {
    setBidInputValue(snapBidAmount(parseBidInput(bidInput.value)));
  }
})();
