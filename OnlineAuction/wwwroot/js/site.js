// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Fix Contact Us anchors that may be rendered with href="#".
(function () {
	try {
		var anchors = Array.from(document.querySelectorAll('a'));
		anchors.forEach(function (a) {
			if (!a) return;
			var txt = (a.textContent || a.innerText || '').trim();
			if (txt === 'Contact Us' || txt === 'Contact') {
				if (!a.getAttribute('href') || a.getAttribute('href') === '#') {
					a.setAttribute('href', '/Contact');
				}
			}
		});
	}
	catch (e) {
		// silent
		console.error(e);
	}
})();

(function () {
	var header = document.querySelector('[data-notification-header]');
	if (!header) return;

	var toggle = header.querySelector('[data-notification-toggle]');
	var dropdown = header.querySelector('[data-notification-dropdown]');
	var badge = header.querySelector('[data-notification-badge]');
	var markAllButton = header.querySelector('[data-mark-all-read]');

	function setDropdown(open) {
		if (!dropdown || !toggle) return;
		dropdown.classList.toggle('hidden', !open);
		toggle.setAttribute('aria-expanded', String(open));
	}

	function refreshHeaderCount() {
		if (!badge) return;
		var unreadItems = header.querySelectorAll('[data-notification-preview][data-read="false"]').length;
		badge.textContent = String(unreadItems);
		badge.classList.toggle('hidden', unreadItems === 0);
	}

	if (toggle) {
		toggle.addEventListener('click', function (event) {
			event.stopPropagation();
			setDropdown(dropdown.classList.contains('hidden'));
		});
	}

	if (markAllButton) {
		markAllButton.addEventListener('click', function () {
			header.querySelectorAll('[data-notification-preview]').forEach(function (item) {
				item.dataset.read = 'true';
				item.classList.remove('bg-amber-50/70');
				item.classList.add('bg-white');
				var dot = item.querySelector('.bg-amber-700');
				if (dot) dot.remove();
			});
			refreshHeaderCount();
		});
	}

	document.addEventListener('click', function (event) {
		if (!header.contains(event.target)) {
			setDropdown(false);
		}
	});

	document.addEventListener('keydown', function (event) {
		if (event.key === 'Escape') {
			setDropdown(false);
		}
	});

	refreshHeaderCount();
})();
