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
      refreshBidAmount(currentPrice);
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
      // Kiểm tra user đã đăng nhập chưa.
      // Nếu chưa đăng nhập thì mở modal login/register như logic cũ của project.
      var isLoggedIn = bidPanel.getAttribute('data-is-logged-in') === 'true';
      if (!isLoggedIn) {
        openAuthModal();
        return;
      }

      // Lấy auctionId từ data-auction-id trong _ProductBidPanel.cshtml
      var auctionId = bidPanel.getAttribute('data-auction-id');
      if (!auctionId) {
        showFeedback(registrationFeedback, 'Không tìm thấy mã phiên đấu giá.', false);
        return;
      }

      // Disable nút để tránh user bấm nhiều lần tạo nhiều PayPal order
      registerAuctionBtn.disabled = true;

      // Đổi text để user biết hệ thống đang tạo yêu cầu đặt cọc
      registerAuctionBtn.textContent = i18n.registering || 'Creating deposit…';

      // Gọi API mới:
      // Không gọi /Auction/Register nữa vì Register cũ approve trực tiếp.
      // Luồng mới phải là:
      // InitiateDeposit -> tạo registration pending -> tạo deposit pending -> tạo PayPal order.
      postForm('/Auction/InitiateDeposit', { auctionId: auctionId })
          .then(function (response) {
            // Nếu hết session hoặc chưa login thì mở modal đăng nhập
            if (response.status === 401) {
              openAuthModal();
              throw new Error(i18n.registrationFailed || 'Please sign in to register.');
            }

            return response.json().then(function (data) {
              // Nếu server trả lỗi nghiệp vụ:
              // seller tự đăng ký, auction ended, productValue <= 0, PayPal lỗi...
              if (!response.ok || !data.success) {
                throw new Error(data.message || i18n.registrationFailed || 'Unable to create deposit.');
              }

              return data;
            });
          })
          .then(function (data) {
            // Server phải trả về approvalUrl từ PayPal.
            // Đây là URL để redirect user sang PayPal Sandbox thanh toán tiền cọc.
            if (!data.approvalUrl) {
              throw new Error('Không nhận được link thanh toán PayPal.');
            }

            // Hiển thị thông báo trước khi chuyển trang
            showFeedback(
                registrationFeedback,
                data.message || 'Đang chuyển sang PayPal để thanh toán tiền cọc...',
                true
            );

            // Redirect user sang PayPal Sandbox
            window.location.href = data.approvalUrl;
          })
          .catch(function (error) {
            // Nếu lỗi thì hiện message ra giao diện
            showFeedback(registrationFeedback, error.message, false);
          })
          .finally(function () {
            // Nếu redirect thành công thì dòng này gần như không thấy.
            // Nếu lỗi thì enable lại nút để user thử lại.
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

  window.addEventListener('auction:bid-updated', function (event) {
    var data = event.detail;
    if (!data) return;

    var pageAuctionId = config.auctionId || (bidPanel && Number(bidPanel.getAttribute('data-auction-id')));
    if (pageAuctionId && data.auctionId && Number(data.auctionId) !== Number(pageAuctionId)) {
      return;
    }

    if (data.isEnded) {
      disableBidding(i18n.auctionEnded || 'Auction ended');
    }

    applyBidSuccess(data);
  });

  startCountdown();

  if (bidInput && canPlaceBid) {
    setBidInputValue(snapBidAmount(parseBidInput(bidInput.value)));
  }
})();
