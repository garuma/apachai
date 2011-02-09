// Author: Jérémie Laval

setTimeout (function () {
	(function($){
		/* MIT License
		 * Paul Irish     | @paul_irish | www.paulirish.com
		 * Andree Hansson | @peolanha   | www.andreehansson.se
		 * 2010.
		 */
		$.event.special.load = {
			add: function (hollaback) {
				if ( this.nodeType === 1 && this.tagName.toLowerCase() === 'img' && this.src !== '' ) {
					if ( this.complete || this.readyState === 4 ) {
						hollaback.handler.apply(this);
					} else if ( this.readyState === 'uninitialized' && this.src.indexOf('data:') === 0 ) {
						$(this).trigger('error');
					} else {
						$(this).bind('load', hollaback.handler);
					}
				}
			}};
	})(window.jQuery);

	(function($){
		var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
		if (img == 'i')
			return;

		var baseUrl = '/Pictures/';
		var mainImage = $('#mainImage');

		mainImage.bind('load', function (e) {
			if (mainImage.attr ('src').indexOf ('transparent.png') == -1)
				return;

			$.getJSON('/infos/' + img, function (data) {
				var ptable = $('#pictable');

				if (data.length == 0) {
					ptable.append ('<em>Sorry, nothing to see here</em>');
					return;
				}

				$.each (data, function (key, value) {
					ptable.append ("<tr><td class=\"title\">" + key + "</td><td>" + value + "</td></tr>");
				});

				ptable.parent().css ('opacity', 1);
			});

			$.getJSON("/tweet/" + img, function (data) {
				$("#imgAvatar").attr ("src", data["avatar"]);
				$("#tweetText").html (data['tweet'].length == 0 ? '<em>(No tweet data to show)</em>' : data['tweet']);

				var thedude = $('#thedude');
				thedude.find('span').append(data['name']);
				thedude.attr('href', data['url']);
				thedude.attr('title', data['desc']);

				$.getJSON("/links/" + img, function (link) {
					if (link.length != 0) {
						var linktable = $('#linktable');
						linktable.append('<li class="linkentry"><a href="' + link["short"] + '">Short</a></li>');
						linktable.append('<li class="linkentry"><a href="' + link["permanent"] + '">Permalink</a></li>');
						linktable.append('<li class="linkentry"><a href="http://www.facebook.com/sharer.php?u='+encodeURIComponent(link["facebook"])+'&src=sp" target="_blank"><img src="/Content/img/share_fb.png"></a></li>');
						linktable.append('<li class="linkentry"><a href="http://twitter.com/share?url='+encodeURIComponent(link["short"])+'&counturl='+encodeURIComponent(link["permanent"])+'&via='+data['screenname']+'" target="_blank"><img src="/Content/img/share_tweet.png"></a></li>');
					}
				});

				$("#twitbox").css ('opacity', 1);
			});

			$.getJSON("/recent/" + img, function (list) {
				if (list.length == 0)
					return;

				var slider = $('#slider');
				var width = 0;
				list = list.slice (0, 5);

				$.each (list, function (e) {
					var imgEntry = $('<div class="imgEntry"><a href="/i/' + list[e] + '"><img src="' + baseUrl + list[e] + '?v=small' + '"></a></div>');
					slider.append(imgEntry);
				});

				var current = 0;
				var transition = 5000;
				var transitionProperties = ['-webkit-transition-duration', '-moz-transition-duration', '-o-transition-duration', 'transition-duration'];
				var transformProperties = ['-webkit-transform', '-moz-transform', '-o-transform', 'transform'];

				var mouseBind = function (data) {
					if (width == 0) {
						slider.find ('.imgEntry a img').each (function () { width += $(this).width (); });
						width += 50;
						slider.width (width);
						width -= 150;
						transition /= width;
					}

					if (data.direction == "right") {
						$.each (transitionProperties, function (i, j) {
							slider.css (j, (((width - current) * transition) | 0) + 'ms');
						});
						$.each (transformProperties, function (i, j) {
							slider.css (j, 'translateX(-' + width + 'px)');
						});
					} else {
						$.each (transitionProperties, function (i, j) {
							slider.css (j, ((current * transition) | 0) + 'ms');
						});
						$.each (transformProperties, function (i, j) {
							slider.css (j, 'translateX(0)');
						});
					}
				};
				var mouseUnbind = function (data) {
					var elem = document.getElementById ('slider');
					var pos = null;
					$.each (transformProperties, function (i, j) {
						if (pos == null)
							pos = window.getComputedStyle(elem,null).getPropertyValue(j);
						if (pos != null) {
							current = Math.abs(new Number(pos.split(',')[4].trim()));
							slider.css(j, 'translateX(-' + current + 'px)');
						}
					});
				};

				$('#goRight')
					.mouseenter (function () { mouseBind ({ direction: "right"}); })
					.mouseleave(function () { mouseUnbind ({ direction: "right"}); });
				$('#goLeft')
					.mouseenter (function () { mouseBind ({ direction: "left"}); })
					.mouseleave(function () { mouseUnbind ({ direction: "left"}); });

				$("#sliderbox").css ('opacity', 1);
			});

			$.getJSON("/geo/" + img, function (data) {
				if (data.length == 0)
					return;

				var lat = data['latitude'];
				var lon = data['longitude'];
				var w = $('#rightcolumn').width () - 20;
				var m = $('#mapBox');

				var googleMapUrl = 'http://maps.google.com/maps/api/staticmap?center='+lat+','+lon+'&zoom=7&sensor=false&maptype=roadmap&size='+w+'x'+w+'&markers='+lat+','+lon;
				m.find('#mapImage').attr ('src', googleMapUrl);
				m.children('a').attr ('href', 'http://maps.google.com/maps?z=10&q='+lat+','+lon);
				m.css ('opacity', 1);
			});
		});

		var src = baseUrl + img;
		mainImage.attr('src', src);

		(function loop(){
			setTimeout(function(){
				if (mainImage.attr('src').indexOf ('transparent.png') == -1)
					return;

				mainImage.attr('src', src);
			}, 250);
		})();
	})(window.jQuery);
}, 10);
