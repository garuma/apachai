/* Author: Jérémie Laval
   
*/

var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
if (img != "i") {
	var baseUrl = "/Pictures/";

	$('#mainImage').bind('load', function (e) {
		if ($(this).attr ('src') == "/Content/img/transparent.png")
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
			var start;
			var current = 0;
			var transitionTime = 5000;
			var transition = transitionTime;
			var transitionProperties = ['-webkit-transition-duration', '-moz-transition-duration', '-o-transition-duration', 'transition-duration'];

			var mouseBind = function (data) {
				if (width == 0) {
					width = slider.width () - $('#sliderContainer').width ();
					width -= 20;
					transition /= width;
				}

				if (data.direction == "right") {
					$.each (transitionProperties, function (i, j) {
						slider.css(j, (((width - current) * transition) | 0) + 'ms');
					});
					slider.css('margin-left', '-' + width + 'px');
				} else {
					$.each (transitionProperties, function (i, j) {
						slider.css(j, ((current * transition) | 0) + 'ms');
					});
					slider.css('margin-left',  '0');
				}
				start = new Date ().getTime ();
			};
			var mouseUnbind = function (data) {
				var pos = width / transitionTime;
				var now = new Date ().getTime () - start;
				if (data.direction == "right")
					pos = current + pos * now;
				else
					pos = current - pos * now;
				current = pos = Math.max (Math.min (pos | 0, width), 0);

				slider.css ('margin-left', '-' + pos + 'px');
			};

			$('#goRight')
				.mouseenter (function () { mouseBind ({ direction: "right"}); })
				.mouseleave(function () { mouseUnbind ({ direction: "right"}); });
			$('#goLeft')
				.mouseenter (function () { mouseBind ({ direction: "left"}); })
				.mouseleave(function () { mouseUnbind ({ direction: "left"}); });

			$("#sliderbox").css ('opacity', 1);
		}, "json");
	});

	var src = baseUrl + img;
	$("#mainImage").attr("src", src);
}
