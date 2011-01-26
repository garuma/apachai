(function ($) {
	var updateFunc = function (data) {
		if (data.length == 0)
			return;

		var picNumber = data['picNumber'];
		var userNumber = data['userNumber'];
		var latestPics = data['latestPics'];

		$('#picNumber').text(picNumber);
		$('#userNumber').text(userNumber);

		var container = $('#submissions .cell');

		container.each (function (index) {
			if (index >= latestPics.length)
				return;

			var img = $(this).children ('img');
			img.attr ('src', '/Pictures/' + latestPics[index]);
			img.css ('visibility', 'visible');
		});
	};

	$.getJSON('/stats', updateFunc);

	(function loop(){
		setTimeout(function(){
		  $.getJSON('/stats', updateFunc);
		  loop();
	  }, 10000);
	})();
})(window.jQuery);