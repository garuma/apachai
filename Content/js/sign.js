(function ($) {
	var referrer = document.referrer;
	if (referrer.length == 0)
		return;

	if (referrer.search ("http://apch.fr/i/") != 0)
		return;

	var warn = $('#warn');
	var a = warn.find ('#url')
	a.attr ("href", referrer);
	warn.removeClass ('hidden');
})(window.jQuery);