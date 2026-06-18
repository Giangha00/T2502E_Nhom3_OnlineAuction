(function () {
  'use strict';

  var config = window.productDetailConfig || {};
  var i18n = config.i18n || {};
  var mainImage = document.getElementById('mainProductImage');
  var thumbs = document.querySelectorAll('.gallery-thumb');
  var bidSelect = document.getElementById('bidAmount');
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
  var bidOptionCount = config.bidOptionCount || 35;
  var endDateMs = Date.parse((bidPanel && bidPanel.getAttribute('data-end-date')) || config.endDate || '');
  var canBid = (bidPanel && bidPanel.getAttribute('data-can-bid') === 'true') || config.canBid === true;
  var canPlaceBid = bidPanel && bidPanel.getAttribute('data-can-place-bid') === 'true';
  var requiresRegistration = bidPanel && bidPanel.getAttribute('data-requires-registration') === 'true';
  var countdownTimer = null;

  function formatCurrency(value) {
    var amount = Number(value);
    if (Number.isNaN(amount)) {
      return '$0';
    }

    return '$' + amount.toLocaleString(undefined, { maximumFractionDigits: 0 });
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

  function getCsrfToken() {
    var csrfMeta = document.querySelector('meta[name="request-verification-token"]');
    return csrfMeta ? csrfMeta.getAttribute('content') : '';
  }

  function postForm(url, payload) {
    var body = new URLSearchParams();
    Object.keys(payload).forEach(function (key) {
      body.append(key, payload[key]);
    });

    var csrfToken = getCsrfToken();
    if (csrfToken) {
      body.append('__RequestVerificationToken', csrfToken);
    }

    return fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'X-Requested-With': 'XMLHttpRequest'
      },
      body: body.toString()
    });
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

  function refreshBidOptions(currentPrice) {
    if (!bidSelect || !bidStep) {
      return;
    }

    var minBid = currentPrice + bidStep;
    bidSelect.innerHTML = '';

    for (var i = 0; i < bidOptionCount; i++) {
      var amount = minBid + bidStep * i;
      var option = document.createElement('option');
      option.value = String(amount);
      option.textContent = formatCurrency(amount);
      bidSelect.appendChild(option);
    }

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
      var winning = !!bid.isWinning;
      var badgeClass = winning
        ? 'inline-flex rounded-full bg-emerald-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-emerald-700'
        : 'inline-flex rounded-full bg-slate-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-500';
      var statusLabel = winning ? (i18n.winning || 'Winning') : (i18n.outbid || 'Outbid');

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

    if (bidSelect) {
      bidSelect.disabled = true;
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

  function updateCountdown() {
    if (!endDateMs || Number.isNaN(endDateMs)) {
      return;
    }

    var remainingMs = endDateMs - Date.now();
    if (remainingMs <= 0) {
      disableBidding(i18n.auctionEnded || 'Auction ended');
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
      countdownSummary.textContent = days + 'd ' + hours + 'h ' + minutes + 'm remaining';
    }
  }

  function startCountdown() {
    if (!endDateMs || Number.isNaN(endDateMs)) {
      return;
    }

    updateCountdown();
    if (canPlaceBid) {
      countdownTimer = window.setInterval(updateCountdown, 1000);
    }
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
      refreshBidOptions(currentPrice);
    }

    if (data.endDate) {
      endDateMs = Date.parse(data.endDate);
      if (bidPanel) {
        bidPanel.setAttribute('data-end-date', data.endDate);
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
        return;
      }

      registerAuctionBtn.disabled = true;
      registerAuctionBtn.textContent = i18n.registering || 'Registering…';

      postForm('/Auction/Register', { auctionId: auctionId })
        .then(function (response) {
          if (response.status === 401) {
            openAuthModal();
            throw new Error(i18n.registrationFailed || 'Please sign in to register.');
          }

          return response.json().then(function (data) {
            if (!response.ok || !data.success) {
              throw new Error(data.message || i18n.registrationFailed || 'Unable to register. Please try again.');
            }

            return data;
          });
        })
        .then(handleRegistrationSuccess)
        .catch(function (error) {
          showFeedback(registrationFeedback, error.message, false);
        })
        .finally(function () {
          registerAuctionBtn.disabled = false;
          registerAuctionBtn.textContent = i18n.registerForAuction || 'Register for auction';
        });
    });
  }

  if (cancelRegistrationBtn && bidPanel) {
    cancelRegistrationBtn.addEventListener('click', function () {
      var auctionId = bidPanel.getAttribute('data-auction-id');
      if (!auctionId) {
        return;
      }

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
          showFeedback(registrationFeedback, data.message || i18n.registrationCancelled || 'Registration cancelled.', true);
          window.setTimeout(function () {
            window.location.reload();
          }, 600);
        })
        .catch(function (error) {
          showFeedback(registrationFeedback, error.message, false);
        })
        .finally(function () {
          cancelRegistrationBtn.disabled = false;
        });
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
      var amount = bidSelect ? bidSelect.value : null;
      if (!auctionId || !amount) {
        return;
      }

      placeBidBtn.disabled = true;
      placeBidBtn.textContent = i18n.placingBid || 'Placing bid…';

      postForm('/Auction/PlaceBid', { auctionId: auctionId, amount: amount })
        .then(function (response) {
          if (response.status === 401) {
            openAuthModal();
            throw new Error(i18n.bidFailed || 'Please sign in to place a bid.');
          }

          return response.json().then(function (data) {
            if (!response.ok || !data.success) {
              throw new Error(data.message || i18n.bidFailed || 'Unable to place bid. Please try again.');
            }

            return data;
          });
        })
        .then(function (data) {
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

  startCountdown();
})();
