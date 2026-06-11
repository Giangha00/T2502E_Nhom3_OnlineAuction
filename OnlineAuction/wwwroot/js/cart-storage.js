(function (window) {
    const STORAGE_KEY = 'auctionCartWatching';

    function readWatching() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            const parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed.filter(function (id) { return Number.isInteger(id); }) : [];
        } catch {
            return [];
        }
    }

    function writeWatching(ids) {
        const unique = Array.from(new Set(ids));
        localStorage.setItem(STORAGE_KEY, JSON.stringify(unique));
        updateBadge(unique.length);
        window.dispatchEvent(new CustomEvent('auctionCartUpdated', { detail: { count: unique.length } }));
        return unique;
    }

    function updateBadge(watchingCount) {
        const badges = document.querySelectorAll('[data-cart-badge]');
        badges.forEach(function (badge) {
            const total = watchingCount + (parseInt(badge.getAttribute('data-won-count') || '0', 10) || 0);
            badge.textContent = String(total);
            badge.classList.toggle('hidden', total <= 0);
        });
    }

    function isInCart(auctionId) {
        return readWatching().includes(auctionId);
    }

    function addToCart(auctionId) {
        const ids = readWatching();
        if (!ids.includes(auctionId)) {
            ids.push(auctionId);
            writeWatching(ids);
        }
        return ids;
    }

    function removeFromCart(auctionId) {
        const ids = readWatching().filter(function (id) { return id !== auctionId; });
        writeWatching(ids);
        return ids;
    }

    function getWatchingCount() {
        return readWatching().length;
    }

    function initBadges() {
        const badges = document.querySelectorAll('[data-cart-badge]');
        if (!badges.length) return;
        const watchingCount = getWatchingCount();
        updateBadge(watchingCount);
    }

    window.AuctionCart = {
        readWatching: readWatching,
        addToCart: addToCart,
        removeFromCart: removeFromCart,
        isInCart: isInCart,
        getWatchingCount: getWatchingCount,
        updateBadge: updateBadge,
        initBadges: initBadges
    };

    function initAddToCartButtons() {
        document.querySelectorAll('[data-add-to-cart]').forEach(function (btn) {
            const id = parseInt(btn.getAttribute('data-add-to-cart'), 10);
            if (isInCart(id)) {
                btn.textContent = 'In Cart';
                btn.classList.add('bg-amber-50');
            }
        });

        document.addEventListener('click', function (event) {
            const btn = event.target.closest('[data-add-to-cart]');
            if (!btn) return;

            event.preventDefault();
            const id = parseInt(btn.getAttribute('data-add-to-cart'), 10);
            if (!id) return;

            if (isInCart(id)) {
                window.location.href = '/Cart';
                return;
            }

            addToCart(id);

            if (btn.hasAttribute('data-add-to-cart-redirect')) {
                window.location.href = '/Cart';
                return;
            }

            btn.textContent = 'Added ✓';
            btn.classList.add('bg-amber-700', 'text-white');
            btn.classList.remove('text-amber-700');

            setTimeout(function () {
                btn.textContent = 'In Cart';
                btn.classList.remove('bg-amber-700', 'text-white');
                btn.classList.add('bg-amber-50', 'text-amber-700');
            }, 800);
        });
    }

    function init() {
        initBadges();
        initAddToCartButtons();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})(window);
