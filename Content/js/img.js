/* Author: Jérémie Laval
   
*/

var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
if (img != "i") {
	var baseUrl = "/Pictures/";

	$('#mainImage').bind('load', function (e) {
		if ($(this).attr ('src').indexOf ('transparent.png') == -1)
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
			$("#tweetText").html (data['tweet'].length == 0 ? '<em>(No tweet data to show)</em>' : data['tweet']);

			$.getJSON ('http://api.twitter.com/1/users/show.json?screen_name='+data['name'], function (profile) {
				var thedude = $('#thedude');
				thedude.find('span').append(profile['name']);
				thedude.attr('href', profile['url']);
				thedude.attr('title', profile['description']);
			});

			$.getJSON("/links/" + img, function (link) {
				if (link.length != 0) {
					var linktable = $('#linktable');
					linktable.append('<li class="linkentry"><a href="' + link["short"] + '">Short</a></li>');
					linktable.append('<li class="linkentry"><a href="' + link["permanent"] + '">Permalink</a></li>');
					linktable.append('<li class="linkentry"><a href="http://www.facebook.com/sharer.php?u='+encodeURIComponent(link["facebook"])+'&src=sp" target="_blank"><img src="/Content/img/share_fb.png"></a></li>');
					linktable.append('<li class="linkentry"><a href="http://twitter.com/share?url='+encodeURIComponent(link["short"])+'&counturl='+encodeURIComponent(link["permanent"])+'&via='+data['name']+'" target="_blank"><img src="/Content/img/share_tweet.png"></a></li>');
				}
			});

			$("#twitbox").css ('opacity', 1);
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
			var transformProperties = ['-webkit-transform', '-moz-transform', '-o-transform', 'transform'];

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
					$.each (transformProperties, function (i, j) {
						slider.css(j, 'translateX(-' + width + 'px)');
					});
				} else {
					$.each (transitionProperties, function (i, j) {
						slider.css(j, ((current * transition) | 0) + 'ms');
					});
					$.each (transformProperties, function (i, j) {
						slider.css(j, 'translateX(0)');
					});
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

				$.each (transformProperties, function (i, j) {
					slider.css(j, 'translateX(-' + pos + 'px)');
				});
			};

			$('#goRight')
				.mouseenter (function () { mouseBind ({ direction: "right"}); })
				.mouseleave(function () { mouseUnbind ({ direction: "right"}); });
			$('#goLeft')
				.mouseenter (function () { mouseBind ({ direction: "left"}); })
				.mouseleave(function () { mouseUnbind ({ direction: "left"}); });

			$("#sliderbox").css ('opacity', 1);
		}, "json");

		$.get("/geo/" + img, callback = function (data, textStatus, xhr) {
			if (data.length == 0)
				return;

			var lat = data['latitude'];
			var lon = data['longitude'];
			var w = $('#rightcolumn').width () - 20;

			var googleMapUrl = 'http://maps.google.com/maps/api/staticmap?center='+lat+','+lon+'&zoom=7&sensor=false&maptype=roadmap&size='+w+'x'+w+'&markers='+lat+','+lon;
			$('#mapImage').attr ('src', googleMapUrl);
			$('#mapBox').find('a').attr ('href', 'http://maps.google.com/maps?z=10&q='+lat+','+lon);
			$("#mapBox").css ('opacity', 1);
		}, "json");
	});

	var src = baseUrl + img;
	$("#mainImage").attr("src", src);
}
