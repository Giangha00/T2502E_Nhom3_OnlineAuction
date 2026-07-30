(function () {
  'use strict';

  var i18n = (window.buyNowDetailConfig && window.buyNowDetailConfig.i18n) || {};
  var panel = document.querySelector('.product-buy-panel');
  var addToCartBtn = document.getElementById('addToCartBtn');

  if (!panel || !addToCartBtn || !window.buyNowCart) return;

  function updateOrderBadge(count) {
    if (window.realtimeHub && typeof window.realtimeHub.updateOrderBadge === 'function') {
      window.realtimeHub.updateOrderBadge(count);
      return;
    }

    var badge = document.getElementById('orderCountBadge');
    var link = document.getElementById('orderNavLink');
    if (!badge || typeof count !== 'number') {
      return;
    }

    if (count > 0) {
      badge.textContent = count > 9 ? '9+' : String(count);
      badge.classList.remove('hidden');
      if (link) link.setAttribute('data-order-count', String(count));
    } else {
      badge.classList.add('hidden');
      if (link) link.setAttribute('data-order-count', '0');
    }
  }

  function notify(message, isSuccess) {
    var text = message || '';
    if (!text) {
      return;
    }

    if (typeof window.showAlertModal === 'function') {
      window.showAlertModal({
        title: isSuccess === false
          ? ((window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.errorTitle) || 'Error')
          : ((window.confirmModalConfig && window.confirmModalConfig.i18n && window.confirmModalConfig.i18n.successTitle) || 'Success'),
        message: text,
        variant: isSuccess === false ? 'danger' : 'success'
      });
      return;
    }

    window.alert(text);
  }

  addToCartBtn.addEventListener('click', function () {
    var auctionId = panel.getAttribute('data-auction-id');
    var isLoggedIn = panel.getAttribute('data-is-logged-in') === 'true';

    addToCartBtn.disabled = true;
    addToCartBtn.textContent = i18n.adding || 'Adding…';

    window.buyNowCart.add(auctionId, {
      isLoggedIn: isLoggedIn,
      signInMessage: i18n.signInRequired || 'Please sign in to continue.',
      failedMessage: i18n.addFailed || 'Unable to add to cart. Please try again.'
    })
      .then(function (data) {
        if (typeof data.orderCount === 'number') {
          updateOrderBadge(data.orderCount);
        }

        addToCartBtn.disabled = false;
        addToCartBtn.textContent = i18n.addToCart || 'Add to Cart';
        notify(data.message || i18n.addedToCart || 'Added to your orders.', true);
      })
      .catch(function (error) {
        addToCartBtn.disabled = false;
        addToCartBtn.textContent = i18n.addToCart || 'Add to Cart';
        if (error && error.message === (i18n.signInRequired || 'Please sign in to continue.')) {
          return;
        }
        var msg = (error && error.serverMessage) || i18n.addFailed || 'Unable to add to cart. Please try again.';
        notify(msg, false);
      });
  });
})();
