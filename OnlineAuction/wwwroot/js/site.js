// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
	function updateSiteHeaderHeight() {
		var header = document.getElementById('siteHeader');
		if (!header) return;
		document.documentElement.style.setProperty('--site-header-height', header.offsetHeight + 'px');
	}

	updateSiteHeaderHeight();
	window.addEventListener('resize', updateSiteHeaderHeight);
	window.addEventListener('load', updateSiteHeaderHeight);
})();

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
