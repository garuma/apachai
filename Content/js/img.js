/* Author: Jérémie Laval
   
*/

var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
if (img != "i") {
	var baseUrl = "/Content/img/";

	$('#mainImage').bind('load', function (e) {
		if ($(this).attr ('src').length == 0)
			return;

		$.get("/infos/" + img, callback = function (data, textStatus, xhr) {
			if (data.length == 0) {
				$("#pictable").append ("<em>Sorry, nothing to see here</em>");
				return;
			}

			$.each (data, function (key, value) {
				$("#pictable").append ("<tr><td class=\"title\">" + key + "</td><td>" + value + "</td></tr>");
			});

			$("#picinfos").css ('opacity', 1);
		}, "json");

		$.get("/tweet/" + img, callback = function (data, textStatus, xhr) {
			$("#imgAvatar").attr ("src", data["avatar"]);
			$("#tweetText").html (data["tweet"]);
			$("#twitbox").css ('opacity', 1);
		}, "json");

		$.get("/links/" + img, callback = function (data, textStatus, xhr) {
			if (data.length == 0)
				return;

			$('#linktable').append("<tr><td class=\"linkentry\"><a href=\"" + data["short"] + "\">Short url</a></td></tr>");
			$('#linktable').append('<tr><td class="linkentry"><a href="' + data["permanent"] + '">Permalink</a></td></tr>');
			$("#linkbox").css ('opacity', 1);
		}, "json");

		$.get("/recent/" + img, callback = function (data, textStatus, xhr) {
			if (data.length == 0)
				return;

			var slider = $('#slider');
			var width = 0;
			var count = data.length;

			$.each (data, function (e) {
				var imgEntry = $('<div class="imgEntry"><a href="/i/' + data[e] + '"><img src="' + baseUrl + data[e] +  '"></a></div>');
				slider.append(imgEntry);
			});

			var deviation = 0;

			var sliderMove = function (e) {
				if (width == 0) {
					var items = $('.imgEntry a img');
					$.each (items, function (i) {
						width += items[i].width;
					});
				}

				var tmp = e.data.update (deviation);
				if (tmp <= 0)
					tmp = 0;
				if (tmp >= width)
					return;
				slider.css('margin-left', '-' + (deviation = tmp) + 'px');
			};
			var transitionEvents = 'webkitTransitionEnd transitionend';
			var mouseBind = function (e) {
				slider.bind (transitionEvents, e.data, sliderMove);
				slider.trigger (transitionEvents);
			};
			var mouseUnbind = function (e) {
				slider.unbind (transitionEvents);
			};

			$('#goRight')
				.bind ('mouseover', {
					update: function (val) { return val + 50; }
				}, mouseBind)
				.mouseleave(mouseUnbind);
			$('#goLeft')
				.bind ('mouseenter', {
					update: function (val) { return val - 50; }
				}, mouseBind)
				.mouseleave(mouseUnbind);

			$("#sliderbox").css ('opacity', 1);
		}, "json");
	});

	var src = baseUrl + img;
	$("#mainImage").attr("src", src);
}
