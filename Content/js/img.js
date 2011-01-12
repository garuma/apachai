/* Author: Jérémie Laval
   
*/

var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
if (img != "i") {
	$('#mainImage').bind('load', function (e) {
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
	});

	var src = "/Content/img/" + img;
	$("#mainImage").attr("src", src);
	$("meta[property=\"og:image\"]").attr ("content", src);
}
